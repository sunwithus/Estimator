OPEN CHAT


ты разработчик C#, используешь все лучшие практики программирования

напиши код реализующий транскрибирование аудио с использованием FasterWhisper с диаризацией говорящих, работать должно всё оффлайн
используя
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
пусть это будет консольное приложение

коды языков
https://ru.wikipedia.org/wiki/%D0%A1%D0%BF%D0%B8%D1%81%D0%BE%D0%BA_%D0%BA%D0%BE%D0%B4%D0%BE%D0%B2_ISO_639-1
https://snipp.ru/handbk/iso-639-1?ysclid=m82lu7pgqh64460276

whisperx C:/temp/wav/1.wav --model large-v3 --output_dir C:/temp/wav/output --output_format txt --device cpu --diarize --compute_type float32 

--model: выбор модели (например, large-v2)
--language: язык аудио (например, ru для русского)
--compute_type: тип вычислений (float16 для ускорения на GPU)
--output_dir: директория для сохранения результатов
--output_format: формат вывода (txt, vtt, srt, tsv, json)
--device: устройство для вычислений (cuda для GPU, cpu для CPU)
--batch_size: размер батча для ускорения обработки
--diarize: включить диаризацию (определение говорящих)


посоветуй хорошую языковую модель для запуска на локальном ПК (intel Core i9, 64Ram, Geforce Ti 3060 8Gb), запускать модель буду при помощи koboldcpp (или посоветуй другое средство)
для принятия решения о важности информации (оценка наличия нарушений законодательства), чтоб выставляла оценку материала
и напиши для этого prompt, чтоб получился ответ в виде - 0 Краткое содержание. (первый символ - это цифра, если 0 - нет нарушений, если 1 - преступления с наркотиками, 2 - если другое преступление, а после написать краткое содержание материала)
и напиши параметры с которыми лучше запустить модель (temperature и другие)

Для запуска на локальном ПК с указанными характеристиками (Intel Core i9, 64GB RAM, GeForce RTX 3060 8GB) хорошим выбором будет модель Llama 2 7B или 13B1. Эти модели обеспечивают хороший баланс между производительностью и требованиями к ресурсам.
KoboldCPP - подходящий инструмент для запуска модели локально3. Альтернативой может быть llama.cpp, который также оптимизирован для локального запуска3.
Вот пример промпта для оценки материала на наличие нарушений законодательства:
text

/////////////////////////////////////
Проанализируй следующий текст на наличие нарушений законодательства. Оцени содержание и предоставь ответ в следующем формате:

[Оценка] Краткое содержание

Где [Оценка] - это одна цифра:
0 - нет нарушений законодательства
1 - преступления, связанные с наркотиками
2 - 
3 - 
...
...
9 - другие виды преступлений

Краткое содержание должно в нескольких предложениях описывать суть материала.
/////////////////////////////////////
/////////////////////////////////////
Проанализируй текст и определи, есть ли в нем нарушения законодательства. Ответ предоставь в следующем формате:

0 Краткое содержание. (если нарушений нет)
1 Краткое содержание. (если есть преступления, связанные с наркотиками)
2 Краткое содержание. (если есть другие преступления)

Текст: {вставь текст для анализа}

Текст для анализа:
[Вставьте анализируемый текст здесь]
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////


./build/bin/whisper-server -h

usage: ./build/bin/whisper-server [options]

options:
  -h,        --help              [default] show this help message and exit
  -t N,      --threads N         [4      ] number of threads to use during computation
  -p N,      --processors N      [1      ] number of processors to use during computation
  -ot N,     --offset-t N        [0      ] time offset in milliseconds
  -on N,     --offset-n N        [0      ] segment index offset
  -d  N,     --duration N        [0      ] duration of audio to process in milliseconds
  -mc N,     --max-context N     [-1     ] maximum number of text context tokens to store
  -ml N,     --max-len N         [0      ] maximum segment length in characters
  -sow,      --split-on-word     [false  ] split on word rather than on token
  -bo N,     --best-of N         [2      ] number of best candidates to keep
  -bs N,     --beam-size N       [-1     ] beam size for beam search
  -wt N,     --word-thold N      [0.01   ] word timestamp probability threshold
  -et N,     --entropy-thold N   [2.40   ] entropy threshold for decoder fail
  -lpt N,    --logprob-thold N   [-1.00  ] log probability threshold for decoder fail
  -debug,    --debug-mode        [false  ] enable debug mode (eg. dump log_mel)
  -tr,       --translate         [false  ] translate from source language to english
  -di,       --diarize           [false  ] stereo audio diarization
  -tdrz,     --tinydiarize       [false  ] enable tinydiarize (requires a tdrz model)
  -nf,       --no-fallback       [false  ] do not use temperature fallback while decoding
  -ps,       --print-special     [false  ] print special tokens
  -pc,       --print-colors      [false  ] print colors
  -pr,       --print-realtime    [false  ] print output in realtime
  -pp,       --print-progress    [false  ] print progress
  -nt,       --no-timestamps     [false  ] do not print timestamps
  -l LANG,   --language LANG     [en     ] spoken language ('auto' for auto-detect)
  -dl,       --detect-language   [false  ] exit after automatically detecting language
             --prompt PROMPT     [       ] initial prompt
  -m FNAME,  --model FNAME       [models/ggml-base.en.bin] model path
  -oved D,   --ov-e-device DNAME [CPU    ] the OpenVINO device used for encode inference
  --host HOST,                   [127.0.0.1] Hostname/ip-adress for the server
  --port PORT,                   [8080   ] Port number for the server
  --convert,                     [false  ] Convert audio to WAV, requires ffmpeg on the server
  
  
  
  
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////



В Windows, скорее всего, понадобится MSYS2 или MinGW. Возможно, CMake тоже нужен для сборки.
Потом нужно клонировать репозиторий с GitHub. После клонирования, наверное, нужно собрать проект с помощью CMake. Важно проверить, есть ли в репозитории инструкции для Windows, возможно, есть особенности.

После сборки, вероятно, нужно скачать модель для Whisper. Пользователь хочет использовать серверный режим, значит, нужно запустить бинарник с параметрами сервера. Надо посмотреть, какие аргументы командной строки нужны для запуска сервера, например, указать порт и модель.

Может возникнуть проблема с зависимостями, например, отсутствие DLL или проблемы с путями. Нужно обратить внимание на установку правильных версий библиотек. Также, возможно, потребуется установить Python для скриптов или дополнительных инструментов, но это не точно.

Нужно проверить документацию whisper.cpp на предмет специфичных для Windows шагов. Возможно, есть предупреждения или дополнительные требования. Если в процессе возникнут ошибки, пользователю придется их диагностировать, например, отсутствие libssl или других библиотек.

Важно также убедиться, что порт, который использует сервер, не заблокирован брандмауэром. Или что антивирус не блокирует запуск бинарника. Возможно, потребуется добавить исключение.

В итоге, шаги будут примерно такими: установка MSYS2 и необходимых пакетов, клонирование репозитория, сборка проекта, загрузка модели, запуск сервера с указанием модели и порта. Затем тестирование через curl или браузер.
</think>

Вот пошаговая инструкция по установке и запуску whisper.cpp в режиме сервера на Windows 10:

обновить cmake

Установка MSYS2 и зависимостей

bash
- Скачайте MSYS2: https://www.msys2.org/
- Установите, обновите пакеты:
  pacman -Syu
- Установите необходимые компоненты:
  pacman -S git cmake make mingw-w64-x86_64-toolchain
Клонирование репозитория

bash
git clone https://github.com/ggerganov/whisper.cpp
cd whisper.cpp

cd /d/dotnet/whisper.cpp
cd d:/dotnet/whisper.cpp

Сборка проекта

bash
mkdir build
cd build
cmake -G "MinGW Makefiles" ..
mingw32-make -j4
Скачивание модели

bash
cd d:/dotnet/whisper.cpp
# Из папки whisper.cpp
.\models\download-ggml-model.cmd base.en
# Или через PowerShell:
Invoke-WebRequest -Uri "https://huggingface.co/datasets/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin" -OutFile "models\ggml-base.en.bin"
Запуск сервера

bash
# Из папки build
.\bin\Release\server.exe -m ..\models\ggml-base.en.bin --port 8080
d:\dotnet\whisper.cpp.cpu\build\bin>whisper-server --model ..\..\models\ggml-base.en.bin --port 8080
c:\dotnet\whisper.cpp.cpu\build\bin>whisper-server --model ..\..\models\ggml-base.en.bin --host 192.168.2.254 --port 8080 -t 20 -p 8
# Вариант 1: 1 процесс, 20 потоков
whisper-server ... -t 20 -p 1
# Вариант 2: 8 процессов, 4 потока на каждый (как в исходном примере)
whisper-server ... -t 4 -p 8

 Проверка работы
Откройте в браузере:

text
http://localhost:8080
Или отправьте аудио через curl:

bash
curl -X POST http://localhost:8080/inference -H "accept: application/json" -H "Content-Type: multipart/form-data" -F "audio=@sample.wav"
Дополнительно:

Для использования других моделей замените base.en на нужную (tiny, small, medium)

Добавьте флаг --host 0.0.0.0 для доступа из сети

Используйте --threads N для указания количества ядер CPU

Для реального использования рекомендуется модель medium или large-v3

Пример полной команды запуска:

bash
.\server.exe -m ..\models\ggml-large-v3.bin --port 9000 --threads 8 --host 0.0.0.0
Если возникнут проблемы с зависимостями, проверьте наличие:

Microsoft Visual C++ Redistributable

Правильных путей к Mingw-w64 в переменной PATH

Последней версии CMake (3.27+)