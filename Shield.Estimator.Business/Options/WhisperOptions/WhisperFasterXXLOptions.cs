using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shield.Estimator.Business.Options.WhisperOptions;

public class WhisperFasterXXLOptions
{

    [JsonPropertyName("pathToFasterExe")]
    public string PathToFasterExe { get; set; }

    [JsonPropertyName("device")]
    public string Device { get; set; }

    [JsonPropertyName("diarize_device")]
    public string DiarizeDevice { get; set; }

    [JsonPropertyName("vad_device")]
    public string VadDevice { get; set; }

    [JsonPropertyName("mdx_device")]
    public string MdxDevice { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = "large-v3";

    [JsonPropertyName("model_dir")]
    public string ModelDir { get; set; }

    [JsonPropertyName("output_dir")]
    public string OutputDir { get; set; } = "OUTPUT";

    [JsonPropertyName("output_format")]
    public string OutputFormat { get; set; } = "txt";

    [JsonPropertyName("compute_type")]
    public string ComputeType { get; set; }

    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; }

    [JsonPropertyName("task")]
    public string Task { get; set; } = "transcribe";

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("language_detection_threshold")]
    public string LanguageDetectionThreshold { get; set; } = "0.6";

    [JsonPropertyName("temperature")]
    public string Temperature { get; set; } = "0.0";

    [JsonPropertyName("beam_size")]
    public string BeamSize { get; set; } = "10";

    [JsonPropertyName("patience")]
    public string Patience { get; set; } = "4";

    [JsonPropertyName("length_penalty")]
    public string LengthPenalty { get; set; } = "1.5";

    [JsonPropertyName("repetition_penalty")]
    public string RepetitionPenalty { get; set; } = "1.5";

    [JsonPropertyName("no_repeat_ngram_size")]
    public string NoRepeatNgramSize { get; set; } = "3";

    [JsonPropertyName("suppress_blank")]
    public string SuppressBlank { get; set; } = "True";

    [JsonPropertyName("suppress_tokens")]
    public string SuppressTokens { get; set; }

    [JsonPropertyName("condition_on_previous_text")]
    public string ConditionOnPreviousText { get; set; } = "False";

    [JsonPropertyName("word_timestamps")]
    public string WordTimestamps { get; set; } = "True";

    [JsonPropertyName("vad_filter")]
    public string VadFilter { get; set; } = "True";

    [JsonPropertyName("vad_threshold")]
    public string VadThreshold { get; set; } = "0.4";

    [JsonPropertyName("diarize")]
    public string Diarize { get; set; } = "pyannote_v3.1";

    [JsonPropertyName("threads")]
    public string Threads { get; set; } = "4";

    [JsonPropertyName("chunk_length")]
    public string ChunkLength { get; set; } = "30";

    [JsonPropertyName("batch_size")]
    public string BatchSize { get; set; } = "4";

    [JsonPropertyName("max_initial_timestamp")]
    public string MaxInitialTimestamp { get; set; } = "5.0";

    [JsonPropertyName("temperature_increment_on_fallback")]
    public string TemperatureIncrementOnFallback { get; set; } = "0.1";

    [JsonPropertyName("logprob_threshold")]
    public string LogprobThreshold { get; set; } = "-2.0";

    [JsonPropertyName("no_speech_threshold")]
    public string NoSpeechThreshold { get; set; } = "0.4";

    [JsonPropertyName("speaker")]
    public string Speaker { get; set; } = "SPEAKER";    
    
    [JsonPropertyName("min_speakers")]
    public string MinSpeakers { get; set; } = "1";   
    
    [JsonPropertyName("max_speakers")]
    public string MaxSpeakers { get; set; } = "4";

    [JsonPropertyName("print_progress")]
    public bool PrintProgress { get; set; } = true;

    [JsonPropertyName("ff_mdx_kim2")]
    public bool FfMdxKim2 { get; set; } = false;

}