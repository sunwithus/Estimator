namespace Shield.WhisperNet.MiniApi.SeedLib
{
    public static class FileOperations
    {
        public static bool IsSupportedMediaFormat(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext is
                // Аудио
                ".wav" or ".mp3" or ".ogg" or ".flac" or ".aac" or
                ".m4a" or ".aiff" or ".wma" or ".opus" or ".ac3" or
                ".amr" or ".ape" or ".mka" or ".ra" or ".au" or
                // Видео и медиаконтейнеры
                ".mp4" or ".avi" or ".mov" or ".mkv" or ".webm" or
                ".flv" or ".wmv" or ".mpeg" or ".mpg" or ".3gp" or
                ".vob" or ".ts" or ".m4v" or ".ogv" or ".asf" or
                ".rm" or ".rmvb" or ".mxf" or ".divx" or ".f4v";
        }

        public static async Task<string> SaveTempFile(IFormFile file)
        {
            var tempFilePath = Path.GetTempFileName();
            using var stream = File.Create(tempFilePath);
            await file.CopyToAsync(stream);
            return tempFilePath;
        }
    }
}
