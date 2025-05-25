namespace Shield.Estimator.Business.Models.WhisperCppDto;

public class InferenceRequestDto
{
    // Основные параметры запроса из примеров cURL
    public float? Temperature { get; set; }
    public float? TemperatureInc { get; set; }
    public string ResponseFormat { get; set; } = "text";

    // Параметры модели с значениями по умолчанию из документации
    public int? Threads { get; set; } = 4;                 // -t, --threads [4]
    public int? Processors { get; set; } = 1;              // -p, --processors [1]
    public int? OffsetT { get; set; } = 0;                 // -ot, --offset-t [0]
    public int? OffsetN { get; set; } = 0;                 // -on, --offset-n [0]
    public int? Duration { get; set; } = 0;                // -d, --duration [0]
    public int? MaxContext { get; set; } = -1;             // -mc, --max-context [-1]
    public int? MaxLen { get; set; } = 0;                  // -ml, --max-len [0]
    public bool? SplitOnWord { get; set; } = false;        // -sow, --split-on-word [false]
    public int? BestOf { get; set; } = 2;                  // -bo, --best-of [2]
    public int? BeamSize { get; set; } = -1;               // -bs, --beam-size [-1]
    public float? WordThold { get; set; } = 0.01f;         // -wt, --word-thold [0.01]
    public float? EntropyThold { get; set; } = 2.40f;      // -et, --entropy-thold [2.40]
    public float? LogprobThold { get; set; } = -1.00f;     // -lpt, --logprob-thold [-1.00]
    public bool? DebugMode { get; set; } = false;          // -debug, --debug-mode [false]
    public bool? Translate { get; set; } = false;          // -tr, --translate [false]
    public bool? Diarize { get; set; } = false;            // -di, --diarize [false]
    public bool? Tinydiarize { get; set; } = false;        // -tdrz, --tinydiarize [false]
    public bool? NoFallback { get; set; } = false;         // -nf, --no-fallback [false]
    public bool? PrintSpecial { get; set; } = false;       // -ps, --print-special [false]
    public bool? PrintColors { get; set; } = false;        // -pc, --print-colors [false]
    public bool? PrintRealtime { get; set; } = false;      // -pr, --print-realtime [false]
    public bool? PrintProgress { get; set; } = false;      // -pp, --print-progress [false]
    public bool? NoTimestamps { get; set; } = false;       // -nt, --no-timestamps [false]
    public string Language { get; set; } = "en";           // -l, --language [en]
    public bool? DetectLanguage { get; set; } = false;     // -dl, --detect-language [false]
    public string Prompt { get; set; } = "";               // --prompt [empty]
    public string Model { get; set; } = "models/ggml-base.en.bin"; // -m, --model
    public string OvEDevice { get; set; } = "CPU";         // -oved, --ov-e-device [CPU]
    public bool? Convert { get; set; } = false;            // --convert [false]

    public float? NoSpeechThold { get; set; } = 0.6f;            // --no-speech-thold N [0.60]
    public bool? SuppressNst  { get; set; } = false;            //  --suppress-nst      [false  ]
    public bool? NoContext { get; set; } = false;            //  --no-context        [false  ]
    public bool? NoGpu { get; set; } = false;            // --no-gpu            [false  ]
    public bool? FlashAttn  { get; set; } = false;            // --flash-attn        [false  ]
}
