namespace TShockEconomyExp.Database.Models
{
    public class UserRpgData
    {
        public string AccountName { get; set; } = string.Empty;
        public string Job { get; set; } = "초보자";
        public int StatPoints { get; set; } = 0;
        public int Strength { get; set; } = 0;
        public int Dexterity { get; set; } = 0;
        public int Intelligence { get; set; } = 0;
        public int Vitality { get; set; } = 0;

        // 퀘스트 상태
        public string ActiveQuestTarget { get; set; } = string.Empty;
        public int ActiveQuestTargetNetId { get; set; } = 0;
        public int QuestRequiredCount { get; set; } = 0;
        public int QuestCurrentCount { get; set; } = 0;
        public long QuestRewardExp { get; set; } = 0;
        public long QuestRewardMoney { get; set; } = 0;
        public string LastQuestDate { get; set; } = string.Empty;

        // 칭호 & 업적 통계
        public string EquippedTitleId { get; set; } = "novice"; // 현재 장착 중인 칭호 ID
        public string UnlockedTitlesJson { get; set; } = "[\"novice\"]"; // 해금된 칭호 ID 목록 (JSON Array)
        public long TotalMonsterKills { get; set; } = 0;
        public long TotalBossKills { get; set; } = 0;
        public long TotalMiningCount { get; set; } = 0;
        public long TotalFishingCount { get; set; } = 0;

        public DateTime LastUpdated { get; set; }
    }
}
