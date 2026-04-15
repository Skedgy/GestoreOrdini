namespace GestoreOrdini
{
    public enum PromotionKind
    {
        None,
        Offer,
        SeasonalDiscount
    }

    public sealed class CartItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string PriceText { get; set; } = string.Empty;
        public PromotionKind PromotionKind { get; set; }
        public string PromotionText { get; set; } = string.Empty;
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public sealed class PaymentDetails
    {
        public string Cardholder { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
    }

    public sealed class OrderRecord
    {
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
