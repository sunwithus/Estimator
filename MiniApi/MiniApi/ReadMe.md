### Shield.WhisperNet.MiniApi

��� ����� MiniApi ������������ HTTP-������ � ���������� �������� curl ��� �������� ���������� �� ������ (WhisperNet) � �������������� API (� cmd �������� ������ "/" �� "^")

curl -X POST "http://192.168.2.252:7321/api/transcribe/net" \
     -H "Content-Type: multipart/form-data" \
     -F "audioFile=@D:\dotnet\wav\1.wav" \
     -F "model=ru" \
	 -F "useWordTimestamps=true"

###
����������� ����� ������ � ������� json � ����������� � ���� ���������� � ������ �� �����, �������� ����� ����� ������� ���:

    public string ExtractResultFromResponse(string responseText)
    {
        var dataEntries = responseText.Split(new[] { "data: " }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in dataEntries)
        {
            if (entry.Contains("\"result\""))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(entry.Trim());
                    if (jsonDoc.RootElement.TryGetProperty("result", out var resultProp))
                    {
                        return resultProp.GetString();
                    }
                }
                catch (JsonException ex)
                {
                    // ������ ��������
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                }
            }
        }

        return null; // ���� ��������� �� ������
    }

###
http://192.168.2.252:7321/api/transcribe/net - ����� (192.168.2.252) � ���� (7321) ��� �������� �� WhisperNet.MiniApi - �������� � ����� appsettings.json
����� � ������ appsettings.json �� ������ � �� WhisperNet.MiniApi - ����� ������ ���������
audioFile="���� � ������ �����", model="� ����� appsettings.json ������� ���� � ������ ��������� ������� ��� ������� �����"
������� ����������� ������ � ����� languages.txt, ������ �� ��������� � ������ ������ ����� ����� ������������������ ����� Docker Whisper (FasterWhisper ����� ������)

��������: 
appsettings.json �� ������
    "CustomModels": [ "zh", "uz", "tg", "tt", "mn", "ru" ], //"chinese", "uzbek", "tajik", "tatar", "mongolian", "russian"
    "WhisperNetApi": "http://192.168.2.252:7321/api/transcribe/net
appsettings.json �� WhisperNet.MiniApi 
    "Url": "http://0.0.0.0:7321"
    "CustomModels": {
        "uz": "D:\\AiModels\\Whisper\\ggml-large-uzbek.bin"
        ... ����� ���������
  }
  
###
����������� ��������� �������� ������������, ������, ���� ���� ����� ����������� ����� �����, �������� ������ �����. ������ ������ ��������� ������ � ������ ������.


