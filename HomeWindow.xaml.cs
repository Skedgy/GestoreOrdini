using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Media;

namespace GestoreOrdini
{
    // Finestra principale dopo la registrazione.
    public partial class HomeWindow : Window
    {
        // Utente loggato nella sessione corrente.
        private readonly Utente? _utente;

        // Link rapidi ai marketplace dell'app.
        private const string AppStoreUrl = "https://mcdonalds.smart.link/e8od41ysl?site_id=download-app&creative_id=1col-publication";
        private const string GooglePlayUrl = "https://mcdonalds.smart.link/pfej9495o?site_id=download-app&creative_id=1col-publication";

        // Estensioni file immagini supportate per i menu.
        private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".bmp"];

        // Prezzo base per categoria, poi incrementato in base all'indice del prodotto.
        private static readonly Dictionary<string, decimal> CategoryBasePrices = new(StringComparer.OrdinalIgnoreCase)
        {
            ["McBacon"] = 9.90m,
            ["McWrap"] = 8.90m,
            ["McChicken"] = 8.40m,
            ["Insalate"] = 7.90m,
            ["Altri"] = 6.90m
        };
        // Pennelli usati per differenziare visivamente prezzi/promozioni.
        private static readonly Brush StandardPriceBrush = new SolidColorBrush(Color.FromRgb(176, 0, 32));
        private static readonly Brush SeasonalDiscountBrush = new SolidColorBrush(Color.FromRgb(0, 137, 209));
        private static readonly Brush OfferGoldBrush = new LinearGradientBrush(
            Color.FromRgb(140, 110, 15),
            Color.FromRgb(247, 204, 92),
            new Point(0, 0),
            new Point(1, 0));

        // Dizionari del carrello: quantità e dettaglio prodotto per titolo.
        private readonly Dictionary<string, int> _cartQuantities = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CartItem> _cartProducts = new(StringComparer.OrdinalIgnoreCase);

        // Collezione card menu visualizzate nella UI.
        public ObservableCollection<MenuCardViewModel> MenuCards { get; } = [];

        // Costruttore base: inizializza finestra e aggancia eventi.
        public HomeWindow()
        {
            InitializeComponent();

            if (FindName("LogoutButton") is System.Windows.Controls.Button logoutButton)
            {
                logoutButton.Click += LogoutButton_Click;
            }

            if (FindName("CreditsTopButton") is System.Windows.Controls.Button creditsTopButton)
            {
                creditsTopButton.Click += CreditsButton_Click;
            }

            if (FindName("OrdersReportTopButton") is System.Windows.Controls.Button ordersReportTopButton)
            {
                ordersReportTopButton.Click += OrdersReportTopButton_Click;
            }

            if (FindName("CartButton") is System.Windows.Controls.Button cartButton)
            {
                cartButton.Click += CartButton_Click;
            }

            if (FindName("CreditsCloseButton") is System.Windows.Controls.Button creditsCloseButton)
            {
                creditsCloseButton.Click += CreditsButton_Click;
            }

            if (FindName("MenuContentGrid") is System.Windows.Controls.Grid contentGrid)
            {
                contentGrid.Visibility = Visibility.Collapsed;
            }

            if (FindName("MenuCategoryTitleText") is System.Windows.Controls.TextBlock titleText)
            {
                titleText.Visibility = Visibility.Collapsed;
            }
        }

        // Aggiunge/aggiorna il prodotto selezionato nel carrello.
        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not MenuCardViewModel menuCard)
            {
                return;
            }

            var quantity = menuCard.SelectedQuantity <= 0 ? 1 : menuCard.SelectedQuantity;
            _cartQuantities[menuCard.Title] = quantity;
            _cartProducts[menuCard.Title] = new CartItem
            {
                ProductName = menuCard.Title,
                Quantity = quantity,
                UnitPrice = menuCard.UnitPrice,
                PriceText = menuCard.Price,
                PromotionKind = menuCard.PromotionKind,
                PromotionText = menuCard.PromotionText
            };

            menuCard.IsAdded = true;

            UpdateCartCountBadge();
        }

        // Diminuisce la quantità (minimo 1).
        private void QuantityMinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button { Tag: MenuCardViewModel menuCard })
            {
                return;
            }

            if (menuCard.SelectedQuantity > 1)
            {
                menuCard.SelectedQuantity--;
            }
        }

        // Aumenta la quantità (massimo 10).
        private void QuantityPlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button { Tag: MenuCardViewModel menuCard })
            {
                return;
            }

            if (menuCard.SelectedQuantity < 10)
            {
                menuCard.SelectedQuantity++;
            }
        }

        // Mostra/nasconde il pannello crediti/disclaimer.
        private void CreditsButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("CreditsDisclaimerPanel") is not System.Windows.Controls.Border panel)
            {
                return;
            }

            panel.Visibility = panel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // Apre il checkout con gli articoli nel carrello.
        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cartProducts.Count == 0)
            {
                MessageBox.Show("Il carrello è vuoto.", "Checkout", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var checkoutWindow = new CheckoutWindow(_utente, _cartProducts.Values.ToList())
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var completed = checkoutWindow.ShowDialog();
            if (completed != true)
            {
                return;
            }

            _cartQuantities.Clear();
            _cartProducts.Clear();
            foreach (var menu in MenuCards)
            {
                menu.IsAdded = false;
                menu.SelectedQuantity = 1;
            }

            UpdateCartCountBadge();
        }

        // Apre la finestra report ordini.
        private void OrdersReportTopButton_Click(object sender, RoutedEventArgs e)
        {
            var reportWindow = new OrdersReportWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            reportWindow.ShowDialog();
        }

        // Costruttore con utente: popola i dati anagrafici in alto.
        public HomeWindow(Utente? utente)
            : this()
        {
            _utente = utente;

            if (_utente is null)
            {
                return;
            }

            if (FindName("UtenteNomeCognomeText") is System.Windows.Controls.TextBlock nomeCognomeText)
            {
                nomeCognomeText.Text = $"{_utente.Nome} {_utente.Cognome}".Trim();
            }

            if (FindName("UtentePosizioneText") is System.Windows.Controls.TextBlock posizioneText)
            {
                posizioneText.Text = string.IsNullOrWhiteSpace(_utente.Posizione)
                    ? "Posizione non disponibile"
                    : _utente.Posizione;
            }

            if (FindName("WelcomeMessageText") is System.Windows.Controls.TextBlock welcomeText)
            {
                var nome = string.IsNullOrWhiteSpace(_utente.Nome) ? "" : _utente.Nome.Trim();
                welcomeText.Text = $"Benvenuto {nome}!\nPer iniziare a navigare i nostri menù apri il menù a tendina!";
            }
        }

        // Esegue il logout tornando alla schermata registrazione.
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new MainWindow
            {
                WindowState = WindowState
            };

            Application.Current.MainWindow = registrationWindow;
            registrationWindow.Show();
            Close();
        }

        // Apre il link App Store.
        private void AppStoreButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(AppStoreUrl);
        }

        // Apre il link Google Play.
        private void GooglePlayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(GooglePlayUrl);
        }

        // Apre un social in base all'URL nel tag del bottone.
        private void SocialButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button)
            {
                return;
            }

            if (button.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                OpenUrl(url);
            }
        }

        // Apre un URL nel browser predefinito.
        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

        // Carica i prodotti della categoria selezionata.
        private void MenuCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not string category)
            {
                return;
            }

            LoadMenuCategory(category);

            if (FindName("MenusDropDownButton") is System.Windows.Controls.Primitives.ToggleButton dropDownToggle)
            {
                dropDownToggle.IsChecked = false;
            }
        }

        // Costruisce le card della categoria e aggiorna il layout.
        private void LoadMenuCategory(string category)
        {
            if (FindName("WelcomeMessageText") is System.Windows.Controls.TextBlock welcomeText)
            {
                welcomeText.Visibility = Visibility.Collapsed;
            }

            if (FindName("MenuContentGrid") is System.Windows.Controls.Grid contentGrid)
            {
                contentGrid.Visibility = Visibility.Visible;
            }

            if (FindName("MenuCategoryTitleText") is System.Windows.Controls.TextBlock titleText)
            {
                titleText.Text = $"Menù {category}";
                titleText.Visibility = Visibility.Visible;
            }

            MenuCards.Clear();

            var files = GetCategoryImageFiles(category);
            if (files.Count == 0)
            {
                if (FindName("NoMenusText") is System.Windows.Controls.TextBlock noMenusText)
                {
                    noMenusText.Visibility = Visibility.Visible;
                }

                return;
            }

            if (FindName("NoMenusText") is System.Windows.Controls.TextBlock noMenusTextVisible)
            {
                noMenusTextVisible.Visibility = Visibility.Collapsed;
            }

            var basePrice = CategoryBasePrices.TryGetValue(category, out var resolvedBasePrice)
                ? resolvedBasePrice
                : 7.90m;

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var title = Path.GetFileNameWithoutExtension(file)
                    .Replace("_", " ")
                    .Replace("-", " ");

                var price = basePrice + (i * 0.50m);
                var priceInfo = BuildPriceInfo(title, price);

                MenuCards.Add(new MenuCardViewModel
                {
                    Title = title,
                    Price = priceInfo.Text,
                    PriceBrush = priceInfo.Brush,
                    UnitPrice = priceInfo.UnitPrice,
                    PromotionKind = priceInfo.PromotionKind,
                    PromotionText = priceInfo.PromotionText,
                    ImagePath = new Uri(file),
                    SelectedQuantity = _cartQuantities.TryGetValue(title, out var qty) ? qty : 1,
                    IsAdded = _cartQuantities.ContainsKey(title)
                });
            }

            UpdateCartCountBadge();
        }

        // Aggiorna il badge con il totale quantità nel carrello.
        private void UpdateCartCountBadge()
        {
            if (FindName("CartCountText") is not System.Windows.Controls.TextBlock countText)
            {
                return;
            }

            var totalItems = _cartQuantities.Values.Sum();
            countText.Text = totalItems.ToString(CultureInfo.InvariantCulture);
        }

        // Applica regole prezzo/promozione in base al nome prodotto.
        private static PriceInfo BuildPriceInfo(string title, decimal defaultPrice)
        {
            var normalizedTitle = NormalizeTitle(title);

            static string FormatEuro(decimal value)
            {
                return value.ToString("0.##", CultureInfo.GetCultureInfo("it-IT")) + "€";
            }

            if (normalizedTitle.Contains("big mac", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(10m)}", 10m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("happy meal", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(11.50m)}", 11.50m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("gran crispy mcbacon") &&
                (normalizedTitle.Contains("bacon jam") || normalizedTitle.Contains("sweet onion")))
                return new PriceInfo($"DA {FormatEuro(9.50m)}", 9.50m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("gran crispy mcbacon", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(9.50m)}", 9.50m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("crispy mcbacon", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(8m)}", 8m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("crispy chicken mcwrap", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(8m)}", 8m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("crispy mcwrap", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(5m)} - OFFERTA!", 5m, OfferGoldBrush, PromotionKind.Offer, "OFFERTA!");
            if (normalizedTitle.Contains("mcwrap") && normalizedTitle.Contains("parmigiano"))
                return new PriceInfo($"DA {FormatEuro(8.30m)}", 8.30m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("double chicken bbq", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(9m)}", 9m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("mcchicken", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(8m)}", 8m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("mccrunchy chicken", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(8.50m)}", 8.50m, StandardPriceBrush, PromotionKind.None, string.Empty);
            if (normalizedTitle.Contains("double cheeseburger", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(6m)} - OFFERTA!", 6m, OfferGoldBrush, PromotionKind.Offer, "OFFERTA!");
            if (normalizedTitle.Contains("filet") && normalizedTitle.Contains("fish"))
                return new PriceInfo($"DA {FormatEuro(5m)} - OFFERTA!", 5m, OfferGoldBrush, PromotionKind.Offer, "OFFERTA!");
            if (normalizedTitle.Contains("fiordilatte", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(3m)} - SCONTO STAGIONE", 3m, SeasonalDiscountBrush, PromotionKind.SeasonalDiscount, "SCONTO STAGIONE");
            if (normalizedTitle.Contains("mccafe", StringComparison.OrdinalIgnoreCase) || normalizedTitle.Contains("mc café", StringComparison.OrdinalIgnoreCase))
                return new PriceInfo($"DA {FormatEuro(2m)}", 2m, StandardPriceBrush, PromotionKind.None, string.Empty);

            return new PriceInfo($"Da {defaultPrice.ToString("C", CultureInfo.GetCultureInfo("it-IT"))}", defaultPrice, StandardPriceBrush, PromotionKind.None, string.Empty);
        }

        // Normalizza il titolo (minuscolo, senza accenti) per confronti robusti.
        private static string NormalizeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            var normalized = title.Normalize(NormalizationForm.FormD);
            var filtered = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(filtered.ToArray()).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        // Cerca le immagini categoria in varie cartelle candidate.
        private static List<string> GetCategoryImageFiles(string category)
        {
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = ResolveProjectRoot(baseDir);
            var candidateFolders = new[]
            {
                Path.Combine(baseDir, category),
                Path.Combine(baseDir, "..", "..", "..", category),
                projectRoot is null ? string.Empty : Path.Combine(projectRoot, category)
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in candidateFolders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                var files = Directory
                    .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => SupportedImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(Path.GetFileName)
                    .ToList();

                if (files.Count > 0)
                {
                    return files;
                }
            }

            return [];
        }

        // Risale le directory per trovare la root progetto con il `.csproj`.
        private static string? ResolveProjectRoot(string startPath)
        {
            var current = new DirectoryInfo(startPath);
            for (var i = 0; i < 10 && current is not null; i++)
            {
                var csprojPath = Path.Combine(current.FullName, "GestoreOrdini.csproj");
                if (File.Exists(csprojPath))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }

        // Struttura interna: dati prezzo + promozione usati nella costruzione card.
        private readonly record struct PriceInfo(string Text, decimal UnitPrice, Brush Brush, PromotionKind PromotionKind, string PromotionText);

        // ViewModel di una singola card menu mostrata in home.
        public sealed class MenuCardViewModel : INotifyPropertyChanged
        {
            private int _selectedQuantity = 1;
            private bool _isAdded;

            public string Title { get; init; } = string.Empty;
            public string Price { get; init; } = string.Empty;
            public Brush PriceBrush { get; init; } = StandardPriceBrush;
            public decimal UnitPrice { get; init; }
            public PromotionKind PromotionKind { get; init; } = PromotionKind.None;
            public string PromotionText { get; init; } = string.Empty;
            public Uri? ImagePath { get; init; }
            public int[] Quantities { get; } = Enumerable.Range(1, 10).ToArray();

            // Quantità selezionata per la card corrente.
            public int SelectedQuantity
            {
                get => _selectedQuantity;
                set
                {
                    if (_selectedQuantity == value)
                    {
                        return;
                    }

                    _selectedQuantity = value;
                    OnPropertyChanged();
                }
            }

            // Indica se la card è già stata aggiunta al carrello.
            public bool IsAdded
            {
                get => _isAdded;
                set
                {
                    if (_isAdded == value)
                    {
                        return;
                    }

                    _isAdded = value;
                    OnPropertyChanged();
                }
            }

            // Evento standard WPF per notificare aggiornamenti proprietà al binding.
            public event PropertyChangedEventHandler? PropertyChanged;

            // Notifica alla UI il cambiamento della proprietà indicata.
            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
