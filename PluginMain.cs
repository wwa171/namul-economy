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
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "나물이 & 나구";
        public override string Description => "독립 SQLite 기반 고성능 경험치 & 경제 코어 플러그인";

        public static PluginConfig Config { get; private set; } = new();
        public static DatabaseManager Database { get; private set; } = null!;
        public static IEconomyService EconomyService { get; private set; } = null!;
        public static IExpService ExpService { get; private set; } = null!;

        public PluginMain(Main game) : base(game)
        {
            Order = 1;
        }

        public override void Initialize()
        {
            string configDir = Path.Combine(TShock.SavePath, "economy_exp");
            string configPath = Path.Combine(configDir, "config.json");

            // 설정 파일 로드
            Config = PluginConfig.Load(configPath);

            // DB 초기화 (경제: economy_data.sqlite / 경험치: exp_data.sqlite 독립 분리)
            Database = new DatabaseManager(configDir);
            Database.Initialize();

            // 서비스 레이어 바인딩
            EconomyService = new EconomyService(Database);
            ExpService = new ExpService(Database, Config);

            // 레벨업 및 상점/경제 이벤트 리스너 등록
            ExpService.OnLevelUp += OnLevelUpEvent;
            EconomyService.OnShopTransaction += OnShopTransactionEvent;

            // 게임 훅 및 명령어 등록
            GameHooksHandler.RegisterHooks(this);
            CommandManager.RegisterCommands();

            TShock.Log.ConsoleInfo($"[TShockEconomyExp] 플러그인 로드 완료! (저장소: {configDir})");
        }

        private void OnLevelUpEvent(object? sender, LevelUpEventArgs e)
        {
            var player = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Account.Name.Equals(e.AccountName, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                player.SendSuccessMessage($"🎉 축하합니다! 레벨이 상승했습니다! (Lv.{e.OldLevel} -> Lv.{e.NewLevel})");
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
