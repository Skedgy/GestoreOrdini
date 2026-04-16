namespace GestoreOrdini
{
    // Tipologia promozione associata al prodotto.
    public enum PromotionKind
    {
        None, // Nessuna promozione.
        Offer, // Prezzo in offerta.
        SeasonalDiscount // Sconto legato alla stagione.
    }

    // Singola riga prodotto nel carrello/ordine.
    public sealed class CartItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string PriceText { get; set; } = string.Empty;
        public PromotionKind PromotionKind { get; set; }
        public string PromotionText { get; set; } = string.Empty;

        // Totale riga = prezzo unitario × quantità.
        public decimal LineTotal => UnitPrice * Quantity;
    }

    // Dati carta inseriti nel checkout.
    public sealed class PaymentDetails
    {
        public string Cardholder { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modello completo di un ordine salvato su file.
    /// </summary>
    public sealed class OrderRecord
    {
        // Numero progressivo basato sui tick temporali.
        public long OrderNumber { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerSurname { get; set; } = string.Empty;
        public DateTime CustomerBirthDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<CartItem> Items { get; set; } = [];
        public decimal TotalAmount { get; set; }
        public PaymentDetails Payment { get; set; } = new();
    }
}
