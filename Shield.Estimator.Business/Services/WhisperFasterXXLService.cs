// WhisperFasterXXLService.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shield.Estimator.Business.Options.WhisperOptions;
using System.Diagnostics;
using System.Text;

public class WhisperFasterXXLService
{
    private readonly string _executablePath;
    private readonly IOptions<WhisperFasterXXLOptions> _options;
    private readonly ILogger<WhisperFasterXXLService> _logger;

    public WhisperFasterXXLService(IOptions<WhisperFasterXXLOptions> options, ILogger<WhisperFasterXXLService> logger)
    {
        _options = options;
        _executablePath = _options.Value.PathToFasterExe;
        _logger = logger;
    }

    public async Task<string> DetectLanguageAsync(string audioPath, IProgress<string> progress = null)
    {
        var arguments = BuildOptionsArguments();
        arguments += $" \"{audioPath}\" --task detect";
        return await ExecuteProcessAsync(arguments, progress);
    }

    public async Task<string> TranscribeAsync(IEnumerable<string> audioFiles, string inputPath, IProgress<string> progress = null, CancellationToken cts = default, string device = "cuda", bool withDiarization = true)
    {
        var filesArgs = string.Join(" ", audioFiles.Select(f => $"\"{f}\""));
        var optionsArgs = BuildOptionsArguments(inputPath, device, withDiarization);
        var arguments = $"{filesArgs} {optionsArgs}";

        _logger.LogInformation($"Final command: {_executablePath} {arguments}");
        return await ExecuteProcessAsync(arguments, progress, cts);
    }

    private string BuildOptionsArguments(string inputPath = null, string device = "cuda", bool withDiarization = true)
    {
        var opt = _options.Value;
        var args = new List<string>();

        // Явно прописываем все параметры согласно требованиям
        AddArg(args, "print_progress", opt.PrintProgress);
        AddArg(args, "ff_mdx_kim2", opt.FfMdxKim2); // default: False

        // Остальные параметры
        AddArg(args, "output_format", opt.OutputFormat);
        AddArg(args, "threads", opt.Threads);
        AddArg(args, "temperature", opt.Temperature);
        AddArg(args, "max_initial_timestamp", opt.MaxInitialTimestamp);
        AddArg(args, "length_penalty", opt.LengthPenalty);
        AddArg(args, "temperature_increment_on_fallback", opt.TemperatureIncrementOnFallback);
        AddArg(args, "logprob_threshold", opt.LogprobThreshold);
        AddArg(args, "no_speech_threshold", opt.NoSpeechThreshold);
        AddArg(args, "condition_on_previous_text", opt.ConditionOnPreviousText);

        //AddArg(args, "word_timestamps", opt.WordTimestamps);
        AddArg(args, "word_timestamps", "True"); // False // почему-то ошибка Error: --sentence requires --word_timestamps=True

        AddArg(args, "suppress_blank", opt.SuppressBlank);
        AddArg(args, "beam_size", opt.BeamSize);
        AddArg(args, "patience", opt.Patience);
        AddArg(args, "repetition_penalty", opt.RepetitionPenalty);
        AddArg(args, "chunk_length", opt.ChunkLength);
        AddArg(args, "language_detection_threshold", opt.LanguageDetectionThreshold);
        AddArg(args, "no_repeat_ngram_size", opt.NoRepeatNgramSize);
        AddArg(args, "model", opt.Model);
        AddArg(args, "batch_size", opt.BatchSize);
        AddArg(args, "output_dir", string.IsNullOrEmpty(inputPath) ? opt.OutputDir : inputPath);
        AddArg(args, "speaker", opt.Speaker);
        AddArg(args, "min_speakers", opt.MinSpeakers);
        AddArg(args, "max_speakers", opt.MaxSpeakers);

        if (withDiarization && !string.IsNullOrEmpty(opt.Diarize)) AddArg(args, "diarize", opt.Diarize);

        if (!string.IsNullOrEmpty(device))
        {
            AddArg(args, "device", device);
            AddArg(args, "diarize_device", device);
            AddArg(args, "vad_device", device);
            //AddArg(args, "mdx_device", device);
        }
        else 
        {
            if (!string.IsNullOrEmpty(opt.DiarizeDevice)) AddArg(args, "diarize_device", opt.DiarizeDevice);
            if (!string.IsNullOrEmpty(opt.VadDevice)) AddArg(args, "vad_device", opt.VadDevice);
            //if (!string.IsNullOrEmpty(opt.MdxDevice)) AddArg(args, "mdx_device", opt.MdxDevice);
        }
        return string.Join(" ", args);
    }

    private void AddArg<T>(List<string> args, string name, T value)
    {
        if (value == null) return;

        if (value is bool b)
        {
            if(b == true)
                args.Add($"--{name}");
        }
        else
        {
            args.Add($"--{name} {value}");
        }
    }

    private async Task<string> ExecuteProcessAsync(string arguments, IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        if(File.Exists(_executablePath))
        {
            _logger.LogInformation("");
            progress.Report("");
        }
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = new Process { StartInfo = startInfo };
        var tcs = new TaskCompletionSource<bool>();

        cancellationToken.Register(() =>
        {
            if (!process.HasExited)
            {
                process.Kill();
                tcs.TrySetCanceled(cancellationToken);
            }
        });


        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                _logger.LogInformation(e.Data);
                progress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogError(e.Data);
                progress?.Report($"[Err/Warn] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 || process.ExitCode != 1073740791)
        {
            var errorMessage = errorBuilder.ToString();
            _logger.LogError("Process failed: {Error}", errorMessage);
            throw new Exception($"Process exited with code {process.ExitCode}. Error: {errorMessage}");
        }

        return outputBuilder.ToString();
    }
}
