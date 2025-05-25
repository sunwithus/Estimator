namespace Shield.Estimator.WpfMiniApi;

public class TranscriptionRequest
{
    public IFormFile AudioFile { get; set; }
    public string Model { get; set; } = "base";
    /*
    public bool UseWordTimestamps { get; set; } = true;
    public string Device { get; set; } = "cpu";
    public bool IsDiarizationEnabled { get; set; } = false;
    */
}
