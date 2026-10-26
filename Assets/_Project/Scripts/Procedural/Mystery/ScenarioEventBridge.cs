using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Procedural.Mystery
{
    /// <summary>
    /// Compiles a procedural murder scenario and NPC routines into runtime EventDefinitions
    /// and registers them with the deterministic CausalEventEngine.
    /// </summary>
    public static class ScenarioEventBridge
    {
        public static List<EventDefinition> CompileScenarioEvents(MysteryGroundTruth truth)
        {
            var eventDefs = new List<EventDefinition>();
            if (truth == null || truth.CrimeTimeline == null) return eventDefs;

            foreach (var step in truth.CrimeTimeline)
            {
                var evtAsset = ScriptableObject.CreateInstance<EventDefinition>();
                evtAsset.Initialize(
                    step.EventId,
                    step.ActionType,
                    step.SimTimestamp,
                    priority: 10,
                    failurePolicy: PrerequisiteFailurePolicy.Skip,
                    description: step.NarrativeDescription
                );

                // Configure Causal Preconditions & Effects
                if (step.ActionType == "AcquireMeans")
                {
                    // Precondition: weapon must be at baseline
                    evtAsset.AddPrecondition(new KeyLocationPrecondition(step.TargetObjectId, PropLocationState.AtBaseline));

                    // Effect: weapon is held
                    evtAsset.AddEffect(new PropLocationEffect(step.TargetObjectId, PropLocationState.HeldByPlayer));
                }
                else if (step.ActionType == "EnterCrimeScene")
                {
                    // Prerequisite: AcquireMeans must have succeeded
                    evtAsset.AddPrerequisite("evt_crime_acquire_means");

                    // Effect: Door unlocks and opens
                    string doorId = truth.RequiredAccessKeyId != null
                        ? truth.RequiredAccessKeyId.Replace("obj_key_", "door_")
                        : "door_room_102";
                    evtAsset.AddEffect(new DoorStateEffect(doorId, DoorLockState.Unlocked, DoorOpenState.Open));
                }
                else if (step.ActionType == "ExecuteMurder")
                {
                    // Prerequisite: EnterCrimeScene must have succeeded
                    evtAsset.AddPrerequisite("evt_crime_enter_scene");
                }

                eventDefs.Add(evtAsset);
            }

            return eventDefs;
        }

        public static void RegisterWithEngine(CausalEventScheduler scheduler, List<EventDefinition> eventDefs)
        {
            if (scheduler == null || eventDefs == null) return;
            var engine = scheduler.Engine;
            if (engine == null) return;

            foreach (var def in eventDefs)
            {
                if (engine.GetDefinition(def.Id) == null)
                {
                    engine.RegisterEvent(def);
                }
            }
        }
    }
}
