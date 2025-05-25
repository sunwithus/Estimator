//ProcessStateWpf.cs 

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
//using System.Management;
using System.Windows.Input;

namespace Shield.Estimator.Voice2Text.Data;

public class ProcessState : INotifyPropertyChanged
{
    private string _consoleMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isProcessing;
    private bool _useFasterWhisper;
    private bool _useWordTimestamps;

    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramCounter;
    private float _cpuUsage;
    private float _ramUsage;

    public string Device => UseCpu ? "cpu" : "cuda";


    public ProcessState()
    {
        CheckCudaAvailability();
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
    }


    private string _cudaMemoryGB;
    public string CudaMemoryGB
    {
        get => _cudaMemoryGB;
        set
        {
            _cudaMemoryGB = value;
            OnPropertyChanged(nameof(CudaMemoryGB));
        }
    }

    private void CheckCudaAvailability()
    {
        try
        {
            /*
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                string description = obj["Description"]?.ToString();
                if (description?.Contains("NVIDIA") == true)
                {
                    CudaAvailable = true;
                    ulong vramBytes = Convert.ToUInt64(obj["AdapterRAM"]);
                    CudaMemoryGB = $"Видеопамять: {vramBytes / (1024 * 1024 * 1024)} GB CUDA";
                    return;
                }
            }
            */
        }
        catch
        {
            CudaAvailable = false;
            CudaMemoryGB = string.Empty;
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////

    private bool _useCpu = true;
    public bool UseCpu
    {
        get => _useCpu;
        set
        {
            if (_useCpu != value)
            {
                _useCpu = value;
                OnPropertyChanged(nameof(UseCpu));
            }
        }
    }


    public ICommand ToggleCpuCommand => new RelayCommand(() => UseCpu = !UseCpu);


    private string _loadingStatus;
    public string LoadingStatus
    {
        get => _loadingStatus;
        set
        {
            if (_loadingStatus == value) return;
            _loadingStatus = value;
            OnPropertyChanged(nameof(LoadingStatus));
        }
    }

    private bool _cudaAvailable;
    public bool CudaAvailable
    {
        get => _cudaAvailable;
        set
        {
            if (_cudaAvailable == value) return;
            _cudaAvailable = value;
            OnPropertyChanged(nameof(CudaAvailable));
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////
    private bool _isDiarizationEnabled;
    public bool IsDiarizationEnabled
    {
        get => _isDiarizationEnabled;
        set
        {
            if (_isDiarizationEnabled == value) return;
            _isDiarizationEnabled = value;
            OnPropertyChanged(nameof(IsDiarizationEnabled));
        }
    }

    private ObservableCollection<string> _availableModels = new ObservableCollection<string>();
    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        set
        {
            if (_availableModels == value) return;
            _availableModels = value;
            OnPropertyChanged(nameof(AvailableModels));
        }
    }

    private string _selectedModel;
    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (_selectedModel == value) return;
            _selectedModel = value;
            OnPropertyChanged(nameof(SelectedModel));
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////
    public bool UseFasterWhisper
    {
        get => _useFasterWhisper;
        set
        {
            if (_useFasterWhisper != value)
            {
                _useFasterWhisper = value;
                OnPropertyChanged(nameof(UseFasterWhisper));
                OnPropertyChanged(nameof(IsModelSelectionEnabled));
            }
        }
    }

    public bool UseWordTimestamps
    {
        get => _useWordTimestamps;
        set
        {
            if (_useWordTimestamps != value)
            {
                _useWordTimestamps = value;
                OnPropertyChanged(nameof(UseWordTimestamps));
                OnPropertyChanged(nameof(IsModelSelectionEnabled));
            }
        }
    }

    public bool IsModelSelectionEnabled => (!UseFasterWhisper && !IsProcessing) || (!IsProcessing && !UseFasterWhisper);
    //public bool IsModelSelectionEnabled => !UseFasterWhisper && !IsProcessing;

    public float CpuUsage
    {
        get => _cpuUsage;
        set { _cpuUsage = value; OnPropertyChanged(nameof(CpuUsage)); }
    }

    private float _freeMemory;
    public float FreeMemory
    {
        get => _freeMemory;
        set
        {
            _freeMemory = value;
            OnPropertyChanged(nameof(FreeMemory));
            OnPropertyChanged(nameof(MemoryStatus));
        }
    }

    private float _totalMemoryGb;
    public float TotalMemoryGb
    {
        get => _totalMemoryGb;
        set
        {
            _totalMemoryGb = value;
            OnPropertyChanged(nameof(TotalMemoryGb));
            OnPropertyChanged(nameof(MemoryStatus));
        }
    }
    public string MemoryStatus => $"(Доступно RAM: {FreeMemory:F0} Мб)"; ///{TotalMemoryGb:F1}

    public float RamUsage
    {
        get => _ramUsage;
        set { _ramUsage = value; OnPropertyChanged(nameof(RamUsage)); }
    }

    public void UpdateMetrics()
    {
        CpuUsage = _cpuCounter.NextValue();
        FreeMemory = _ramCounter.NextValue();
        TotalMemoryGb = GetTotalMemory() / 1024; // Конвертация Мб в Гб
        RamUsage = 100 - (_ramCounter.NextValue() / GetTotalMemory() * 100);
    }

    private float GetTotalMemory()
    {
        /*
        using var mc = new ManagementClass("Win32_ComputerSystem");
        foreach (ManagementObject mo in mc.GetInstances())
        {
            return (float)(Convert.ToDouble(mo["TotalPhysicalMemory"]) / (1024 * 1024));
        }
        */
        return 0;
    }


    public string ConsoleMessage
    {
        get => _consoleMessage;
        set
        {
            _consoleMessage = value;
            OnPropertyChanged(nameof(ConsoleMessage));
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged(nameof(IsProcessing));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object parameter) => _execute();
}
