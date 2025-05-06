// FileProcessor.cs

using Shield.Estimator.Business.Services.WhisperNet;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Shield.Estimator.Business.Options.WhisperOptions;
using Shield.Estimator.Business._SeedLibs;
using System.Text;
using System.Media;


namespace Shield.Estimator.Wpf;

/// <summary>
/// Класс для обработки аудиофайлов и их транскрибирования с помощью WhisperNet.
/// </summary>
public class FileProcessor
{
    private readonly WhisperNetService _whisperNetService;
    private readonly WhisperFasterXXLService _whisperFasterXXLService;

    private readonly ILogger<FileProcessor> _logger;
    private CancellationTokenSource _cts;

    private readonly IOptions<WhisperNetOptions> _options;
    private readonly IOptions<WhisperFasterXXLOptions> _optionsFasterXXL;

    private readonly ConcurrentQueue<string> _fileQueue = new();
    private readonly SemaphoreSlim _processingSemaphore;
    private readonly HashSet<string> _processedFiles = new();
    private readonly object _syncRoot = new();

    /// <summary>
    /// Конструктор класса FileProcessor.
    /// </summary>
    /// <param name="whisperService">Сервис для транскрибирования аудио.</param>
    /// <param name="logger">Логгер для записи ошибок и информации.</param>
    public FileProcessor(
        WhisperNetService whisperService,
        WhisperFasterXXLService whisperFasterXXLService,
        ILogger<FileProcessor> logger,
        IOptions<WhisperNetOptions> options,
        IOptions<WhisperFasterXXLOptions> optionsFasterXXL)
    {
        _whisperNetService = whisperService;
        _whisperFasterXXLService = whisperFasterXXLService;
        _logger = logger;
        _options = options;
        _optionsFasterXXL = optionsFasterXXL;
        _processingSemaphore = new SemaphoreSlim(_options.Value.MaxConcurrentTasks, _options.Value.MaxConcurrentTasks);
    }

    /// <summary>
    /// FasterWhisper
    /// </summary>
    public async Task ProcessFasterExistingFilesAsync(string inputPath, string outputPath, ProcessStateWpf state, CancellationTokenSource? cts = default)
    {
        bool useWordTamestamps = state.UseWordTimestamps;
        bool isDiarizationEnabled = state.IsDiarizationEnabled;


        const int limitation = 3;
        //_cts = new CancellationTokenSource();
        _cts = cts;
        var mediaExtensions = GetSupportedMediaExtensions();
        var progress = new Progress<string>(message => UpdateConsole(state, message, true));

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var files = Directory.GetFiles(inputPath, "*.*")
                    .Where(file => !IsEnqueuedOrProcessed(file) && IsSupportedFile(file, mediaExtensions))
                    .ToList();

                if (files.Any())
                {
                    if (files.Count > limitation)
                        files = files.Take(limitation).ToList();

                    UpdateConsole(state, $"Порция файлов для обработки за 1 раз: {limitation}", true);
                    var tempFiles = new List<string>();

                    foreach (var file in files)
                    {
                        _logger?.LogInformation(file);

                        var tempPath = await CopyFileWithSecureName(file);
                        UpdateConsole(state, $"Добавлен в очередь: {file}", true);
                        UpdateConsole(state, $"Времменый файл: {tempPath}", true);

                        if (string.IsNullOrEmpty(tempPath)) continue;

                        tempFiles.Add(tempPath);
                    }


                    if (tempFiles.Any())
                    {
                        var tempDirectory = Path.Combine(inputPath, "tempTxt");
                        Directory.CreateDirectory(tempDirectory);
                        
                        try
                        {
                            await _whisperFasterXXLService.TranscribeAsync(tempFiles, tempDirectory, progress, _cts.Token, state.Device, isDiarizationEnabled);
                        }
                        catch (OperationCanceledException)
                        {
                            UpdateConsole(state, "Обработка отменена", true);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            UpdateConsole(state, $"Ошибка транскрибирования: {ex.Message}", true);
                            _logger?.LogError(ex, "Ошибка транскрибирования");
                        }
                        finally
                        {
                            try
                            {
                                PostProcessTranscripts(tempDirectory, inputPath, useWordTamestamps);
                            }
                            catch (Exception ex)
                            {
                                UpdateConsole(state, $"Ошибка постобработки транскрипций => {ex.Message}", true);
                                _logger?.LogError(ex, "Ошибка постобработки транскрипций");
                                // Дополнительная обработка ошибок
                            }
                        }
                        foreach (var file in files)
                        {
                            MoveProcessedFile(file, outputPath);
                        }
                        UpdateConsole(state, $"Исходные файлы будут перемещены в: {outputPath}", true);
                        await Task.Delay(1000);
                        Files.DeleteFilesByPath(tempFiles.ToArray());
                        Files.DeleteDirectory(tempDirectory);
                    }
                }
                else
                {
                    UpdateConsole(state, $"Ожидание файлов для обработки...", true);
                }

                await Task.Delay(TimeSpan.FromSeconds(11), _cts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки файлов");
            UpdateConsole(state, $"Критическая ошибка: {ex.Message}", true);
        }
    }

    public void PostProcessTranscripts(string tempDir, string outputDir, bool use = true)
    {
        Parallel.ForEach(Directory.GetFiles(tempDir, "*.txt"), async file =>
        {
            string originalFileName = Path.GetFileName(file);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            string extension = Path.GetExtension(file);
            string newFileName = $"{fileNameWithoutExtension}_NoTimeStamps{extension}";

            try
            {
                string input = File.ReadAllText(file);
                string output = ProcessSubtitles(input);
                string tempFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFile, output, Encoding.UTF8);
                File.Move(tempFile, file, true); // Atomic replace;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                _logger.LogError($"Error processing {file}: {ex.Message}");
            }


            try
            {
                var lines = File.ReadAllLines(file)
                    .AsParallel()
                    .ToList();

                File.WriteAllLines(
                    Path.Combine(outputDir, originalFileName),
                    lines,
                    Encoding.UTF8);


                lines = lines
                    .Select(LineWithNoTimeStamps)
                    .Where(line => !string.IsNullOrEmpty(line))
                    .ToList();

                File.WriteAllLines(
                    Path.Combine(outputDir, newFileName),
                    lines,
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing {file}");
            }
        });
    }

    private string LineWithNoTimeStamps(string line)
    {
        // Разделяем строку по ']' и берем часть после метки
        var parts = line.Split(new[] { ']' }, 2); // Максимум 2 части

        // Если метка присутствует и это не Собеседник (по длинне строки)
        if (parts.Length > 1 && parts[0].Length > 18)
        {
            _logger.LogInformation(parts[1].TrimStart());
            return parts[1].TrimStart();

        }
        _logger.LogInformation("без изм");
        // Если метки нет, возвращаем исходную строку
        return line;
    }

    public string ProcessSubtitles(string input)
    {
        var lines = input.Split('\n');
        var output = new StringBuilder();
        string currentSpeaker = null;

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { ']' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                output.AppendLine(line); // Если формат нарушен, оставляем как есть
                continue;
            }

            // Извлекаем и чистим компоненты
            var time = parts[0].Trim().TrimStart('[').Trim();
            var speaker = parts[1].Trim().TrimStart('[').Trim().TrimEnd(':').Trim();
            var text = parts[2].Trim().TrimStart(':').Trim();

            // Форматирование вывода
            if (speaker != currentSpeaker)
            {
                output.AppendLine($"[{speaker}]");
                currentSpeaker = speaker;
            }
            output.AppendLine($"[{time}]  {text}");
        }
        return output.ToString().TrimEnd();
    }

    /// <summary>
    /// WhisperNet
    /// </summary>
    public async Task ProcessNetExistingFilesAsync(string inputPath, string outputPath, string selectedModel, ProcessStateWpf state, bool UseWordTamestamps = true, CancellationTokenSource? cts = default)
    {
        //_cts = new CancellationTokenSource();

        _cts = cts;
        var mediaExtensions = GetSupportedMediaExtensions();
        var processingTask = Task.Run(() => ProcessQueueAsync(outputPath, selectedModel, state, UseWordTamestamps), _cts.Token);

        while (!_cts.Token.IsCancellationRequested)
        {
            var files = Directory.GetFiles(inputPath, "*.*")
                .Where(file => !IsEnqueuedOrProcessed(file))
                .ToList();

            foreach (var file in files)
            {
                if (IsSupportedFile(file, mediaExtensions))
                {
                    EnqueueFile(file);
                    UpdateConsole(state, $"Добавлен в очередь: {file}", true);
                }

            }
            if (!_processedFiles.Any())
            {
                UpdateConsole(state, $"Ожидание новых файлов...", true);
            }

            await Task.Delay(TimeSpan.FromSeconds(11), _cts.Token); // Уменьшено время задержки
        }
        await processingTask; // Ожидаем завершения только при отмене
    }

    private async Task<string> CopyFileWithSecureName(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        try
        {
            var tempDir = Path.GetTempPath();
            var extension = Path.GetExtension(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileNamewhithoutSpaces = fileName.Replace(" ", "_");
            var safeFileName = $"{fileNamewhithoutSpaces}{extension}";
            var destPath = Path.Combine(tempDir, safeFileName);

            if (File.Exists(destPath))
            {
                Files.DeleteFilesByPath(destPath);
            }

            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var destStream = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            return destPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error copying file {filePath}", filePath);
            return string.Empty;
        }
    }

    private void EnqueueFile(string filePath)
    {
        lock (_syncRoot)
        {
            if (_processedFiles.Contains(filePath)) return;
            _fileQueue.Enqueue(filePath);
            _processedFiles.Add(filePath);
        }
    }
    private HashSet<string> GetSupportedMediaExtensions() => new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".aac", ".flac", ".m4a", ".wma", ".aiff", ".alac",
        ".mp4", ".avi", ".mov", ".mkv", ".flv", ".wmv", ".webm", ".mpeg", ".3gp",
        ".m4v", ".vob", ".mts", ".m2ts", ".ts"
    };

    private bool IsSupportedFile(string file, HashSet<string> extensions) =>
    extensions.Contains(Path.GetExtension(file));

    private bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private void MoveToErrorFolder(string filePath, string outputPath)
    {
        try
        {
            var errorPath = Path.Combine(outputPath, "Errors");
            Directory.CreateDirectory(errorPath);
            File.Move(filePath, Path.Combine(errorPath, Path.GetFileName(filePath)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка перемещения файла в папку ошибок");
        }
    }

    private async Task ProcessQueueAsync(string outputPath, string selectedModel, ProcessStateWpf state, bool UseWordTamestamps = true)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (_fileQueue.TryDequeue(out var filePath))
            {
                await _processingSemaphore.WaitAsync(_cts.Token);

                // Запускаем обработку файла в отдельной задаче без ожидания
                _ = ProcessFileWithRetryAsync(filePath, outputPath, selectedModel, state, UseWordTamestamps)
                    .ContinueWith(_ => _processingSemaphore.Release());
            }
            else
            {
                await Task.Delay(500, _cts.Token);
            }
        }
    }

    private async Task ProcessFileWithRetryAsync(
    string filePath,
    string outputPath,
    string selectedModel,
    ProcessStateWpf state,
    bool UseWordTamestamps = true)
    {

        int maxRetries = 2;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                if (IsFileLocked(filePath) || new FileInfo(filePath).Length == 0)
                {
                    await Task.Delay(1000 * (retry + 1), _cts.Token);
                    continue;
                }

                await ProcessAudioFileAsync(filePath, outputPath, selectedModel, state, _cts.Token, UseWordTamestamps);
                MarkAsProcessed(filePath);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки файла {FilePath} (попытка {Retry})", filePath, retry + 1);
                if (retry == maxRetries - 1)
                    MoveToErrorFolder(filePath, outputPath);
            }
        }
    }

    /// <summary>
    /// после выполнения и перемещения файла - очистка очереди
    /// </summary>
    private void MarkAsProcessed(string filePath)
    {
        lock (_syncRoot)
        {
            _processedFiles.Remove(filePath);
        }
    }

    /// <summary>
    /// true, если файл обрабатывался
    /// </summary>
    private bool IsEnqueuedOrProcessed(string filePath)
    {
        lock (_syncRoot)
        {
            return _processedFiles.Contains(filePath);
        }
    }


    /// <summary>
    /// Асинхронно обрабатывает отдельный аудиофайл.
    /// </summary>
    /// <param name="filePath">Путь к обрабатываемому файлу.</param>
    /// <param name="outputPath">Путь к выходной директории.</param>
    /// <param name="state">Объект состояния процесса.</param>
    /// <param name="ct">Токен отмены.</param>
    private async Task ProcessAudioFileAsync(string filePath, string outputPath, string selectedModel, ProcessStateWpf state, CancellationToken ct, bool UseWordTamestamps = true)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            UpdateConsole(state, $"Выполняется: {Path.GetFileName(filePath)}\n####################", true);

            var progress = new Progress<string>(message => UpdateConsole(state, message, true));


            var transcription = await _whisperNetService.TranscribeAsync(filePath, selectedModel, progress, ct, UseWordTamestamps);

            var dirName = Path.GetDirectoryName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            var txtFilePath = Path.Combine(dirName, $"{fileNameWithoutExt}.txt");
            await File.WriteAllTextAsync(txtFilePath, transcription, ct);

            var txtFilePathWithNoTimeStamps = Path.Combine(dirName, $"{fileNameWithoutExt}_NoTimeStamps.txt");
            var lines = File.ReadAllLines(txtFilePath)
                .Select(LineWithNoTimeStamps)
                .Where(line => !string.IsNullOrEmpty(line))
                .ToList();
            File.WriteAllLines(txtFilePathWithNoTimeStamps, lines, Encoding.UTF8);

            MoveProcessedFile(filePath, outputPath);

            UpdateConsole(state, $"Обработан: {Path.GetFileName(filePath)}", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки файла {FilePath}", filePath);
            UpdateConsole(state, $"Ошибка при обработке файла {Path.GetFileName(filePath)}: {ex.Message}", true);
        }
        finally
        {
            PlayAudio("audio\\balloon.wav");
        }
    }

    /// <summary>
    /// Перемещает обработанный файл в выходную директорию.
    /// </summary>
    /// <param name="filePath">Путь к исходному файлу.</param>
    /// <param name="outputPath">Путь к выходной директории.</param>
    private void MoveProcessedFile(string filePath, string outputPath)
    {
        try
        {
            if (!File.Exists(filePath))
                return;

            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(outputPath, fileName);

            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Copy(filePath, destPath, overwrite: true);
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при перемещении файла {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Останавливает процесс обработки файлов.
    /// </summary>
    public void StopProcessing()
    {
        lock (_syncRoot)
        {
            _fileQueue.Clear();
            _processedFiles.Clear();
        }
    }

    /// <summary>
    /// Обновляет консольное сообщение о состоянии процесса.
    /// </summary>
    /// <param name="state">Объект состояния процесса.</param>
    /// <param name="message">Новое сообщение.</param>
    /// <param name="prepend">Флаг для добавления сообщения в начало (true) или в конец (false).</param>
    private void UpdateConsole(ProcessStateWpf state, string message, bool prepend)
    {
        var newMessage = prepend
            ? $"[{DateTime.Now:T}] {message}\n{state.ConsoleMessage}"

            : $"{state.ConsoleMessage}\n[{DateTime.Now:T}] {message}";

        state.ConsoleMessage = string.Join("\n", newMessage.Split('\n').Take(150));
    }

    public void PlayAudio(string filePath)
    {
        using (var player = new SoundPlayer(filePath))
        {
            player.Play();
        }
    }
}
