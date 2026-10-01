namespace TShockEconomyExp.Database.Models
{
    public class UserRpgData
    {
        public string AccountName { get; set; } = string.Empty;
        public string Job { get; set; } = "초보자"; // 초보자, 전사, 궁수, 마법사, 소환사
        public int StatPoints { get; set; } = 0;
        public int Strength { get; set; } = 0;   // 근력: 근접 데미지 강화
        public int Dexterity { get; set; } = 0;  // 민첩: 원거리 데미지 및 이동 속도
        public int Intelligence { get; set; } = 0; // 지능: 마법/소환 데미지 및 마나
        public int Vitality { get; set; } = 0;   // 생명력: 최대 체력 보너스
        
        // 퀘스트 상태
        public string ActiveQuestTarget { get; set; } = string.Empty; // 목표 몬스터 이름
        public int ActiveQuestTargetNetId { get; set; } = 0;
        public int QuestRequiredCount { get; set; } = 0;
        public int QuestCurrentCount { get; set; } = 0;
        public long QuestRewardExp { get; set; } = 0;
        public long QuestRewardMoney { get; set; } = 0;
        public string LastQuestDate { get; set; } = string.Empty; // 일일 퀘스트 체크용
        
        public DateTime LastUpdated { get; set; }
    }
}
