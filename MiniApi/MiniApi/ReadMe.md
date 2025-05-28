### Shield.WhisperNet.MiniApi

для теста MiniApi использовать HTTP-запрос с консольной утилитой curl для отправки аудиофайла на сервер (WhisperNet) с использованием API (в cmd заменить символ "/" на "^")

curl -X POST "http://192.168.2.252:7321/api/transcribe/net" \
     -H "Content-Type: multipart/form-data" \
     -F "audioFile=@D:\dotnet\wav\1.wav" \
     -F "model=ru" \
	 -F "useWordTimestamps=true"

###
Результатом будет строка в формате json с информацией о ходе выполнения и тестом из аудио, отдельно текст можно извлечь так:

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
                    // Ошибки парсинга
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                }
            }
        }

        return null; // если результат не найден
    }

###
http://192.168.2.252:7321/api/transcribe/net - адрес (192.168.2.252) и порт (7321) где запущено ПО WhisperNet.MiniApi - поменять в файле appsettings.json
Также в файлах appsettings.json ПО Оценка и ПО WhisperNet.MiniApi - порты должны совпадать
audioFile="путь к вашему аудио", model="в файле appsettings.json укажите пути к файлам кастомных моделей для нужного языка"
кодовые обозначения языков в файле languages.txt, другие не указанные в данных файлах языки будут транскрибироваться через Docker Whisper (FasterWhisper общая модель)

например: 
appsettings.json ПО Оценка
    "CustomModels": [ "zh", "uz", "tg", "tt", "mn", "ru" ], //"chinese", "uzbek", "tajik", "tatar", "mongolian", "russian"
    "WhisperNetApi": "http://192.168.2.252:7321/api/transcribe/net
appsettings.json ПО WhisperNet.MiniApi 
    "Url": "http://0.0.0.0:7321"
    "CustomModels": {
        "uz": "D:\\AiModels\\Whisper\\ggml-large-uzbek.bin"
        ... далее остальные
  }
  
###
Допускается несколько запросов одновременно, однако, если весь объём видеопамяти будет занят, скорость сильно упадёт. Каждый запрос загружает модель в память заново.


