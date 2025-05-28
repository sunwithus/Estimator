using System.ComponentModel.DataAnnotations;

namespace Shield.WhisperNet.MiniApi.Models;

public class TranscribeRequest
{
    [Required] public IFormFile AudioFile { get; set; }
    public string Model { get; set; } = "uz";
    public bool UseWordTimestamps { get; set; } = true;
}
