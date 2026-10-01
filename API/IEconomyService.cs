using System;

namespace TShockEconomyExp.API
{
    public interface IEconomyService
    {
        /// <summary>
        /// 계정의 현재 잔액 조회
        /// </summary>
        long GetBalance(string accountName);

        /// <summary>
        /// 해당 금액 이상 소지하고 있는지 확인 (상점 구매 검증용)
        /// </summary>
        bool HasEnough(string accountName, long amount);

        /// <summary>
        /// 잔액 추가 (입금 / 보상 / 판매 대금)
        /// </summary>
        bool AddBalance(string accountName, long amount, string reason = "");

        /// <summary>
        /// 잔액 차감 (출금 / 구매 결제)
        /// </summary>
        bool RemoveBalance(string accountName, long amount, string reason = "");

        /// <summary>
        /// 계좌 간 송금
        /// </summary>
        bool Transfer(string fromAccount, string toAccount, long amount);

        /// <summary>
        /// 상점 전용 결제 처리 (충분한 잔액 검증 후 차감 및 이벤트 발행)
        /// </summary>
        bool ProcessPurchase(string accountName, long price, string itemName, int quantity = 1);

        /// <summary>
        /// 상점 전용 판매 대금 정산 처리
        /// </summary>
        bool ProcessSale(string accountName, long reward, string itemName, int quantity = 1);

        /// <summary>
        /// 잔액 변동 시 호출되는 이벤트
        /// </summary>
        event EventHandler<BalanceChangedEventArgs>? OnBalanceChanged;

        /// <summary>
        /// 상점 거래 발생 시 호출되는 이벤트
        /// </summary>
        event EventHandler<ShopTransactionEventArgs>? OnShopTransaction;
    }

    public class BalanceChangedEventArgs : EventArgs
    {
        public string AccountName { get; init; } = string.Empty;
        public long OldBalance { get; init; }
        public long NewBalance { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class ShopTransactionEventArgs : EventArgs
    {
        public string AccountName { get; init; } = string.Empty;
        public bool IsPurchase { get; init; } // true: 구매, false: 판매
        public string ItemName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public long TotalPrice { get; init; }
    }
}
