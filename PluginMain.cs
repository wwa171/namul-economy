using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockEconomyExp.API;
using TShockEconomyExp.Commands;
using TShockEconomyExp.Config;
using TShockEconomyExp.Database;
using TShockEconomyExp.Handlers;
using TShockEconomyExp.Services;

namespace TShockEconomyExp
{
    [ApiVersion(2, 1)]
    public class PluginMain : TerrariaPlugin
    {
        public override string Name => "TShockEconomyExp";
        public override Version Version => new Version(1, 5, 0);
        public override string Author => "나물이 & 나구 (Vibe Coding)";
        public override string Description => "독립 SQLite 분리 기반 종합 RPG 코어 플러그인 (경제, 경험치, 직업/스탯, 채광/낚시, 칭호/업적, 파티 사냥, 상점)";

        public static PluginConfig Config { get; private set; } = new();
        public static ShopConfig ShopConfig { get; private set; } = new();
        public static TitleConfig TitleConfig { get; private set; } = new();

        public static DatabaseManager Database { get; private set; } = null!;
        public static IEconomyService EconomyService { get; private set; } = null!;
        public static IExpService ExpService { get; private set; } = null!;
        public static RpgService RpgService { get; private set; } = null!;
        public static TitleService TitleService { get; private set; } = null!;

        public PluginMain(Main game) : base(game)
        {
            Order = 1;
        }

        public override void Initialize()
        {
            string configDir = Path.Combine(TShock.SavePath, "economy_exp");
            string configPath = Path.Combine(configDir, "config.json");
            string shopConfigPath = Path.Combine(configDir, "shop.json");
            string titleConfigPath = Path.Combine(configDir, "titles.json");

            // 1. 설정 파일 로드
            Config = PluginConfig.Load(configPath);
            ShopConfig = ShopConfig.Load(shopConfigPath);
            TitleConfig = TitleConfig.Load(titleConfigPath);

            // 2. DB 초기화 (경제 / 경험치 / RPG 분리)
            Database = new DatabaseManager(configDir);
            Database.Initialize();

            // 3. 서비스 레이어 바인딩
            EconomyService = new EconomyService(Database);
            ExpService = new ExpService(Database, Config);
            RpgService = new RpgService(Database);
            TitleService = new TitleService(Database, TitleConfig);

            // 4. 레벨업 시 스탯 포인트 보너스 & 칭호 해금 체크
            ExpService.OnLevelUp += OnLevelUpEvent;
            EconomyService.OnShopTransaction += OnShopTransactionEvent;

            // 5. 게임 훅 및 통합 명령어 등록
            GameHooksHandler.RegisterHooks(this);
            CommandManager.RegisterCommands();

            TShock.Log.ConsoleInfo($"[TShockEconomyExp] RPG v1.5.0 종합 시스템 로드 완료! (저장소: {configDir})");
        }

        private void OnLevelUpEvent(object? sender, LevelUpEventArgs e)
        {
            int gainedPoints = (e.NewLevel - e.OldLevel) * 5;
            RpgService.AddStatPoints(e.AccountName, gainedPoints);

            var player = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Account.Name.Equals(e.AccountName, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                player.SendSuccessMessage($"🎉 축하합니다! 레벨업! (Lv.{e.OldLevel} -> Lv.{e.NewLevel})");
                player.SendSuccessMessage($"[보너스] 스탯 포인트 +{gainedPoints}개 획득! (/스탯분배 로 투자하세요)");

                // 레벨업에 따른 칭호 해금 체크
                long totalExp = ExpService.GetTotalExp(e.AccountName);
                long money = EconomyService.GetBalance(e.AccountName);
                var newlyUnlocked = TitleService.CheckAndUnlockTitles(e.AccountName, e.NewLevel, totalExp, money);
                foreach (var title in newlyUnlocked)
                {
                    player.SendSuccessMessage($"🏆 [새 칭호 해금!] '{title.Name}' - {title.Description} (/칭호장착 {title.Name})");
                }
            }
            TShock.Utils.Broadcast($"[레벨업] {e.AccountName} 님이 Lv.{e.NewLevel} 에 도달했습니다!", Microsoft.Xna.Framework.Color.LightGreen);
        }

        private void OnShopTransactionEvent(object? sender, ShopTransactionEventArgs e)
        {
            string action = e.IsPurchase ? "구매" : "판매";
            TShock.Log.ConsoleInfo($"[Shop Log] {e.AccountName} - {action}: {e.ItemName} x{e.Quantity} (총 {e.TotalPrice:N0} {Config.CurrencyName})");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooksHandler.UnregisterHooks(this);
                if (ExpService != null) ExpService.OnLevelUp -= OnLevelUpEvent;
                if (EconomyService != null) EconomyService.OnShopTransaction -= OnShopTransactionEvent;
            }
            base.Dispose(disposing);
        }
    }
}
