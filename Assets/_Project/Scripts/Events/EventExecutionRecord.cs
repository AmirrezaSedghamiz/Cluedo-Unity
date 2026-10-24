namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Immutable historical snapshot of an event evaluation for debugging,
    /// narrative auditing, and HUD display.
    /// </summary>
    public class EventExecutionRecord
    {
        public string EventId { get; }
        public string EventName { get; }
        public float SimTime { get; }
        public int LoopIndex { get; }
        public EventStatus Status { get; }
        public string Explanation { get; }

        public EventExecutionRecord(
            string eventId,
            string eventName,
            float simTime,
            int loopIndex,
            EventStatus status,
            string explanation)
        {
            EventId = eventId;
            EventName = eventName;
            SimTime = simTime;
            LoopIndex = loopIndex;
            Status = status;
            Explanation = explanation;
        }

        public string GetFormattedTime()
        {
            int min = (int)(SimTime / 60f);
            int sec = (int)(SimTime % 60f);
            return $"{min:D2}:{sec:D2}";
        }
    }
}
