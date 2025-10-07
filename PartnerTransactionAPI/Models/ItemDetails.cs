namespace PartnerTransactionAPI.Models
{
    public class ItemDetail
    {
        public string PartnerItemRef { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Qty { get; set; }
        public long UnitPrice { get; set; }
    }
}
