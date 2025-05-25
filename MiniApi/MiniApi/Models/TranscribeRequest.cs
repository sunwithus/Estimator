namespace MiniApi.Models;

public class TranscribeRequest
{
    public IFormFile AudioFile { get; set; }
    public string Model { get; set; }
    public bool UseWordTimestamps { get; set; }
    public bool UseDiarization { get; set; }
    public string Device { get; set; } = "cpu";
}
