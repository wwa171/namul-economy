using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockEconomyExp.Config;

namespace TShockEconomyExp.Handlers
{
    public static class GameHooksHandler
    {
        private static DateTime _lastPassiveTick = DateTime.UtcNow;
        private static DateTime _lastHudTick = DateTime.UtcNow;
        private static readonly int[] _lastHeldItemNetId = new int[Main.maxPlayers];
        private static readonly byte[] _lastHeldItemPrefix = new byte[Main.maxPlayers];
        private static readonly DateTime[] _itemTooltipDisplayUntil = new DateTime[Main.maxPlayers];

        public static void RegisterHooks(TerrariaPlugin plugin)
        {
            ServerApi.Hooks.NpcSpawn.Register(plugin, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Register(plugin, OnNpcStrike);
            ServerApi.Hooks.NpcKilled.Register(plugin, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Register(plugin, OnGreetPlayer);
            ServerApi.Hooks.GameUpdate.Register(plugin, OnGameUpdate);
            ServerApi.Hooks.ServerChat.Register(plugin, OnServerChat);

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
            ServerApi.Hooks.ServerChat.Deregister(plugin, OnServerChat);

            GetDataHandlers.TileEdit.UnRegister(OnTileEdit);
            GetDataHandlers.ItemDrop.UnRegister(OnItemDrop);
        }

        /// <summary>
        /// 🌟 플레이어 또는 특정 좌표 위에 숫자/텍스트 팝업 (CombatTextString 패킷 119번)
        /// </summary>
        public static void ShowCombatText(TSPlayer player, string text, Color color, float offsetX = 0f, float offsetY = -10f)
        {
            if (player == null || !PluginMain.Config.EnableFloatingCombatText) return;
            try
            {
                var netText = NetworkText.FromLiteral(text);
                // Terraria Packet 119 (CombatTextString):
                // number = (int)color.PackedValue
                // number2 = posX
                // number3 = posY
                // text = netText
                NetMessage.SendData(
                    119,
                    -1,
                    -1,
                    netText,
                    (int)color.PackedValue,
                    player.X + offsetX,
                    player.Y + offsetY,
                    0f,
                    0,
                    0,
                    0
                );
            }
            catch
            {
            }
        }

        public static void ShowCombatTextAt(float x, float y, string text, Color color)
        {
            if (!PluginMain.Config.EnableFloatingCombatText) return;
            try
            {
                var netText = NetworkText.FromLiteral(text);
                NetMessage.SendData(
                    119,
                    -1,
                    -1,
                    netText,
                    (int)color.PackedValue,
                    x,
                    y,
                    0f,
                    0,
                    0,
                    0
                );
            }
            catch
            {
            }
        }

        private static void OnServerChat(ServerChatEventArgs args)
        {
            if (args.Handled) return;

            if (args.Text.StartsWith(TShock.Config.Settings.CommandSpecifier) ||
                args.Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
                return;

            var player = TShock.Players[args.Who];
            if (player == null || !player.IsLoggedIn) return;

            // 🌟 1. 플레이어가 일반 채팅을 쳤을 때 머리 위에 말풍선(CombatText) 팝업!
            ShowCombatText(player, args.Text, Color.White);

            if (!PluginMain.TitleConfig.Enabled) return;

            var title = PluginMain.TitleService.GetEquippedTitle(player.Account.Name);
            if (title == null) return;

            args.Handled = true;

            string prefix = string.Format(PluginMain.TitleConfig.PrefixFormat, title.Name);
            string formattedMessage = $"{prefix} {player.Group.Prefix}{player.Name}{player.Group.Suffix}: {args.Text}";

            var color = Color.White;
            if (!string.IsNullOrEmpty(title.ColorHex) && title.ColorHex.Length == 6)
            {
                try
                {
                    byte r = Convert.ToByte(title.ColorHex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(title.ColorHex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(title.ColorHex.Substring(4, 2), 16);
                    color = new Color(r, g, b);
                }
                catch
                {
                    color = Color.Gold;
                }
            }

            TShock.Utils.Broadcast(formattedMessage, color);
            TShock.Log.Info($"[Chat] {formattedMessage}");
        }

        private static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null || !player.IsLoggedIn) return;

            // 서버사이드 캐릭터(SSC) 필수 활성화 체크
            if (PluginMain.Config.RequireServerSideCharacter && !Main.ServerSideCharacter)
            {
                player.SendErrorMessage("⚠️ [경고] 서버사이드 캐릭터(SSC)가 비활성화되어 있어 RPG 플러그인이 보호 모드로 동작합니다.");
                player.SendErrorMessage("서버 관리자 콘솔에서 SSC를 활성화해주세요 (/ssc on).");
            }

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

            CheckTitleUnlocks(player);
            SyncPlayerStats(player);
        }

        /// <summary>
        /// 🌟 플레이어 스탯 실시간 동기화 (소서러 = 최대마나 증폭, 레인저 = 이동속도)
        /// </summary>
        public static void SyncPlayerStats(TSPlayer player)
        {
            if (player == null || !player.Active || !player.IsLoggedIn) return;
            try
            {
                var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
                var tPlayer = player.TPlayer;

                // 소서러 스탯 1당 최대 마나 보너스
                int bonusMana = rpg.Sorcerer * PluginMain.Config.StatDamage.SorcererBonusManaPerPoint;
                int baseMana = Math.Max(20, tPlayer.statManaMax);
                tPlayer.statManaMax2 = baseMana + bonusMana;

                // 마나 패킷 동기화 (Packet 16: PlayerLifeMana)
                NetMessage.SendData(16, -1, -1, null, player.Index);

                // 레인저 스탯이 10 이상일 때 신속 버프 지원
                if (rpg.Ranger >= 10)
                {
                    player.SetBuff(BuffID.Swiftness, 3600);
                }
            }
            catch
            {
            }
        }

        private static void CheckTitleUnlocks(TSPlayer player)
        {
            if (player == null || !player.IsLoggedIn) return;
            int level = PluginMain.ExpService.GetLevel(player.Account.Name);
            long totalExp = PluginMain.ExpService.GetTotalExp(player.Account.Name);
            long money = PluginMain.EconomyService.GetBalance(player.Account.Name);

            var unlocked = PluginMain.TitleService.CheckAndUnlockTitles(player.Account.Name, level, totalExp, money);
            foreach (var title in unlocked)
            {
                player.SendSuccessMessage($"🏆 [새 칭호 해금!] '{title.Name}' - {title.Description} (/칭호장착 {title.Name})");
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            var pCfg = PluginMain.Config.JobPassives;
            var dCfg = PluginMain.Config.DistanceScaling;

            if ((DateTime.UtcNow - _lastPassiveTick).TotalSeconds >= Math.Max(1, pCfg.IntervalSeconds))
            {
                _lastPassiveTick = DateTime.UtcNow;

                foreach (var player in TShock.Players)
                {
                    if (player == null || !player.Active || !player.IsLoggedIn) continue;

                    SyncPlayerStats(player);

                    var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
                    List<int>? buffsToApply = rpg.Job switch
                    {
                        "전사" => pCfg.WarriorBuffs,
                        "궁수" => pCfg.RangerBuffs,
                        "마법사" => pCfg.MageBuffs,
                        "소환사" => pCfg.SummonerBuffs,
                        _ => null
                    };

                    if (pCfg.Enabled && buffsToApply != null && buffsToApply.Count > 0)
                    {
                        foreach (int buffId in buffsToApply)
                        {
                            player.SetBuff(buffId, 600);
                        }
                    }

                    // 멀리 나갈수록 스폰율 상승
                    if (dCfg.Enabled && dCfg.IncreaseSpawnRateWithDistance)
                    {
                        double dx = Math.Abs((player.X / 16.0) - Main.spawnTileX);
                        if (dx > (dCfg.SafeZoneTileRadius + 400.0))
                        {
                            player.SetBuff(BuffID.Battle, 300);
                        }
                        if (dx > (dCfg.SafeZoneTileRadius + 800.0))
                        {
                            player.SetBuff(BuffID.WaterCandle, 300);
                        }
                    }
                }
            }

            // 🌟 들고 있는 아이템 변경 감지 및 커스텀 툴팁 출력
            foreach (var player in TShock.Players)
            {
                if (player == null || !player.Active || !player.IsLoggedIn) continue;

                var tPlayer = player.TPlayer;
                Item heldItem = tPlayer.HeldItem;
                int netId = (heldItem != null && !heldItem.IsAir) ? heldItem.type : 0;
                byte prefix = (heldItem != null && !heldItem.IsAir) ? heldItem.prefix : (byte)0;

                int pIndex = player.Index;
                if (pIndex >= 0 && pIndex < Main.maxPlayers)
                {
                    if (_lastHeldItemNetId[pIndex] != netId || _lastHeldItemPrefix[pIndex] != prefix)
                    {
                        _lastHeldItemNetId[pIndex] = netId;
                        _lastHeldItemPrefix[pIndex] = prefix;

                        if (netId > 0 && heldItem != null && !heldItem.IsAir && (heldItem.damage > 0 || heldItem.defense > 0 || heldItem.accessory))
                        {
                            var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
                            var statCfg = PluginMain.Config.StatDamage;

                            string category = "일반";
                            double bonusRatio = 0.0;

                            if (heldItem.magic || heldItem.mana > 0)
                            {
                                category = "마법";
                                bonusRatio = rpg.Sorcerer * statCfg.SorcererDamagePerPoint;
                            }
                            else if (heldItem.summon || heldItem.sentry)
                            {
                                category = "소환";
                                bonusRatio = rpg.Summoner * statCfg.SummonerDamagePerPoint;
                            }
                            else if (heldItem.ranged)
                            {
                                category = "원거리";
                                bonusRatio = rpg.Ranger * statCfg.RangerDamagePerPoint;
                            }
                            else if (heldItem.melee || !heldItem.noMelee)
                            {
                                category = "근접";
                                bonusRatio = rpg.Warrior * statCfg.WarriorDamagePerPoint;
                            }

                            string bonusStr = bonusRatio > 0 ? $"💥 {category} 보너스 +{(bonusRatio * 100):F1}%" : "";
                            string secondLine = string.IsNullOrEmpty(bonusStr) ? $"공격력: {heldItem.damage}" : $"공격력: {heldItem.damage} | {bonusStr}";
                            string itemTooltip = $"✨ [{heldItem.AffixName()}]\n{secondLine}";

                            // 4초간 툴팁 표시
                            _itemTooltipDisplayUntil[pIndex] = DateTime.UtcNow.AddSeconds(4);
                            HudHelper.ShowStatusText(player, itemTooltip);
                        }
                    }
                }
            }

            // 미니맵/상태 영역 실시간 RPG HUD 안내 (5초 주기)
            if (PluginMain.Config.EnableHudBroadcast && (DateTime.UtcNow - _lastHudTick).TotalSeconds >= 5)
            {
                _lastHudTick = DateTime.UtcNow;
                foreach (var player in TShock.Players)
                {
                    if (player == null || !player.Active || !player.IsLoggedIn) continue;

                    // 아이템 툴팁 표시 중인 동안에는 정기 HUD 출력을 건너뜀
                    if (player.Index >= 0 && player.Index < Main.maxPlayers && DateTime.UtcNow < _itemTooltipDisplayUntil[player.Index])
                    {
                        continue;
                    }

                    var rpg = PluginMain.RpgService.GetRpgData(player.Account.Name);
                    int lvl = PluginMain.ExpService.GetLevel(player.Account.Name);
                    long curExp = PluginMain.ExpService.GetCurrentLevelExpProgress(player.Account.Name);
                    long reqExp = PluginMain.ExpService.GetExpToNextLevel(lvl);
                    long money = PluginMain.EconomyService.GetBalance(player.Account.Name);
                    var title = PluginMain.TitleService.GetEquippedTitle(player.Account.Name);

                    string titleStr = title != null ? $"[{title.Name}] " : "";
                    string line1 = $"🌱 {titleStr}{player.Name} | {rpg.Job} Lv.{lvl}";
                    string line2 = $"⭐ EXP: {curExp:N0}/{reqExp:N0} | 💰 {money:N0} {PluginMain.Config.CurrencyName}";
                    string hud = $"{line1}\n{line2}";
                    // 미니맵 아래 상태창(StatusText)에만 출력 (채팅창 도배 방지)
                    HudHelper.ShowStatusText(player, hud);
                }
            }
        }

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
                PluginMain.TitleService.IncrementStat(player.Account.Name, "fishing", 1);
                CheckTitleUnlocks(player);

                long exp = fCfg.DefaultExp;
                long money = fCfg.DefaultMoney;

                if (isCrate)
                {
                    exp = (long)(exp * fCfg.CrateExpMultiplier);
                    money = (long)(money * fCfg.CrateMoneyMultiplier);
                }

                var title = PluginMain.TitleService.GetEquippedTitle(player.Account.Name);
                if (title != null)
                {
                    if (title.BonusExpRatio > 0) exp = (long)(exp * (1.0 + title.BonusExpRatio));
                    if (title.BonusMoneyRatio > 0) money = (long)(money * (1.0 + title.BonusMoneyRatio));
                }

                PluginMain.ExpService.AddExp(player.Account.Name, exp, isCrate ? "희귀 상자 낚시 성공" : "물고기 낚시 성공");
                PluginMain.EconomyService.AddBalance(player.Account.Name, money, isCrate ? "희귀 상자 낚시 성공" : "물고기 낚시 성공");

                ShowCombatText(player, $"+{exp} EXP! (+{money}골드)", Color.Cyan);

                if (fCfg.NotifyInChat)
                {
                    string targetType = isCrate ? "🎁 희귀 상자 낚시!" : "🐟 물고기 낚시 성공!";
                    player.SendMessage($"[낚시] {targetType} +{exp:N0} EXP | +{money:N0} {PluginMain.Config.CurrencyName}", Color.Aqua);
                }
            }
        }

        private static void OnTileEdit(object? sender, GetDataHandlers.TileEditEventArgs args)
        {
            var mCfg = PluginMain.Config.Mining;
            if (!mCfg.Enabled || args.Handled) return;

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
                PluginMain.TitleService.IncrementStat(player.Account.Name, "mining", 1);
                CheckTitleUnlocks(player);

                long exp = reward.Exp;
                long money = reward.Money;

                var title = PluginMain.TitleService.GetEquippedTitle(player.Account.Name);
                if (title != null)
                {
                    if (title.BonusExpRatio > 0) exp = (long)(exp * (1.0 + title.BonusExpRatio));
                    if (title.BonusMoneyRatio > 0) money = (long)(money * (1.0 + title.BonusMoneyRatio));
                }

                if (exp > 0)
                {
                    PluginMain.ExpService.AddExp(player.Account.Name, exp, $"광물 채광: {reward.OreName}");
                }
                if (money > 0)
                {
                    PluginMain.EconomyService.AddBalance(player.Account.Name, money, $"광물 채광: {reward.OreName}");
                }

                ShowCombatText(player, $"+{exp} EXP (+{money}골드)", Color.Orange);

                if (mCfg.NotifyInChat)
                {
                    player.SendMessage($"[채광] {reward.OreName}! +{exp} EXP | +{money} {PluginMain.Config.CurrencyName}", Color.Orange);
                }
            }
        }

        private static void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            var cfg = PluginMain.Config.DistanceScaling;
            if (!cfg.Enabled) return;

            int npcIndex = args.NpcId;
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return;

            NPC npc = Main.npc[npcIndex];
            if (npc == null || !npc.active || npc.friendly || npc.boss) return;
            if (PluginMain.Config.BlacklistedNpcNetIds.Contains(npc.netID)) return;

            double npcTileX = npc.position.X / 16.0;
            double npcTileY = npc.position.Y / 16.0;

            TSPlayer? nearbyPlayer = null;
            double minPlayerDistance = double.MaxValue;

            foreach (var p in TShock.Players)
            {
                if (p != null && p.Active && p.IsLoggedIn)
                {
                    double pDist = Math.Sqrt(Math.Pow((p.X / 16.0) - npcTileX, 2) + Math.Pow((p.Y / 16.0) - npcTileY, 2));
                    if (pDist < minPlayerDistance)
                    {
                        minPlayerDistance = pDist;
                        nearbyPlayer = p;
                    }
                }
            }

            if (nearbyPlayer == null) return;

            double serverSpawnTileX = Main.spawnTileX;
            double serverSpawnTileY = Main.spawnTileY;

            double playerTileX = nearbyPlayer.X / 16.0;
            double playerTileY = nearbyPlayer.Y / 16.0;

            double dx = Math.Abs(playerTileX - serverSpawnTileX);
            double dy = Math.Abs(playerTileY - serverSpawnTileY);

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
            double defenseMultiplier = Math.Min(cfg.MaxDefenseMultiplier, 1.0 + (steps * cfg.DefenseIncreasePerStep));

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

            if (defenseMultiplier > 1.0)
            {
                npc.defense = (int)(npc.defense * defenseMultiplier);
            }

            NetMessage.SendData(23, -1, -1, null, npcIndex);
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
            var statCfg = PluginMain.Config.StatDamage;

            // 🌟 무기 타입 판별 (들고 있는 무기 속성 검사)
            Item heldItem = player.TPlayer.HeldItem;
            double statBonusRatio = 0.0;
            string damageCategory = "일반";

            if (heldItem != null && !heldItem.IsAir)
            {
                // 1. 소서러 (마법 무기 또는 마나를 사용하는 무기)
                if (heldItem.magic || heldItem.mana > 0)
                {
                    statBonusRatio = rpg.Sorcerer * statCfg.SorcererDamagePerPoint;
                    damageCategory = "마법";
                }
                // 2. 서머너 (소환 무기 및 센트리)
                else if (heldItem.summon || heldItem.sentry)
                {
                    statBonusRatio = rpg.Summoner * statCfg.SummonerDamagePerPoint;
                    damageCategory = "소환";
                }
                // 3. 레인저 (원거리 물리 무기: 활, 총기 등)
                else if (heldItem.ranged)
                {
                    statBonusRatio = rpg.Ranger * statCfg.RangerDamagePerPoint;
                    damageCategory = "원거리";
                }
                // 4. 워리어 (근접 물리 무기: 검, 창, 도끼 등)
                else if (heldItem.melee || !heldItem.noMelee)
                {
                    statBonusRatio = rpg.Warrior * statCfg.WarriorDamagePerPoint;
                    damageCategory = "근접";
                }
            }

            var title = PluginMain.TitleService.GetEquippedTitle(player.Account.Name);
            if (title != null && title.BonusDamageRatio > 0)
            {
                statBonusRatio += title.BonusDamageRatio;
            }

            int rawBonusDamage = (int)(baseDamage * statBonusRatio);
            int bonusDamage = 0;

            // 🌟 테라리아 표준 방어력 공식 적용 (방어력 감쇄: 클래식 50%, 전문가 75%, 마스터 100%)
            // NPC defense는 Main.GameModeInfo에 따라 방어율이 다르거나 기본 0.5f로 감소
            if (rawBonusDamage > 0)
            {
                float defenseFactor = 0.5f;
                if (Main.masterMode)
                {
                    defenseFactor = 1.0f;
                }
                else if (Main.expertMode)
                {
                    defenseFactor = 0.75f;
                }

                int defenseReduction = (int)Math.Round(npc.defense * defenseFactor);
                // 방어력을 차감하되 최소 1의 추가 데미지는 보장
                bonusDamage = Math.Max(1, rawBonusDamage - defenseReduction);
            }

            // 🌟 실제 몬스터 체력 추가 감소 로직 (방어력 적용된 bonusDamage만큼 체력 차감)
            if (bonusDamage > 0 && npc.life > 0)
            {
                int effectiveBonus = Math.Min(bonusDamage, npc.life);
                npc.life -= effectiveBonus;
                if (npc.life <= 0)
                {
                    npc.life = 0;
                    npc.checkDead();
                }
                // 패킷 23(NpcUpdate)으로 서버가 몬스터 체력 상태를 모든 클라이언트에 동기화
                NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npc.whoAmI);
            }

            // 🌟 추가데미지 플로팅 텍스트 (몬스터 위치에 띄우기, 텍스트는 짧게 + 직업별 컬러로 구분)
            // 전사: 빨간톤 (Red/Crimson), 궁수/레인저: 초록톤 (LimeGreen), 마법사/소서러: 푸른톤 (DeepSkyBlue/Cyan), 서머너: 흰색톤 (White/Silver)
            if (bonusDamage > 0)
            {
                Color damageColor = damageCategory switch
                {
                    "근접" => new Color(255, 75, 75),       // 전사: 빨간톤
                    "원거리" => new Color(75, 255, 100),    // 레인저: 초록톤
                    "마법" => new Color(80, 190, 255),      // 소서러: 푸른톤
                    "소환" => new Color(245, 245, 255),     // 서머너: 흰색톤
                    _ => new Color(255, 200, 80)
                };
                // 몬스터 위치(npc.position.X, npc.position.Y - 10f)에 추가 데미지 숫자 팝업!
                ShowCombatTextAt(npc.position.X + (npc.width / 2f), npc.position.Y - 10f, $"+{bonusDamage}", damageColor);
            }

            int effectiveTotalDamage = Math.Min(baseDamage, Math.Max(1, npc.life));
            var (expRatio, moneyRatio) = GetRewardRatios(npc);

            // 최소 1 이상의 경험치/골드가 들어오도록 보정 (0.02비율로 40 이하 데미지일 때 0이 되는 현상 방지)
            long totalExpGain = Math.Max(1, (long)Math.Ceiling(effectiveTotalDamage * expRatio));
            long totalMoneyGain = Math.Max(1, (long)Math.Ceiling(effectiveTotalDamage * moneyRatio));

            if (title != null)
            {
                if (title.BonusExpRatio > 0) totalExpGain = (long)(totalExpGain * (1.0 + title.BonusExpRatio));
                if (title.BonusMoneyRatio > 0) totalMoneyGain = (long)(totalMoneyGain * (1.0 + title.BonusMoneyRatio));
            }

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
                player.SendMessage($"[전투] +{totalExpGain:N0} EXP | +{totalMoneyGain:N0} {PluginMain.Config.CurrencyName} ({damageCategory} 보너스: +{bonusDamage} dmg)", Color.Yellow);
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

            PluginMain.TitleService.IncrementStat(player.Account.Name, npc.boss ? "boss" : "kill", 1);
            CheckTitleUnlocks(player);

            if (PluginMain.RpgService.ProgressQuest(player.Account.Name, npc.netID, npc.FullName, out bool completed, out long qExp, out long qMoney))
            {
                if (completed)
                {
                    PluginMain.ExpService.AddExp(player.Account.Name, qExp, "퀘스트 완료 보상");
                    PluginMain.EconomyService.AddBalance(player.Account.Name, qMoney, "퀘스트 완료 보상");
                    player.SendSuccessMessage($"🎉 [퀘스트 완료!] 토벌 목표를 달성했습니다! (+{qExp:N0} EXP, +{qMoney:N0} {PluginMain.Config.CurrencyName})");
                    ShowCombatText(player, "퀘스트 완료! +보상", Color.Gold);
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

            // 🌟 2. 몬스터 처치 시 골드/경험치 플로팅 텍스트 팝업 (나구 요청: 잡았을 때 띄우기)
            // 총 지급 경험치/골드 계산 (기본 처치 보너스 + 몬스터 스펙 기반)
            long killExp = bonusExp;
            long killMoney = bonusMoney;

            if (killExp == 0)
            {
                killExp = Math.Max(5, (long)(npc.lifeMax * 0.15));
            }
            if (killMoney == 0)
            {
                killMoney = Math.Max(2, (long)(npc.lifeMax * 0.08));
            }

            var title = PluginMain.TitleService.GetEquippedTitle(player.Account.Name);
            if (title != null)
            {
                if (title.BonusExpRatio > 0) killExp = (long)(killExp * (1.0 + title.BonusExpRatio));
                if (title.BonusMoneyRatio > 0) killMoney = (long)(killMoney * (1.0 + title.BonusMoneyRatio));
            }

            if (bonusExp == 0 && killExp > 0)
            {
                PluginMain.ExpService.AddExp(player.Account.Name, killExp, $"처치 보너스: {npc.FullName}");
            }
            if (bonusMoney == 0 && killMoney > 0)
            {
                PluginMain.EconomyService.AddBalance(player.Account.Name, killMoney, $"처치 보너스: {npc.FullName}");
            }

            // 플레이어 머리 위에 EXP와 골드 처치 보상 팝업! (골드: 황금색, EXP: 밝은 청록색)
            ShowCombatText(player, $"+{killExp} EXP (+{killMoney}G)", Color.Gold);

            if (npc.boss)
            {
                TShock.Utils.Broadcast($"[보스 토벌] {player.Name} 님이 {npc.FullName}을(를) 토벌했습니다! (보너스: +{killExp:N0} EXP, +{killMoney:N0} {PluginMain.Config.CurrencyName})", Color.Gold);
                ShowCombatText(player, "BOSS KILLED!", Color.Gold);
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
