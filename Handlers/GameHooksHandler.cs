using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockEconomyExp.Config;

namespace TShockEconomyExp.Handlers
{
    public static class GameHooksHandler
    {
        public static void RegisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcStrike.Register(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Register(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Register(plugin, OnGreetPlayer);
        }

        public static void UnregisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcStrike.Deregister(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Deregister(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Deregister(plugin, OnGreetPlayer);
        }

        private static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null || !player.IsLoggedIn) return;

            // 신규 유저 초기 지원금 지급 확인
            var ecoData = PluginMain.Database.GetEconomy(player.Account.Name);
            if (ecoData.Balance == 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, PluginMain.Config.StartingBalance, "신규 정착금");
                player.SendSuccessMessage($"[알림] 신규 정착금 {PluginMain.Config.StartingBalance:N0} {PluginMain.Config.CurrencyName}이 지급되었습니다!");
            }
        }

        /// <summary>
        /// 몬스터 피격 시 (플레이어가 입힌 실질 데미지 기준 경험치 & 골드 실시간 지급)
        /// </summary>
        private static void OnNpcStrike(NpcStrikeEventArgs args)
        {
            if (!PluginMain.Config.EnableRewards) return;

            NPC npc = args.Npc;
            if (npc == null || npc.friendly) return;

            // 1. 타겟 더미(Target Dummy, netID: 488) 및 블랙리스트 몬스터 완전 차단
            if (PluginMain.Config.BlacklistedNpcNetIds.Contains(npc.netID)) return;

            // 2. 공격한 플레이어 판별
            int playerIndex = args.Player.whoAmI;
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;

            TSPlayer player = TShock.Players[playerIndex];
            if (player == null || !player.IsLoggedIn) return;

            // 3. 실제 적용된 데미지 계산 (몬스터의 남은 체력을 초과하지 않는 유효 데미지 기준)
            int dealtDamage = args.Damage;
            if (dealtDamage <= 0) return;
            int effectiveDamage = Math.Min(dealtDamage, Math.Max(1, npc.life));

            // 4. 비율 설정 조회 (몬스터별 개별 설정 > 기본 설정)
            var (expRatio, moneyRatio) = GetRewardRatios(npc);

            long expGain = Math.Max(0, (long)(effectiveDamage * expRatio));
            long moneyGain = Math.Max(0, (long)(effectiveDamage * moneyRatio));

            if (expGain > 0)
            {
                PluginMain.ExpService.AddExp(player.Account.Name, expGain, $"데미지 보상: {npc.FullName} ({effectiveDamage} dmg)");
            }

            if (moneyGain > 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, moneyGain, $"데미지 보상: {npc.FullName} ({effectiveDamage} dmg)");
            }

            if (PluginMain.Config.NotifyRewardsInChat && (expGain > 0 || moneyGain > 0))
            {
                player.SendMessage($"[전투] +{expGain:N0} EXP | +{moneyGain:N0} {PluginMain.Config.CurrencyName}", Microsoft.Xna.Framework.Color.Yellow);
            }
        }

        /// <summary>
        /// 몬스터 처치 완료 시 추가 킬 보너스 지급
        /// </summary>
        private static void OnNpcKilled(NpcKilledEventArgs args)
        {
            if (!PluginMain.Config.EnableRewards) return;

            NPC npc = args.npc;
            if (npc == null || npc.friendly) return;
            if (PluginMain.Config.BlacklistedNpcNetIds.Contains(npc.netID)) return;

            int targetIndex = npc.target;
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers) return;

            TSPlayer player = TShock.Players[targetIndex];
            if (player == null || !player.IsLoggedIn) return;

            var overrideSetting = FindMonsterSetting(npc);
            long bonusExp = overrideSetting?.KillBonusExp ?? 0;
            long bonusMoney = overrideSetting?.KillBonusMoney ?? 0;

            // 기본 킬 보너스가 0이고 보스인 경우 기본 보너스 산정
            if (npc.boss && bonusExp == 0 && bonusMoney == 0)
            {
                bonusExp = Math.Max(100, npc.lifeMax / 10);
                bonusMoney = Math.Max(500, npc.lifeMax / 5);
            }

            if (bonusExp > 0)
            {
                PluginMain.ExpService.AddExp(player.Account.Name, bonusExp, $"처치 보너스: {npc.FullName}");
            }

            if (bonusMoney > 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, bonusMoney, $"처치 보너스: {npc.FullName}");
            }

            if (npc.boss)
            {
                TShock.Utils.Broadcast($"[보스 토벌] {player.Name} 님이 {npc.FullName}을(를) 토벌했습니다! (보너스: +{bonusExp:N0} EXP, +{bonusMoney:N0} {PluginMain.Config.CurrencyName})", Microsoft.Xna.Framework.Color.Gold);
            }
        }

        private static (double expRatio, double moneyRatio) GetRewardRatios(NPC npc)
        {
            var setting = FindMonsterSetting(npc);
            if (setting != null)
            {
                return (setting.ExpPerDamage, setting.MoneyPerDamage);
            }

            // 개별 설정이 없는 경우 기본 설정 적용
            double exp = PluginMain.Config.DefaultExpPerDamage;
            double money = PluginMain.Config.DefaultMoneyPerDamage;

            if (npc.boss)
            {
                exp *= PluginMain.Config.BossMultiplier;
                money *= PluginMain.Config.BossMultiplier;
            }

            return (exp, money);
        }

        private static MonsterRewardSetting? FindMonsterSetting(NPC npc)
        {
            if (PluginMain.Config.MonsterOverrides == null) return null;

            // 1. 이름으로 매칭
            if (PluginMain.Config.MonsterOverrides.TryGetValue(npc.FullName, out var byName))
                return byName;

            // 2. NetID 문자열로 매칭
            if (PluginMain.Config.MonsterOverrides.TryGetValue(npc.netID.ToString(), out var byId))
                return byId;

            return null;
        }
    }
}
