using Shield.Estimator.Business.Services.WhisperNet;
using Shield.Estimator.Business.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Shield.WhisperNet.MiniApi.Models;
using Shield.WhisperNet.MiniApi.SeedLib;
using Shield.WhisperNet.MiniApi.Endpoints;
using Shield.AudioConverter.AudioConverterServices.FFMpeg;
using Shield.AudioConverter.AudioConverterServices.Decoder;
using Shield.AudioConverter.AudioConverterServices;
using Shield.Estimator.Business.Options.WhisperOptions;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using System.ComponentModel.DataAnnotations;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services
    .AddOpenApi()
    .AddOptions<WhisperNetOptions>().BindConfiguration("WhisperNet")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddSingleton<AudioConverterFactory>()
    .AddSingleton<FFMpegConverter>()
    .AddSingleton<DecoderConverter>()
    .AddScoped<WhisperNetService>()
    .AddHttpContextAccessor()
    .AddHealthChecks();

// Настройка ограничений размера файла
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 1024 * 1024 * 1024);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024);

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler(exceptionHandlerApp =>
    exceptionHandlerApp.Run(async context =>
        await HandleGlobalException(context)));

// Настройка CORS для разработки
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Эндпоинт для транскрипции
app.MapTranscriptionEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.Run();


async Task HandleGlobalException(HttpContext context)
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogError(exception, "Global exception handler");

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";

    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Internal Server Error",
        Detail = "An unexpected error occurred"
    });
}

