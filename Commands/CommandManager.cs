using TShockAPI;

namespace TShockEconomyExp.Commands
{
    public static class CommandManager
    {
        public static void RegisterCommands()
        {
            // 경제 관련 명령어
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

            // 경험치 & 레벨 관련 명령어
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
    }
}
