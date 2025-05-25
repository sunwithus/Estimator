using Microsoft.AspNetCore.Mvc;
using Shield.AudioConverter.AudioConverterServices;
using Shield.AudioConverter.AudioConverterServices.FFMpeg;
using Shield.Estimator.Business.Services.WhisperNet;
using Shield.Estimator.WpfMiniApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<WhisperNetService>();
builder.Services.AddScoped<WhisperFasterXXLService>();
builder.Services.AddSingleton<AudioConverterFactory>();
builder.Services.AddSingleton<FFMpegConverter>();

// CORS 
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()));

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 1_000_000_000); // 1GB

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapPost("/transcribe", async (HttpContext context, [FromServices] WhisperNetService whisperNet) => {
    var request = await context.Request.ReadFormAsync();
    var transcriptionRequest = new TranscriptionRequest
    {
        AudioFile = request.Files["audioFile"],
        Model = request["model"],
     };

    // Сохраняем файл во временную папку
    var tempFilePath = Path.GetTempFileName();
    using (var stream = new FileStream(tempFilePath, FileMode.Create))
    {
        await transcriptionRequest.AudioFile.CopyToAsync(stream);
    }

    Console.WriteLine($"Size of the file: {transcriptionRequest.AudioFile.Length}");
    Console.WriteLine($"Temp file size after save: {new FileInfo(tempFilePath).Length}");

    // Обрабатываем файл
    try
    {
        var result = await whisperNet.TranscribeAsync(
            tempFilePath, 
            transcriptionRequest.Model);
        //+
        //IProgress<string> progress = null,
        //CancellationToken ct = default,
        //bool UseWordTamestamps = true

        return Results.Ok(result);
    }
    catch(Exception e)
    {
        Console.WriteLine($"ERROR: {e.Message}");
        return Results.Problem();
    }
    finally
    {
        File.Delete(tempFilePath);
    }
})
.Accepts<TranscriptionRequest>("multipart/form-data")
.Produces<string>(200)
.Produces(400);

app.Run();
