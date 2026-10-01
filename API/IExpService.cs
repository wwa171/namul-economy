using System;

namespace TShockEconomyExp.API
{
    public interface IExpService
    {
        /// <summary>
        /// 계정의 누적 경험치 조회
        /// </summary>
        long GetTotalExp(string accountName);

        /// <summary>
        /// 계정의 현재 레벨 조회
        /// </summary>
        int GetLevel(string accountName);

        /// <summary>
        /// 현재 레벨에서 다음 레벨로 가기 위한 필요 경험치
        /// </summary>
        long GetExpToNextLevel(int currentLevel);

        /// <summary>
        /// 현재 레벨 기준 진행 중인 경험치
        /// </summary>
        long GetCurrentLevelExpProgress(string accountName);

        /// <summary>
        /// 경험치 추가 지급 (레벨업 발생 시 자동 처리 및 이벤트 발생)
        /// </summary>
        void AddExp(string accountName, long amount, string reason = "");

        /// <summary>
        /// 레벨 직접 설정 (관리자용)
        /// </summary>
        void SetLevel(string accountName, int level);

        /// <summary>
        /// 상위 랭킹 조회 (limit 만큼)
        /// </summary>
        IEnumerable<(string AccountName, int Level, long TotalExp)> GetTopRankings(int limit = 10);

        /// <summary>
        /// 레벨업 시 호출되는 이벤트
        /// </summary>
        event EventHandler<LevelUpEventArgs>? OnLevelUp;

        /// <summary>
        /// 경험치 획득 시 호출되는 이벤트
        /// </summary>
        event EventHandler<ExpGainEventArgs>? OnExpGained;
    }

    public class LevelUpEventArgs : EventArgs
    {
        public string AccountName { get; init; } = string.Empty;
        public int OldLevel { get; init; }
        public int NewLevel { get; init; }
    }

    public class ExpGainEventArgs : EventArgs
    {
        public string AccountName { get; init; } = string.Empty;
        public long GainedExp { get; init; }
        public long NewTotalExp { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}
