namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Explicit high-level behavior state for an NPC during the simulation loop.
    /// </summary>
    public enum NPCBehaviorState
    {
        /// <summary>Standing at designated station or awaiting next scheduled activity.</summary>
        Idle,

        /// <summary>Navigating toward a scheduled destination waypoint.</summary>
        Moving,

        /// <summary>Arrived at destination and actively executing scheduled interaction.</summary>
        PerformingAction,

        /// <summary>Examining an anomalous world condition (e.g., missing key from expected table).</summary>
        Investigating,

        /// <summary>Navigation route is blocked or destination cannot be reached.</summary>
        Blocked,

        /// <summary>Awaiting resolution or fallback recovery before resuming schedule.</summary>
        Waiting
    }

    /// <summary>
    /// Status of the NPC's NavMesh navigation intention.
    /// </summary>
    public enum NPCNavigationStatus
    {
        /// <summary>No navigation request active.</summary>
        Idle,

        /// <summary>Calculating path to target destination.</summary>
        PathPending,

        /// <summary>Actively following valid path toward destination.</summary>
        Traversing,

        /// <summary>Reached destination within configured stopping distance.</summary>
        Arrived,

        /// <summary>Target destination is unreachable or path is partial/invalid.</summary>
        Unreachable,

        /// <summary>Agent is stuck or physically obstructed along path.</summary>
        Blocked
    }

    /// <summary>
    /// Categorization of scheduled NPC activities.
    /// </summary>
    public enum ActivityType
    {
        /// <summary>Stationary wait at default station (e.g. reception desk).</summary>
        WaitAtPost,

        /// <summary>General patrol or walk to destination.</summary>
        Patrol,

        /// <summary>Walk to table and attempt to acquire the Room 102 keycard.</summary>
        RetrieveKey,

        /// <summary>Walk to Room 102 and attempt to unlock/open the door.</summary>
        UnlockDoor,

        /// <summary>Examine environment anomaly.</summary>
        Investigate,

        /// <summary>General idle fallback.</summary>
        Idle
    }
}
