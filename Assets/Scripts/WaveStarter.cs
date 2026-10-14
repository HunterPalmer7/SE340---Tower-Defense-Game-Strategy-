using UnityEngine;

/// <summary>
/// Holds the waves until the player has placed their first tower, so nobody gets rushed
/// while they are still reading the hint.
///
/// Setup: untick "Auto Start" on the Enemy Spawner, then put this component on the
/// same GameObject as BuildManager.
/// </summary>
public class WaveStarter : MonoBehaviour
{
    private EnemySpawner spawner;
    private bool started;

    private void OnEnable()
    {
        BuildManager.OnTowerBuilt += HandleTowerBuilt;
    }

    private void OnDisable()
    {
        BuildManager.OnTowerBuilt -= HandleTowerBuilt;
    }

    private void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
            Debug.LogWarning("WaveStarter: no EnemySpawner found in the scene.");
    }

    private void HandleTowerBuilt(Tower tower)
    {
        if (started || spawner == null) return;

        started = true;
        spawner.StartWaves();
        Debug.Log("First tower placed - starting the waves.");
    }
}
