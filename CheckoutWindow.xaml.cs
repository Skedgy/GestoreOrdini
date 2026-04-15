using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace GestoreOrdini
{
    public partial class CheckoutWindow : Window
    {
        private readonly Utente? _utente;
        public ObservableCollection<CartItem> CheckoutItems { get; }
        public string TotalFormatted => CheckoutItems.Sum(i => i.LineTotal).ToString("C", CultureInfo.GetCultureInfo("it-IT"));

        public CheckoutWindow(Utente? utente, List<CartItem> cartItems)
        {
            InitializeComponent();
            _utente = utente;
            CheckoutItems = new ObservableCollection<CartItem>(cartItems.Select(c => new CartItem
            {
                ProductName = c.ProductName,
                Quantity = c.Quantity,
                UnitPrice = c.UnitPrice,
                PriceText = c.PriceText,
                PromotionKind = c.PromotionKind,
                PromotionText = c.PromotionText
            }));

            DataContext = this;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CardholderTextBox.Text) ||
                string.IsNullOrWhiteSpace(CardNumberTextBox.Text) ||
                string.IsNullOrWhiteSpace(ExpirationTextBox.Text) ||
                string.IsNullOrWhiteSpace(CvvTextBox.Text))
            {
                MessageBox.Show("Compila tutti i dettagli di pagamento.", "Checkout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var order = new OrderRecord
            {
                OrderNumber = DateTime.Now.Ticks,
                OrderId = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                CreatedAt = DateTime.Now,
                CustomerName = _utente?.Nome ?? string.Empty,
                CustomerSurname = _utente?.Cognome ?? string.Empty,
                CustomerBirthDate = _utente?.DataDiNascita ?? DateTime.MinValue,
                Location = _utente?.Posizione ?? string.Empty,
                Items = CheckoutItems.ToList(),
                TotalAmount = CheckoutItems.Sum(i => i.LineTotal),
                Payment = new PaymentDetails
                {
                    Cardholder = CardholderTextBox.Text.Trim(),
                    CardNumber = CardNumberTextBox.Text.Trim(),
                    Expiration = ExpirationTextBox.Text.Trim(),
                    Cvv = CvvTextBox.Text.Trim()
                }
            };

            OrderStorage.AppendOrder(order);
            MessageBox.Show("Pagamento completato. Ordine salvato.", "Checkout", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }
}
