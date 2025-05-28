using Microsoft.AspNetCore.Mvc;
using Shield.Estimator.Business.Services.WhisperNet;
using Shield.WhisperNet.MiniApi.Models;
using Shield.WhisperNet.MiniApi.SeedLib;
using System.Text.Json;

namespace Shield.WhisperNet.MiniApi.Endpoints;

public static class TranscriptionEndpoints
{
    public static void MapTranscriptionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/transcribe/net", async (HttpContext context,
            [FromServices] WhisperNetService whisperNetService,
            [FromServices] IConfiguration config,
            [FromServices] ILogger<Program> logger) =>
        {
            using var scope = logger.BeginScope("Transcription request");

            try
            {
                var request = await ParseRequest(context);
                var customModels = config.GetSection("CustomModels").Get<Dictionary<string, string>>();

                if (!customModels.TryGetValue(request.Model, out var modelPath))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = $"Model '{request.Model}' not found" });
                    return;
                }

                var tempFilePath = await FileOperations.SaveTempFile(request.AudioFile);

                // Настройка ответа перед началом отправки данных
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json; charset=utf-8";
                //context.Response.ContentType = "text/event-stream";

                var progress = new Progress<string>(message =>
                {
                    var jsonMessage = JsonSerializer.Serialize(new { progress = message });
                    context.Response.WriteAsync($"data: {jsonMessage}\n\n");
                    Console.WriteLine($"data: {jsonMessage}\n\n");
                });

                try
                {
                    var result = await whisperNetService.TranscribeAsync(
                        tempFilePath,
                        modelPath,
                        progress,
                        context.RequestAborted,
                        request.UseWordTimestamps);

                    // Финализация ответа
                    var finalResult = JsonSerializer.Serialize(new { result });
                    string extractedResult = ExtractResultFromResponse(finalResult);

                    await context.Response.WriteAsync($"data: {finalResult}\n\n");
                    Console.WriteLine($"data: {extractedResult}\n\n");
                }
                finally
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (BadHttpRequestException ex)
            {
                await WriteProblemDetails(context, ex.StatusCode, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                await WriteProblemDetails(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (TaskCanceledException)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");
                await WriteProblemDetails(context,
                    StatusCodes.Status500InternalServerError,
                    "Internal server error");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }

        })
        .WithName("TranscribeNet")
        .Accepts<TranscribeRequest>("multipart/form-data")
        .Produces<string>(200)
        .ProducesProblem(400);
    }

    private static async Task WriteProblemDetails(
    HttpContext context,
    int statusCode,
    string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitleForStatusCode(statusCode),
            Detail = detail
        });
    }

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        404 => "Not Found",
        500 => "Internal Server Error",
        _ => "Error"
    };

    private static string ExtractResultFromResponse(string responseText)
    {
        var dataEntries = responseText.Split(new[] { "data: " }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in dataEntries)
        {
            if (entry.Contains("\"result\""))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(entry.Trim());
                    if (jsonDoc.RootElement.TryGetProperty("result", out var resultProp))
                    {
                        return resultProp.GetString();
                    }
                }
                catch (JsonException ex)
                {
                    // Обработка ошибок парсинга
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                }
            }
        }

        return null; // или выбросить исключение, если результат не найден
    }

    private static async Task<TranscribeRequest> ParseRequest(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync();

        // Проверка обязательного файла
        var audioFile = form.Files["audioFile"] ??
            throw new ArgumentException("Audio file is required");

        if (!FileOperations.IsSupportedMediaFormat(audioFile.FileName))
            throw new BadHttpRequestException("Unsupported file format", 400);

        if (!form.TryGetValue("model", out var model) || string.IsNullOrEmpty(model))
            throw new BadHttpRequestException("Model is required", 400);


        bool.TryParse(form["useWordTimestamps"], out var useWordTimestamps);

        return new TranscribeRequest
        {
            AudioFile = audioFile, //form.Files["audioFile"],
            Model = form["model"],
            UseWordTimestamps = useWordTimestamps
        };
    }
}
