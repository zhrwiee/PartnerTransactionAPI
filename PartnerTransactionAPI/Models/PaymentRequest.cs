namespace PartnerTransactionAPI.Models
{
    public class PaymentRequest
    {
        public string PartnerRefNo { get; set; }   // contoh field tambahan
        public decimal TotalAmount { get; set; }   // jumlah keseluruhan
        public string Timestamp { get; set; }      // untuk expired validation
    }
}
