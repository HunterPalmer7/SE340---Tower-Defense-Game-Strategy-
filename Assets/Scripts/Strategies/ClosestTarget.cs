using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shoots whichever enemy is physically nearest to the tower.
/// Ties go to the first one found, so repeat runs come out the same.
/// </summary>
public class ClosestTarget : ITargetingStrategy
{
    public string DisplayName => "Spongebob";
    public string Description => "Shoots whatever is nearest to the tower right now.";

    public Enemy SelectTarget(Tower tower, List<Enemy> enemiesInRange)
    {
        Enemy best = null;
        float nearest = float.MaxValue;

        foreach (Enemy enemy in enemiesInRange)
        {
            float distance = Vector2.Distance(tower.transform.position, enemy.transform.position);
            if (distance < nearest)
            {
                nearest = distance;
                best = enemy;
            }
        }

        return best;
    }
}
