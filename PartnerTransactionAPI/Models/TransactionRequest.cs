using System.Collections.Generic;

namespace PartnerTransactionAPI.Models
{
    public class TransactionRequest
    {
        public string PartnerKey { get; set; } = string.Empty;
        public string PartnerRefNo { get; set; } = string.Empty;
        public string PartnerPassword { get; set; } = string.Empty;
        public long TotalAmount { get; set; }
        public List<ItemDetail>? Items { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Sig { get; set; } = string.Empty;
    }
}
