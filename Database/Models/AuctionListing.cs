namespace TShockEconomyExp.Database.Models
{
    public class AuctionListing
    {
        public int Id { get; set; }
        public string SellerAccount { get; set; } = string.Empty;
        public int ItemNetId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Stack { get; set; } = 1;
        public byte Prefix { get; set; } = 0;
        public long Price { get; set; }
        public DateTime ListedAt { get; set; } = DateTime.UtcNow;
    }
}
