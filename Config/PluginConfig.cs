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

        // 거리 계산 모드: "Euclidean"(직선거리), "HorizontalOnly"(X좌표 거리만), "Taxicab"(맨해튼 거리)
        [JsonProperty("DistanceCalculationMode")]
        public string DistanceCalculationMode { get; set; } = "Euclidean";

        // 스폰 지점(기준점)으로부터 안전 지대 반경 (타일 단위, 기본 200타일)
        [JsonProperty("SafeZoneTileRadius")]
        public double SafeZoneTileRadius { get; set; } = 200.0;

        // 강화 스텝 단위 (기본 100타일마다 강화)
        [JsonProperty("TilesPerScalingStep")]
        public double TilesPerScalingStep { get; set; } = 100.0;

        // 1스텝당 증가하는 체력 배율 (기본 0.05 = 100타일마다 +5% 증가)
        [JsonProperty("HealthIncreasePerStep")]
        public double HealthIncreasePerStep { get; set; } = 0.05;

        // 1스텝당 증가하는 공격력 배율 (기본 0.03 = 100타일마다 +3% 증가)
        [JsonProperty("DamageIncreasePerStep")]
        public double DamageIncreasePerStep { get; set; } = 0.03;

        // 최대 허용 체력 배율 (기본 최대 10배)
        [JsonProperty("MaxHealthMultiplier")]
        public double MaxHealthMultiplier { get; set; } = 10.0;

        // 최대 허용 공격력 배율 (기본 최대 5배)
        [JsonProperty("MaxDamageMultiplier")]
        public double MaxDamageMultiplier { get; set; } = 5.0;

        // 거리로 강해진 만큼 경험치/골드 보상도 비례 증가 여부
        [JsonProperty("ScaleRewardsWithDistance")]
        public bool ScaleRewardsWithDistance { get; set; } = true;
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
