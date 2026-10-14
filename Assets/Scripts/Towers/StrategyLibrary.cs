using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The one place that knows what targeting behaviours exist and what each one looks like.
///
/// Adding a fourth strategy is: write the class, add one line to the options list below,
/// add one row in the Inspector with its sprite. Nothing else in the project changes.
///
/// Put this component on the same GameObject as BuildManager.
/// </summary>
public class StrategyLibrary : MonoBehaviour
{
    public static StrategyLibrary Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        [Tooltip("Must match the strategy's DisplayName exactly: First, Closest, Strongest...")]
        public string displayName;
        public Sprite towerSprite;
    }

    [SerializeField] private Sprite defaultTowerSprite;
    [SerializeField] private List<Entry> entries = new List<Entry>();

    // Every targeting behaviour the game offers. Strategies hold no state, so one shared
    // instance each is fine - every tower can point at the same object.
    private readonly List<ITargetingStrategy> options = new List<ITargetingStrategy>
    {
        new FirstTarget(),
        new ClosestTarget(),
        new StrongestTarget(),
    };

    private readonly Dictionary<string, Sprite> spriteByName = new Dictionary<string, Sprite>();

    public IReadOnlyList<ITargetingStrategy> Options => options;
    public ITargetingStrategy DefaultStrategy => options.Count > 0 ? options[0] : null;
    public Sprite DefaultTowerSprite => defaultTowerSprite;

    private void Awake()
    {
        Instance = this;

        spriteByName.Clear();
        foreach (Entry entry in entries)
        {
            if (string.IsNullOrEmpty(entry.displayName) || entry.towerSprite == null) continue;
            spriteByName[entry.displayName] = entry.towerSprite;
        }
    }

    /// <summary>Art for a strategy. No type checks - a name lookup, so a new strategy needs no code here.</summary>
    public Sprite SpriteFor(ITargetingStrategy strategy)
    {
        if (strategy == null) return defaultTowerSprite;

        if (spriteByName.TryGetValue(strategy.DisplayName, out Sprite sprite))
            return sprite;

        Debug.LogWarning($"StrategyLibrary has no sprite entry for \"{strategy.DisplayName}\".");
        return defaultTowerSprite;
    }

    public ITargetingStrategy FindByName(string displayName)
    {
        foreach (ITargetingStrategy strategy in options)
            if (strategy.DisplayName == displayName) return strategy;

        return null;
    }
}
