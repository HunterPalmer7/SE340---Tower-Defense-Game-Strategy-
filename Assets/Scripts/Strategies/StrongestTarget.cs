using System.Collections.Generic;

/// <summary>
/// Shoots whichever enemy has the most health left.
/// Ties go to the first one found, so repeat runs come out the same.
/// </summary>
public class StrongestTarget : ITargetingStrategy
{
    public string DisplayName => "Patrick";
    public string Description => "Shoots the enemy with the most health left in range.";

    public Enemy SelectTarget(Tower tower, List<Enemy> enemiesInRange)
    {
        Enemy best = null;
        float mostHealth = float.MinValue;

        foreach (Enemy enemy in enemiesInRange)
        {
            if (enemy.CurrentHealth > mostHealth)
            {
                mostHealth = enemy.CurrentHealth;
                best = enemy;
            }
        }

        return best;
    }
}
