using Ookii.Dialogs.Wpf;
using System.Windows;

namespace Shield.Estimator.Voice2Text.Data;

public static class WindowsFolderPicker
{
    public static Task<string> PickFolderAsync()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Выберите папку",
            UseDescriptionForTitle = true,
            // Дополнительные настройки:
            SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ShowNewFolderButton = true,
            RootFolder = Environment.SpecialFolder.Desktop
        };

        // Получаем главное окно приложения
        var mainWindow = Application.Current.MainWindow;

        // Показываем диалог (синхронно, но возвращаем Task для совместимости)
        bool? result = dialog.ShowDialog(mainWindow);

        return Task.FromResult(result == true ? dialog.SelectedPath : null);
    }
}
