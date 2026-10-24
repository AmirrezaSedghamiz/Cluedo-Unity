namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Mutable per-loop runtime state tracking the execution lifecycle of an EventDefinition.
    /// Resets deterministically upon loop turnover.
    /// </summary>
    public class EventRuntimeInstance
    {
        public EventDefinition Definition { get; }
        public string Id => Definition.Id;
        public string EventName => Definition.EventName;
        public float ScheduledTimeSeconds => Definition.ScheduledTimeSeconds;
        public int Priority => Definition.Priority;

        public EventStatus Status { get; private set; }
        public float? ExecutionSimTime { get; private set; }
        public int LoopIndex { get; private set; }
        public string ResultExplanation { get; private set; }

        public bool IsTerminal =>
            Status is EventStatus.Executed or EventStatus.Failed or EventStatus.Cancelled or EventStatus.Skipped;

        public bool HasExecutedSuccessfully => Status == EventStatus.Executed;

        public EventRuntimeInstance(EventDefinition definition, int loopIndex)
        {
            Definition = definition;
            LoopIndex = loopIndex;
            ResetState(loopIndex);
        }

        public void MarkExecuting()
        {
            Status = EventStatus.Executing;
        }

        public void MarkExecuted(float simTime, string explanation)
        {
            Status = EventStatus.Executed;
            ExecutionSimTime = simTime;
            ResultExplanation = explanation;
        }

        public void MarkFailed(float simTime, string explanation)
        {
            Status = EventStatus.Failed;
            ExecutionSimTime = simTime;
            ResultExplanation = explanation;
        }

        public void MarkSkipped(float simTime, string explanation)
        {
            Status = EventStatus.Skipped;
            ExecutionSimTime = simTime;
            ResultExplanation = explanation;
        }

        public void MarkCancelled(float simTime, string explanation)
        {
            Status = EventStatus.Cancelled;
            ExecutionSimTime = simTime;
            ResultExplanation = explanation;
        }

        public void ResetState(int loopIndex)
        {
            Status = EventStatus.Pending;
            ExecutionSimTime = null;
            LoopIndex = loopIndex;
            ResultExplanation = "Pending scheduled execution.";
        }
    }
}
