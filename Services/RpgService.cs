using TShockEconomyExp.Database;
using TShockEconomyExp.Database.Models;

namespace TShockEconomyExp.Services
{
    public class RpgService
    {
        private readonly DatabaseManager _db;
        private readonly object _lock = new();

        public static readonly string[] AvailableJobs = { "초보자", "전사", "궁수", "마법사", "소환사" };

        public RpgService(DatabaseManager db)
        {
            _db = db;
        }

        public UserRpgData GetRpgData(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return new UserRpgData();
            return _db.GetRpg(accountName);
        }

        public bool ChangeJob(string accountName, string newJob)
        {
            if (!AvailableJobs.Contains(newJob)) return false;

            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                data.Job = newJob;
                _db.SaveRpg(data);
                return true;
            }
        }

        public void AddStatPoints(string accountName, int points)
        {
            if (points <= 0) return;
            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                data.StatPoints += points;
                _db.SaveRpg(data);
            }
        }

        public bool AllocateStat(string accountName, string statName, int amount)
        {
            if (amount <= 0) return false;

            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                if (data.StatPoints < amount) return false;

                switch (statName.ToLower())
                {
                    case "str":
                    case "힘":
                    case "근력":
                        data.Strength += amount;
                        break;
                    case "dex":
                    case "민첩":
                        data.Dexterity += amount;
                        break;
                    case "int":
                    case "지능":
                    case "마력":
                        data.Intelligence += amount;
                        break;
                    case "vit":
                    case "체력":
                    case "생명력":
                        data.Vitality += amount;
                        break;
                    default:
                        return false;
                }

                data.StatPoints -= amount;
                _db.SaveRpg(data);
                return true;
            }
        }

        public bool ResetStats(string accountName)
        {
            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                int totalPoints = data.Strength + data.Dexterity + data.Intelligence + data.Vitality;
                if (totalPoints == 0) return false;

                data.StatPoints += totalPoints;
                data.Strength = 0;
                data.Dexterity = 0;
                data.Intelligence = 0;
                data.Vitality = 0;

                _db.SaveRpg(data);
                return true;
            }
        }

        public void AssignRandomQuest(string accountName, int playerLevel)
        {
            lock (_lock)
            {
                var data = _db.GetRpg(accountName);

                // 레벨대별 목표 몬스터 테이블
                (string name, int netId, int count, long exp, long money)[] pool = playerLevel switch
                {
                    < 10 => new[]
                    {
                        ("Green Slime", -3, 10, 150L, 200L),
                        ("Blue Slime", -1, 10, 150L, 200L),
                        ("Zombie", 3, 5, 200L, 300L),
                        ("Demon Eye", 2, 5, 250L, 350L)
                    },
                    < 25 => new[]
                    {
                        ("Skeleton", 21, 15, 600L, 800L),
                        ("Cave Bat", 49, 15, 500L, 700L),
                        ("Eater of Souls", 6, 12, 800L, 1000L),
                        ("Crimera", 173, 12, 800L, 1000L)
                    },
                    < 50 => new[]
                    {
                        ("Hornet", 42, 20, 2000L, 2500L),
                        ("Man Eater", 43, 10, 2500L, 3000L),
                        ("Hellbat", 59, 20, 3000L, 4000L),
                        ("Lava Slime", 60, 20, 3000L, 4000L)
                    },
                    _ => new[]
                    {
                        ("Armored Skeleton", 77, 25, 6000L, 8000L),
                        ("Derpling", 166, 20, 8000L, 10000L),
                        ("Giant Tortoise", 153, 15, 10000L, 12000L),
                        ("Chaos Elemental", 120, 10, 15000L, 20000L)
                    }
                };

                var random = new Random();
                var quest = pool[random.Next(pool.Length)];

                data.ActiveQuestTarget = quest.name;
                data.ActiveQuestTargetNetId = quest.netId;
                data.QuestRequiredCount = quest.count;
                data.QuestCurrentCount = 0;
                data.QuestRewardExp = quest.exp;
                data.QuestRewardMoney = quest.money;
                data.LastQuestDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                _db.SaveRpg(data);
            }
        }

        public bool ProgressQuest(string accountName, int npcNetId, string npcName, out bool completed, out long rewardExp, out long rewardMoney)
        {
            completed = false;
            rewardExp = 0;
            rewardMoney = 0;

            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                if (data.QuestRequiredCount <= 0 || data.QuestCurrentCount >= data.QuestRequiredCount)
                    return false;

                // NetID 또는 이름 매칭
                bool match = (data.ActiveQuestTargetNetId != 0 && data.ActiveQuestTargetNetId == npcNetId) ||
                             (!string.IsNullOrEmpty(data.ActiveQuestTarget) && data.ActiveQuestTarget.Equals(npcName, StringComparison.OrdinalIgnoreCase));

                if (!match) return false;

                data.QuestCurrentCount++;
                if (data.QuestCurrentCount >= data.QuestRequiredCount)
                {
                    completed = true;
                    rewardExp = data.QuestRewardExp;
                    rewardMoney = data.QuestRewardMoney;
                    data.QuestRequiredCount = 0; // 퀘스트 완료 처리
                    data.ActiveQuestTarget = "";
                }

                _db.SaveRpg(data);
                return true;
            }
        }
    }
}
