using System.Collections.Generic;
using Newtonsoft.Json;

namespace TShockEconomyExp.Config
{
    public class MonsterRewardSetting
    {
        [JsonProperty("ExpPerDamage")]
        public double ExpPerDamage { get; set; } = 0.05;

        [JsonProperty("MoneyPerDamage")]
        public double MoneyPerDamage { get; set; } = 0.02;

        [JsonProperty("KillBonusExp")]
        public long KillBonusExp { get; set; } = 0;

        [JsonProperty("KillBonusMoney")]
        public long KillBonusMoney { get; set; } = 0;
    }

    /// <summary>
    /// 개별 플레이어 기준 거리 비례 몬스터 강화 설정
    /// </summary>
    public class DistanceScalingConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        // "PlayerPersonalSpawn": 플레이어의 개인 스폰/침대 기준 (침대 없으면 기본스폰), "WorldSpawn": 월드 공용 스폰 기준
        [JsonProperty("SpawnOriginType")]
        public string SpawnOriginType { get; set; } = "PlayerPersonalSpawn";

        // 거리 계산 모드: "Euclidean"(직선거리), "HorizontalOnly"(X축 좌우거리만), "Taxicab"(맨해튼거리)
        [JsonProperty("DistanceCalculationMode")]
        public string DistanceCalculationMode { get; set; } = "Euclidean";

        // 플레이어 기준 안전 지대 반경 (타일 단위, 기본 200타일)
        [JsonProperty("SafeZoneTileRadius")]
        public double SafeZoneTileRadius { get; set; } = 200.0;

        // 강화 스텝 단위 (기본 100타일마다 강화)
        [JsonProperty("TilesPerScalingStep")]
        public double TilesPerScalingStep { get; set; } = 100.0;

        // 스텝당 증가하는 체력 배율 (기본 +5%)
        [JsonProperty("HealthIncreasePerStep")]
        public double HealthIncreasePerStep { get; set; } = 0.05;

        // 스텝당 증가하는 공격력 배율 (기본 +3%)
        [JsonProperty("DamageIncreasePerStep")]
        public double DamageIncreasePerStep { get; set; } = 0.03;

        // 최대 허용 체력 배율 (최대 10배)
        [JsonProperty("MaxHealthMultiplier")]
        public double MaxHealthMultiplier { get; set; } = 10.0;

        // 최대 허용 공격력 배율 (최대 5배)
        [JsonProperty("MaxDamageMultiplier")]
        public double MaxDamageMultiplier { get; set; } = 5.0;

        // 보상 연동 여부
        [JsonProperty("ScaleRewardsWithDistance")]
        public bool ScaleRewardsWithDistance { get; set; } = true;
    }

    public class MiningConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("NotifyInChat")]
        public bool NotifyInChat { get; set; } = false;

        [JsonProperty("OreRewards")]
        public Dictionary<string, OreRewardSetting> OreRewards { get; set; } = new()
        {
            ["7"] = new OreRewardSetting { OreName = "Copper Ore", Exp = 5, Money = 2 },
            ["166"] = new OreRewardSetting { OreName = "Tin Ore", Exp = 5, Money = 2 },
            ["6"] = new OreRewardSetting { OreName = "Iron Ore", Exp = 10, Money = 5 },
            ["167"] = new OreRewardSetting { OreName = "Lead Ore", Exp = 10, Money = 5 },
            ["9"] = new OreRewardSetting { OreName = "Silver Ore", Exp = 20, Money = 10 },
            ["168"] = new OreRewardSetting { OreName = "Tungsten Ore", Exp = 20, Money = 10 },
            ["8"] = new OreRewardSetting { OreName = "Gold Ore", Exp = 35, Money = 25 },
            ["169"] = new OreRewardSetting { OreName = "Platinum Ore", Exp = 35, Money = 25 },
            ["37"] = new OreRewardSetting { OreName = "Meteorite", Exp = 50, Money = 40 },
            ["56"] = new OreRewardSetting { OreName = "Demonite Ore", Exp = 60, Money = 50 },
            ["107"] = new OreRewardSetting { OreName = "Cobalt Ore", Exp = 100, Money = 100 },
            ["108"] = new OreRewardSetting { OreName = "Mythril Ore", Exp = 150, Money = 150 },
            ["111"] = new OreRewardSetting { OreName = "Adamantite Ore", Exp = 250, Money = 250 },
            ["211"] = new OreRewardSetting { OreName = "Chlorophyte Ore", Exp = 400, Money = 400 }
        };
    }

    public class OreRewardSetting
    {
        [JsonProperty("OreName")]
        public string OreName { get; set; } = string.Empty;

        [JsonProperty("Exp")]
        public long Exp { get; set; } = 10;

        [JsonProperty("Money")]
        public long Money { get; set; } = 5;
    }

    public class FishingConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("NotifyInChat")]
        public bool NotifyInChat { get; set; } = true;

        [JsonProperty("DefaultExp")]
        public long DefaultExp { get; set; } = 50;

        [JsonProperty("DefaultMoney")]
        public long DefaultMoney { get; set; } = 30;

        [JsonProperty("CrateExpMultiplier")]
        public double CrateExpMultiplier { get; set; } = 3.0;

        [JsonProperty("CrateMoneyMultiplier")]
        public double CrateMoneyMultiplier { get; set; } = 4.0;
    }

    public class JobPassiveConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("IntervalSeconds")]
        public int IntervalSeconds { get; set; } = 3;

        [JsonProperty("WarriorBuffs")]
        public List<int> WarriorBuffs { get; set; } = new() { 2, 5 };

        [JsonProperty("RangerBuffs")]
        public List<int> RangerBuffs { get; set; } = new() { 3, 16 };

        [JsonProperty("MageBuffs")]
        public List<int> MageBuffs { get; set; } = new() { 6, 7 };

        [JsonProperty("SummonerBuffs")]
        public List<int> SummonerBuffs { get; set; } = new() { 115, 3 };
    }

    public class PartyConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("ShareTileRadius")]
        public double ShareTileRadius { get; set; } = 100.0;

        [JsonProperty("BonusExpPerMemberRatio")]
        public double BonusExpPerMemberRatio { get; set; } = 0.15;

        [JsonProperty("ShareMoney")]
        public bool ShareMoney { get; set; } = true;
    }

    public class EnhanceConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("BaseCost")]
        public long BaseCost { get; set; } = 1000;

        [JsonProperty("GodlyChanceMultiplier")]
        public double GodlyChanceMultiplier { get; set; } = 1.0;
    }

    public class PluginConfig
    {
        [JsonProperty("CurrencyName")]
        public string CurrencyName { get; set; } = "골드";

        [JsonProperty("StartingBalance")]
        public long StartingBalance { get; set; } = 1000;

        [JsonProperty("BaseExpRequirement")]
        public long BaseExpRequirement { get; set; } = 100;

        [JsonProperty("ExpRequirementMultiplier")]
        public double ExpRequirementMultiplier { get; set; } = 1.25;

        [JsonProperty("MaxLevel")]
        public int MaxLevel { get; set; } = 100;

        [JsonProperty("EnableRewards")]
        public bool EnableRewards { get; set; } = true;

        [JsonProperty("NotifyRewardsInChat")]
        public bool NotifyRewardsInChat { get; set; } = false;

        [JsonProperty("DefaultExpPerDamage")]
        public double DefaultExpPerDamage { get; set; } = 0.02;

        [JsonProperty("DefaultMoneyPerDamage")]
        public double DefaultMoneyPerDamage { get; set; } = 0.01;

        [JsonProperty("BossMultiplier")]
        public double BossMultiplier { get; set; } = 3.0;

        [JsonProperty("BlacklistedNpcNetIds")]
        public List<int> BlacklistedNpcNetIds { get; set; } = new()
        {
            488 // Target Dummy
        };

        [JsonProperty("DistanceScaling")]
        public DistanceScalingConfig DistanceScaling { get; set; } = new();

        [JsonProperty("Mining")]
        public MiningConfig Mining { get; set; } = new();

        [JsonProperty("Fishing")]
        public FishingConfig Fishing { get; set; } = new();

        [JsonProperty("Party")]
        public PartyConfig Party { get; set; } = new();

        [JsonProperty("Enhance")]
        public EnhanceConfig Enhance { get; set; } = new();

        [JsonProperty("JobPassives")]
        public JobPassiveConfig JobPassives { get; set; } = new();

        [JsonProperty("MonsterOverrides")]
        public Dictionary<string, MonsterRewardSetting> MonsterOverrides { get; set; } = new()
        {
            ["Eye of Cthulhu"] = new MonsterRewardSetting
            {
                ExpPerDamage = 0.08,
                MoneyPerDamage = 0.04,
                KillBonusExp = 500,
                KillBonusMoney = 1000
            },
            ["Eater of Worlds"] = new MonsterRewardSetting
            {
                ExpPerDamage = 0.06,
                MoneyPerDamage = 0.03,
                KillBonusExp = 800,
                KillBonusMoney = 1500
            },
            ["King Slime"] = new MonsterRewardSetting
            {
                ExpPerDamage = 0.05,
                MoneyPerDamage = 0.03,
                KillBonusExp = 300,
                KillBonusMoney = 500
            },
            ["Green Slime"] = new MonsterRewardSetting
            {
                ExpPerDamage = 0.02,
                MoneyPerDamage = 0.01,
                KillBonusExp = 1,
                KillBonusMoney = 1
            }
        };

        public static PluginConfig Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<PluginConfig>(json) ?? new PluginConfig();
                }
            }
            catch
            {
            }

            var config = new PluginConfig();
            config.Save(path);
            return config;
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
