using System;
using System.Collections.Generic;

namespace TomorrowsCrime.Procedural.Mystery
{
    public enum MurderMethod
    {
        PoisonTincture,
        BluntForceCandlestick,
        BluntForceWrench,
        SharpCleaver
    }

    [Serializable]
    public class TimelineCausalEvent
    {
        public string EventId;
        public float SimTimestamp; // In seconds (0 to 300)
        public string ActorId;
        public string ActionType; // "AcquireKey", "EnterRoom", "AdministerPoison", "StrikeVictim", "FleeScene"
        public string TargetRoomId;
        public string TargetObjectId;
        public string NarrativeDescription;

        public TimelineCausalEvent(string id, float time, string actor, string action, string room, string obj, string desc)
        {
            EventId = id;
            SimTimestamp = time;
            ActorId = actor;
            ActionType = action;
            TargetRoomId = room;
            TargetObjectId = obj;
            NarrativeDescription = desc;
        }
    }

    [Serializable]
    public class EvidenceClueLink
    {
        public string EvidenceId;
        public string ClueType; // "PhysicalWeapon", "AccessKey", "MotiveDocument", "WitnessTestimony"
        public string TargetRoomId;
        public string AssociatedObjectId;
        public string Explanation;

        public EvidenceClueLink(string evId, string type, string roomId, string objId, string explanation)
        {
            EvidenceId = evId;
            ClueType = type;
            TargetRoomId = roomId;
            AssociatedObjectId = objId;
            Explanation = explanation;
        }
    }

    /// <summary>
    /// Pure, internal Ground Truth of a procedurally generated murder mystery.
    /// Strictly separated from player knowledge. Used by the simulation engine and solvability validator.
    /// </summary>
    [Serializable]
    public class MysteryGroundTruth
    {
        public string CaseTitle;
        public string VictimId;
        public string VictimName;
        public string CulpritId;
        public string CulpritName;
        public string Motive;
        public MurderMethod Method;
        public string MurderWeaponObjectId;
        public string CrimeSceneRoomId;
        public float MurderTimestamp; // In seconds (e.g. 180s = 03:00)
        public string RequiredAccessKeyId;

        public List<TimelineCausalEvent> CrimeTimeline = new();
        public List<EvidenceClueLink> RequiredEvidenceLinks = new();
        public List<string> ExoneratedSuspectIds = new();
        [System.NonSerialized] public Dictionary<string, string> SuspectAlibis = new(); // SuspectId -> AlibiDescription
        [System.NonSerialized] public Dictionary<string, string> MandatoryObjectPlacements = new(); // ObjectId -> RoomId

        public override string ToString()
        {
            return $"[GroundTruth] '{CaseTitle}': Culprit '{CulpritName}' murdered '{VictimName}' in '{CrimeSceneRoomId}' at {MurderTimestamp:F0}s using '{Method}' (Motive: {Motive}).";
        }
    }
}
