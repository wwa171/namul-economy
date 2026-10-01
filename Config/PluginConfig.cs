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

        // 기본 데미지당 보상 비율 (몬스터별 개별 설정이 없을 때 적용)
        [JsonProperty("DefaultExpPerDamage")]
        public double DefaultExpPerDamage { get; set; } = 0.02; // 데미지 100당 2 EXP

        [JsonProperty("DefaultMoneyPerDamage")]
        public double DefaultMoneyPerDamage { get; set; } = 0.01; // 데미지 100당 1 골드

        // 보스 몬스터 기본 배율
        [JsonProperty("BossMultiplier")]
        public double BossMultiplier { get; set; } = 3.0;

        // 보상 제외 NPC 넷아이디 목록 (488: Target Dummy 등)
        [JsonProperty("BlacklistedNpcNetIds")]
        public List<int> BlacklistedNpcNetIds { get; set; } = new()
        {
            488 // Target Dummy (타겟 더미)
        };

        // 몬스터별 개별 비율 설정 (Key: NPC 이름 또는 NetID 문자열)
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
                // 실패 시 기본 설정 반환
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
