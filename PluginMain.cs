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
        public override Version Version => new Version(1, 1, 0);
        public override string Author => "나물이 & 나구";
        public override string Description => "독립 SQLite 분리 기반 종합 RPG 코어 플러그인 (경제, 경험치, 직업/스탯, 일일퀘스트, 상점)";

        public static PluginConfig Config { get; private set; } = new();
        public static ShopConfig ShopConfig { get; private set; } = new();
        public static DatabaseManager Database { get; private set; } = null!;
        public static IEconomyService EconomyService { get; private set; } = null!;
        public static IExpService ExpService { get; private set; } = null!;
        public static RpgService RpgService { get; private set; } = null!;

        public PluginMain(Main game) : base(game)
        {
            Order = 1;
        }

        public override void Initialize()
        {
            string configDir = Path.Combine(TShock.SavePath, "economy_exp");
            string configPath = Path.Combine(configDir, "config.json");
            string shopConfigPath = Path.Combine(configDir, "shop.json");

            // 1. 설정 파일 로드
            Config = PluginConfig.Load(configPath);
            ShopConfig = ShopConfig.Load(shopConfigPath);

            // 2. DB 초기화 (경제: economy_data.sqlite / 경험치: exp_data.sqlite / RPG: rpg_data.sqlite 각각 3중 분리)
            Database = new DatabaseManager(configDir);
            Database.Initialize();

            // 3. 서비스 레이어 바인딩
            EconomyService = new EconomyService(Database);
            ExpService = new ExpService(Database, Config);
            RpgService = new RpgService(Database);

            // 4. 레벨업 시 스탯 포인트 5개 자동 지급 및 알림
            ExpService.OnLevelUp += OnLevelUpEvent;
            EconomyService.OnShopTransaction += OnShopTransactionEvent;

            // 5. 게임 훅 및 통합 명령어 등록
            GameHooksHandler.RegisterHooks(this);
            CommandManager.RegisterCommands();

            TShock.Log.ConsoleInfo($"[TShockEconomyExp] RPG 종합 시스템 로드 완료! (저장소: {configDir})");
        }

        private void OnLevelUpEvent(object? sender, LevelUpEventArgs e)
        {
            // 레벨당 스탯 포인트 5개 보너스 지급
            int gainedPoints = (e.NewLevel - e.OldLevel) * 5;
            RpgService.AddStatPoints(e.AccountName, gainedPoints);

            var player = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Account.Name.Equals(e.AccountName, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                player.SendSuccessMessage($"🎉 축하합니다! 레벨업! (Lv.{e.OldLevel} -> Lv.{e.NewLevel})");
                player.SendSuccessMessage($"[보너스] 스탯 포인트 +{gainedPoints}개 획득! (/스탯분배 로 투자하세요)");
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
