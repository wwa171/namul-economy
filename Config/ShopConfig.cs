using System.Collections.Generic;
using Newtonsoft.Json;

namespace TShockEconomyExp.Config
{
    public class ShopItem
    {
        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("NetId")]
        public int NetId { get; set; }

        [JsonProperty("BuyPrice")]
        public long BuyPrice { get; set; } // 0 이하면 구매 불가

        [JsonProperty("SellPrice")]
        public long SellPrice { get; set; } // 0 이하면 판매 불가

        [JsonProperty("Category")]
        public string Category { get; set; } = "기타";
    }

    public class ShopConfig
    {
        [JsonProperty("EnableShop")]
        public bool EnableShop { get; set; } = true;

        [JsonProperty("Items")]
        public List<ShopItem> Items { get; set; } = new()
        {
            new ShopItem { Name = "Iron Broadsword", NetId = 4, BuyPrice = 500, SellPrice = 100, Category = "무기" },
            new ShopItem { Name = "Lesser Healing Potion", NetId = 28, BuyPrice = 50, SellPrice = 10, Category = "물약" },
            new ShopItem { Name = "Healing Potion", NetId = 188, BuyPrice = 200, SellPrice = 40, Category = "물약" },
            new ShopItem { Name = "Mana Potion", NetId = 110, BuyPrice = 150, SellPrice = 30, Category = "물약" },
            new ShopItem { Name = "Iron Pickaxe", NetId = 1, BuyPrice = 600, SellPrice = 120, Category = "도구" },
            new ShopItem { Name = "Wood", NetId = 9, BuyPrice = 5, SellPrice = 1, Category = "재료" },
            new ShopItem { Name = "Gel", NetId = 23, BuyPrice = 10, SellPrice = 2, Category = "재료" },
            new ShopItem { Name = "Fallen Star", NetId = 75, BuyPrice = 300, SellPrice = 50, Category = "재료" },
            new ShopItem { Name = "Life Crystal", NetId = 29, BuyPrice = 5000, SellPrice = 1000, Category = "특수" },
            new ShopItem { Name = "Mana Crystal", NetId = 109, BuyPrice = 2500, SellPrice = 500, Category = "특수" }
        };

        public static ShopConfig Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<ShopConfig>(json) ?? new ShopConfig();
                }
            }
            catch
            {
            }

            var cfg = new ShopConfig();
            cfg.Save(path);
            return cfg;
        }

        public void Save(string path)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch
            {
            }
        }
    }
}
