using System.Windows;

namespace Shield.Estimator.Wpf
{
    public partial class AboutWindow : Window
    {
        //public string Version => GetAssemblyVersion();

        public AboutWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Или this.Close();
        }
    }
}
