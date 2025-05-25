using Shield.Estimator.Business.Models.WhisperCppDto;

namespace Shield.Estimator.Business.Options.WhisperOptions
{
    public class WhisperCppOptions
    {
        public string InferenceUrl { get; set; }
        public string LoadUrl { get; set; }
        public Dictionary<string, string> CustomModels { get; set; }
        public InferenceRequestDto Params { get; set; }
    }
}
