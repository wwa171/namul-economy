using Xunit;
using TShockEconomyExp.Database;
using TShockEconomyExp.Database.Models;
using TShockEconomyExp.Services;

namespace TShockEconomyExp.Tests
{
    public class RpgStatAllocationTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly DatabaseManager _db;
        private readonly RpgService _rpgService;

        public RpgStatAllocationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TShockTests_" + Guid.NewGuid().ToString("N"));
            _db = new DatabaseManager(_tempDir);
            _db.Initialize();
            _rpgService = new RpgService(_db);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // ignore temp cleanup error
            }
        }

        [Fact]
        public void AllocateStat_Succeeds_WhenJobMatchesAndPointsSufficient()
        {
            string user = "TestWarrior";
            _rpgService.ChangeJob(user, "전사");
            _rpgService.AddStatPoints(user, 10);

            bool success = _rpgService.AllocateStat(user, "워리어", 5, out string error);

            Assert.True(success);
            Assert.Empty(error);

            var data = _rpgService.GetRpgData(user);
            Assert.Equal(5, data.Warrior);
            Assert.Equal(5, data.StatPoints);
        }

        [Fact]
        public void AllocateStat_Fails_WhenJobMismatch()
        {
            string user = "TestRanger";
            _rpgService.ChangeJob(user, "궁수");
            _rpgService.AddStatPoints(user, 10);

            // 궁수 직업인데 워리어 스탯 투자 시도
            bool success = _rpgService.AllocateStat(user, "워리어", 5, out string error);

            Assert.False(success);
            Assert.Contains("[스탯 제한]", error);

            var data = _rpgService.GetRpgData(user);
            Assert.Equal(0, data.Warrior);
            Assert.Equal(10, data.StatPoints);
        }

        [Fact]
        public void AllocateStat_Fails_WhenPointsInsufficient()
        {
            string user = "TestMage";
            _rpgService.ChangeJob(user, "마법사");
            _rpgService.AddStatPoints(user, 2);

            bool success = _rpgService.AllocateStat(user, "소서러", 5, out string error);

            Assert.False(success);
            Assert.Contains("포인트가 부족합니다", error);

            var data = _rpgService.GetRpgData(user);
            Assert.Equal(0, data.Sorcerer);
            Assert.Equal(2, data.StatPoints);
        }

        [Fact]
        public void ResetStats_RefundsAllAllocatedPoints()
        {
            string user = "TestSummoner";
            _rpgService.ChangeJob(user, "소환사");
            _rpgService.AddStatPoints(user, 20);
            _rpgService.AllocateStat(user, "서머너", 15, out _);

            bool resetResult = _rpgService.ResetStats(user);

            Assert.True(resetResult);
            var data = _rpgService.GetRpgData(user);
            Assert.Equal(0, data.Summoner);
            Assert.Equal(20, data.StatPoints);
        }
    }
}
