using System.Windows;
using System.Windows.Controls;
namespace PanoramaViewer;
internal sealed class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Title = "Panorama Viewer – Nastavení";
        Width = 440; Height = 240; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var stack = new StackPanel { Margin = new Thickness(22) };
        stack.Children.Add(new TextBlock { Text = "API klíč Mapy.com", FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
        stack.Children.Add(new TextBlock { Text = "Klíč se uloží šifrovaně pro aktuálního uživatele Windows.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        var key = new PasswordBox { Height = 30, Password = KeyStore.Load() };
        stack.Children.Add(key);
        var save = new Button { Content = "Uložit", Width = 110, Height = 32, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        save.Click += (_, _) =>
        {
            try
            {
                KeyStore.Save(key.Password);
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("API klíč se nepodařilo uložit: " + ex.Message, "Panorama Viewer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        stack.Children.Add(save);
        Content = stack;
    }
}
