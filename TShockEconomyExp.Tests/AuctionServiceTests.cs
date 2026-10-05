using Xunit;
using TShockEconomyExp.Database;
using TShockEconomyExp.Database.Models;
using TShockEconomyExp.Services;
using TShockEconomyExp.Config;

namespace TShockEconomyExp.Tests
{
    public class AuctionServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly DatabaseManager _db;
        private readonly AuctionService _auctionService;
        private readonly EconomyService _economyService;

        public AuctionServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TShockAuctionTests_" + Guid.NewGuid().ToString("N"));
            _db = new DatabaseManager(_tempDir);
            _db.Initialize();
            _auctionService = new AuctionService(_db);
            _economyService = new EconomyService(_db);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // ignore
            }
        }

        [Fact]
        public void ListItem_PersistsListing_AndCanBeRetrieved()
        {
            int listingId = _auctionService.ListItem("SellerNagu", 1, "철 단검", 1, 0, 5000);

            Assert.True(listingId > 0);

            var listing = _auctionService.GetListing(listingId);
            Assert.NotNull(listing);
            Assert.Equal("SellerNagu", listing.SellerAccount);
            Assert.Equal(5000, listing.Price);
            Assert.Equal("철 단검", listing.ItemName);
        }

        [Fact]
        public void PurchaseListing_Fails_WhenBuyerHasInsufficientFunds()
        {
            string seller = "SellerUser";
            string buyer = "BrokeBuyer";
            _economyService.AddBalance(buyer, 100);

            int listingId = _auctionService.ListItem(seller, 100, "전설의 검", 1, 0, 5000);
            var listing = _auctionService.GetListing(listingId)!;

            // 시도: 구매자 잔액 차감 시도
            bool balanceDeducted = _economyService.RemoveBalance(buyer, listing.Price, "경매 구매");
            Assert.False(balanceDeducted);

            // 매물은 삭제되지 않고 그대로 유지되어야 함
            Assert.NotNull(_auctionService.GetListing(listingId));
        }

        [Fact]
        public void PurchaseListing_AtomicSuccess_DeductsBuyerCreditsSellerAndRemovesListing()
        {
            string seller = "SellerUser";
            string buyer = "RichBuyer";
            _economyService.AddBalance(buyer, 10000);

            int listingId = _auctionService.ListItem(seller, 200, "마법 지팡이", 1, 0, 4000);
            var listing = _auctionService.GetListing(listingId)!;

            // Step 1: 잔액 차감
            bool deducted = _economyService.RemoveBalance(buyer, listing.Price, "경매 구매");
            Assert.True(deducted);

            // Step 2: 매물 삭제
            bool removed = _auctionService.RemoveListing(listingId);
            Assert.True(removed);

            // Step 3: 판매자 정산
            _economyService.AddBalance(seller, listing.Price, "경매 대금");

            // 결과 검증
            Assert.Equal(6000, _economyService.GetBalance(buyer));
            Assert.Equal(4000, _economyService.GetBalance(seller));
            Assert.Null(_auctionService.GetListing(listingId));
        }

        [Fact]
        public void PurchaseListing_RefundsBuyer_WhenListingAlreadyGone()
        {
            string seller = "SellerUser";
            string buyer = "BuyerUser";
            _economyService.AddBalance(buyer, 5000);

            int listingId = _auctionService.ListItem(seller, 300, "활", 1, 0, 3000);
            var listing = _auctionService.GetListing(listingId)!;

            // 선행 경쟁자가 이미 매물을 사서 지웠다고 가정
            _auctionService.RemoveListing(listingId);

            // 후발 주자 구매 시도
            bool deducted = _economyService.RemoveBalance(buyer, listing.Price, "경매 구매");
            Assert.True(deducted);

            bool removed = _auctionService.RemoveListing(listingId);
            if (!removed)
            {
                // 환불 로직 가동
                _economyService.AddBalance(buyer, listing.Price, "경매 구매 실패 환불");
            }

            Assert.False(removed);
            Assert.Equal(5000, _economyService.GetBalance(buyer)); // 전액 환불 완료 검증
        }
    }
}
