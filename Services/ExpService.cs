using TShockEconomyExp.API;
using TShockEconomyExp.Config;
using TShockEconomyExp.Database;

namespace TShockEconomyExp.Services
{
    public class ExpService : IExpService
    {
        private readonly DatabaseManager _db;
        private readonly PluginConfig _config;
        private readonly object _lock = new();

        public event EventHandler<LevelUpEventArgs>? OnLevelUp;
        public event EventHandler<ExpGainEventArgs>? OnExpGained;

        public ExpService(DatabaseManager db, PluginConfig config)
        {
            _db = db;
            _config = config;
        }

        public long GetTotalExp(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return 0;
            return _db.GetExp(accountName).TotalExp;
        }

        public int GetLevel(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return 1;
            return _db.GetExp(accountName).Level;
        }

        /// <summary>
        /// 다음 레벨로 가기 위한 필요 경험치 공식: BaseExpRequirement * (Level ^ ExpRequirementMultiplier)
        /// </summary>
        public long GetExpToNextLevel(int currentLevel)
        {
            if (currentLevel >= _config.MaxLevel) return 0;
            return (long)(_config.BaseExpRequirement * Math.Pow(currentLevel, _config.ExpRequirementMultiplier));
        }

        public long GetCurrentLevelExpProgress(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return 0;
            var data = _db.GetExp(accountName);
            long totalRequiredForPrevLevels = 0;
            for (int lvl = 1; lvl < data.Level; lvl++)
            {
                totalRequiredForPrevLevels += GetExpToNextLevel(lvl);
            }
            return Math.Max(0, data.TotalExp - totalRequiredForPrevLevels);
        }

        public void AddExp(string accountName, long amount, string reason = "")
        {
            if (string.IsNullOrWhiteSpace(accountName) || amount <= 0) return;

            lock (_lock)
            {
                var data = _db.GetExp(accountName);
                if (data.Level >= _config.MaxLevel) return;

                data.TotalExp += amount;

                OnExpGained?.Invoke(this, new ExpGainEventArgs
                {
                    AccountName = accountName,
                    GainedExp = amount,
                    NewTotalExp = data.TotalExp,
                    Reason = reason
                });

                // 레벨업 계산
                int initialLevel = data.Level;
                while (data.Level < _config.MaxLevel)
                {
                    long reqExp = GetExpToNextLevel(data.Level);
                    long currentProgress = GetCurrentLevelExpProgress(accountName);

                    if (currentProgress >= reqExp)
                    {
                        data.Level++;
                    }
                    else
                    {
                        break;
                    }
                }

                _db.SaveExp(data);

                if (data.Level > initialLevel)
                {
                    OnLevelUp?.Invoke(this, new LevelUpEventArgs
                    {
                        AccountName = accountName,
                        OldLevel = initialLevel,
                        NewLevel = data.Level
                    });
                }
            }
        }

        public void SetLevel(string accountName, int level)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return;

            lock (_lock)
            {
                var data = _db.GetExp(accountName);
                int oldLevel = data.Level;
                data.Level = Math.Clamp(level, 1, _config.MaxLevel);

                // 레벨에 맞는 최소 누적 경험치 역계산
                long expSum = 0;
                for (int l = 1; l < data.Level; l++)
                {
                    expSum += GetExpToNextLevel(l);
                }
                data.TotalExp = expSum;

                _db.SaveExp(data);

                if (data.Level != oldLevel)
                {
                    OnLevelUp?.Invoke(this, new LevelUpEventArgs
                    {
                        AccountName = accountName,
                        OldLevel = oldLevel,
                        NewLevel = data.Level
                    });
                }
            }
        }

        public IEnumerable<(string AccountName, int Level, long TotalExp)> GetTopRankings(int limit = 10)
        {
            return _db.GetTopExp(limit).Select(x => (x.AccountName, x.Level, x.TotalExp));
        }
    }
}
