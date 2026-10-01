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

            // TShock 채광(TileEdit) 및 낚시(ItemDrop) 이벤트 등록
            GetDataHandlers.TileEdit.Register(OnTileEdit);
            GetDataHandlers.ItemDrop.Register(OnItemDrop);
        }

        public static void UnregisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcSpawn.Deregister(plugin, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Deregister(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Deregister(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Deregister(plugin, OnGreetPlayer);
            ServerApi.Hooks.GameUpdate.Deregister(plugin, OnGameUpdate);

            GetDataHandlers.TileEdit.UnRegister(OnTileEdit);
            GetDataHandlers.ItemDrop.UnRegister(OnItemDrop);
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
        /// 🌟 직업별 지속 패시브 버프 유지
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
                        player.SetBuff(buffId, 600);
                    }
                }
            }
        }

        /// <summary>
        /// 🌟 낚시(Fishing) 보상: 플레이어가 낚아올려 드롭된 아이템 감지
        /// </summary>
        private static void OnItemDrop(object? sender, GetDataHandlers.ItemDropEventArgs args)
        {
            var fCfg = PluginMain.Config.Fishing;
            if (!fCfg.Enabled) return;

            TSPlayer player = args.Player;
            if (player == null || !player.IsLoggedIn) return;

            int itemId = args.Type;
            if (itemId <= 0) return;

            bool isCrate = (itemId >= 2334 && itemId <= 2336) || (itemId >= 3203 && itemId <= 3208) || (itemId >= 3979 && itemId <= 4002);
            bool isFish = (itemId >= 2290 && itemId <= 2321) || (itemId >= 2425 && itemId <= 2430);

            if (isCrate || isFish)
            {
                long exp = fCfg.DefaultExp;
                long money = fCfg.DefaultMoney;

                if (isCrate)
                {
                    exp = (long)(exp * fCfg.CrateExpMultiplier);
                    money = (long)(money * fCfg.CrateMoneyMultiplier);
                }

                PluginMain.ExpService.AddExp(player.Account.Name, exp, isCrate ? "희귀 상자 낚시 성공" : "물고기 낚시 성공");
                PluginMain.EconomyService.AddBalance(player.Account.Name, money, isCrate ? "희귀 상자 낚시 성공" : "물고기 낚시 성공");

                if (fCfg.NotifyInChat)
                {
                    string targetType = isCrate ? "🎁 희귀 상자 낚시!" : "🐟 물고기 낚시 성공!";
                    player.SendMessage($"[낚시] {targetType} +{exp:N0} EXP | +{money:N0} {PluginMain.Config.CurrencyName}", Microsoft.Xna.Framework.Color.Aqua);
                }
            }
        }

        /// <summary>
        /// 🌟 채광(Mining) 생활 콘텐츠
        /// </summary>
        private static void OnTileEdit(object? sender, GetDataHandlers.TileEditEventArgs args)
        {
            var mCfg = PluginMain.Config.Mining;
            if (!mCfg.Enabled || args.Handled) return;

            if (args.Action != 0) return; // 0 = 타일 파괴

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
        /// 🌟 스폰 거리 비례 몬스터 스펙 강화
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

        /// <summary>
        /// 몬스터 피격 시 (데미지 비례 보상 및 파티 사냥 공유 분배)
        /// </summary>
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

            long totalExpGain = Math.Max(0, (long)(effectiveTotalDamage * expRatio));
            long totalMoneyGain = Math.Max(0, (long)(effectiveTotalDamage * moneyRatio));

            // 🌟 파티/팀 사냥 경험치·골드 공유
            var partyCfg = PluginMain.Config.Party;
            List<TSPlayer> nearbyTeamMembers = new();

            if (partyCfg.Enabled && player.Team > 0)
            {
                foreach (var other in TShock.Players)
                {
                    if (other != null && other.Active && other.IsLoggedIn && other.Team == player.Team)
                    {
                        double distPixels = Math.Sqrt(Math.Pow(other.X - player.X, 2) + Math.Pow(other.Y - player.Y, 2));
                        if (distPixels <= (partyCfg.ShareTileRadius * 16.0))
                        {
                            nearbyTeamMembers.Add(other);
                        }
                    }
                }
            }

            if (nearbyTeamMembers.Count > 1)
            {
                double partyBonusMult = 1.0 + ((nearbyTeamMembers.Count - 1) * partyCfg.BonusExpPerMemberRatio);
                long sharedExp = Math.Max(1, (long)((totalExpGain * partyBonusMult) / nearbyTeamMembers.Count));
                long sharedMoney = partyCfg.ShareMoney ? Math.Max(1, totalMoneyGain / nearbyTeamMembers.Count) : totalMoneyGain;

                foreach (var member in nearbyTeamMembers)
                {
                    PluginMain.ExpService.AddExp(member.Account.Name, sharedExp, $"파티 사냥: {npc.FullName}");
                    if (partyCfg.ShareMoney || member == player)
                    {
                        PluginMain.EconomyService.AddBalance(member.Account.Name, sharedMoney, $"파티 사냥: {npc.FullName}");
                    }
                }
            }
            else
            {
                if (totalExpGain > 0)
                {
                    PluginMain.ExpService.AddExp(player.Account.Name, totalExpGain, $"데미지 보상: {npc.FullName} ({effectiveTotalDamage} dmg)");
                }
                if (totalMoneyGain > 0)
                {
                    PluginMain.EconomyService.AddBalance(player.Account.Name, totalMoneyGain, $"데미지 보상: {npc.FullName} ({effectiveTotalDamage} dmg)");
                }
            }

            if (PluginMain.Config.NotifyRewardsInChat && (totalExpGain > 0 || totalMoneyGain > 0))
            {
                player.SendMessage($"[전투] +{totalExpGain:N0} EXP | +{totalMoneyGain:N0} {PluginMain.Config.CurrencyName} (보너스: +{bonusDamage} dmg)", Microsoft.Xna.Framework.Color.Yellow);
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
