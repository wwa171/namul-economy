using Microsoft.Data.Sqlite;
using TShockEconomyExp.Database.Models;

namespace TShockEconomyExp.Database
{
    public partial class DatabaseManager
    {
        private void InitAuctionTable()
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AuctionListings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SellerAccount TEXT NOT NULL COLLATE NOCASE,
                        ItemNetId INTEGER NOT NULL,
                        ItemName TEXT NOT NULL,
                        Stack INTEGER NOT NULL,
                        Prefix INTEGER NOT NULL,
                        Price INTEGER NOT NULL,
                        ListedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS idx_auction_listed ON AuctionListings(ListedAt DESC);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public int AddAuctionListing(AuctionListing listing)
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO AuctionListings (SellerAccount, ItemNetId, ItemName, Stack, Prefix, Price, ListedAt)
                    VALUES ($seller, $netId, $name, $stack, $prefix, $price, $listedAt);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("$seller", listing.SellerAccount);
                cmd.Parameters.AddWithValue("$netId", listing.ItemNetId);
                cmd.Parameters.AddWithValue("$name", listing.ItemName);
                cmd.Parameters.AddWithValue("$stack", listing.Stack);
                cmd.Parameters.AddWithValue("$prefix", listing.Prefix);
                cmd.Parameters.AddWithValue("$price", listing.Price);
                cmd.Parameters.AddWithValue("$listedAt", listing.ListedAt.ToString("o"));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<AuctionListing> GetAuctionListings(int page, int pageSize)
        {
            var list = new List<AuctionListing>();
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, SellerAccount, ItemNetId, ItemName, Stack, Prefix, Price, ListedAt
                    FROM AuctionListings
                    ORDER BY Id DESC
                    LIMIT $limit OFFSET $offset;
                ";
                cmd.Parameters.AddWithValue("$limit", pageSize);
                cmd.Parameters.AddWithValue("$offset", (page - 1) * pageSize);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new AuctionListing
                    {
                        Id = reader.GetInt32(0),
                        SellerAccount = reader.GetString(1),
                        ItemNetId = reader.GetInt32(2),
                        ItemName = reader.GetString(3),
                        Stack = reader.GetInt32(4),
                        Prefix = (byte)reader.GetInt32(5),
                        Price = reader.GetInt64(6),
                        ListedAt = DateTime.Parse(reader.GetString(7))
                    });
                }
            }
            return list;
        }

        public AuctionListing? GetAuctionListing(int id)
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, SellerAccount, ItemNetId, ItemName, Stack, Prefix, Price, ListedAt FROM AuctionListings WHERE Id = $id LIMIT 1;";
                cmd.Parameters.AddWithValue("$id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new AuctionListing
                    {
                        Id = reader.GetInt32(0),
                        SellerAccount = reader.GetString(1),
                        ItemNetId = reader.GetInt32(2),
                        ItemName = reader.GetString(3),
                        Stack = reader.GetInt32(4),
                        Prefix = (byte)reader.GetInt32(5),
                        Price = reader.GetInt64(6),
                        ListedAt = DateTime.Parse(reader.GetString(7))
                    };
                }
            }
            return null;
        }

        public bool DeleteAuctionListing(int id)
        {
            lock (_economyLock)
            {
                using var conn = GetEconomyConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM AuctionListings WHERE Id = $id;";
                cmd.Parameters.AddWithValue("$id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
