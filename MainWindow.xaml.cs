using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using XamlAnimatedGif;

namespace GestoreOrdini
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string AppStoreUrl = "https://mcdonalds.smart.link/e8od41ysl?site_id=download-app&creative_id=1col-publication";
        private const string GooglePlayUrl = "https://mcdonalds.smart.link/pfej9495o?site_id=download-app&creative_id=1col-publication";
        private const double OffersScrollStep = 1.5;
        private static readonly HttpClient LocationHttpClient = CreateLocationHttpClient();
        private Utente? _utenteSessioneCorrente;
        private readonly DispatcherTimer _offersAutoScrollTimer;
        private bool _isOffersAutoScrollPaused;
        private ScrollViewer? _offersScrollViewer;

        public MainWindow()
        {
            InitializeComponent();

            _offersAutoScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _offersAutoScrollTimer.Tick += OffersAutoScrollTimer_Tick;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AnimationBehavior.SetSourceUri(LoadingGifImage, new Uri("/Gifs/LoadingWB.gif", UriKind.Relative));
            PosizioneTextBlock.Text = await GetLocationFromIpAsync();
            ClearValidationState();
            UpdateRegisterButtonState();

            _offersScrollViewer = FindName("OffersScrollViewer") as ScrollViewer;
            if (_offersScrollViewer is not null)
            {
                Dispatcher.InvokeAsync(() => _offersAutoScrollTimer.Start(), DispatcherPriority.Background);
            }
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

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRegistrationInputs())
            {
                return;
            }

            _utenteSessioneCorrente = new Utente
            {
                Nome = NomeTextBox.Text.Trim(),
                Cognome = CognomeTextBox.Text.Trim(),
                DataDiNascita = DataNascitaDatePicker.SelectedDate!.Value,
                Posizione = PosizioneTextBlock.Text
            };

            RegisterButton.IsEnabled = false;

            await AnimateOpacityAsync(RegistrationPage, 1, 0, TimeSpan.FromMilliseconds(320));
            RegistrationPage.Visibility = Visibility.Collapsed;

            LoadingPage.Visibility = Visibility.Visible;
            LoadingProgressBar.Value = 0;

            var progressAnimation = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(1.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.HoldEnd
            };

            LoadingProgressBar.BeginAnimation(ProgressBar.ValueProperty, progressAnimation);
            await Task.Delay(progressAnimation.Duration.TimeSpan + TimeSpan.FromMilliseconds(150));

            var homeWindow = new HomeWindow(_utenteSessioneCorrente)
            {
                WindowState = WindowState
            };

            homeWindow.Show();
            Close();
        }

        private void InputField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SetControlValidState(textBox);
            }

            UpdateRegisterButtonState();
        }

        private void DataNascitaDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            SetControlValidState(DataNascitaDatePicker);
            UpdateRegisterButtonState();
        }

        private void OnlyLettersTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(c => !IsAllowedLetterChar(c));
        }

        private void OnlyLettersTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (pastedText.Any(c => !IsAllowedLetterChar(c)))
            {
                e.CancelCommand();
            }
        }

        private void OffersScrollViewer_MouseEnter(object sender, MouseEventArgs e)
        {
            _isOffersAutoScrollPaused = true;
        }

        private void OffersScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            _isOffersAutoScrollPaused = false;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

        private void OffersAutoScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (_isOffersAutoScrollPaused)
            {
                return;
            }

            if (_offersScrollViewer is null || _offersScrollViewer.ScrollableHeight <= 0)
            {
                return;
            }

            var nextOffset = _offersScrollViewer.VerticalOffset + OffersScrollStep;
            var restartThreshold = _offersScrollViewer.ScrollableHeight / 2;

            if (nextOffset >= restartThreshold)
            {
                nextOffset -= restartThreshold;
            }

            _offersScrollViewer.ScrollToVerticalOffset(nextOffset);
        }

        private static Task AnimateOpacityAsync(UIElement element, double from, double to, TimeSpan duration)
        {
            var tcs = new TaskCompletionSource<object?>();

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (_, _) => tcs.TrySetResult(null);
            element.BeginAnimation(UIElement.OpacityProperty, animation);

            return tcs.Task;
        }

        private bool ValidateRegistrationInputs()
        {
            var isNomeValid = IsValidName(NomeTextBox.Text);
            var isCognomeValid = IsValidName(CognomeTextBox.Text);
            var isDataValid = DataNascitaDatePicker.SelectedDate.HasValue;

            ApplyValidationState(NomeTextBox, isNomeValid);
            ApplyValidationState(CognomeTextBox, isCognomeValid);
            ApplyValidationState(DataNascitaDatePicker, isDataValid);

            if (isNomeValid && isCognomeValid && isDataValid)
            {
                return true;
            }

            if (!isNomeValid)
            {
                ShakeControl(NomeTextBox);
            }

            if (!isCognomeValid)
            {
                ShakeControl(CognomeTextBox);
            }

            if (!isDataValid)
            {
                ShakeControl(DataNascitaDatePicker);
            }

            return false;
        }

        private void UpdateRegisterButtonState()
        {
            RegisterButton.IsEnabled =
                !string.IsNullOrWhiteSpace(NomeTextBox.Text) &&
                !string.IsNullOrWhiteSpace(CognomeTextBox.Text) &&
                DataNascitaDatePicker.SelectedDate.HasValue;
        }

        private static bool IsAllowedLetterChar(char c)
        {
            return char.IsLetter(c) || c == ' ' || c == '\'';
        }

        private static bool IsValidName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim();
            return normalized.All(IsAllowedLetterChar) && normalized.Any(char.IsLetter);
        }

        private void ClearValidationState()
        {
            SetControlValidState(NomeTextBox);
            SetControlValidState(CognomeTextBox);
            SetControlValidState(DataNascitaDatePicker);
        }

        private void ApplyValidationState(Control control, bool isValid)
        {
            if (isValid)
            {
                SetControlValidState(control);
                return;
            }

            control.BorderBrush = Brushes.Red;
            control.Foreground = Brushes.Red;

            if (control is DatePicker datePicker)
            {
                if (datePicker.Template.FindName("PART_TextBox", datePicker) is DatePickerTextBox textBox)
                {
                    textBox.Foreground = Brushes.Red;
                    textBox.BorderBrush = Brushes.Red;
                }
            }
        }

        private void SetControlValidState(Control control)
        {
            control.BorderBrush = Brushes.Black;
            control.Foreground = Brushes.Black;

            if (control is DatePicker datePicker)
            {
                if (datePicker.Template.FindName("PART_TextBox", datePicker) is DatePickerTextBox textBox)
                {
                    textBox.Foreground = Brushes.Black;
                    textBox.BorderBrush = Brushes.Transparent;
                }
            }
        }

        private static void ShakeControl(UIElement element)
        {
            element.RenderTransform = new TranslateTransform();

            var shakeAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(360)
            };

            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromPercent(0.15)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(8, KeyTime.FromPercent(0.3)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromPercent(0.45)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(6, KeyTime.FromPercent(0.6)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-3, KeyTime.FromPercent(0.75)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(3, KeyTime.FromPercent(0.9)));
            shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));

            (element.RenderTransform as TranslateTransform)?.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
        }

        private static async Task<string> GetLocationFromIpAsync()
        {
            var providers = new[]
            {
                (Url: "https://ipapi.co/json/", Parser: (Func<JsonElement, string?>)ParseIpApiCoLocation),
                (Url: "https://ipwho.is/", Parser: (Func<JsonElement, string?>)ParseIpWhoIsLocation)
            };

            foreach (var provider in providers)
            {
                try
                {
                    using var response = await LocationHttpClient.GetAsync(provider.Url);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    var location = provider.Parser(doc.RootElement);
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        return location;
                    }
                }
                catch
                {
                    // Prova provider successivo.
                }
            }

            return "Posizione non disponibile";
        }

        private static string? ParseIpApiCoLocation(JsonElement root)
        {
            var city = ReadString(root, "city");
            var region = ReadString(root, "region");
            var country = ReadString(root, "country_name");

            return BuildLocation(city, region, country);
        }

        private static string? ParseIpWhoIsLocation(JsonElement root)
        {
            if (root.TryGetProperty("success", out var successProp) &&
                successProp.ValueKind == JsonValueKind.False)
            {
                return null;
            }

            var city = ReadString(root, "city");
            var region = ReadString(root, "region");
            var country = ReadString(root, "country");

            return BuildLocation(city, region, country);
        }

        private static string? ReadString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var prop)
                ? prop.GetString()
                : null;
        }

        private static string? BuildLocation(params string?[] values)
        {
            var parts = values
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (parts.Length == 0)
            {
                return null;
            }

            return string.Join(", ", parts);
        }

        private static HttpClient CreateLocationHttpClient()
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GestoreOrdini/1.0 (+WPF)");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            return httpClient;
        }
    }
}