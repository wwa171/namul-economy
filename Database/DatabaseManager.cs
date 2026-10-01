using Microsoft.Data.Sqlite;
using TShockEconomyExp.Database.Models;

namespace TShockEconomyExp.Database
{
    /// <summary>
    /// 경제, 경험치, RPG 데이터를 각각 독립된 SQLite DB 파일로 분리 관리합니다.
    /// - tshock/economy_exp/economy_data.sqlite
    /// - tshock/economy_exp/exp_data.sqlite
    /// - tshock/economy_exp/rpg_data.sqlite
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _economyDbPath;
        private readonly string _expDbPath;
        private readonly string _rpgDbPath;
        private readonly object _economyLock = new();
        private readonly object _expLock = new();
        private readonly object _rpgLock = new();

        public DatabaseManager(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            _economyDbPath = Path.Combine(baseDirectory, "economy_data.sqlite");
            _expDbPath = Path.Combine(baseDirectory, "exp_data.sqlite");
            _rpgDbPath = Path.Combine(baseDirectory, "rpg_data.sqlite");
        }

        public void Initialize()
        {
            InitEconomyTable();
            InitExpTable();
            InitRpgTable();
        }

        #region Economy SQLite

        private SqliteConnection GetEconomyConnection()
        {
            var conn = new SqliteConnection($"Data Source={_economyDbPath}");
            conn.Open();
            return conn;
        }

        private void InitEconomyTable()
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS UserEconomy (
                        AccountName TEXT PRIMARY KEY COLLATE NOCASE,
                        Balance INTEGER NOT NULL DEFAULT 0,
                        LastUpdated TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS idx_economy_balance ON UserEconomy(Balance DESC);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public UserEconomyData GetEconomy(string accountName)
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT AccountName, Balance, LastUpdated FROM UserEconomy WHERE AccountName = $name LIMIT 1;";
                cmd.Parameters.AddWithValue("$name", accountName);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new UserEconomyData
                    {
                        AccountName = reader.GetString(0),
                        Balance = reader.GetInt64(1),
                        LastUpdated = DateTime.Parse(reader.GetString(2))
                    };
                }

                var defaultData = new UserEconomyData
                {
                    AccountName = accountName,
                    Balance = 0,
                    LastUpdated = DateTime.UtcNow
                };
                SaveEconomy(defaultData);
                return defaultData;
            }
        }

        public void SaveEconomy(UserEconomyData data)
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO UserEconomy (AccountName, Balance, LastUpdated)
                    VALUES ($name, $balance, $updated)
                    ON CONFLICT(AccountName) DO UPDATE SET
                        Balance = excluded.Balance,
                        LastUpdated = excluded.LastUpdated;
                ";
                cmd.Parameters.AddWithValue("$name", data.AccountName);
                cmd.Parameters.AddWithValue("$balance", data.Balance);
                cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<UserEconomyData> GetTopEconomy(int limit = 10)
        {
            var list = new List<UserEconomyData>();
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT AccountName, Balance, LastUpdated FROM UserEconomy ORDER BY Balance DESC LIMIT $limit;";
                cmd.Parameters.AddWithValue("$limit", limit);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new UserEconomyData
                    {
                        AccountName = reader.GetString(0),
                        Balance = reader.GetInt64(1),
                        LastUpdated = DateTime.Parse(reader.GetString(2))
                    });
                }
            }
            return list;
        }

        #endregion

        #region Exp SQLite

        private SqliteConnection GetExpConnection()
        {
            var conn = new SqliteConnection($"Data Source={_expDbPath}");
            conn.Open();
            return conn;
        }

        private void InitExpTable()
        {
            lock (_expLock)
            {
                using var conn = GetExpConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS UserExp (
                        AccountName TEXT PRIMARY KEY COLLATE NOCASE,
                        Level INTEGER NOT NULL DEFAULT 1,
                        TotalExp INTEGER NOT NULL DEFAULT 0,
                        LastUpdated TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS idx_exp_level ON UserExp(Level DESC, TotalExp DESC);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public UserExpData GetExp(string accountName)
        {
            lock (_expLock)
            {
                using var conn = GetExpConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT AccountName, Level, TotalExp, LastUpdated FROM UserExp WHERE AccountName = $name LIMIT 1;";
                cmd.Parameters.AddWithValue("$name", accountName);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new UserExpData
                    {
                        AccountName = reader.GetString(0),
                        Level = reader.GetInt32(1),
                        TotalExp = reader.GetInt64(2),
                        LastUpdated = DateTime.Parse(reader.GetString(3))
                    };
                }

                var defaultData = new UserExpData
                {
                    AccountName = accountName,
                    Level = 1,
                    TotalExp = 0,
                    LastUpdated = DateTime.UtcNow
                };
                SaveExp(defaultData);
                return defaultData;
            }
        }

        public void SaveExp(UserExpData data)
        {
            lock (_expLock)
            {
                using var conn = GetExpConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO UserExp (AccountName, Level, TotalExp, LastUpdated)
                    VALUES ($name, $level, $exp, $updated)
                    ON CONFLICT(AccountName) DO UPDATE SET
                        Level = excluded.Level,
                        TotalExp = excluded.TotalExp,
                        LastUpdated = excluded.LastUpdated;
                ";
                cmd.Parameters.AddWithValue("$name", data.AccountName);
                cmd.Parameters.AddWithValue("$level", data.Level);
                cmd.Parameters.AddWithValue("$exp", data.TotalExp);
                cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<UserExpData> GetTopExp(int limit = 10)
        {
            var list = new List<UserExpData>();
            lock (_expLock)
            {
                using var conn = GetExpConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT AccountName, Level, TotalExp, LastUpdated FROM UserExp ORDER BY Level DESC, TotalExp DESC LIMIT $limit;";
                cmd.Parameters.AddWithValue("$limit", limit);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new UserExpData
                    {
                        AccountName = reader.GetString(0),
                        Level = reader.GetInt32(1),
                        TotalExp = reader.GetInt64(2),
                        LastUpdated = DateTime.Parse(reader.GetString(3))
                    });
                }
            }
            return list;
        }

        #endregion

        #region RPG (Job & Stats & Quest) SQLite

        private SqliteConnection GetRpgConnection()
        {
            var conn = new SqliteConnection($"Data Source={_rpgDbPath}");
            conn.Open();
            return conn;
        }

        private void InitRpgTable()
        {
            lock (_rpgLock)
            {
                using var conn = GetRpgConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS UserRpg (
                        AccountName TEXT PRIMARY KEY COLLATE NOCASE,
                        Job TEXT NOT NULL DEFAULT '초보자',
                        StatPoints INTEGER NOT NULL DEFAULT 0,
                        Strength INTEGER NOT NULL DEFAULT 0,
                        Dexterity INTEGER NOT NULL DEFAULT 0,
                        Intelligence INTEGER NOT NULL DEFAULT 0,
                        Vitality INTEGER NOT NULL DEFAULT 0,
                        ActiveQuestTarget TEXT NOT NULL DEFAULT '',
                        ActiveQuestTargetNetId INTEGER NOT NULL DEFAULT 0,
                        QuestRequiredCount INTEGER NOT NULL DEFAULT 0,
                        QuestCurrentCount INTEGER NOT NULL DEFAULT 0,
                        QuestRewardExp INTEGER NOT NULL DEFAULT 0,
                        QuestRewardMoney INTEGER NOT NULL DEFAULT 0,
                        LastQuestDate TEXT NOT NULL DEFAULT '',
                        LastUpdated TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public UserRpgData GetRpg(string accountName)
        {
            lock (_rpgLock)
            {
                using var conn = GetRpgConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT AccountName, Job, StatPoints, Strength, Dexterity, Intelligence, Vitality,
                           ActiveQuestTarget, ActiveQuestTargetNetId, QuestRequiredCount, QuestCurrentCount,
                           QuestRewardExp, QuestRewardMoney, LastQuestDate, LastUpdated
                    FROM UserRpg WHERE AccountName = $name LIMIT 1;
                ";
                cmd.Parameters.AddWithValue("$name", accountName);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new UserRpgData
                    {
                        AccountName = reader.GetString(0),
                        Job = reader.GetString(1),
                        StatPoints = reader.GetInt32(2),
                        Strength = reader.GetInt32(3),
                        Dexterity = reader.GetInt32(4),
                        Intelligence = reader.GetInt32(5),
                        Vitality = reader.GetInt32(6),
                        ActiveQuestTarget = reader.GetString(7),
                        ActiveQuestTargetNetId = reader.GetInt32(8),
                        QuestRequiredCount = reader.GetInt32(9),
                        QuestCurrentCount = reader.GetInt32(10),
                        QuestRewardExp = reader.GetInt64(11),
                        QuestRewardMoney = reader.GetInt64(12),
                        LastQuestDate = reader.GetString(13),
                        LastUpdated = DateTime.Parse(reader.GetString(14))
                    };
                }

                var defaultData = new UserRpgData
                {
                    AccountName = accountName,
                    Job = "초보자",
                    StatPoints = 0,
                    Strength = 0,
                    Dexterity = 0,
                    Intelligence = 0,
                    Vitality = 0,
                    LastUpdated = DateTime.UtcNow
                };
                SaveRpg(defaultData);
                return defaultData;
            }
        }

        public void SaveRpg(UserRpgData data)
        {
            lock (_rpgLock)
            {
                using var conn = GetRpgConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO UserRpg (
                        AccountName, Job, StatPoints, Strength, Dexterity, Intelligence, Vitality,
                        ActiveQuestTarget, ActiveQuestTargetNetId, QuestRequiredCount, QuestCurrentCount,
                        QuestRewardExp, QuestRewardMoney, LastQuestDate, LastUpdated
                    ) VALUES (
                        $name, $job, $statPoints, $str, $dex, $int, $vit,
                        $target, $targetNetId, $reqCount, $curCount,
                        $rewardExp, $rewardMoney, $questDate, $updated
                    ) ON CONFLICT(AccountName) DO UPDATE SET
                        Job = excluded.Job,
                        StatPoints = excluded.StatPoints,
                        Strength = excluded.Strength,
                        Dexterity = excluded.Dexterity,
                        Intelligence = excluded.Intelligence,
                        Vitality = excluded.Vitality,
                        ActiveQuestTarget = excluded.ActiveQuestTarget,
                        ActiveQuestTargetNetId = excluded.ActiveQuestTargetNetId,
                        QuestRequiredCount = excluded.QuestRequiredCount,
                        QuestCurrentCount = excluded.QuestCurrentCount,
                        QuestRewardExp = excluded.QuestRewardExp,
                        QuestRewardMoney = excluded.QuestRewardMoney,
                        LastQuestDate = excluded.LastQuestDate,
                        LastUpdated = excluded.LastUpdated;
                ";
                cmd.Parameters.AddWithValue("$name", data.AccountName);
                cmd.Parameters.AddWithValue("$job", data.Job);
                cmd.Parameters.AddWithValue("$statPoints", data.StatPoints);
                cmd.Parameters.AddWithValue("$str", data.Strength);
                cmd.Parameters.AddWithValue("$dex", data.Dexterity);
                cmd.Parameters.AddWithValue("$int", data.Intelligence);
                cmd.Parameters.AddWithValue("$vit", data.Vitality);
                cmd.Parameters.AddWithValue("$target", data.ActiveQuestTarget);
                cmd.Parameters.AddWithValue("$targetNetId", data.ActiveQuestTargetNetId);
                cmd.Parameters.AddWithValue("$reqCount", data.QuestRequiredCount);
                cmd.Parameters.AddWithValue("$curCount", data.QuestCurrentCount);
                cmd.Parameters.AddWithValue("$rewardExp", data.QuestRewardExp);
                cmd.Parameters.AddWithValue("$rewardMoney", data.QuestRewardMoney);
                cmd.Parameters.AddWithValue("$questDate", data.LastQuestDate);
                cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        #endregion
    }
}
