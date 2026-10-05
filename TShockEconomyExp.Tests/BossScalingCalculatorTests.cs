using Xunit;
using TShockEconomyExp.Services;

namespace TShockEconomyExp.Tests
{
    public class BossScalingCalculatorTests
    {
        [Fact]
        public void Calculate_Level10_CalculatesExpectedHealth()
        {
            // base: 10000, level: 10, perLevel: 0.1, max: 5.0
            // bonus: 10 * 0.1 = 1.0 -> total: 2.0 -> scaled: 20000
            var result = BossScalingCalculator.Calculate(10000, 10, 0.1, 5.0);

            Assert.Equal(20000, result.ScaledLife);
            Assert.Equal(2.0, result.Multiplier);
        }

        [Fact]
        public void Calculate_ExceedingMaxMultiplier_CapsAtMax()
        {
            // base: 10000, level: 100, perLevel: 0.1, max: 3.0
            // bonus: 10.0 -> total 11.0 -> capped at 3.0 -> scaled: 30000
            var result = BossScalingCalculator.Calculate(10000, 100, 0.1, 3.0);

            Assert.Equal(30000, result.ScaledLife);
            Assert.Equal(3.0, result.Multiplier);
        }

        [Fact]
        public void Calculate_InvalidBaseLife_ReturnsZero()
        {
            var result = BossScalingCalculator.Calculate(0, 10, 0.1, 5.0);
            Assert.Equal(0, result.ScaledLife);
        }
    }
}
