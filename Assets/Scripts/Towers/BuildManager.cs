using System;
using UnityEngine;

/// <summary>
/// The Client in the Strategy pattern.
/// Places towers and hands each one a targeting strategy. Towers never choose for themselves.
///
/// Flow: click an empty spot -> a plain tower is placed with no strategy yet -> the targeting
/// panel opens -> the player picks, and the tower takes on that behaviour and that art.
/// Clicking a built tower reopens the panel so the choice can be changed mid-match.
/// </summary>
public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    /// <summary>Raised whenever a tower is placed. The hint banner listens for this.</summary>
    public static event Action<Tower> OnTowerBuilt;

    [SerializeField] private Tower towerPrefab;
    [SerializeField] private bool openTargetingPanelAfterBuilding = true;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Called by TowerSpot when the player clicks an empty spot.</summary>
    public void BuildOn(TowerSpot spot)
    {
        if (spot == null || !spot.IsEmpty) return;

        if (towerPrefab == null)
        {
            Debug.LogError("BuildManager has no Tower Prefab assigned in the Inspector.");
            return;
        }

        Tower tower = Instantiate(towerPrefab, spot.transform.position, Quaternion.identity);
        spot.tower = tower;

        // Plain tower for now - no strategy, neutral art. It holds fire until the player picks.
        if (StrategyLibrary.Instance != null)
            tower.SetSprite(StrategyLibrary.Instance.DefaultTowerSprite);

        Debug.Log($"Built a tower on {spot.name}. Waiting for a targeting strategy.");

        OnTowerBuilt?.Invoke(tower);

        if (openTargetingPanelAfterBuilding) SelectTower(tower);
    }

    /// <summary>Opens the per-tower targeting controls for an already-built tower.</summary>
    public void SelectTower(Tower tower)
    {
        if (tower == null) return;

        HudUI hud = FindFirstObjectByType<HudUI>();
        if (hud == null)
        {
            Debug.LogWarning("No HudUI found to show tower targeting controls.");
            return;
        }

        hud.ShowTargeting(tower);
    }
}
