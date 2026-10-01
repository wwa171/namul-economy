using System;
using System.IO;
using Terraria;
using Terraria.Localization;
using TShockAPI;

namespace TShockEconomyExp.Handlers
{
    public static class HudHelper
    {
        /// <summary>
        /// 미니맵 아래 상태 메시지(StatusText, 패킷 9) 출력
        /// 테라리아 UI 구조상 StatusText는 화면 우측 상단 체력바/미니맵 근처에 렌더링되므로,
        /// 앞에 줄바꿈(\n)을 넉넉히 주어 미니맵 바로 아래(초록색 박스 위치)로 내려오도록 보정합니다.
        /// </summary>
        public static void ShowStatusText(TSPlayer player, string text)
        {
            if (player == null || !player.Active) return;
            try
            {
                // 줄바꿈 5줄 추가로 미니맵 아래로 위치 조정
                string adjustedText = $"\n\n\n\n\n{text}";
                var netText = NetworkText.FromLiteral(adjustedText);
                NetMessage.SendData((int)PacketTypes.Status, player.Index, -1, netText, 0, 0f, 0f, 0f, 0, 0, 0);
            }
            catch
            {
            }
        }
    }
}
