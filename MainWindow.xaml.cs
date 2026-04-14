using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestoreOrdini
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string AppStoreUrl = "https://mcdonalds.smart.link/e8od41ysl?site_id=download-app&creative_id=1col-publication";
        private const string GooglePlayUrl = "https://mcdonalds.smart.link/pfej9495o?site_id=download-app&creative_id=1col-publication";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PosizioneTextBlock.Text = await GetLocationFromIpAsync();
        }

        private void AppStoreButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(AppStoreUrl);
        }

        private void GooglePlayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(GooglePlayUrl);
        }

        private void TermsLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

        private static async Task<string> GetLocationFromIpAsync()
        {
            try
            {
                using var http = new HttpClient();
                using var response = await http.GetAsync("https://ipapi.co/json/");

                if (!response.IsSuccessStatusCode)
                {
                    return "Posizione non disponibile";
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                string city = doc.RootElement.TryGetProperty("city", out var cityProp)
                    ? cityProp.GetString() ?? string.Empty
                    : string.Empty;
                string region = doc.RootElement.TryGetProperty("region", out var regionProp)
                    ? regionProp.GetString() ?? string.Empty
                    : string.Empty;
                string country = doc.RootElement.TryGetProperty("country_name", out var countryProp)
                    ? countryProp.GetString() ?? string.Empty
                    : string.Empty;

                var parts = new[] { city, region, country }
                    .Where(part => !string.IsNullOrWhiteSpace(part));

                var location = string.Join(", ", parts);
                return string.IsNullOrWhiteSpace(location) ? "Posizione non disponibile" : location;
            }
            catch
            {
                return "Posizione non disponibile";
            }
        }
    }
}