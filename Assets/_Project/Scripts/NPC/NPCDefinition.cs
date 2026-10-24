using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Immutable character definition authored as a ScriptableObject.
    /// Defines static character identity, baseline spawn transform, locomotion settings,
    /// perception parameters, and baseline 5-minute schedule activities.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNPCDefinition", menuName = "Tomorrow's Crime/NPC Definition")]
    public class NPCDefinition : ScriptableObject
    {
        [Header("Character Identity")]
        [SerializeField] private string characterId = "npc_concierge";
        [SerializeField] private string displayName = "Concierge Pierre";

        [Header("Baseline Transform")]
        [SerializeField] private Vector3 defaultPosition = new Vector3(0f, 1.0f, 6.0f);
        [SerializeField] private Quaternion defaultRotation = Quaternion.Euler(0f, 180f, 0f);

        [Header("Locomotion")]
        [SerializeField] private float movementSpeed = 3.0f;
        [SerializeField] private float stoppingDistance = 0.8f;

        [Header("Perception")]
        [SerializeField] private float perceptionViewRange = 8.0f;
        [SerializeField] private float perceptionFieldOfView = 90.0f;

        [Header("Schedule Activities (5-Minute Loop)")]
        [SerializeField] private List<ScheduleActivity> scheduleActivities = new List<ScheduleActivity>();

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public Vector3 DefaultPosition => defaultPosition;
        public Quaternion DefaultRotation => defaultRotation;
        public float MovementSpeed => movementSpeed;
        public float StoppingDistance => stoppingDistance;
        public float PerceptionViewRange => perceptionViewRange;
        public float PerceptionFieldOfView => perceptionFieldOfView;
        public IReadOnlyList<ScheduleActivity> ScheduleActivities => scheduleActivities;

        public void Initialize(
            string id,
            string name,
            Vector3 spawnPos,
            Quaternion spawnRot,
            float speed = 3.0f,
            float stopDist = 0.8f,
            float viewRange = 8.0f,
            float fov = 90.0f)
        {
            characterId = id ?? throw new ArgumentNullException(nameof(id));
            displayName = name ?? id;
            defaultPosition = spawnPos;
            defaultRotation = spawnRot;
            movementSpeed = speed;
            stoppingDistance = stopDist;
            perceptionViewRange = viewRange;
            perceptionFieldOfView = fov;
            scheduleActivities = new List<ScheduleActivity>();
        }

        public void AddActivity(ScheduleActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            scheduleActivities.Add(activity);
        }

        public NPCScheduleEvaluator CreateEvaluator()
        {
            return new NPCScheduleEvaluator(scheduleActivities);
        }
    }
}
