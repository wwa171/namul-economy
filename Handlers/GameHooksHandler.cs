using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockEconomyExp.Config;

namespace TShockEconomyExp.Handlers
{
    public static class GameHooksHandler
    {
        private static DateTime _lastPassiveTick = DateTime.UtcNow;

        public static void RegisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcSpawn.Register(plugin, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Register(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Register(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Register(plugin, OnGreetPlayer);
            ServerApi.Hooks.GameUpdate.Register(plugin, OnGameUpdate);

            // TShock 채광(TileEdit) 패킷 핸들러 등록
            GetDataHandlers.TileEdit.Register(OnTileEdit);
        }

        public static void UnregisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcSpawn.Deregister(plugin, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Deregister(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Deregister(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Deregister(plugin, OnGreetPlayer);
            ServerApi.Hooks.GameUpdate.Deregister(plugin, OnGameUpdate);

            GetDataHandlers.TileEdit.UnRegister(OnTileEdit);
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
        /// 🌟 직업별 고유 지속 패시브 버프 주기적 부여 (전사=철피부/재생, 궁수=신속/양궁, 마법사=마력재생/강화, 소환사=소환/신속)
        /// </summary>
        private static void OnGameUpdate(EventArgs args)
        {
            var pCfg = PluginMain.Config.JobPassives;
            if (!pCfg.Enabled) return;

            if ((DateTime.UtcNow - _lastPassiveTick).TotalSeconds < Math.Max(1, pCfg.IntervalSeconds))
                return;

            _lastPassiveTick = DateTime.UtcNow;

            foreach (var player in TShock.Players)
            {
                if (player == null || !player.Active || !player.IsLoggedIn) continue;

                var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
                List<int>? buffsToApply = rpg.Job switch
                {
                    "전사" => pCfg.WarriorBuffs,
                    "궁수" => pCfg.RangerBuffs,
                    "마법사" => pCfg.MageBuffs,
                    "소환사" => pCfg.SummonerBuffs,
                    _ => null
                };

                if (buffsToApply != null && buffsToApply.Count > 0)
                {
                    foreach (int buffId in buffsToApply)
                    {
                        // 10초간 버프 부여 (주기적으로 갱신되어 영구 지속)
                        player.SetBuff(buffId, 600);
                    }
                }
            }
        }

        /// <summary>
        /// 🌟 채광(Mining) 생활 콘텐츠: 광물 타일을 캤을 때 경험치 및 골드 보상 지급
        /// </summary>
        private static void OnTileEdit(object? sender, GetDataHandlers.TileEditEventArgs args)
        {
            var mCfg = PluginMain.Config.Mining;
            if (!mCfg.Enabled || args.Handled) return;

            // Action 0 = 타일 파괴/채광 (KillTile)
            if (args.Action != 0) return;

            TSPlayer player = args.Player;
            if (player == null || !player.IsLoggedIn) return;

            int tileX = args.X;
            int tileY = args.Y;

            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY) return;

            ITile tile = Main.tile[tileX, tileY];
            if (tile == null || !tile.active()) return;

            ushort tileType = tile.type;
            string key = tileType.ToString();

            if (mCfg.OreRewards != null && mCfg.OreRewards.TryGetValue(key, out var reward))
            {
                if (reward.Exp > 0)
                {
                    PluginMain.ExpService.AddExp(player.Account.Name, reward.Exp, $"광물 채광: {reward.OreName}");
                }
                if (reward.Money > 0)
                {
                    PluginMain.EconomyService.AddBalance(player.Account.Name, reward.Money, $"광물 채광: {reward.OreName}");
                }

                if (mCfg.NotifyInChat)
                {
                    player.SendMessage($"[채광] {reward.OreName}! +{reward.Exp} EXP | +{reward.Money} {PluginMain.Config.CurrencyName}", Microsoft.Xna.Framework.Color.Orange);
                }
            }
        }

        /// <summary>
        /// 🌟 스폰 위치 기준 거리 비례 몬스터 스펙(체력/공격력) 동적 스케일링
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

            double spawnTileX = Main.spawnTileX;
            double spawnTileY = Main.spawnTileY;

            double npcTileX = npc.position.X / 16.0;
            double npcTileY = npc.position.Y / 16.0;

            double dx = Math.Abs(npcTileX - spawnTileX);
            double dy = Math.Abs(npcTileY - spawnTileY);

            double distanceTiles = cfg.DistanceCalculationMode.ToLowerInvariant() switch
            {
                "horizontalonly" => dx,
                "taxicab" => dx + dy,
                _ => Math.Sqrt(dx * dx + dy * dy)
            };

            if (distanceTiles <= cfg.SafeZoneTileRadius) return;

            double effectiveDistance = distanceTiles - cfg.SafeZoneTileRadius;
            double steps = effectiveDistance / Math.Max(1.0, cfg.TilesPerScalingStep);

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
