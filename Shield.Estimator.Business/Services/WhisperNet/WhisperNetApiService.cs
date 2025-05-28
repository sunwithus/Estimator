// WhisperNetApiService.cs

using Microsoft.Extensions.Logging;
using Shield.Estimator.Business.Exceptions;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace Shield.Estimator.Business.Services.WhisperNet;

public class WhisperNetApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhisperNetApiService> _logger;

    public WhisperNetApiService(
        HttpClient httpClient,
        ILogger<WhisperNetApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath, string url, string model)
    {
        try
        {
            return await SendAudioRequestAsync(audioFilePath, url, model);
        }
        catch (FailedWhisperRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for {FilePath}", audioFilePath);
            throw new FailedWhisperRequestException(
                $"TranscribeAsync error: {ex.Message}",
                ex);
        }
    }

    private async Task<string> SendAudioRequestAsync(string audioFilePath, string requestUrl, string model)
    {
        using var form = new MultipartFormDataContent();
        var startTime = DateTime.UtcNow;

        try
        {
            // Добавляем параметр модели
            form.Add(new StringContent(model), "model");
            form.Add(new StringContent("true"), "useWordTimestamps");

            // Добавляем аудиофайл
            var fileStream = File.OpenRead(audioFilePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Octet);
            form.Add(fileContent, "audioFile", Path.GetFileName(audioFilePath));
            

            _logger.LogInformation("\n\n\n");
            _logger.LogInformation(
                "Sending request to {Url} with model {Model} and file {File}",
                requestUrl,
                model,
                audioFilePath);

            var response = await _httpClient.PostAsync(requestUrl, form);

            _logger.LogDebug(
                "Received response: {StatusCode}\nHeaders: {Headers}",
                response.StatusCode,
                response.Headers);

            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "Request to {Url} completed in {Seconds} seconds",
                requestUrl,
                (DateTime.UtcNow - startTime).TotalSeconds.ToString("F2"));

            responseText = ExtractResultFromResponse(responseText);

            return responseText;
        }
        catch (HttpRequestException ex)
        {
            throw new FailedWhisperRequestException(
                $"HTTP request failed: {ex.Message}",
                ex);
        }
        catch (IOException ex)
        {
            throw new FailedWhisperRequestException(
                $"File IO error: {ex.Message}",
                ex);
        }
        finally
        {
            form.Dispose();
        }
    }

    public string ExtractResultFromResponse(string responseText)
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
}
