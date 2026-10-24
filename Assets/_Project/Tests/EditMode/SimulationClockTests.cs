using NUnit.Framework;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class SimulationClockTests
    {
        [Test]
        public void Clock_InitializesWithDefaultFiveMinutes()
        {
            var clock = new SimulationClockModel();

            Assert.AreEqual(300f, clock.Duration, 0.001f);
            Assert.AreEqual(0f, clock.ElapsedSeconds, 0.001f);
            Assert.AreEqual(300f, clock.RemainingSeconds, 0.001f);
            Assert.AreEqual(1, clock.LoopCount);
            Assert.AreEqual(1.0f, clock.TimeScale, 0.001f);
            Assert.IsFalse(clock.IsPaused);
            Assert.AreEqual("00:00", clock.GetFormattedTime());
            Assert.AreEqual("05:00", clock.GetFormattedRemainingTime());
        }

        [Test]
        public void Clock_TickAdvancesElapsedTimeAndDecreasesRemainingTime()
        {
            var clock = new SimulationClockModel(300f);

            clock.Tick(30f);

            Assert.AreEqual(30f, clock.ElapsedSeconds, 0.001f);
            Assert.AreEqual(270f, clock.RemainingSeconds, 0.001f);
            Assert.AreEqual("00:30", clock.GetFormattedTime());
            Assert.AreEqual("04:30", clock.GetFormattedRemainingTime());
            Assert.AreEqual(0.1f, clock.NormalizedProgress, 0.001f);
        }

        [Test]
        public void Clock_PausedClockDoesNotAdvanceOnTick()
        {
            var clock = new SimulationClockModel(300f);
            clock.SetPaused(true);

            clock.Tick(15f);

            Assert.AreEqual(0f, clock.ElapsedSeconds, 0.001f);
            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void Clock_TimeScaleAcceleratesProgression()
        {
            var clock = new SimulationClockModel(300f);
            clock.SetTimeScale(2.0f);

            clock.Tick(10f); // 10 real seconds * 2x = 20 sim seconds

            Assert.AreEqual(20f, clock.ElapsedSeconds, 0.001f);
            Assert.AreEqual(280f, clock.RemainingSeconds, 0.001f);
        }

        [Test]
        public void Clock_LoopRolloverOccursAtDuration()
        {
            var clock = new SimulationClockModel(300f);
            bool loopCompletedFired = false;
            int completedLoopIndex = 0;

            clock.OnLoopCompleted += (idx) =>
            {
                loopCompletedFired = true;
                completedLoopIndex = idx;
            };

            clock.Tick(300f);

            Assert.IsTrue(loopCompletedFired, "OnLoopCompleted should fire when duration is reached.");
            Assert.AreEqual(1, completedLoopIndex, "Completed loop should be 1.");
            Assert.AreEqual(2, clock.LoopCount, "New active loop should be 2.");
            Assert.AreEqual(0f, clock.ElapsedSeconds, 0.001f, "Elapsed seconds should reset to 0 for the new loop.");
            Assert.AreEqual(300f, clock.RemainingSeconds, 0.001f);
        }

        [Test]
        public void Clock_ManualTurnoverResetsElapsedAndIncrementsLoop()
        {
            var clock = new SimulationClockModel(300f);
            clock.Tick(120f);

            clock.TriggerLoopTurnover();

            Assert.AreEqual(2, clock.LoopCount);
            Assert.AreEqual(0f, clock.ElapsedSeconds, 0.001f);
        }
    }
}
