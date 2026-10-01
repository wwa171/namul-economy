using System;
using System.IO;
using Terraria;
using Terraria.Localization;
using TShockAPI;

namespace TShockEconomyExp.Handlers
{
    public static class HudHelper
    {
        private static readonly bool[] _isPlayerHudVisible = new bool[Main.maxPlayers];

        static HudHelper()
        {
            // 기본값: 모든 플레이어 HUD 활성화
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                _isPlayerHudVisible[i] = true;
            }
        }

        public static bool IsHudVisible(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return false;
            return _isPlayerHudVisible[playerIndex];
        }

        public static void SetHudVisible(int playerIndex, bool visible)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;
            _isPlayerHudVisible[playerIndex] = visible;
        }

        public static bool ToggleHud(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return false;
            _isPlayerHudVisible[playerIndex] = !_isPlayerHudVisible[playerIndex];
            return _isPlayerHudVisible[playerIndex];
        }

        /// <summary>
        /// 미니맵 아래 상태 메시지(StatusText, 패킷 9) 출력
        /// 테라리아 UI 구조상 StatusText는 화면 우측 상단 체력바/미니맵 근처에 렌더링되므로,
        /// 앞에 줄바꿈(\n)을 넉넉히 주어 미니맵 바로 아래(초록색 박스 위치)로 내려오도록 보정합니다.
        /// UnrealMultiple의 StatusTextManager와 동일하게 0x1f(HideStatusTextPercent) 플래그를 적용하여 로딩 퍼센트 숫자가 겹치지 않게 클리어합니다.
        /// </summary>
        public static void ShowStatusText(TSPlayer player, string text)
        {
            if (player == null || !player.Active) return;
            if (!IsHudVisible(player.Index)) return;

            try
            {
                // 줄바꿈 10줄 추가로 미니맵 하단으로 위치 조정
                string adjustedText = $"\n\n\n\n\n\n\n\n\n\n{text}";
                var netText = NetworkText.FromLiteral(adjustedText);
                // 4번째 인자(number2): 0x1f -> HideStatusTextPercent (백분율/숫자 가림)
                NetMessage.SendData((int)PacketTypes.Status, player.Index, -1, netText, 0, 0x1f, 0f, 0f, 0, 0, 0);
            }
            catch
            {
            }
        }

        /// <summary>
        /// HUD 텍스트를 즉시 지웁니다 (0x1f 빈 문자열 전송)
        /// </summary>
        public static void ClearStatusText(TSPlayer player)
        {
            if (player == null || !player.Active) return;
            try
            {
                var empty = NetworkText.FromLiteral("");
                NetMessage.SendData((int)PacketTypes.Status, player.Index, -1, empty, 0, 0x1f, 0f, 0f, 0, 0, 0);
            }
            catch
            {
            }
        }
    }
}
