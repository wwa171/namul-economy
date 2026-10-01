using Microsoft.Data.Sqlite;
using TShockEconomyExp.Database.Models;

namespace TShockEconomyExp.Database
{
    /// <summary>
    /// 경제와 경험치 데이터를 각각 독립된 SQLite DB 파일로 관리합니다.
    /// - tshock/economy_data.sqlite
    /// - tshock/exp_data.sqlite
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _economyDbPath;
        private readonly string _expDbPath;
        private readonly object _economyLock = new();
        private readonly object _expLock = new();

        public DatabaseManager(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            _economyDbPath = Path.Combine(baseDirectory, "economy_data.sqlite");
            _expDbPath = Path.Combine(baseDirectory, "exp_data.sqlite");
        }

        public void Initialize()
        {
            InitEconomyTable();
            InitExpTable();
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

                // 존재하지 않으면 신규 생성
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

                // 존재하지 않으면 레벨 1 기본값 생성
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
    }
}
