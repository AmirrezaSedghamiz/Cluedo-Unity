namespace TomorrowsCrime.Core
{
    /// <summary>
    /// Contract implemented by any runtime entity or system that responds
    /// to the 5-minute loop reset (e.g. Player, Doors, Props, NPCs).
    /// </summary>
    public interface ILoopResetReceiver
    {
        /// <summary>
        /// Called when the 5-minute time loop resets back to midnight (00:00).
        /// Implementations must restore their state to their initial conditions.
        /// </summary>
        void OnLoopReset();
    }
}
