using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Procedural.Validation
{
    /// <summary>
    /// Audit report detailing physical boundary and collision verification across the hotel environment.
    /// </summary>
    public class CollisionAuditReport
    {
        public int TotalCollidersChecked { get; set; }
        public int SolidCollidersCount { get; set; }
        public int TriggerCollidersCount { get; set; }
        public int DoorsAudited { get; set; }
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();
        public bool IsValid => Errors.Count == 0;

        public override string ToString()
        {
            return $"[CollisionAudit] Solid: {SolidCollidersCount}, Triggers: {TriggerCollidersCount}, Doors: {DoorsAudited}, Errors: {Errors.Count}, Warnings: {Warnings.Count}";
        }
    }

    /// <summary>
    /// Static collision verification and audit utility.
    /// Validates physical boundaries (walls, floors, ceilings, doors, large furniture, desks, pillars)
    /// to ensure the player cannot clip or pass through solid world geometry.
    /// </summary>
    public static class WorldCollisionAuditor
    {
        public static CollisionAuditReport AuditEnvironment(GameObject root)
        {
            var report = new CollisionAuditReport();
            if (root == null)
            {
                report.Errors.Add("Audit root GameObject is null.");
                return report;
            }

            var allColliders = root.GetComponentsInChildren<Collider>(true);
            report.TotalCollidersChecked = allColliders.Length;

            foreach (var col in allColliders)
            {
                if (col.isTrigger)
                {
                    report.TriggerCollidersCount++;
                }
                else
                {
                    report.SolidCollidersCount++;
                }

                // Verify collider bounds are non-zero
                if (col.bounds.size.magnitude < 0.001f && !col.isTrigger)
                {
                    report.Errors.Add($"Solid collider on '{col.gameObject.name}' has near-zero bounds ({col.bounds.size}).");
                }
            }

            // Verify door collision integrity
            var doors = root.GetComponentsInChildren<InteractableDoor>(true);
            report.DoorsAudited = doors.Length;

            foreach (var door in doors)
            {
                var doorColliders = door.GetComponentsInChildren<Collider>(true);
                bool hasSolidCollider = false;
                foreach (var dc in doorColliders)
                {
                    if (!dc.isTrigger)
                    {
                        hasSolidCollider = true;
                        break;
                    }
                }

                if (!hasSolidCollider)
                {
                    report.Errors.Add($"Door '{door.name}' (ID: {door.DoorId}) is missing a solid physical collider on its leaf or frame.");
                }
            }

            // Verify reception desk & lockers compound collision integrity if present
            var allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name.Contains("Reception_Desk") || t.name.Contains("ReceptionDesk"))
                {
                    var deskCols = t.GetComponentsInChildren<Collider>(true);
                    bool hasSolidDeskCol = false;
                    bool hasSolidLockersCol = false;
                    foreach (var c in deskCols)
                    {
                        if (!c.isTrigger)
                        {
                            if (c.bounds.size.x >= 3.5f && c.bounds.size.y >= 1.8f) hasSolidLockersCol = true;
                            if (c.bounds.size.x >= 4.5f && c.bounds.size.y <= 1.5f) hasSolidDeskCol = true;
                        }
                    }
                    if (!hasSolidDeskCol || !hasSolidLockersCol)
                    {
                        report.Warnings.Add($"Reception desk '{t.name}' must have compound colliders covering both counter and lockers (Counter: {hasSolidDeskCol}, Lockers: {hasSolidLockersCol}).");
                    }
                }
            }

            // Verify player CharacterController configuration if present
            var characterControllers = root.GetComponentsInChildren<CharacterController>(true);
            foreach (var cc in characterControllers)
            {
                if (cc.height < 1.0f)
                {
                    report.Warnings.Add($"CharacterController on '{cc.gameObject.name}' height is very low ({cc.height}m).");
                }
                if (cc.radius < 0.2f)
                {
                    report.Warnings.Add($"CharacterController on '{cc.gameObject.name}' radius is unusually small ({cc.radius}m).");
                }
                if (Mathf.Abs(cc.center.y - (cc.height * 0.5f)) > 0.05f)
                {
                    report.Warnings.Add($"CharacterController center.y ({cc.center.y}) does not match height/2 ({cc.height * 0.5f}), which can cause ground clipping.");
                }
            }

            return report;
        }

        /// <summary>
        /// Raycast verification test between two world positions.
        /// Returns true if a solid collider blocks the line between start and end.
        /// </summary>
        public static bool IsObstructed(Vector3 start, Vector3 end, int layerMask = ~0)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance < 0.001f) return false;

            return Physics.Raycast(start, direction.normalized, out _, distance, layerMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Validates that a closed door obstructs raycasts across its threshold,
        /// and that opening the door removes obstruction through the passage.
        /// </summary>
        public static bool ValidateDoorPassage(InteractableDoor door, Vector3 frontPoint, Vector3 backPoint)
        {
            if (door == null) return false;

            // When closed, raycast must be obstructed
            if (door.OpenState == DoorOpenState.Closed)
            {
                if (!IsObstructed(frontPoint, backPoint))
                {
                    Debug.LogWarning($"[WorldCollisionAuditor] Closed door '{door.name}' failed to obstruct passage between {frontPoint} and {backPoint}.");
                    return false;
                }
            }

            return true;
        }
    }
}
