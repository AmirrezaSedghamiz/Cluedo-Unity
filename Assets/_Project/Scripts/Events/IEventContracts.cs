using TomorrowsCrime.World;

namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Evaluates a specific world condition when an event is eligible to execute.
    /// Returns false with a human-readable failure reason if the condition is not met.
    /// </summary>
    public interface IEventPrecondition
    {
        string Description { get; }

        /// <summary>
        /// Evaluates whether the condition is currently satisfied.
        /// </summary>
        /// <param name="worldState">The current mutable world state registry.</param>
        /// <param name="engine">The event engine to query event statuses and history.</param>
        /// <param name="failureReason">Output explanation if the condition failed.</param>
        /// <returns>True if satisfied; false otherwise.</returns>
        bool Evaluate(WorldStateRegistry worldState, CausalEventEngine engine, out string failureReason);
    }

    /// <summary>
    /// Applies a discrete modification to the world state when an event succeeds.
    /// </summary>
    public interface IEventEffect
    {
        string Description { get; }

        /// <summary>
        /// Applies the effect to the world state.
        /// </summary>
        /// <param name="worldState">The mutable world state registry to modify.</param>
        /// <param name="failureReason">Output explanation if the effect failed to apply.</param>
        /// <returns>True if applied successfully; false if effect encountered a failure.</returns>
        bool Apply(WorldStateRegistry worldState, out string failureReason);
    }
}
