namespace Coffee.ViewModel
{
    public class OrderDetailViewModel
    {
        public int OrderId { get; set; }

        public DateTime OrderDate { get; set; }

        public string ReceiverName { get; set; } = string.Empty;

        public string ReceiverPhone { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public List<OrderDetailItemViewModel> Items { get; set; } = new();
    }
}
