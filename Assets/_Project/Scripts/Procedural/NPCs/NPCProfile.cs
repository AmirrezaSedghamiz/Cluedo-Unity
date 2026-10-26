using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.Procedural.NPCs
{
    public enum NPCRole
    {
        Concierge,
        Receptionist,
        HeadChef,
        HousekeepingMaid,
        MaintenanceEngineer,
        HotelManager,
        WealthyGuest,
        SocietyLady,
        DoctorGuest,
        JournalistGuest
    }

    [Serializable]
    public class PersonalityProfile
    {
        public float Sociability = 0.5f;   // 0.0 = Hermit, 1.0 = Social butterfly
        public float Caution = 0.5f;       // 0.0 = Reckless, 1.0 = Highly vigilant
        public float Curiosity = 0.5f;     // 0.0 = Indifferent, 1.0 = Inquisitive
        public float Irritability = 0.5f;   // 0.0 = Patient, 1.0 = Hot-headed
        public float Honesty = 0.5f;        // 0.0 = Deceptive/secretive, 1.0 = Truthful
    }

    /// <summary>
    /// Persistent identity definition of a generated NPC for the current investigation.
    /// Preserved across time-loop resets and restored to baseline every 5 minutes.
    /// </summary>
    [Serializable]
    public class NPCProfile
    {
        public string NpcId;
        public string DisplayName;
        public NPCRole Role;
        public int Age;
        public PersonalityProfile Personality = new();
        public string AssignedRoomId; // Bedroom or office assignment
        public HashSet<string> PermittedRoomIds = new();
        public Vector3 BaselinePosition;
        public Vector3 BaselineRotation;
        public string ModelPrefabPath;
        public string MaterialKey;

        // Scenario and Case Roles
        public bool IsVictim;
        public bool IsCulprit;
        public string MotiveDescription;
        public string SecretDescription;

        public NPCProfile(string id, string name, NPCRole role, int age, string assignedRoom = null)
        {
            NpcId = id;
            DisplayName = name;
            Role = role;
            Age = age;
            AssignedRoomId = assignedRoom;
        }

        public bool HasAccessTo(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return true;
            return PermittedRoomIds.Contains(roomId) || AssignedRoomId == roomId;
        }
    }

    /// <summary>
    /// Dynamic, simulation-driven needs model for NPCs.
    /// Driven by simulation clock time (not framerate).
    /// </summary>
    [Serializable]
    public class NPCNeedsModel
    {
        [Range(0f, 1f)] public float Energy = 0.85f;
        [Range(0f, 1f)] public float Hunger = 0.20f;
        [Range(0f, 1f)] public float Stress = 0.10f;
        [Range(0f, 1f)] public float Social = 0.50f;

        public void Tick(float deltaSeconds)
        {
            // Needs evolve gradually across the 300-second loop
            Energy = Mathf.Clamp01(Energy - (0.0003f * deltaSeconds));
            Hunger = Mathf.Clamp01(Hunger + (0.0004f * deltaSeconds));
            Stress = Mathf.Clamp01(Stress - (0.0002f * deltaSeconds)); // Natural calming unless triggered
        }

        public void AddStress(float amount)
        {
            Stress = Mathf.Clamp01(Stress + amount);
        }

        public void SatisfyHunger()
        {
            Hunger = Mathf.Clamp01(Hunger - 0.5f);
        }

        public void Rest(float amount = 0.3f)
        {
            Energy = Mathf.Clamp01(Energy + amount);
        }
    }

    [Serializable]
    public class RoutineActivity
    {
        public string ActivityName;
        public string TargetRoomId;
        public Vector3 Destination;
        public float StartSimTime; // In seconds (0 to 300)
        public float EndSimTime;
        public string RequiredObjectId;
        public string Description;

        public RoutineActivity(string name, string roomId, Vector3 dest, float start, float end, string reqObj = null, string desc = null)
        {
            ActivityName = name;
            TargetRoomId = roomId;
            Destination = dest;
            StartSimTime = start;
            EndSimTime = end;
            RequiredObjectId = reqObj;
            Description = desc ?? name;
        }
    }

    /// <summary>
    /// Complete routine schedule for an NPC within the 5-minute loop.
    /// </summary>
    [Serializable]
    public class NPCRoutinePlan
    {
        public string NpcId;
        public List<RoutineActivity> Activities = new();

        public NPCRoutinePlan(string npcId)
        {
            NpcId = npcId;
        }

        public void AddActivity(string name, string roomId, Vector3 dest, float start, float end, string reqObj = null, string desc = null)
        {
            Activities.Add(new RoutineActivity(name, roomId, dest, start, end, reqObj, desc));
        }

        public RoutineActivity GetCurrentActivity(float simSeconds)
        {
            foreach (var act in Activities)
            {
                if (simSeconds >= act.StartSimTime && simSeconds < act.EndSimTime)
                {
                    return act;
                }
            }
            return Activities.Count > 0 ? Activities[0] : null;
        }
    }
}
