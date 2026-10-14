using System;
using System.Windows;
using System.Windows.Controls;

namespace PanoramaViewer;

internal sealed class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Title = "Panorama Viewer \u2013 Nastaven\u00ed";
        Width = 510;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (Application.Current?.MainWindow is Window owner) Owner = owner;

        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(Label("API kl\u00ed\u010d Mapy.com"));
        var mapyKey = new PasswordBox
        {
            Height = 30,
            Password = KeyStore.Load(PanoramaProvider.Mapy)
        };
        panel.Children.Add(mapyKey);

        panel.Children.Add(Label("Google Maps API kl\u00ed\u010d (voliteln\u00fd)"));
        var googleKey = new PasswordBox
        {
            Height = 30,
            Password = KeyStore.Load(PanoramaProvider.Google)
        };
        panel.Children.Add(googleKey);
        panel.Children.Add(new TextBlock
        {
            Text = "Pro Street View v okn\u011b zapn\u011bte Maps JavaScript API a fakturaci " +
                   "v Google Cloud. Pou\u017e\u00edv\u00e1n\u00ed m\u016f\u017ee b\u00fdt zpoplatn\u011bno. " +
                   "Bez tohoto kl\u00ed\u010de se Google otev\u0159e v b\u011b\u017en\u00e9m prohl\u00ed\u017ee\u010di " +
                   "bez synchronizace polohy.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Kl\u00ed\u010de se ukl\u00e1daj\u00ed \u0161ifrovan\u011b pro tohoto u\u017eivatele Windows. " +
                   "Zm\u011bna se pou\u017eije p\u0159i dal\u0161\u00edm kliknut\u00ed do mapy. " +
                   "Pr\u00e1zdn\u00e9 pole odstran\u00ed ulo\u017een\u00fd kl\u00ed\u010d.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        var cancel = new Button
        {
            Content = "Zru\u0161it", Width = 90, Height = 32, IsCancel = true,
            Margin = new Thickness(0, 0, 10, 0)
        };
        var save = new Button { Content = "Ulo\u017eit", Width = 100, Height = 32, IsDefault = true };
        save.Click += (_, _) =>
        {
            try
            {
                KeyStore.Save(PanoramaProvider.Mapy, mapyKey.Password);
                KeyStore.Save(PanoramaProvider.Google, googleKey.Password);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Kl\u00ed\u010de se nepoda\u0159ilo ulo\u017eit: " + ex.Message,
                    "Panorama Viewer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);
        Content = panel;
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text, FontSize = 16, FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 10, 0, 8)
    };
}
