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
    /// 직업 4대 특화 스탯 데미지 증가 배율 설정 (1포인트당 데미지 % 증가)
    /// </summary>
    public class StatDamageConfig
    {
        [JsonProperty("WarriorDamagePerPoint")]
        public double WarriorDamagePerPoint { get; set; } = 0.02; // 워리어: 근접 물리 데미지 (+2.0%/pt)

        [JsonProperty("RangerDamagePerPoint")]
        public double RangerDamagePerPoint { get; set; } = 0.02;  // 레인저: 원거리 물리 데미지 (+2.0%/pt)

        [JsonProperty("SorcererDamagePerPoint")]
        public double SorcererDamagePerPoint { get; set; } = 0.025; // 소서러: 마법/마나소모 데미지 (+2.5%/pt)

        [JsonProperty("SummonerDamagePerPoint")]
        public double SummonerDamagePerPoint { get; set; } = 0.025; // 서머너: 소환수/채찍 데미지 (+2.5%/pt)

        [JsonProperty("SorcererBonusManaPerPoint")]
        public int SorcererBonusManaPerPoint { get; set; } = 5; // 소서러 1포인트당 최대 마나 보너스 (+5)
    }

    /// <summary>
    /// 서버 스폰포인트(Main.spawnTileX, Y) 기준 거리 비례 몬스터 강화 및 스폰율 설정
    /// </summary>
    public class DistanceScalingConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("DistanceCalculationMode")]
        public string DistanceCalculationMode { get; set; } = "HorizontalOnly";

        [JsonProperty("SafeZoneTileRadius")]
        public double SafeZoneTileRadius { get; set; } = 200.0;

        [JsonProperty("TilesPerScalingStep")]
        public double TilesPerScalingStep { get; set; } = 100.0;

        [JsonProperty("HealthIncreasePerStep")]
        public double HealthIncreasePerStep { get; set; } = 0.05;

        [JsonProperty("DamageIncreasePerStep")]
        public double DamageIncreasePerStep { get; set; } = 0.03;

        [JsonProperty("DefenseIncreasePerStep")]
        public double DefenseIncreasePerStep { get; set; } = 0.03;

        [JsonProperty("MaxHealthMultiplier")]
        public double MaxHealthMultiplier { get; set; } = 10.0;

        [JsonProperty("MaxDamageMultiplier")]
        public double MaxDamageMultiplier { get; set; } = 5.0;

        [JsonProperty("MaxDefenseMultiplier")]
        public double MaxDefenseMultiplier { get; set; } = 5.0;

        [JsonProperty("ScaleRewardsWithDistance")]
        public bool ScaleRewardsWithDistance { get; set; } = true;

        // 🌟 멀리 나갈수록 스폰율(전투/워터캔들 버프 등) 동적 증가 설정
        [JsonProperty("IncreaseSpawnRateWithDistance")]
        public bool IncreaseSpawnRateWithDistance { get; set; } = true;
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

    public class BossScalingConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        // 🌟 서버 자연 발생 보스 스폰 차단 (예: 밤에 자연적으로 뜨는 크툴루의 눈 등)
        [JsonProperty("BlockNaturalBossSpawn")]
        public bool BlockNaturalBossSpawn { get; set; } = true;

        // 🌟 레벨당 보스 체력 증가 계수 (예: 0.1 이면 레벨 24일 때 24 * 0.1 = 2.4배 추가, 즉 체력 + (체력 x 2.4))
        [JsonProperty("HealthIncreasePerLevel")]
        public double HealthIncreasePerLevel { get; set; } = 0.1;

        // 🌟 보스 최대 체력 배율 상한선 (예: 50.0배)
        [JsonProperty("MaxHealthMultiplier")]
        public double MaxHealthMultiplier { get; set; } = 50.0;

        // 🌟 레벨별 보스 소환 아이템 사용 최소 요구 레벨 설정 (아이템 NetID 기준)
        [JsonProperty("BossItemLevelRequirements")]
        public Dictionary<string, int> BossItemLevelRequirements { get; set; } = new()
        {
            ["43"] = 10,   // 의심스러운 눈 (Eye of Cthulhu) -> Lv.10
            ["560"] = 15,  // 슬라임 왕관 (King Slime) -> Lv.15
            ["70"] = 20,   // 지렁이 먹이 (Eater of Worlds) -> Lv.20
            ["1331"] = 20, // 피투성이 장 (Brain of Cthulhu) -> Lv.20
            ["1133"] = 25, // 에이비에이션 (Queen Bee) -> Lv.25
            ["1307"] = 30, // 사슴뿔 (Deerclops) -> Lv.30
            ["267"] = 40,  // 가이드 부두 인형 (Wall of Flesh) -> Lv.40
            ["556"] = 50,  // 기계 벌레 (The Destroyer) -> Lv.50
            ["557"] = 50,  // 기계 두개골 (Skeletron Prime) -> Lv.50
            ["544"] = 50,  // 기계 눈 (The Twins) -> Lv.50
            ["4988"] = 55, // 젤라틴 결정 (Queen Slime) -> Lv.55
            ["1293"] = 65, // 리자드 전지 (Golem) -> Lv.65
            ["1156"] = 70, // 송로버섯 지렁이 (Duke Fishron) -> Lv.70
            ["4961"] = 75, // 프리즘 풀잠자리 (Empress of Light) -> Lv.75
            ["3601"] = 80  // 천상의 신호기 (Moon Lord) -> Lv.80
        };
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

        // 🌟 머리 위 텍스트(Floating CombatText) 팝업 활성화
        [JsonProperty("EnableFloatingCombatText")]
        public bool EnableFloatingCombatText { get; set; } = true;

        // 🌟 미니맵/상태창 HUD 실시간 브로드캐스트 활성화
        [JsonProperty("EnableHudBroadcast")]
        public bool EnableHudBroadcast { get; set; } = true;

        // 🌟 서버사이드 캐릭터(SSC) 필수 활성화 강제 체크 (비활성화 시 플러그인 보호 모드)
        [JsonProperty("RequireServerSideCharacter")]
        public bool RequireServerSideCharacter { get; set; } = true;

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

        // 🌟 4대 직업 데미지 스탯 설정
        [JsonProperty("StatDamage")]
        public StatDamageConfig StatDamage { get; set; } = new();

        // 🌟 보스 스펙 스케일링 & 소환 아이템 레벨 제한 & 자연 스폰 차단 설정
        [JsonProperty("BossScaling")]
        public BossScalingConfig BossScaling { get; set; } = new();

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
