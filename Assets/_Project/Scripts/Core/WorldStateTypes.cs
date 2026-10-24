namespace TomorrowsCrime.Core
{
    public enum DoorLockState
    {
        Unlocked,
        Locked
    }

    public enum DoorOpenState
    {
        Closed,
        Open
    }

    public enum LightState
    {
        Off,
        On
    }

    public enum PropLocationState
    {
        AtBaseline,
        HeldByPlayer,
        DroppedElsewhere
    }

    public enum ScenarioExecutionStatus
    {
        Pending,
        Succeeded,
        Failed
    }
}
