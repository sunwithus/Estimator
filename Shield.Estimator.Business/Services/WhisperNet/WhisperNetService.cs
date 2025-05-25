//WhisperNetService.cs

using Microsoft.Extensions.Options;
using Whisper.net.LibraryLoader;
using Whisper.net;
using Whisper.net.Wave;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

using Shield.AudioConverter.AudioConverterServices;
using Shield.Estimator.Business.Options.WhisperOptions;

namespace Shield.Estimator.Business.Services.WhisperNet;

public class WhisperNetService : IDisposable
{
    private readonly IOptions<WhisperNetOptions> _options;
    private readonly AudioConverterFactory _converterFactory;
    private readonly ILogger<WhisperNetService> _logger;


    public WhisperNetService(
        IOptions<WhisperNetOptions> options,
        AudioConverterFactory converterFactory,
        ILogger<WhisperNetService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _converterFactory = converterFactory ?? throw new ArgumentNullException(nameof(_converterFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeRuntime();
    }
    private void InitializeRuntime()
    {
        RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Cuda, RuntimeLibrary.Vulkan, RuntimeLibrary.Cpu, RuntimeLibrary.CpuNoAvx, RuntimeLibrary.OpenVino];
        //using var whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.Info);
    }

    public async Task<string> TranscribeAsync(
        string audioFilePath,
        string selectedModel,
        IProgress<string> progress = null,
        CancellationToken ct = default,
        bool UseWordTamestamps = true)
    {
        using var _ = _logger.BeginScope("Transcription for {File}", Path.GetFileName(audioFilePath));

        try
        {
            progress?.Report("Initializing processing...");

            using var whisperFactory = WhisperFactory.FromPath(selectedModel);
            using var processor = CreateProcessor(whisperFactory);
            using var waveData = await ConvertAudioFile(audioFilePath);

            progress?.Report("Starting transcription...");

            var samples = await waveData.Parser.GetAvgSamplesAsync();
            var resultBuilder = new TranscriptionStringBuilder(waveData.Parser.Channels);

            await foreach (var segment in processor.ProcessAsync(samples).WithCancellation(ct))
            {
                var channel = await CalculateMaxEnergyChannel(waveData, segment);
                resultBuilder.AppendSegment(
                    segment.Text.TrimStart(),
                    channel,
                    //_options.Value.PrintTimestamps
                    UseWordTamestamps
                        ? $"[{FormatTime(segment.Start)} --> {FormatTime(segment.End)}]  "
                        : null);
            }

            progress?.Report("Transcription completed.");
            return resultBuilder.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio transcription failed");
            progress?.Report($"Audio transcription failed: {ex.Message}");
            Trace.TraceError($"Error: {ex.Message}");
            throw;
        }
    }

    // Вспомогательный метод для форматирования TimeSpan (также как FasterWhisperXXL)
    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
    }

    private WhisperProcessor CreateProcessor(WhisperFactory factory)
    {
        var options = _options.Value;
        var builder = factory!.CreateBuilder()

            .WithMaxLastTextTokens(options.MaxLastTextTokens)
            .WithOffset(options.Offset)
            .WithDuration(options.Duration)
            .WithLanguage(options.Language)

            .WithPrintTimestamps(options.PrintTimestamps)
            .WithTokenTimestampsThreshold(options.TokenTimestampsThreshold)
            .WithTokenTimestampsSumThreshold(options.TokenTimestampsSumThreshold)
            .WithMaxSegmentLength(options.MaxSegmentLength)
            .WithMaxTokensPerSegment(options.MaxTokensPerSegment)
            .WithTemperature(options.Temperature)
            .WithMaxInitialTs(options.MaxInitialTs)
            .WithLengthPenalty(options.LengthPenalty)
            .WithTemperatureInc(options.TemperatureInc)
            .WithEntropyThreshold(options.EntropyThreshold)
            .WithLogProbThreshold(options.LogProbThreshold)
            .WithNoSpeechThreshold(options.NoSpeechThreshold);

        if (options.Threads > 0) builder.WithThreads(options.Threads);
        if (options.UseTokenTimestamps) builder.WithTokenTimestamps();
        if (options.ComputeProbabilities) builder.WithProbabilities();
        if (options.Translate) builder.WithTranslate();
        if (options.NoContext) builder.WithNoContext();
        if (options.SingleSegment) builder.WithSingleSegment();
        if (options.SplitOnWord) builder.SplitOnWord();

        return builder.Build();
    }

    private async Task<WaveData> ConvertAudioFile(string audioFilePath)
    {
        foreach (var converterType in Enum.GetValues<ConverterType>())
        {
            try
            {
                var converter = _converterFactory.CreateConverter(converterType);
                var stream = await converter.ConvertFileToStreamAsync(audioFilePath);

                if (stream.Length == 0)
                {
                    stream.Dispose(); // Освобождаем пустой поток
                    continue;
                }

                // Тестовый вывод информации
                _logger.LogInformation($"Converter {converterType} loaded {stream.Length} bytes");

                var parser = new WaveParser(stream);
                await parser.InitializeAsync();
                return new WaveData(stream, parser);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Converter} conversion failed", converterType);
                throw;
            }
        }
        throw new InvalidOperationException("All audio conversions failed");
    }

    private async Task<int> CalculateMaxEnergyChannel(WaveData waveData, SegmentData segment)
    {
        var (startSample, endSample) = CalculateSampleRange(segment, waveData.Parser.SampleRate);
        var buffer = await ReadSegmentWaveData(waveData, startSample, endSample);

        return FindMaxEnergyChannel(buffer, waveData.Parser.Channels);
    }

    public int FindMaxEnergyChannel(short[] buffer, int channels)
    {
        var energy = new double[channels];
        var maxEnergy = 0d;
        var maxEnergyChannel = 0;

        for (var i = 0; i < buffer.Length; i++)
        {
            var channel = i % channels;
            energy[channel] += Math.Pow(buffer[i], 2);

            if (energy[channel] > maxEnergy)
            {
                maxEnergy = energy[channel];
                maxEnergyChannel = channel;
            }
        }
        return maxEnergyChannel;
    }

    private (long Start, long End) CalculateSampleRange(SegmentData segment, uint sampleRate) => (
        (long)(segment.Start.TotalMilliseconds * sampleRate / 1000),
        (long)(segment.End.TotalMilliseconds * sampleRate / 1000)
    );

    private async Task<short[]> ReadSegmentWaveData(WaveData waveData, long startSample, long endSample)
    {
        var frameSize = waveData.Parser.BitsPerSample / 8 * waveData.Parser.Channels;
        var bufferSize = (int)(endSample - startSample) * frameSize;
        var readBuffer = new byte[bufferSize];

        waveData.Stream.Position = waveData.Parser.DataChunkPosition + startSample * frameSize;
        await waveData.Stream.ReadAsync(readBuffer.AsMemory());

        var buffer = new short[readBuffer.Length / 2];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = BitConverter.IsLittleEndian
            ? (short)(readBuffer[i * 2] | (readBuffer[i * 2 + 1] << 8))
            : (short)((readBuffer[i * 2] << 8) | readBuffer[i * 2 + 1]);
        }
        return buffer;

    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public sealed class WaveData : IDisposable
    {
        public MemoryStream Stream { get; }
        public WaveParser Parser { get; }

        public WaveData(MemoryStream stream, WaveParser parser)
        {
            Stream = stream;
            Parser = parser;
        }
        public void Dispose()
        {
            Stream?.Dispose();
        }
    }

    public class TranscriptionStringBuilder
    {
        private readonly StringBuilder _sb = new();
        private int? _previousChannel;
        private readonly int _totalChannels;

        public TranscriptionStringBuilder(int totalChannels)
        {
            _totalChannels = totalChannels;
        }

        public void AppendSegment(string text, int currentChannel, string? segmentStartEnd = null)
        {
            var speakerLabel = GetSpeakerLabel(currentChannel);
            var isNewSpeaker = _previousChannel != currentChannel;

            _sb.Append(isNewSpeaker ? $"\n{speakerLabel}" : "");

            if (segmentStartEnd != null)
            {
                _sb.Append($"\n{segmentStartEnd}");
            }

            _sb.Append($"{text}");
            _previousChannel = currentChannel;
        }
        private string GetSpeakerLabel(int currentChannel)
        {
            return currentChannel <= _totalChannels ? $"[Собеседник_0{currentChannel}]" : "[Неизвестный]";
        }

        public string Build() => _sb.ToString().Trim();
    }

}