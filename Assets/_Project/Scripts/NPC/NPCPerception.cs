using UnityEngine;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Basic, lightweight sensory model evaluating line-of-sight, range, and field-of-view.
    /// Ensures NPCs only react to world events they could reasonably perceive.
    /// </summary>
    public static class NPCPerception
    {
        /// <summary>
        /// Pure geometric and raycast visibility test.
        /// </summary>
        /// <param name="eyePosition">Position of NPC's eyes / head.</param>
        /// <param name="eyeForward">Forward facing vector of the NPC.</param>
        /// <param name="targetPosition">Target point being observed.</param>
        /// <param name="viewRange">Maximum view distance.</param>
        /// <param name="fieldOfView">Total horizontal FOV angle in degrees.</param>
        /// <param name="obstructionMask">Physics layers that block vision (e.g. walls, pillars).</param>
        /// <param name="reason">Output diagnostic explanation if perception fails.</param>
        /// <returns>True if target is within range, within FOV, and unobstructed by geometry.</returns>
        public static bool CanPerceive(
            Vector3 eyePosition,
            Vector3 eyeForward,
            Vector3 targetPosition,
            float viewRange,
            float fieldOfView,
            LayerMask obstructionMask,
            out string reason)
        {
            float dist = Vector3.Distance(eyePosition, targetPosition);
            if (dist > viewRange)
            {
                reason = $"Target out of range ({dist:F1}m > {viewRange:F1}m).";
                return false;
            }

            Vector3 dirToTarget = (targetPosition - eyePosition).normalized;
            // Handle zero vector case if eye is exactly at target
            if (dirToTarget == Vector3.zero)
            {
                reason = "Target at eye position.";
                return true;
            }

            float angle = Vector3.Angle(eyeForward, dirToTarget);
            float halfFov = fieldOfView * 0.5f;
            if (angle > halfFov)
            {
                reason = $"Target outside field of view ({angle:F1}° > {halfFov:F1}°).";
                return false;
            }

            // Raycast check for physical occlusions
            if (Physics.Raycast(eyePosition, dirToTarget, out RaycastHit hit, dist, obstructionMask))
            {
                // If hit object is before the target
                if (hit.distance < dist - 0.1f)
                {
                    reason = $"Line of sight occluded by '{hit.collider.gameObject.name}'.";
                    return false;
                }
            }

            reason = "Target clearly visible within perception cone.";
            return true;
        }

        /// <summary>
        /// Mathematical-only perception test (distance and FOV angle) without requiring Unity physics scenes.
        /// Useful for pure EditMode unit tests.
        /// </summary>
        public static bool EvaluateSensoryCone(
            Vector3 eyePosition,
            Vector3 eyeForward,
            Vector3 targetPosition,
            float viewRange,
            float fieldOfView,
            out string reason)
        {
            float dist = Vector3.Distance(eyePosition, targetPosition);
            if (dist > viewRange)
            {
                reason = $"Target out of range ({dist:F1}m > {viewRange:F1}m).";
                return false;
            }

            Vector3 dirToTarget = (targetPosition - eyePosition).normalized;
            if (dirToTarget == Vector3.zero)
            {
                reason = "Target at eye position.";
                return true;
            }

            float angle = Vector3.Angle(eyeForward, dirToTarget);
            float halfFov = fieldOfView * 0.5f;
            if (angle > halfFov)
            {
                reason = $"Target outside field of view ({angle:F1}° > {halfFov:F1}°).";
                return false;
            }

            reason = "Target within sensory cone.";
            return true;
        }
    }
}
