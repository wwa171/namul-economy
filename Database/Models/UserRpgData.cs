namespace TShockEconomyExp.Database.Models
{
    public class UserRpgData
    {
        public string AccountName { get; set; } = string.Empty;
        public string Job { get; set; } = "초보자";
        public int StatPoints { get; set; } = 0;

        // 🌟 4대 직업 데미지 특화 스탯
        public int Warrior { get; set; } = 0;   // 워리어 (근접 물리 피해)
        public int Ranger { get; set; } = 0;    // 레인저 (원거리 물리 피해)
        public int Sorcerer { get; set; } = 0;  // 소서러 (마법 피해 & 최대 마나)
        public int Summoner { get; set; } = 0;  // 서머너 (소환 피해)

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
