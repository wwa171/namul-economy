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
                    case "warrior":
                    case "워리어":
                    case "전사":
                    case "str":
                    case "힘":
                        data.Warrior += amount;
                        break;
                    case "ranger":
                    case "레인저":
                    case "궁수":
                    case "dex":
                    case "민첩":
                        data.Ranger += amount;
                        break;
                    case "sorcerer":
                    case "소서러":
                    case "마법사":
                    case "int":
                    case "지능":
                    case "마력":
                        data.Sorcerer += amount;
                        break;
                    case "summoner":
                    case "서머너":
                    case "소환사":
                    case "소환":
                        data.Summoner += amount;
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
                int totalUsed = data.Warrior + data.Ranger + data.Sorcerer + data.Summoner;
                if (totalUsed == 0) return false;

                data.StatPoints += totalUsed;
                data.Warrior = 0;
                data.Ranger = 0;
                data.Sorcerer = 0;
                data.Summoner = 0;
                _db.SaveRpg(data);
                return true;
            }
        }

        public void AssignRandomQuest(string accountName, int playerLevel)
        {
            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                var candidates = GetQuestCandidates(playerLevel);
                if (candidates.Count == 0) return;

                var rnd = new Random();
                var selected = candidates[rnd.Next(candidates.Count)];

                data.ActiveQuestTarget = selected.Name;
                data.ActiveQuestTargetNetId = selected.NetId;
                data.QuestRequiredCount = selected.Count;
                data.QuestCurrentCount = 0;
                data.QuestRewardExp = selected.Exp;
                data.QuestRewardMoney = selected.Money;
                data.LastQuestDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                _db.SaveRpg(data);
            }
        }

        public bool ProgressQuest(string accountName, int killedNetId, string killedName, out bool completed, out long rewardExp, out long rewardMoney)
        {
            completed = false;
            rewardExp = 0;
            rewardMoney = 0;

            lock (_lock)
            {
                var data = _db.GetRpg(accountName);
                if (data.QuestRequiredCount == 0) return false;

                bool match = (data.ActiveQuestTargetNetId > 0 && data.ActiveQuestTargetNetId == killedNetId) ||
                             (!string.IsNullOrEmpty(data.ActiveQuestTarget) && killedName.Contains(data.ActiveQuestTarget, StringComparison.OrdinalIgnoreCase));

                if (!match) return false;

                data.QuestCurrentCount++;
                if (data.QuestCurrentCount >= data.QuestRequiredCount)
                {
                    completed = true;
                    rewardExp = data.QuestRewardExp;
                    rewardMoney = data.QuestRewardMoney;

                    data.ActiveQuestTarget = "";
                    data.ActiveQuestTargetNetId = 0;
                    data.QuestRequiredCount = 0;
                    data.QuestCurrentCount = 0;
                }

                _db.SaveRpg(data);
                return true;
            }
        }

        private List<QuestTemplate> GetQuestCandidates(int level)
        {
            var list = new List<QuestTemplate>();

            // 초반 퀘스트 (1~10레벨)
            list.Add(new QuestTemplate("Green Slime", -1, 5, 50, 100));
            list.Add(new QuestTemplate("Blue Slime", -2, 5, 60, 120));
            list.Add(new QuestTemplate("Zombie", 3, 5, 80, 150));
            list.Add(new QuestTemplate("Demon Eye", 2, 3, 100, 200));

            // 중반 퀘스트 (11레벨 이상)
            if (level >= 10)
            {
                list.Add(new QuestTemplate("Eater of Souls", 6, 5, 200, 400));
                list.Add(new QuestTemplate("Crimera", 173, 5, 200, 400));
                list.Add(new QuestTemplate("Skeleton", 21, 5, 250, 500));
                list.Add(new QuestTemplate("Hornet", 42, 5, 300, 600));
            }

            // 후반 퀘스트 (25레벨 이상)
            if (level >= 25)
            {
                list.Add(new QuestTemplate("Hellbat", 59, 10, 500, 1000));
                list.Add(new QuestTemplate("Lava Slime", 60, 10, 500, 1000));
                list.Add(new QuestTemplate("Fire Imp", 24, 5, 600, 1200));
            }

            return list;
        }

        private record QuestTemplate(string Name, int NetId, int Count, long Exp, long Money);
    }
}
