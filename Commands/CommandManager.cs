using Terraria;
using TShockAPI;

namespace TShockEconomyExp.Commands
{
    public static class CommandManager
    {
        public static void RegisterCommands()
        {
            // ================= 1. 경제 명령어 =================
            TShockAPI.Commands.ChatCommands.Add(new Command("economy.user", BalanceCommand, "돈", "bal", "balance")
            {
                HelpText = "현재 소지하고 있는 돈을 확인합니다. (/돈 [유저이름])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("economy.user", PayCommand, "송금", "pay")
            {
                HelpText = "다른 유저에게 돈을 보냅니다. (/송금 [유저이름] [금액])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("economy.admin", GiveMoneyCommand, "돈지급", "givemoney")
            {
                HelpText = "[관리자] 유저에게 돈을 지급합니다. (/돈지급 [유저이름] [금액])"
            });

            // ================= 2. 경험치 & 레벨 명령어 =================
            TShockAPI.Commands.ChatCommands.Add(new Command("exp.user", ExpCommand, "레벨", "경험치", "lvl", "level", "exp")
            {
                HelpText = "현재 레벨과 경험치 정보를 확인합니다. (/레벨 [유저이름])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("exp.user", RankCommand, "랭킹", "순위", "top", "rank")
            {
                HelpText = "서버 내 레벨 랭킹 TOP 10을 확인합니다."
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("exp.admin", GiveExpCommand, "경험치지급", "giveexp")
            {
                HelpText = "[관리자] 유저에게 경험치를 지급합니다. (/경험치지급 [유저이름] [경험치])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("exp.admin", SetLevelCommand, "레벨설정", "setlevel")
            {
                HelpText = "[관리자] 유저의 레벨을 강제 설정합니다. (/레벨설정 [유저이름] [레벨])"
            });

            // ================= 3. RPG 직업 & 스탯 시스템 명령어 =================
            TShockAPI.Commands.ChatCommands.Add(new Command("rpg.user", JobCommand, "직업", "전직", "job")
            {
                HelpText = "현재 직업을 확인하거나 전직합니다. (/직업 [전사|궁수|마법사|소환사])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("rpg.user", StatCommand, "스탯", "정보", "status", "stat")
            {
                HelpText = "현재 나의 스탯(힘/민첩/지능/체력)과 남은 포인트를 확인합니다."
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("rpg.user", AllocateStatCommand, "스탯분배", "스탯투자", "addstat")
            {
                HelpText = "스탯 포인트를 분배합니다. (/스탯분배 [힘|민첩|지능|체력] [수량])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("rpg.user", ResetStatCommand, "스탯초기화", "resetstat")
            {
                HelpText = "스탯을 초기화하고 모든 포인트를 반환받습니다."
            });

            // ================= 4. 일일 퀘스트 시스템 명령어 =================
            TShockAPI.Commands.ChatCommands.Add(new Command("rpg.user", QuestCommand, "퀘스트", "일일퀘스트", "quest")
            {
                HelpText = "현재 퀘스트를 확인하거나 새로운 퀘스트를 수락합니다. (/퀘스트 받기|포기)"
            });

            // ================= 5. 인게임 상점 시스템 명령어 =================
            TShockAPI.Commands.ChatCommands.Add(new Command("shop.user", ShopListCommand, "상점", "상점목록", "shop")
            {
                HelpText = "상점 아이템 목록을 조회합니다. (/상점 [카테고리] [페이지])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("shop.user", ShopBuyCommand, "구매", "buy")
            {
                HelpText = "상점에서 아이템을 구매합니다. (/구매 [아이템이름] [수량])"
            });

            TShockAPI.Commands.ChatCommands.Add(new Command("shop.user", ShopSellCommand, "판매", "sell")
            {
                HelpText = "손에 들고 있는 아이템이나 지정한 아이템을 상점에 판매합니다. (/판매 [아이템이름] [수량])"
            });
        }

        #region Economy Handlers

        private static void BalanceCommand(CommandArgs args)
        {
            string targetAccount;
            if (args.Parameters.Count > 0)
            {
                targetAccount = args.Parameters[0];
            }
            else
            {
                if (args.Player == null || !args.Player.IsLoggedIn)
                {
                    args.Player?.SendErrorMessage("로그인 상태에서만 본인의 잔액을 확인할 수 있습니다.");
                    return;
                }
                targetAccount = args.Player.Account.Name;
            }

            long balance = PluginMain.EconomyService.GetBalance(targetAccount);
            string curName = PluginMain.Config.CurrencyName;

            if (args.Player != null && args.Player.IsLoggedIn && targetAccount.Equals(args.Player.Account.Name, StringComparison.OrdinalIgnoreCase))
            {
                args.Player.SendInfoMessage($"[경제] 내 잔액: {balance:N0} {curName}");
            }
            else
            {
                args.Player?.SendInfoMessage($"[경제] '{targetAccount}' 님의 잔액: {balance:N0} {curName}");
            }
        }

        private static void PayCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후에 송금할 수 있습니다.");
                return;
            }

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("사용법: /송금 [받는사람] [금액]");
                return;
            }

            string receiver = args.Parameters[0];
            if (!long.TryParse(args.Parameters[1], out long amount) || amount <= 0)
            {
                args.Player.SendErrorMessage("올바른 송금 금액을 입력해주세요.");
                return;
            }

            string sender = args.Player.Account.Name;
            if (sender.Equals(receiver, StringComparison.OrdinalIgnoreCase))
            {
                args.Player.SendErrorMessage("본인에게는 송금할 수 없습니다.");
                return;
            }

            if (!PluginMain.EconomyService.Transfer(sender, receiver, amount))
            {
                args.Player.SendErrorMessage("잔액이 부족하거나 송금에 실패했습니다.");
                return;
            }

            string curName = PluginMain.Config.CurrencyName;
            args.Player.SendSuccessMessage($"[송금 성공] {receiver} 님에게 {amount:N0} {curName}을 보냈습니다.");

            var targetPlayer = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Account.Name.Equals(receiver, StringComparison.OrdinalIgnoreCase));
            targetPlayer?.SendSuccessMessage($"[입금 알림] {sender} 님으로부터 {amount:N0} {curName}이 입금되었습니다!");
        }

        private static void GiveMoneyCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player?.SendErrorMessage("사용법: /돈지급 [유저이름] [금액]");
                return;
            }

            string target = args.Parameters[0];
            if (!long.TryParse(args.Parameters[1], out long amount) || amount <= 0)
            {
                args.Player?.SendErrorMessage("올바른 금액을 입력해주세요.");
                return;
            }

            PluginMain.EconomyService.AddBalance(target, amount, "관리자 지급");
            string curName = PluginMain.Config.CurrencyName;
            args.Player?.SendSuccessMessage($"[관리자] {target} 님에게 {amount:N0} {curName}을 지급했습니다.");
        }

        #endregion

        #region Exp Handlers

        private static void ExpCommand(CommandArgs args)
        {
            string targetAccount;
            if (args.Parameters.Count > 0)
            {
                targetAccount = args.Parameters[0];
            }
            else
            {
                if (args.Player == null || !args.Player.IsLoggedIn)
                {
                    args.Player?.SendErrorMessage("로그인 상태에서만 본인의 레벨 정보를 확인할 수 있습니다.");
                    return;
                }
                targetAccount = args.Player.Account.Name;
            }

            int level = PluginMain.ExpService.GetLevel(targetAccount);
            long totalExp = PluginMain.ExpService.GetTotalExp(targetAccount);
            long curProgress = PluginMain.ExpService.GetCurrentLevelExpProgress(targetAccount);
            long reqExp = PluginMain.ExpService.GetExpToNextLevel(level);

            string targetName = targetAccount;
            if (args.Player != null && args.Player.IsLoggedIn && targetAccount.Equals(args.Player.Account.Name, StringComparison.OrdinalIgnoreCase))
            {
                targetName = "내";
            }

            args.Player?.SendInfoMessage($"====== [{targetName} 레벨 정보] ======");
            args.Player?.SendInfoMessage($"⭐ 레벨: Lv.{level} (최대 Lv.{PluginMain.Config.MaxLevel})");
            if (level >= PluginMain.Config.MaxLevel)
            {
                args.Player?.SendSuccessMessage($"누적 경험치: {totalExp:N0} EXP (MAX LEVEL)");
            }
            else
            {
                double percent = reqExp > 0 ? (double)curProgress / reqExp * 100.0 : 0.0;
                args.Player?.SendInfoMessage($"경험치: {curProgress:N0} / {reqExp:N0} EXP ({percent:F1}%) | 누적: {totalExp:N0} EXP");
            }
        }

        private static void RankCommand(CommandArgs args)
        {
            var topList = PluginMain.ExpService.GetTopRankings(10).ToList();
            if (topList.Count == 0)
            {
                args.Player?.SendInfoMessage("현재 등록된 랭킹 데이터가 없습니다.");
                return;
            }

            args.Player?.SendInfoMessage("====== [서버 레벨 랭킹 TOP 10] ======");
            int rank = 1;
            foreach (var item in topList)
            {
                string medal = rank switch
                {
                    1 => "🥇 1위",
                    2 => "🥈 2위",
                    3 => "🥉 3위",
                    _ => $"   {rank}위"
                };
                args.Player?.SendInfoMessage($"{medal} : {item.AccountName} (Lv.{item.Level} / {item.TotalExp:N0} EXP)");
                rank++;
            }
        }

        private static void GiveExpCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player?.SendErrorMessage("사용법: /경험치지급 [유저이름] [경험치]");
                return;
            }

            string target = args.Parameters[0];
            if (!long.TryParse(args.Parameters[1], out long amount) || amount <= 0)
            {
                args.Player?.SendErrorMessage("올바른 경험치를 입력해주세요.");
                return;
            }

            PluginMain.ExpService.AddExp(target, amount, "관리자 지급");
            args.Player?.SendSuccessMessage($"[관리자] {target} 님에게 {amount:N0} EXP를 지급했습니다.");
        }

        private static void SetLevelCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player?.SendErrorMessage("사용법: /레벨설정 [유저이름] [레벨]");
                return;
            }

            string target = args.Parameters[0];
            if (!int.TryParse(args.Parameters[1], out int level) || level <= 0)
            {
                args.Player?.SendErrorMessage("올바른 레벨(1 이상)을 입력해주세요.");
                return;
            }

            PluginMain.ExpService.SetLevel(target, level);
            args.Player?.SendSuccessMessage($"[관리자] {target} 님의 레벨을 Lv.{level}로 설정했습니다.");
        }

        #endregion

        #region RPG Handlers (Job & Stats)

        private static void JobCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            var rpg = PluginMain.RpgService.GetRpgData(args.Player.Account.Name);

            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage($"[직업] 현재 직업: {rpg.Job}");
                args.Player.SendInfoMessage($"전직 가능한 직업: {string.Join(", ", Services.RpgService.AvailableJobs)}");
                args.Player.SendInfoMessage("변경 방법: /직업 [직업이름]");
                return;
            }

            string targetJob = args.Parameters[0];
            if (!PluginMain.RpgService.ChangeJob(args.Player.Account.Name, targetJob))
            {
                args.Player.SendErrorMessage($"존재하지 않는 직업입니다. ({string.Join(", ", Services.RpgService.AvailableJobs)})");
                return;
            }

            args.Player.SendSuccessMessage($"✨ 직업이 '{targetJob}'(으)로 변경되었습니다!");
            TShock.Utils.Broadcast($"[전직] {args.Player.Name} 님이 {targetJob}(으)로 전직했습니다!", Microsoft.Xna.Framework.Color.Aquamarine);
        }

        private static void StatCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            var rpg = PluginMain.RpgService.GetRpgData(args.Player.Account.Name);
            int level = PluginMain.ExpService.GetLevel(args.Player.Account.Name);

            args.Player.SendInfoMessage($"====== [{args.Player.Name} RPG 캐릭터 정보] ======");
            args.Player.SendInfoMessage($"🗡️ 직업: {rpg.Job} | ⭐ 레벨: Lv.{level}");
            args.Player.SendInfoMessage($"💪 힘(STR): {rpg.Strength} (공격력 +{(rpg.Strength * 1.5):F1}%)");
            args.Player.SendInfoMessage($"🏹 민첩(DEX): {rpg.Dexterity} (공격력 +{(rpg.Dexterity * 1.5):F1}%)");
            args.Player.SendInfoMessage($"🔮 지능(INT): {rpg.Intelligence} (공격력 +{(rpg.Intelligence * 1.5):F1}%)");
            args.Player.SendInfoMessage($"❤️ 체력(VIT): {rpg.Vitality}");
            args.Player.SendSuccessMessage($"🔥 보유 스탯 포인트: {rpg.StatPoints} 포인트");
            args.Player.SendInfoMessage("포인트 투자: /스탯분배 [힘|민첩|지능|체력] [수량] | 초기화: /스탯초기화");
        }

        private static void AllocateStatCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("사용법: /스탯분배 [힘|민첩|지능|체력] [투자할포인트]");
                return;
            }

            string statName = args.Parameters[0];
            if (!int.TryParse(args.Parameters[1], out int amount) || amount <= 0)
            {
                args.Player.SendErrorMessage("올바른 수량을 입력해주세요.");
                return;
            }

            if (!PluginMain.RpgService.AllocateStat(args.Player.Account.Name, statName, amount))
            {
                args.Player.SendErrorMessage("보유 스탯 포인트가 부족하거나 올바르지 않은 스탯 이름입니다.");
                return;
            }

            args.Player.SendSuccessMessage($"[성공] {statName} 스탯에 {amount} 포인트를 투자했습니다!");
        }

        private static void ResetStatCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            if (!PluginMain.RpgService.ResetStats(args.Player.Account.Name))
            {
                args.Player.SendErrorMessage("초기화할 스탯이 없습니다.");
                return;
            }

            args.Player.SendSuccessMessage("[초기화 완료] 모든 스탯이 초기화되고 포인트로 환급되었습니다.");
        }

        #endregion

        #region Quest Handlers

        private static void QuestCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            string sub = args.Parameters.Count > 0 ? args.Parameters[0] : "";
            var rpg = PluginMain.RpgService.GetRpgData(args.Player.Account.Name);

            if (sub == "받기" || sub == "수락" || sub == "get")
            {
                if (rpg.QuestRequiredCount > 0)
                {
                    args.Player.SendErrorMessage($"이미 진행 중인 퀘스트가 있습니다! ({rpg.ActiveQuestTarget} 처치: {rpg.QuestCurrentCount}/{rpg.QuestRequiredCount})");
                    return;
                }

                int level = PluginMain.ExpService.GetLevel(args.Player.Account.Name);
                PluginMain.RpgService.AssignRandomQuest(args.Player.Account.Name, level);
                rpg = PluginMain.RpgService.GetRpgData(args.Player.Account.Name);

                args.Player.SendSuccessMessage($"📜 [새 퀘스트 수락!] 목표: {rpg.ActiveQuestTarget} {rpg.QuestRequiredCount}마리 처치");
                args.Player.SendInfoMessage($"보상: +{rpg.QuestRewardExp:N0} EXP, +{rpg.QuestRewardMoney:N0} {PluginMain.Config.CurrencyName}");
                return;
            }

            if (sub == "포기" || sub == "cancel")
            {
                if (rpg.QuestRequiredCount == 0)
                {
                    args.Player.SendErrorMessage("진행 중인 퀘스트가 없습니다.");
                    return;
                }

                rpg.QuestRequiredCount = 0;
                rpg.ActiveQuestTarget = "";
                PluginMain.Database.SaveRpg(rpg);
                args.Player.SendSuccessMessage("진행 중이던 퀘스트를 포기했습니다.");
                return;
            }

            // 현재 퀘스트 정보 출력
            if (rpg.QuestRequiredCount > 0)
            {
                args.Player.SendInfoMessage($"====== [진행 중인 일일 퀘스트] ======");
                args.Player.SendInfoMessage($"🎯 목표: {rpg.ActiveQuestTarget} ({rpg.QuestCurrentCount}/{rpg.QuestRequiredCount})");
                args.Player.SendInfoMessage($"🎁 보상: +{rpg.QuestRewardExp:N0} EXP | +{rpg.QuestRewardMoney:N0} {PluginMain.Config.CurrencyName}");
                args.Player.SendInfoMessage("포기하려면: /퀘스트 포기");
            }
            else
            {
                args.Player.SendInfoMessage("현재 진행 중인 퀘스트가 없습니다. /퀘스트 받기 로 새 퀘스트를 수락하세요!");
            }
        }

        #endregion

        #region Shop Handlers

        private static void ShopListCommand(CommandArgs args)
        {
            if (!PluginMain.ShopConfig.EnableShop)
            {
                args.Player?.SendErrorMessage("상점 기능이 현재 비활성화되어 있습니다.");
                return;
            }

            var items = PluginMain.ShopConfig.Items;
            args.Player?.SendInfoMessage($"====== [🏪 나물 RPG 상점 목록] ======");
            foreach (var item in items)
            {
                string buyStr = item.BuyPrice > 0 ? $"{item.BuyPrice:N0}원" : "구매불가";
                string sellStr = item.SellPrice > 0 ? $"{item.SellPrice:N0}원" : "판매불가";
                args.Player?.SendInfoMessage($"[{item.Category}] {item.Name} - 구매: {buyStr} | 판매: {sellStr}");
            }
            args.Player?.SendInfoMessage("구매: /구매 [이름] [수량] | 판매: /판매 [이름] [수량]");
        }

        private static void ShopBuyCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("사용법: /구매 [아이템이름] [수량]");
                return;
            }

            string itemName = args.Parameters[0];
            int count = 1;
            if (args.Parameters.Count >= 2 && (!int.TryParse(args.Parameters[1], out count) || count <= 0))
            {
                args.Player.SendErrorMessage("올바른 수량을 입력해주세요.");
                return;
            }

            var item = PluginMain.ShopConfig.Items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) || i.NetId.ToString() == itemName);
            if (item == null || item.BuyPrice <= 0)
            {
                args.Player.SendErrorMessage($"상점에서 판매하지 않는 아이템입니다: {itemName}");
                return;
            }

            long totalPrice = item.BuyPrice * count;
            if (!PluginMain.EconomyService.ProcessPurchase(args.Player.Account.Name, totalPrice, item.Name, count))
            {
                args.Player.SendErrorMessage($"잔액이 부족합니다! (필요 금액: {totalPrice:N0} {PluginMain.Config.CurrencyName})");
                return;
            }

            // 인게임 인벤토리에 아이템 지급
            args.Player.GiveItem(item.NetId, count);
            args.Player.SendSuccessMessage($"[상점] '{item.Name}' x{count}개를 {totalPrice:N0} {PluginMain.Config.CurrencyName}에 구매했습니다!");
        }

        private static void ShopSellCommand(CommandArgs args)
        {
            if (args.Player == null || !args.Player.IsLoggedIn)
            {
                args.Player?.SendErrorMessage("로그인 후 이용할 수 있습니다.");
                return;
            }

            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("사용법: /판매 [아이템이름] [수량]");
                return;
            }

            string itemName = args.Parameters[0];
            int count = 1;
            if (args.Parameters.Count >= 2 && (!int.TryParse(args.Parameters[1], out count) || count <= 0))
            {
                args.Player.SendErrorMessage("올바른 수량을 입력해주세요.");
                return;
            }

            var shopItem = PluginMain.ShopConfig.Items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) || i.NetId.ToString() == itemName);
            if (shopItem == null || shopItem.SellPrice <= 0)
            {
                args.Player.SendErrorMessage($"상점에 판매할 수 없는 아이템입니다: {itemName}");
                return;
            }

            // 플레이어 인벤토리에서 아이템 수량 확인
            var tPlayer = args.Player.TPlayer;
            int totalFound = 0;
            for (int i = 0; i < 58; i++)
            {
                var invItem = tPlayer.inventory[i];
                if (invItem != null && invItem.type == shopItem.NetId && invItem.stack > 0)
                {
                    totalFound += invItem.stack;
                }
            }

            if (totalFound < count)
            {
                args.Player.SendErrorMessage($"인벤토리에 '{shopItem.Name}' 아이템이 부족합니다! (보유: {totalFound}개 / 요청: {count}개)");
                return;
            }

            // 인벤토리에서 차감
            int remainingToRemove = count;
            for (int i = 0; i < 58; i++)
            {
                var invItem = tPlayer.inventory[i];
                if (invItem != null && invItem.type == shopItem.NetId && invItem.stack > 0)
                {
                    int take = Math.Min(invItem.stack, remainingToRemove);
                    invItem.stack -= take;
                    remainingToRemove -= take;

                    if (invItem.stack <= 0)
                    {
                        invItem.TurnToAir();
                    }

                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i);
                    if (remainingToRemove <= 0) break;
                }
            }

            long totalReward = shopItem.SellPrice * count;
            PluginMain.EconomyService.ProcessSale(args.Player.Account.Name, totalReward, shopItem.Name, count);
            args.Player.SendSuccessMessage($"[상점] '{shopItem.Name}' x{count}개를 판매하여 {totalReward:N0} {PluginMain.Config.CurrencyName}을 획득했습니다!");
        }

        #endregion
    }
}
