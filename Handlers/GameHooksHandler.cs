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

            // 1. 신규 유저 초기 지원금 지급 확인
            var ecoData = PluginMain.Database.GetEconomy(player.Account.Name);
            if (ecoData.Balance == 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, PluginMain.Config.StartingBalance, "신규 정착금");
                player.SendSuccessMessage($"[알림] 신규 정착금 {PluginMain.Config.StartingBalance:N0} {PluginMain.Config.CurrencyName}이 지급되었습니다!");
            }

            // 2. 접속 시 RPG 직업 및 스탯 안내
            var rpgData = PluginMain.RpgService.GetRpgData(player.Account.Name);
            if (rpgData.StatPoints > 0)
            {
                player.SendInfoMessage($"[RPG] 미분배 스탯 포인트가 {rpgData.StatPoints}개 있습니다! (/스탯분배 로 스탯을 올리세요)");
            }
        }

        /// <summary>
        /// 몬스터 피격 시 (직업/스탯 데미지 보정 적용 + 데미지 비례 EXP/골드 지급)
        /// </summary>
        private static void OnNpcStrike(NpcStrikeEventArgs args)
        {
            if (!PluginMain.Config.EnableRewards) return;

            NPC npc = args.Npc;
            if (npc == null || npc.friendly) return;

            // 타겟 더미(netID: 488) 및 블랙리스트 완전 차단
            if (PluginMain.Config.BlacklistedNpcNetIds.Contains(npc.netID)) return;

            int playerIndex = args.Player.whoAmI;
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;

            TSPlayer player = TShock.Players[playerIndex];
            if (player == null || !player.IsLoggedIn) return;

            // RPG 스탯에 따른 공격력 증폭 적용
            var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
            double statMultiplier = 1.0;

            // 직업 및 스탯별 보너스
            // 힘(STR): 1포인트당 +1.5% 데미지
            // 민첩(DEX): 1포인트당 +1.5% 데미지
            // 지능(INT): 1포인트당 +1.5% 데미지
            statMultiplier += (rpg.Strength * 0.015) + (rpg.Dexterity * 0.015) + (rpg.Intelligence * 0.015);

            int finalDamage = (int)(args.Damage * statMultiplier);
            args.Damage = finalDamage;

            int effectiveDamage = Math.Min(finalDamage, Math.Max(1, npc.life));
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
        /// 몬스터 처치 완료 시 토벌 보너스 및 퀘스트 카운트 처리
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

            // 1. 퀘스트 진행도 체크
            if (PluginMain.RpgService.ProgressQuest(player.Account.Name, npc.netID, npc.FullName, out bool completed, out long qExp, out long qMoney))
            {
                if (completed)
                {
                    PluginMain.ExpService.AddExp(player.Account.Name, qExp, "퀘스트 완료 보상");
                    PluginMain.EconomyService.AddBalance(player.Account.Name, qMoney, "퀘스트 완료 보상");
                    player.SendSuccessMessage($"🎉 [퀘스트 완료!] 토벌 목표를 달성했습니다! (+{qExp:N0} EXP, +{qMoney:N0} {PluginMain.Config.CurrencyName})");
                }
                else
                {
                    var data = PluginMain.RpgService.GetRpgData(player.Account.Name);
                    player.SendInfoMessage($"[퀘스트] {data.ActiveQuestTarget} 처치 진행도: ({data.QuestCurrentCount}/{data.QuestRequiredCount})");
                }
            }

            // 2. 몬스터 킬 보너스
            var overrideSetting = FindMonsterSetting(npc);
            long bonusExp = overrideSetting?.KillBonusExp ?? 0;
            long bonusMoney = overrideSetting?.KillBonusMoney ?? 0;

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

            if (PluginMain.Config.MonsterOverrides.TryGetValue(npc.FullName, out var byName))
                return byName;

            if (PluginMain.Config.MonsterOverrides.TryGetValue(npc.netID.ToString(), out var byId))
                return byId;

            return null;
        }
    }
}
