using System.Collections.Generic;

/// <summary>
/// The Strategy interface.
/// Given the enemies a tower can currently reach, pick the one it should shoot.
/// Returns null when nothing qualifies (e.g. the list is empty).
/// </summary>
public interface ITargetingStrategy
{
    /// <summary>Name the player sees on buttons and in the info panel.</summary>
    string DisplayName { get; }

    /// <summary>One line explaining what this strategy shoots. Shown in the info panel.</summary>
    string Description { get; }

    Enemy SelectTarget(Tower tower, List<Enemy> enemiesInRange);
}
