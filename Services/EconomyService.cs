using TShockEconomyExp.API;
using TShockEconomyExp.Database;

namespace TShockEconomyExp.Services
{
    public class EconomyService : IEconomyService
    {
        private readonly DatabaseManager _db;
        private readonly object _lock = new();

        public event EventHandler<BalanceChangedEventArgs>? OnBalanceChanged;
        public event EventHandler<ShopTransactionEventArgs>? OnShopTransaction;

        public EconomyService(DatabaseManager db)
        {
            _db = db;
        }

        public long GetBalance(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return 0;
            return _db.GetEconomy(accountName).Balance;
        }

        public bool HasEnough(string accountName, long amount)
        {
            if (amount <= 0) return true;
            return GetBalance(accountName) >= amount;
        }

        public bool AddBalance(string accountName, long amount, string reason = "")
        {
            if (string.IsNullOrWhiteSpace(accountName) || amount <= 0) return false;

            lock (_lock)
            {
                var data = _db.GetEconomy(accountName);
                long oldBal = data.Balance;
                data.Balance += amount;
                _db.SaveEconomy(data);

                OnBalanceChanged?.Invoke(this, new BalanceChangedEventArgs
                {
                    AccountName = accountName,
                    OldBalance = oldBal,
                    NewBalance = data.Balance,
                    Reason = reason
                });
                return true;
            }
        }

        public bool RemoveBalance(string accountName, long amount, string reason = "")
        {
            if (string.IsNullOrWhiteSpace(accountName) || amount <= 0) return false;

            lock (_lock)
            {
                var data = _db.GetEconomy(accountName);
                if (data.Balance < amount) return false;

                long oldBal = data.Balance;
                data.Balance -= amount;
                _db.SaveEconomy(data);

                OnBalanceChanged?.Invoke(this, new BalanceChangedEventArgs
                {
                    AccountName = accountName,
                    OldBalance = oldBal,
                    NewBalance = data.Balance,
                    Reason = reason
                });
                return true;
            }
        }

        public bool Transfer(string fromAccount, string toAccount, long amount)
        {
            if (string.IsNullOrWhiteSpace(fromAccount) || string.IsNullOrWhiteSpace(toAccount) || amount <= 0)
                return false;

            if (fromAccount.Equals(toAccount, StringComparison.OrdinalIgnoreCase))
                return false;

            lock (_lock)
            {
                var fromData = _db.GetEconomy(fromAccount);
                if (fromData.Balance < amount) return false;

                var toData = _db.GetEconomy(toAccount);

                long fromOld = fromData.Balance;
                fromData.Balance -= amount;
                _db.SaveEconomy(fromData);

                long toOld = toData.Balance;
                toData.Balance += amount;
                _db.SaveEconomy(toData);

                OnBalanceChanged?.Invoke(this, new BalanceChangedEventArgs
                {
                    AccountName = fromAccount,
                    OldBalance = fromOld,
                    NewBalance = fromData.Balance,
                    Reason = $"송금 -> {toAccount}"
                });

                OnBalanceChanged?.Invoke(this, new BalanceChangedEventArgs
                {
                    AccountName = toAccount,
                    OldBalance = toOld,
                    NewBalance = toData.Balance,
                    Reason = $"입금 <- {fromAccount}"
                });

                return true;
            }
        }

        public bool ProcessPurchase(string accountName, long price, string itemName, int quantity = 1)
        {
            if (!RemoveBalance(accountName, price, $"상점 구매: {itemName} x{quantity}"))
                return false;

            OnShopTransaction?.Invoke(this, new ShopTransactionEventArgs
            {
                AccountName = accountName,
                IsPurchase = true,
                ItemName = itemName,
                Quantity = quantity,
                TotalPrice = price
            });

            return true;
        }

        public bool ProcessSale(string accountName, long reward, string itemName, int quantity = 1)
        {
            if (!AddBalance(accountName, reward, $"상점 판매: {itemName} x{quantity}"))
                return false;

            OnShopTransaction?.Invoke(this, new ShopTransactionEventArgs
            {
                AccountName = accountName,
                IsPurchase = false,
                ItemName = itemName,
                Quantity = quantity,
                TotalPrice = reward
            });

            return true;
        }
    }
}
