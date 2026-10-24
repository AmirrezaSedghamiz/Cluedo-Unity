using System;
using UnityEngine;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Represents a discrete, immutable scheduled activity block for an NPC during the 5-minute simulation loop.
    /// Time boundaries follow the half-open interval policy: [StartTime, EndTime).
    /// </summary>
    [Serializable]
    public class ScheduleActivity
    {
        [SerializeField] private ActivityType activityType;
        [SerializeField] private float startTime;
        [SerializeField] private float endTime;
        [SerializeField] private string destinationKey;
        [SerializeField] private string associatedEventId;
        [SerializeField] private string description;

        public ActivityType ActivityType => activityType;
        public float StartTime => startTime;
        public float EndTime => endTime;
        public string DestinationKey => destinationKey;
        public string AssociatedEventId => associatedEventId;
        public string Description => description;

        public ScheduleActivity(
            ActivityType type,
            float start,
            float end,
            string destination = null,
            string eventId = null,
            string desc = null)
        {
            if (start < 0f) throw new ArgumentOutOfRangeException(nameof(start), "StartTime cannot be negative.");
            if (end <= start) throw new ArgumentException("EndTime must be strictly greater than StartTime.", nameof(end));

            activityType = type;
            startTime = start;
            endTime = end;
            destinationKey = destination ?? string.Empty;
            associatedEventId = eventId ?? string.Empty;
            description = desc ?? string.Empty;
        }

        /// <summary>
        /// Evaluates whether this activity is active at the specified simulation time using [StartTime, EndTime).
        /// </summary>
        public bool IsActiveAt(float simTime)
        {
            return simTime >= startTime && simTime < endTime;
        }

        public override string ToString()
        {
            return $"[{FormatTime(startTime)} - {FormatTime(endTime)}] {activityType} -> {destinationKey} ({description})";
        }

        private static string FormatTime(float totalSeconds)
        {
            int minutes = Mathf.FloorToInt(totalSeconds / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
