//ProcessState.cs 

using System.Diagnostics;
using System.Management;
using System.Timers;

public class ProcessState : IDisposable
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramCounter;
    private readonly System.Timers.Timer _metricsTimer;
    private float _totalMemoryMB;

    public bool IsProcessing;
    public bool UseFaster;
    public bool UseWordTimestamps;
    public bool IsDiarizationEnabled;
    public bool UseCpu = false;
    public string Device;
    public string ConsoleMessage;

    public double CpuUsage { get; private set; }
    public double RamUsage { get; private set; }
    public event Action<double, double> OnMetricsUpdated;


    public ProcessState()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            InitializeTotalMemory();

            _metricsTimer = new System.Timers.Timer(2000);
            _metricsTimer.Elapsed += (s, e) => UpdateMetrics();
            _metricsTimer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing counters: {ex.Message}");
        }
    }

    private void InitializeTotalMemory()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                _totalMemoryMB = totalBytes / (1024f * 1024f);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting total memory: {ex.Message}");
            _totalMemoryMB = 8192; // Fallback value
        }
    }

    private void UpdateMetrics()
    {
        try
        {
            var cpu = _cpuCounter.NextValue();
            var availableMB = _ramCounter.NextValue();
            var usedMB = _totalMemoryMB - availableMB;
            var ramUsage = (usedMB / _totalMemoryMB) * 100;

            CpuUsage = cpu;
            RamUsage = ramUsage;

            OnMetricsUpdated?.Invoke(CpuUsage, RamUsage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating metrics: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _metricsTimer?.Dispose();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
