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
    /// 스폰 지역 거리 비례 몬스터 강화 설정
    /// </summary>
    public class DistanceScalingConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("DistanceCalculationMode")]
        public string DistanceCalculationMode { get; set; } = "Euclidean";

        [JsonProperty("SafeZoneTileRadius")]
        public double SafeZoneTileRadius { get; set; } = 200.0;

        [JsonProperty("TilesPerScalingStep")]
        public double TilesPerScalingStep { get; set; } = 100.0;

        [JsonProperty("HealthIncreasePerStep")]
        public double HealthIncreasePerStep { get; set; } = 0.05;

        [JsonProperty("DamageIncreasePerStep")]
        public double DamageIncreasePerStep { get; set; } = 0.03;

        [JsonProperty("MaxHealthMultiplier")]
        public double MaxHealthMultiplier { get; set; } = 10.0;

        [JsonProperty("MaxDamageMultiplier")]
        public double MaxDamageMultiplier { get; set; } = 5.0;

        [JsonProperty("ScaleRewardsWithDistance")]
        public bool ScaleRewardsWithDistance { get; set; } = true;
    }

    /// <summary>
    /// 생활 콘텐츠: 광물 채광(Mining) 설정
    /// </summary>
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

    /// <summary>
    /// 생활 콘텐츠: 낚시(Fishing) 보상 설정
    /// </summary>
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

        // 희귀/상자 낚시 보너스 배율
        [JsonProperty("CrateExpMultiplier")]
        public double CrateExpMultiplier { get; set; } = 3.0;

        [JsonProperty("CrateMoneyMultiplier")]
        public double CrateMoneyMultiplier { get; set; } = 4.0;
    }

    /// <summary>
    /// RPG 직업별 고유 지속 패시브 버프 설정
    /// </summary>
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

    /// <summary>
    /// 파티/팀 레이드 사냥 경험치·골드 공유 설정
    /// </summary>
    public class PartyConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        // 같은 팀원 공유 가능 최대 반경 (타일 단위, 기본 100타일)
        [JsonProperty("ShareTileRadius")]
        public double ShareTileRadius { get; set; } = 100.0;

        // 파티 사냥 보너스 경험치 (팀원이 1명 추가될 때마다 +15%씩 전체 경험치 증가)
        [JsonProperty("BonusExpPerMemberRatio")]
        public double BonusExpPerMemberRatio { get; set; } = 0.15;

        // 골드도 팀원들과 균등 분배 여부
        [JsonProperty("ShareMoney")]
        public bool ShareMoney { get; set; } = true;
    }

    /// <summary>
    /// 장비 재련/강화(Reforge/Enhance) 시스템 설정
    /// </summary>
    public class EnhanceConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        // 1회 재련 기본 비용 (골드)
        [JsonProperty("BaseCost")]
        public long BaseCost { get; set; } = 1000;

        // 최상급 접두사(Godly, Legendary, Unreal, Mythical 등) 당첨 보너스 배율
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
