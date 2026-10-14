using System.Collections.Generic;

/// <summary>
/// Shoots whichever enemy is furthest along the path - the one closest to the base.
/// Uses Enemy.DistanceTraveled, which the Enemy script already tracks.
/// Ties go to the first one found, so repeat runs come out the same.
/// </summary>
public class FirstTarget : ITargetingStrategy
{
    public string DisplayName => "Turbo";
    public string Description => "Shoots the enemy furthest along the path - the one about to reach your base.";

    public Enemy SelectTarget(Tower tower, List<Enemy> enemiesInRange)
    {
        Enemy best = null;
        float furthest = float.MinValue;

        foreach (Enemy enemy in enemiesInRange)
        {
            if (enemy.DistanceTraveled > furthest)
            {
                furthest = enemy.DistanceTraveled;
                best = enemy;
            }
        }

        return best;
    }
}
