using System.Windows;

namespace Shield.Estimator.Wpf;

public partial class LangWindow : Window
{
    //public string Version => GetAssemblyVersion();

    public LangWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    // Добавленный обработчик
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close(); // Или this.Close();
    }
}
public class LanguageData
{
    public string Language { get; set; }
    public double ErrorRate { get; set; }
}
