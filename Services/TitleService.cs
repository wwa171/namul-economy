using Newtonsoft.Json;
using TShockEconomyExp.Config;
using TShockEconomyExp.Database;
using TShockEconomyExp.Database.Models;

namespace TShockEconomyExp.Services
{
    public class TitleService
    {
        private readonly DatabaseManager _db;
        private readonly TitleConfig _titleConfig;
        private readonly object _lock = new();

        public TitleService(DatabaseManager db, TitleConfig titleConfig)
        {
            _db = db;
            _titleConfig = titleConfig;
        }

        public TitleDefinition? GetEquippedTitle(string accountName)
        {
            var rpg = _db.GetRpg(accountName);
            return _titleConfig.Titles.FirstOrDefault(t => t.Id.Equals(rpg.EquippedTitleId, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> GetUnlockedTitleIds(string accountName)
        {
            var rpg = _db.GetRpg(accountName);
            try
            {
                return JsonConvert.DeserializeObject<List<string>>(rpg.UnlockedTitlesJson) ?? new List<string> { "novice" };
            }
            catch
            {
                return new List<string> { "novice" };
            }
        }

        public bool EquipTitle(string accountName, string titleIdOrName, out string assignedName)
        {
            assignedName = string.Empty;
            var target = _titleConfig.Titles.FirstOrDefault(t =>
                t.Id.Equals(titleIdOrName, StringComparison.OrdinalIgnoreCase) ||
                t.Name.Equals(titleIdOrName, StringComparison.OrdinalIgnoreCase));

            if (target == null) return false;

            lock (_lock)
            {
                var unlocked = GetUnlockedTitleIds(accountName);
                if (!unlocked.Contains(target.Id, StringComparer.OrdinalIgnoreCase))
                {
                    return false; // 아직 해금되지 않은 칭호
                }

                var rpg = _db.GetRpg(accountName);
                rpg.EquippedTitleId = target.Id;
                _db.SaveRpg(rpg);
                assignedName = target.Name;
                return true;
            }
        }

        public List<TitleDefinition> CheckAndUnlockTitles(string accountName, int level, long totalExp, long money)
        {
            var newlyUnlocked = new List<TitleDefinition>();

            lock (_lock)
            {
                var rpg = _db.GetRpg(accountName);
                var unlocked = GetUnlockedTitleIds(accountName);
                bool updated = false;

                foreach (var title in _titleConfig.Titles)
                {
                    if (unlocked.Contains(title.Id, StringComparer.OrdinalIgnoreCase))
                        continue;

                    bool qualify = title.ConditionType.ToLowerInvariant() switch
                    {
                        "level" => level >= title.ConditionValue,
                        "totalexp" => totalExp >= title.ConditionValue,
                        "killcount" => rpg.TotalMonsterKills >= title.ConditionValue,
                        "bosskillcount" => rpg.TotalBossKills >= title.ConditionValue,
                        "miningcount" => rpg.TotalMiningCount >= title.ConditionValue,
                        "fishingcount" => rpg.TotalFishingCount >= title.ConditionValue,
                        "totalmoney" => money >= title.ConditionValue,
                        _ => false
                    };

                    if (qualify)
                    {
                        unlocked.Add(title.Id);
                        newlyUnlocked.Add(title);
                        updated = true;
                    }
                }

                if (updated)
                {
                    rpg.UnlockedTitlesJson = JsonConvert.SerializeObject(unlocked);
                    _db.SaveRpg(rpg);
                }
            }

            return newlyUnlocked;
        }

        public void IncrementStat(string accountName, string statType, long amount = 1)
        {
            if (string.IsNullOrWhiteSpace(accountName) || amount <= 0) return;

            lock (_lock)
            {
                var rpg = _db.GetRpg(accountName);
                switch (statType.ToLowerInvariant())
                {
                    case "kill":
                        rpg.TotalMonsterKills += amount;
                        break;
                    case "boss":
                        rpg.TotalBossKills += amount;
                        break;
                    case "mining":
                        rpg.TotalMiningCount += amount;
                        break;
                    case "fishing":
                        rpg.TotalFishingCount += amount;
                        break;
                }
                _db.SaveRpg(rpg);
            }
        }
    }
}
