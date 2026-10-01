using TShockEconomyExp.Database;
using TShockEconomyExp.Database.Models;

namespace TShockEconomyExp.Services
{
    public class AuctionService
    {
        private readonly DatabaseManager _db;

        public AuctionService(DatabaseManager db)
        {
            _db = db;
        }

        public int ListItem(string sellerAccount, int itemNetId, string itemName, int stack, byte prefix, long price)
        {
            var listing = new AuctionListing
            {
                SellerAccount = sellerAccount,
                ItemNetId = itemNetId,
                ItemName = itemName,
                Stack = stack,
                Prefix = prefix,
                Price = price,
                ListedAt = DateTime.UtcNow
            };
            return _db.AddAuctionListing(listing);
        }

        public List<AuctionListing> GetListings(int page = 1, int pageSize = 10)
        {
            return _db.GetAuctionListings(page, pageSize);
        }

        public AuctionListing? GetListing(int id)
        {
            return _db.GetAuctionListing(id);
        }

        public bool RemoveListing(int id)
        {
            return _db.DeleteAuctionListing(id);
        }
    }
}
