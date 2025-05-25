using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using MudBlazor.Services;

using Shield.Estimator.Voice2Text.Data;

using Shield.AudioConverter.AudioConverterServices.FFMpeg;
using Shield.AudioConverter.AudioConverterServices.Decoder;
using Shield.AudioConverter.AudioConverterServices;
using Shield.AudioConverter.Options;
using Shield.Estimator.Business.Options.WhisperOptions;
using Shield.Estimator.Business.Services.WhisperNet;

namespace Shield.Estimator.Voice2Text;

public static class Startup
{
    public static IServiceProvider? Services { get; private set; }
    private static IHost? _host;

    public static void Init()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Автоматическое обновление конфигурации
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })

                       .ConfigureServices(WireupServices)
                       .Build();
        // Запуск хоста для активации фоновых сервисов (если есть)
        //_host.Start();
        Services = _host.Services;
    }

    private static void WireupServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddWpfBlazorWebView();
        services.AddMudServices();


        // Options
        services.Configure<WhisperNetOptions>(context.Configuration.GetSection("WhisperNet"));
        services.Configure<WhisperFasterXXLOptions>(context.Configuration.GetSection("WhisperFasterXXLOptions"));
        services.Configure<AudioConverterOptions>(context.Configuration.GetSection("AudioConverterConfig"));

        // Whisper Services
        services.AddSingleton<WhisperNetService>();
        services.AddSingleton<WhisperFasterXXLService>();

        // Audio Converter
        services.AddSingleton<FFMpegConverter>();
        services.AddSingleton<DecoderConverter>();
        services.AddSingleton<AudioConverterFactory>();

        services.AddScoped<ProcessState>();
        services.AddSingleton<FileProcessor>();
        services.AddSingleton<IConfiguration>(context.Configuration);
        //services.AddSingleton<WeatherForecastService>(); //using Shield.Estimator.Voice2Text.Data;

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
    }

}
