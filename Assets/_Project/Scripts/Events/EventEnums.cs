namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Explicit runtime lifecycle states for a causal event during a simulation loop.
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        /// Scheduled but not yet reached in simulation time, or waiting for prerequisites.
        /// </summary>
        Pending,

        /// <summary>
        /// Currently evaluating preconditions or applying effects.
        /// </summary>
        Executing,

        /// <summary>
        /// Preconditions met and all effects applied successfully.
        /// </summary>
        Executed,

        /// <summary>
        /// Reached scheduled time, but one or more preconditions were false.
        /// </summary>
        Failed,

        /// <summary>
        /// Explicitly cancelled or invalidated by causal disruption.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Bypassed because a required prerequisite failed under a Skip policy.
        /// </summary>
        Skipped
    }

    /// <summary>
    /// Policy governing how an event behaves when one of its required prerequisite events
    /// terminates unsuccessfully (Failed, Cancelled, or Skipped).
    /// </summary>
    public enum PrerequisiteFailurePolicy
    {
        /// <summary>
        /// The dependent event is marked as Failed with an explanation.
        /// </summary>
        Fail,

        /// <summary>
        /// The dependent event is quietly Skipped.
        /// </summary>
        Skip,

        /// <summary>
        /// The dependent event is Cancelled.
        /// </summary>
        Cancel
    }
}
