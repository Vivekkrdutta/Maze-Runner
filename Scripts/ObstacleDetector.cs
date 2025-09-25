// ObstacleDetector.cs
using UnityEngine;

public class ObstacleDetector
{
    private const float RaycastDistance = 100f; // How far from above to cast the ray

    /// <summary>
    /// Checks a circular area at a given world position for any objects with an IObstacle component.
    /// </summary>
    /// <param name="worldPosition">The center of the hex cell in world space.</param>
    /// <param name="radius">The radius of the circle to check (e.g., the inner radius of the hex).</param>
    /// <returns>True if an obstacle is found, otherwise false.</returns>
    public static bool IsPositionBlocked(Vector3 worldPosition, float radius,LayerMask obstacleLayer)
    {
        // Cast a circle from above, downwards.
        if (Physics.SphereCast(
                worldPosition + Vector3.up * (RaycastDistance / 2f), // Start position high above
                radius,                                              // The radius of the check
                Vector3.down,                                        // Direction
                out RaycastHit hit,                                  // The hit info
                RaycastDistance,                                     // Max distance
                obstacleLayer))                                      // Only check the obstacle layer
        {
            // If we hit something, check if it has the IObstacle component.
            if (hit.collider.TryGetComponent<Obstacle>(out _))
            {
                return true; // Obstacle found!
            }
        }

        return false; // No obstacles here.
    }
}