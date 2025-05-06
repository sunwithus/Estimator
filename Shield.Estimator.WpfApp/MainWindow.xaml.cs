//MainWindow.xaml.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.ComponentModel;
using System.Windows.Threading;
using System.Text.Encodings.Web;
using System.Collections.ObjectModel;

namespace Shield.Estimator.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private string _modelsDir = "D:\\AiModels\\Whisper";
        private string _modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggml_models");

        private readonly ProcessStateWpf _processStateWpf;
        private readonly FileProcessor _fileProcessor;
        private readonly IConfigurationRoot _configuration;

        private readonly DispatcherTimer _metricsTimer;

        private DispatcherTimer _loadingTimer;
        private int _loadingDotCount = 0;

        private CancellationTokenSource _cts;

        public MainWindow(ProcessStateWpf processStateWpf, FileProcessor fileProcessor, IConfiguration configuration)
        {
            InitializeComponent();
            _processStateWpf = processStateWpf;
            _fileProcessor = fileProcessor;
            DataContext = _processStateWpf;
            _configuration = (IConfigurationRoot)configuration;

            // Добавить таймер для обновления метрик
            _metricsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _metricsTimer.Tick += MetricsTimer_Tick;
            _metricsTimer.Start();

            LoadSettings();

            InitializeLoadingTimer();
        }

        private void MetricsTimer_Tick(object sender, EventArgs e)
        {
            _processStateWpf.UpdateMetrics();
        }

        private void InitializeLoadingTimer()
        {
            _loadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _loadingTimer.Tick += LoadingTimer_Tick;
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            _loadingDotCount = (_loadingDotCount + 1) % 4;
            UpdateLoadingStatus();
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void MenuItemLang_Click(object sender, RoutedEventArgs e)
        {
            var langWindow = new LangWindow();
            langWindow.Owner = this;
            langWindow.ShowDialog();
        }

        private void UpdateLoadingStatus()
        {
            LoadingStatusTextBlock.Text = new string('.', _loadingDotCount);
        }

        private void StartLoadingIndication()
        {
            _loadingDotCount = 0;
            UpdateLoadingStatus();
            LoadingStatusTextBlock.Visibility = Visibility.Visible;
            _loadingTimer.Start();
        }

        private void StopLoadingIndication()
        {
            _loadingTimer.Stop();
            LoadingStatusTextBlock.Visibility = Visibility.Collapsed;
        }

        private void LoadSettings()
        {
            InputPathTextBox.Text = _configuration["InputPath"];
            OutputPathTextBox.Text = _configuration["OutputPath"];
            _processStateWpf.UseFasterWhisper = bool.Parse(_configuration["UseFasterWhisper"] ?? "false");
            _processStateWpf.UseWordTimestamps = bool.Parse(_configuration["UseWordTimestamps"] ?? "true");
            
            _processStateWpf.IsDiarizationEnabled = bool.Parse(_configuration["IsDiarizationEnabled"] ?? "false");
            LoadAvailableModels();


        }
        private void LoadAvailableModels()
        {
            var models = new ObservableCollection<string>();

            if (Directory.Exists(_modelsDir))
            {
                foreach (var file in Directory.GetFiles(_modelsDir, "*.bin"))
                {
                    //models.Add(Path.GetFileName(file));
                    models.Add(Path.GetFullPath(file));
                }
            }

            _processStateWpf.AvailableModels = models;
        }



        private void SaveSettings()
        {
            UpdateConfiguration("InputPath", InputPathTextBox.Text);
            UpdateConfiguration("OutputPath", OutputPathTextBox.Text);
            UpdateConfiguration("UseWordTimestamps", _processStateWpf.UseWordTimestamps.ToString());
            UpdateConfiguration("UseFasterWhisper", _processStateWpf.UseFasterWhisper.ToString());

            UpdateConfiguration("IsDiarizationEnabled", _processStateWpf.IsDiarizationEnabled.ToString());
        }
        
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender == SelectInputButton || sender == SelectOutputButton)
            {
                var dialog = new OpenFolderDialog { Title = "Выберите директорию" };
                if (dialog.ShowDialog() == true)
                {
                    if (sender == SelectInputButton)
                        InputPathTextBox.Text = dialog.FolderName;
                    else
                        OutputPathTextBox.Text = dialog.FolderName;
                }
            }

            /*
            else if (sender == SelectModelButton)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select model file",
                    Filter = "Model files (*.bin)|*.bin",
                    CheckFileExists = true
                };
                if (dialog.ShowDialog() == true)
                {
                    ModelPathTextBox.Text = dialog.FileName;
                }
            }
            */
        }


        private async void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            if (!ValidatePaths()) return;

            _processStateWpf.ConsoleMessage = "Выполнение началось...";
            await Task.Delay(50);

            _processStateWpf.IsProcessing = true;

            SaveSettings();

            StartLoadingIndication();

            try
            {
                if (_processStateWpf.UseFasterWhisper)
                {
                    await _fileProcessor.ProcessFasterExistingFilesAsync(
                        InputPathTextBox.Text,
                        OutputPathTextBox.Text,
                        _processStateWpf,
                        _cts);
                }
                else
                {
                    if(ModelComboBox?.SelectedItem == null)
                    {
                        await Task.Delay(1000);
                        _processStateWpf.ConsoleMessage += "Сначала выберите из модель.";
                        return;
                    }
                    
                    await _fileProcessor.ProcessNetExistingFilesAsync(
                        InputPathTextBox.Text,
                        OutputPathTextBox.Text,
                        ModelComboBox.SelectedItem?.ToString(),
                        //ModelPathTextBox.Text,
                        _processStateWpf,
                        _processStateWpf.UseWordTimestamps,
                        _cts);
                }
            }
            catch (OperationCanceledException)
            {
                _processStateWpf.ConsoleMessage = "Processing stopped";
            }
            finally
            {
                _processStateWpf.IsProcessing = false;
                StopLoadingIndication();
            }
        }

        private void StopProcessing_Click(object sender, RoutedEventArgs e)
        {
            _processStateWpf.ConsoleMessage = "Stopping. Please wait...";

            // 1. Отмена текущих операций
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                _processStateWpf.ConsoleMessage = "Процесс уже завершён.";
            }

            _fileProcessor.StopProcessing();
            StopLoadingIndication();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveSettings();
            base.OnClosing(e);
        }
        /*
        private void EditPrompt_Click(object sender, RoutedEventArgs e)
        {
            var currentPrompt = _configuration["Prompt"] ?? string.Empty;
            var promptDialog = new PromptEditDialog(currentPrompt);
            if (promptDialog.ShowDialog() == true)
            {
                UpdateConfiguration("Prompt", promptDialog.Prompt);
            }
        }
        */
        public void UpdateConfiguration(string key, string value)
        {
            _configuration[key] = value;

            // Получаем провайдер конфигурации JSON
            var jsonConfigProvider = _configuration.Providers
                .FirstOrDefault(p => p is JsonConfigurationProvider) as JsonConfigurationProvider;

            if (jsonConfigProvider != null)
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, ((FileConfigurationSource)jsonConfigProvider.Source).Path);

                // Загрузка с поддержкой комментариев (если нужно)
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                var jsonObject = JsonNode.Parse(File.ReadAllText(filePath), null, options)
                    ?? throw new InvalidOperationException("Invalid JSON");

                // Обновление значения
                jsonObject[key] = value;

                // Сериализация без комментариев
                var writeOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                File.WriteAllText(filePath, jsonObject.ToJsonString(writeOptions));
                _configuration.Reload();
            }
        }


        private bool ValidatePaths()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(InputPathTextBox.Text))
                errors.Add("Input folder is required");

            if (string.IsNullOrWhiteSpace(OutputPathTextBox.Text))
                errors.Add("Output folder is required");
            /*
            if (!_processStateWpf.UseFasterWhisper)
            {
                if (string.IsNullOrWhiteSpace(ModelPathTextBox.Text))
                    errors.Add("Model file is required");
                else if (!File.Exists(ModelPathTextBox.Text))
                    errors.Add("Model file does not exist");
            }
            */
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }


    }
}