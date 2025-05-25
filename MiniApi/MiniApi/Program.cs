using Shield.Estimator.Business.Services.WhisperNet;
using Shield.Estimator.Business.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MiniApi.Models;
using Shield.AudioConverter.AudioConverterServices.FFMpeg;
using Shield.AudioConverter.AudioConverterServices;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Конфигурация сервисов
builder.Services.AddScoped<WhisperNetService>();
builder.Services.AddScoped<WhisperFasterXXLService>();
builder.Services.AddSingleton<AudioConverterFactory>();
builder.Services.AddSingleton<FFMpegConverter>();
builder.Services.AddHttpContextAccessor();

// Настройка ограничений размера файла
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024 * 1024 * 1024; // 1GB
});
/*
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
*/
var app = builder.Build();
/*
app.UseCors("AllowAll");
*/
app.MapPost("/api/transcribe/net", async (HttpContext context,
    [FromServices] WhisperNetService whisperNetService) =>
{
    var request = await ParseRequest(context);
    var tempFilePath = await SaveTempFile(request.AudioFile);

    var progress = new Progress<string>(message =>
        context.Response.WriteAsync($"data: {message}\n\n"));

    var result = await whisperNetService.TranscribeAsync(
        tempFilePath,
        request.Model,
        progress,
        context.RequestAborted,
        request.UseWordTimestamps);

    File.Delete(tempFilePath);
    return Results.Ok(result);
})
.WithName("TranscribeNet")
.Accepts<TranscribeRequest>("multipart/form-data")
.Produces<string>(200);

app.MapPost("/api/transcribe/faster", async (HttpContext context,
    [FromServices] WhisperFasterXXLService fasterService) =>
{
    var request = await ParseRequest(context);
    var tempFilePath = await SaveTempFile(request.AudioFile);

    var progress = new Progress<string>(message =>
        context.Response.WriteAsync($"data: {message}\n\n"));

    var result = await fasterService.TranscribeAsync(
        new[] { tempFilePath },
        Path.GetTempPath(),
        progress,
        context.RequestAborted,
        request.Device,
        request.UseDiarization);

    File.Delete(tempFilePath);
    return Results.Ok(result);
})
.WithName("TranscribeFaster")
.Accepts<TranscribeRequest>("multipart/form-data")
.Produces<string>(200);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


async Task<TranscribeRequest> ParseRequest(HttpContext context)
{
    var form = await context.Request.ReadFormAsync();
    return new TranscribeRequest
    {
        AudioFile = form.Files["audioFile"],
        Model = form["model"],
        UseWordTimestamps = bool.Parse(form["useWordTimestamps"]),
        UseDiarization = bool.Parse(form["useDiarization"]),
        Device = form["device"]
    };
}

async Task<string> SaveTempFile(IFormFile file)
{
    var tempFilePath = Path.GetTempFileName();
    using var stream = File.Create(tempFilePath);
    await file.CopyToAsync(stream);
    return tempFilePath;
}
