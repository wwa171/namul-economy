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
            ServerApi.Hooks.NpcSpawn.Register(plugin, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Register(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Register(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Register(plugin, OnGreetPlayer);
        }

        public static void UnregisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcSpawn.Deregister(plugin, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Deregister(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Deregister(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Deregister(plugin, OnGreetPlayer);
        }

        private static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null || !player.IsLoggedIn) return;

            var ecoData = PluginMain.Database.GetEconomy(player.Account.Name);
            if (ecoData.Balance == 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, PluginMain.Config.StartingBalance, "신규 정착금");
                player.SendSuccessMessage($"[알림] 신규 정착금 {PluginMain.Config.StartingBalance:N0} {PluginMain.Config.CurrencyName}이 지급되었습니다!");
            }

            var rpgData = PluginMain.RpgService.GetRpgData(player.Account.Name);
            if (rpgData.StatPoints > 0)
            {
                player.SendInfoMessage($"[RPG] 미분배 스탯 포인트가 {rpgData.StatPoints}개 있습니다! (/스탯분배 로 스탯을 올리세요)");
            }
        }

        /// <summary>
        /// 🌟 스폰 위치 기준 거리 비례 몬스터 스펙(체력/공격력) 동적 스케일링
        /// - 보스 몬스터, 친화적 NPC(타운 주민 등), 더미 등은 제외
        /// </summary>
        private static void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            var cfg = PluginMain.Config.DistanceScaling;
            if (!cfg.Enabled) return;

            int npcIndex = args.NpcId;
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return;

            NPC npc = Main.npc[npcIndex];
            if (npc == null || !npc.active || npc.friendly || npc.boss) return;
            if (PluginMain.Config.BlacklistedNpcNetIds.Contains(npc.netID)) return;

            // 월드 기본 스폰 지점 (타일 단위 좌표: Main.spawnTileX, Main.spawnTileY)
            double spawnTileX = Main.spawnTileX;
            double spawnTileY = Main.spawnTileY;

            // 몬스터의 현재 타일 위치 (16픽셀 = 1타일)
            double npcTileX = npc.position.X / 16.0;
            double npcTileY = npc.position.Y / 16.0;

            double dx = Math.Abs(npcTileX - spawnTileX);
            double dy = Math.Abs(npcTileY - spawnTileY);

            // 설정된 모드에 따른 거리(타일) 계산
            double distanceTiles = cfg.DistanceCalculationMode.ToLowerInvariant() switch
            {
                "horizontalonly" => dx,
                "taxicab" => dx + dy,
                _ => Math.Sqrt(dx * dx + dy * dy) // 기본: 유클리드 직선거리
            };

            // 안전지대 내에 스폰된 몬스터는 기본 스펙 유지
            if (distanceTiles <= cfg.SafeZoneTileRadius) return;

            // 안전지대를 벗어난 유효 거리 및 강화 스텝 계산
            double effectiveDistance = distanceTiles - cfg.SafeZoneTileRadius;
            double steps = effectiveDistance / Math.Max(1.0, cfg.TilesPerScalingStep);

            // 배율 계산 (최대 상한선 적용)
            double healthMultiplier = Math.Min(cfg.MaxHealthMultiplier, 1.0 + (steps * cfg.HealthIncreasePerStep));
            double damageMultiplier = Math.Min(cfg.MaxDamageMultiplier, 1.0 + (steps * cfg.DamageIncreasePerStep));

            if (healthMultiplier > 1.0)
            {
                int newLifeMax = (int)(npc.lifeMax * healthMultiplier);
                npc.lifeMax = newLifeMax;
                npc.life = newLifeMax;
            }

            if (damageMultiplier > 1.0)
            {
                npc.damage = (int)(npc.damage * damageMultiplier);
            }

            // 모든 클라이언트에 갱신된 NPC 체력 및 스펙 패킷 동기화 전송 (패킷 23: NpcUpdate)
            NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npcIndex);
        }

        private static void OnNpcStrike(NpcStrikeEventArgs args)
        {
            if (!PluginMain.Config.EnableRewards) return;

            NPC npc = args.Npc;
            if (npc == null || !npc.active || npc.friendly || npc.life <= 0) return;
            if (PluginMain.Config.BlacklistedNpcNetIds.Contains(npc.netID)) return;

            int playerIndex = args.Player.whoAmI;
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;

            TSPlayer player = TShock.Players[playerIndex];
            if (player == null || !player.IsLoggedIn) return;

            int baseDamage = args.Damage;
            if (baseDamage <= 0) return;

            var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
            double bonusRatio = (rpg.Strength * 0.015) + (rpg.Dexterity * 0.015) + (rpg.Intelligence * 0.015);

            int bonusDamage = (int)(baseDamage * bonusRatio);
            int totalDealtDamage = baseDamage;

            if (bonusDamage > 0 && npc.life > baseDamage)
            {
                int effectiveBonus = Math.Min(bonusDamage, npc.life - baseDamage);
                if (effectiveBonus > 0)
                {
                    npc.StrikeNPC(effectiveBonus, 0f, args.HitDirection, false, true, playerIndex, args.Player);
                    NetMessage.SendData((int)PacketTypes.NpcStrike, -1, -1, null, npc.whoAmI, effectiveBonus, 0f, args.HitDirection, 0, 0, 0);
                    totalDealtDamage += effectiveBonus;
                }
            }

            int effectiveTotalDamage = Math.Min(totalDealtDamage, Math.Max(1, npc.life));
            var (expRatio, moneyRatio) = GetRewardRatios(npc);

            long expGain = Math.Max(0, (long)(effectiveTotalDamage * expRatio));
            long moneyGain = Math.Max(0, (long)(effectiveTotalDamage * moneyRatio));

            if (expGain > 0)
            {
                PluginMain.ExpService.AddExp(player.Account.Name, expGain, $"데미지 보상: {npc.FullName} ({effectiveTotalDamage} dmg)");
            }

            if (moneyGain > 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, moneyGain, $"데미지 보상: {npc.FullName} ({effectiveTotalDamage} dmg)");
            }

            if (PluginMain.Config.NotifyRewardsInChat && (expGain > 0 || moneyGain > 0))
            {
                player.SendMessage($"[전투] +{expGain:N0} EXP | +{moneyGain:N0} {PluginMain.Config.CurrencyName} (보너스: +{bonusDamage} dmg)", Microsoft.Xna.Framework.Color.Yellow);
            }
        }

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
