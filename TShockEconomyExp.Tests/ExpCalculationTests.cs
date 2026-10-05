using Xunit;
using TShockEconomyExp.Config;
using TShockEconomyExp.Services;

namespace TShockEconomyExp.Tests
{
    public class ExpCalculationTests
    {
        [Fact]
        public void ExpToNextLevel_AtLevel1_ReturnsBaseExp()
        {
            var config = new PluginConfig
            {
                BaseExpRequirement = 100,
                ExpRequirementMultiplier = 1.5,
                MaxLevel = 100
            };
            var service = new ExpService(null!, config);

            // 100 * (1 ^ 1.5) = 100
            long req = service.GetExpToNextLevel(1);
            Assert.Equal(100, req);
        }

        [Fact]
        public void ExpToNextLevel_AtMaxLevel_ReturnsZero()
        {
            var config = new PluginConfig
            {
                BaseExpRequirement = 100,
                ExpRequirementMultiplier = 1.5,
                MaxLevel = 50
            };
            var service = new ExpService(null!, config);

            long req = service.GetExpToNextLevel(50);
            Assert.Equal(0, req);
        }

        [Fact]
        public void ExpToNextLevel_Fails_WhenInvalidLevelRequested()
        {
            var config = new PluginConfig
            {
                BaseExpRequirement = 100,
                ExpRequirementMultiplier = 1.5,
                MaxLevel = 100
            };
            var service = new ExpService(null!, config);

            // TDD Red test: 레벨이 0 이하일 때 ArgumentOutOfRangeException을 던져야 함 (현재는 미구현이라 실패 예정)
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetExpToNextLevel(0));
        }
    }
}
