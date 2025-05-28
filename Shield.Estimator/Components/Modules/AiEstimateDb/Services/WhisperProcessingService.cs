using Microsoft.Extensions.Options;
using Shield.Estimator.Business.Exceptions;
using Shield.Estimator.Business.Options.WhisperOptions;
using Shield.Estimator.Business.Services;
using Shield.Estimator.Business.Services.WhisperNet;
using Shield.Estimator.Shared.Components.EntityFrameworkCore.Sprutora;

namespace Shield.Estimator.Shared.Components.Modules.AiEstimateDb.Services;

public class WhisperProcessingService
{
    private readonly IOptions<WhisperCppOptions> _optionsCpp;
    private readonly IOptions<WhisperNetOptions> _optionsNet;
    private readonly WhisperFasterDockerService _whisperFaster;
    private readonly WhisperCppService _whisperCpp;
    private readonly WhisperNetService _whisperNet;
    private readonly WhisperNetApiService _whisperNetApi;
    private readonly ILogger<WhisperProcessingService> _logger;

    //private string _modelPathWhisperCpp = "";

    public WhisperProcessingService(
        IOptions<WhisperCppOptions> optionsCpp,
        IOptions<WhisperNetOptions> optionsNet,
        WhisperFasterDockerService whisperFaster,
        WhisperCppService whisperCpp,
        WhisperNetService whisperNet,
        WhisperNetApiService whisperNetApi,
        ILogger<WhisperProcessingService> logger)
    {
        _optionsCpp = optionsCpp;
        _optionsNet = optionsNet;
        _whisperFaster = whisperFaster;
        _whisperCpp = whisperCpp;
        _whisperNet = whisperNet;
        _whisperNetApi = whisperNetApi;
        _logger = logger;
    }

    public async Task<string> TranscribeAudioAsync(string audioPath, SprSpeechTable entity)
    {
        string recognizedText = "";
        try
        {
            _logger.LogInformation("WHISPER Started...");

            // Если язык не из списка, на который есть модель - Default через Docker Api
            if (!_optionsNet.Value.CustomModels.Contains(entity.SPostid))
            {
                _logger.LogInformation($"Распознавание _whisperFasterDocker");
                recognizedText = await _whisperFaster.TranscribeAsync(audioPath);
            }
            // Иначе - WhisperNetApi
            else
            {
                try
                {
                    _logger.LogInformation($"Распознавание _whisperNetApi");
                    recognizedText = await _whisperNetApi.TranscribeAsync(audioPath, _optionsNet.Value.WhisperNetApi, entity.SPostid);
                }
                catch
                {
                    Console.WriteLine();
                    Console.WriteLine("ОООООООООООООООООООШШШШШШШШШШШШШШШШШШШШШШШШШИИИИИИИИИИИИИИИИИИИИИИИИИБББББББББББББББББББББББББКККККККККККККККККККККККККАААААААААААААААААААААААА");
                    Console.WriteLine();
                    recognizedText = await _whisperFaster.TranscribeAsync(audioPath);
                }
            }
            return recognizedText;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new FailedWhisperRequestException("Whisper Error: ", e);
        }
        finally
        {
            Files.DeleteFilesByPath(audioPath);
        }
    }
}
