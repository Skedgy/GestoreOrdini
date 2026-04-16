using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace GestoreOrdini
{
    // Finestra report con elenco ordini salvati.
    public partial class OrdersReportWindow : Window
    {
        // Sorgente dati della griglia report.
        public ObservableCollection<OrderReportRow> Orders { get; }

        // Carica gli ordini e costruisce le righe del report.
        public OrdersReportWindow()
        {
            InitializeComponent();

            var rows = OrderStorage
                .GetOrders()
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderReportRow
                {
                    OrderNumber = o.OrderNumber,
                    OrderId = o.OrderId,
                    CreatedAt = o.CreatedAt,
                    CustomerFullName = $"{o.CustomerName} {o.CustomerSurname}".Trim(),
                    CustomerBirthDate = o.CustomerBirthDate,
                    Location = o.Location,
                    TotalAmount = o.TotalAmount,
                    PaymentSummary = $"{o.Payment.Cardholder} | {o.Payment.CardNumber} | {o.Payment.Expiration} | CVV {o.Payment.Cvv}",
                    ItemsSummary = string.Join("; ", o.Items.Select(i => $"{i.ProductName} x{i.Quantity}"))
                });

            Orders = new ObservableCollection<OrderReportRow>(rows);
            DataContext = this;
        }

        // Chiude la finestra report.
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Riga sintetica usata nella tabella del report ordini.
        public sealed class OrderReportRow
        {
            public long OrderNumber { get; init; }
            public string OrderId { get; init; } = string.Empty;
            public DateTime CreatedAt { get; init; }
            public string CustomerFullName { get; init; } = string.Empty;
            public DateTime CustomerBirthDate { get; init; }
            public string Location { get; init; } = string.Empty;
            public decimal TotalAmount { get; init; }
            public string PaymentSummary { get; init; } = string.Empty;
            public string ItemsSummary { get; init; } = string.Empty;
        }
    }
}
