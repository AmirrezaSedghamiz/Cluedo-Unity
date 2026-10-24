using System;
using System.Collections.Generic;
using System.Linq;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Pure, deterministic evaluator for NPC schedules.
    /// Decoupled from MonoBehaviour and UnityEngine scene dependencies to enable comprehensive EditMode unit testing.
    /// 
    /// Boundary Policy:
    /// - Evaluation operates on the half-open interval [StartTime, EndTime).
    /// - At exact StartTime, the activity becomes active.
    /// - At EndTime, the activity transitions to the next scheduled block.
    /// - Negative simulation times are clamped to 0.0s.
    /// - At the 5-minute loop boundary (simTime >= 300.0s) or past the last activity,
    ///   the final scheduled activity or a default Idle activity is returned.
    /// </summary>
    public class NPCScheduleEvaluator
    {
        private readonly List<ScheduleActivity> _activities;

        public IReadOnlyList<ScheduleActivity> Activities => _activities;

        public NPCScheduleEvaluator(IEnumerable<ScheduleActivity> activities)
        {
            if (activities == null) throw new ArgumentNullException(nameof(activities));

            _activities = activities
                .OrderBy(a => a.StartTime)
                .ThenBy(a => a.EndTime)
                .ToList();

            ValidateNoOverlaps();
        }

        private void ValidateNoOverlaps()
        {
            for (int i = 0; i < _activities.Count - 1; i++)
            {
                var current = _activities[i];
                var next = _activities[i + 1];

                if (current.EndTime > next.StartTime)
                {
                    throw new InvalidOperationException(
                        $"Schedule overlap detected between '{current.ActivityType}' (ends at {current.EndTime}s) and '{next.ActivityType}' (starts at {next.StartTime}s).");
                }
            }
        }

        /// <summary>
        /// Evaluates and returns the active activity at the specified simulation time.
        /// </summary>
        public ScheduleActivity GetActivityForTime(float simTime)
        {
            if (_activities.Count == 0)
            {
                return new ScheduleActivity(ActivityType.Idle, 0f, 300f, desc: "Default Idle (Empty Schedule)");
            }

            float clampedTime = Math.Max(0f, simTime);

            // Check if exact match in half-open interval [StartTime, EndTime)
            foreach (var activity in _activities)
            {
                if (activity.IsActiveAt(clampedTime))
                {
                    return activity;
                }
            }

            // If time is before the first activity, return the first activity or pre-loop idle
            if (clampedTime < _activities[0].StartTime)
            {
                return _activities[0];
            }

            // If time is at or beyond the last activity's EndTime (e.g. at loop boundary >= 300s)
            var last = _activities[_activities.Count - 1];
            if (clampedTime >= last.EndTime)
            {
                return last;
            }

            // Fallback to Idle
            return new ScheduleActivity(ActivityType.Idle, clampedTime, clampedTime + 1f, desc: "Idle Fallback");
        }
    }
}
