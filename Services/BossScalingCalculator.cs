using System;

namespace TShockEconomyExp.Services
{
    public static class BossScalingCalculator
    {
        public static (int ScaledLife, double Multiplier) Calculate(int baseLife, int summonerLevel, double increasePerLevel, double maxMultiplier)
        {
            if (baseLife <= 0) return (0, 1.0);
            if (summonerLevel <= 0) summonerLevel = 1;

            double bonusMultiplier = summonerLevel * increasePerLevel;
            double totalMultiplier = Math.Min(maxMultiplier, 1.0 + bonusMultiplier);
            int scaledLife = (int)(baseLife * totalMultiplier);

            return (scaledLife, totalMultiplier);
        }
    }
}
