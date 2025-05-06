using Npgsql.Internal;
using Shield.AudioConverter.AudioConverterServices;
using Whisper.net.Wave;

namespace Shield.Estimator.Shared.Components._SeedLibs
{
    public class AudioConverterKit
    {
        private readonly AudioConverterFactory _converterFactory;

        public AudioConverterKit(AudioConverterFactory converterFactory)
        {
            _converterFactory = converterFactory ?? throw new ArgumentNullException(nameof(_converterFactory));
        }

        private async Task<(int, byte[], byte[])> ConvertAudioFile(string audioFilePath)
        {
            foreach (var converterType in Enum.GetValues<ConverterType>())
            {
                try
                {
                    var converter = _converterFactory.CreateConverter(converterType);

                    return await converter.ConvertFileToByteArrayAsync(audioFilePath);
                   
                    //return (duration, left, right);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{converterType} conversion failed => {ex.Message}");
                }
            }
            throw new InvalidOperationException("All audio conversions failed");
        }
    }
}
