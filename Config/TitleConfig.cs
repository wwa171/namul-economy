using System.Collections.Generic;
using Newtonsoft.Json;

namespace TShockEconomyExp.Config
{
    public class TitleDefinition
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty; // 채팅에 노출될 칭호 이름 (예: [슬레이어])

        [JsonProperty("ColorHex")]
        public string ColorHex { get; set; } = "FFD700"; // 칭호 색상 (HEX)

        [JsonProperty("Description")]
        public string Description { get; set; } = string.Empty;

        // 획득 조건 타입: "Level", "TotalExp", "KillCount", "BossKillCount", "MiningCount", "FishingCount", "TotalMoney"
        [JsonProperty("ConditionType")]
        public string ConditionType { get; set; } = "Level";

        [JsonProperty("ConditionValue")]
        public long ConditionValue { get; set; } = 10;

        // 칭호 장착 시 패시브 스펙 보너스
        [JsonProperty("BonusDamageRatio")]
        public double BonusDamageRatio { get; set; } = 0.0; // 공격력 +%

        [JsonProperty("BonusMoneyRatio")]
        public double BonusMoneyRatio { get; set; } = 0.0; // 골드 획득량 +%

        [JsonProperty("BonusExpRatio")]
        public double BonusExpRatio { get; set; } = 0.0; // 경험치 획득량 +%
    }

    public class TitleConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("PrefixFormat")]
        public string PrefixFormat { get; set; } = "[{0}]"; // {0}에 칭호 이름 치환

        [JsonProperty("Titles")]
        public List<TitleDefinition> Titles { get; set; } = new()
        {
            new TitleDefinition
            {
                Id = "novice",
                Name = "모험의 시작",
                ColorHex = "A6E3A1",
                Description = "테라리아 세계에 발을 들인 모험가 (기본 제공)",
                ConditionType = "Level",
                ConditionValue = 1,
                BonusExpRatio = 0.02
            },
            new TitleDefinition
            {
                Id = "warrior_lv10",
                Name = "초보 전사",
                ColorHex = "89B4FA",
                Description = "레벨 10에 도달한 자",
                ConditionType = "Level",
                ConditionValue = 10,
                BonusDamageRatio = 0.03
            },
            new TitleDefinition
            {
                Id = "slayer_50",
                Name = "학살자",
                ColorHex = "F38BA8",
                Description = "몬스터를 50마리 이상 처치한 자",
                ConditionType = "KillCount",
                ConditionValue = 50,
                BonusDamageRatio = 0.05,
                BonusExpRatio = 0.03
            },
            new TitleDefinition
            {
                Id = "boss_slayer",
                Name = "보스 슬레이어",
                ColorHex = "FAB387",
                Description = "보스 몬스터를 5마리 이상 토벌한 영웅",
                ConditionType = "BossKillCount",
                ConditionValue = 5,
                BonusDamageRatio = 0.08,
                BonusMoneyRatio = 0.05
            },
            new TitleDefinition
            {
                Id = "golden_miner",
                Name = "황금 곡괭이",
                ColorHex = "F9E2AF",
                Description = "광물을 100회 이상 채광한 광부",
                ConditionType = "MiningCount",
                ConditionValue = 100,
                BonusMoneyRatio = 0.08
            },
            new TitleDefinition
            {
                Id = "master_angler",
                Name = "강태공",
                ColorHex = "74C7EC",
                Description = "낚시를 50회 이상 성공한 낚시 장인",
                ConditionType = "FishingCount",
                ConditionValue = 50,
                BonusExpRatio = 0.05,
                BonusMoneyRatio = 0.05
            },
            new TitleDefinition
            {
                Id = "rich_man",
                Name = "백만장자",
                ColorHex = "F5C2E7",
                Description = "보유 골드 100,000 이상 달성",
                ConditionType = "TotalMoney",
                ConditionValue = 100000,
                BonusMoneyRatio = 0.10
            }
        };

        public static TitleConfig Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<TitleConfig>(json) ?? new TitleConfig();
                }
            }
            catch
            {
            }

            var cfg = new TitleConfig();
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
