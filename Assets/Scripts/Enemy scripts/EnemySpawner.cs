using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns waves of enemies at the start of the Path.
/// A wave is a mix of enemy types (e.g. 6 small + 3 medium + 1 large). With "Shuffle" on,
/// the mix is spawned in random order; with it off, the groups spawn in the order listed.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnGroup
    {
        public Enemy enemyPrefab;
        public int count = 5;
    }

    [Serializable]
    public class Wave
    {
        public List<SpawnGroup> groups = new List<SpawnGroup>();
        public bool shuffle = true;
        public float spawnInterval = 1f;
        public float delayAfterWave = 5f;
    }

    [SerializeField] private Wave[] waves;
    [SerializeField] private bool autoStart = true;

    /// <summary>(current wave number starting at 1, total waves). UI can subscribe to this.</summary>
    public event Action<int, int> OnWaveStarted;

    public int CurrentWave { get; private set; }   // 0 until the first wave begins
    public int TotalWaves => waves != null ? waves.Length : 0;

    private bool hudPresent;

    private void Start()
    {
        hudPresent = FindFirstObjectByType<HudUI>() != null; // real HUD exists: hide the temporary text below
        if (autoStart) StartWaves();
    }

    public void StartWaves()
    {
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        foreach (Wave wave in waves)
        {
            CurrentWave++;
            OnWaveStarted?.Invoke(CurrentWave, TotalWaves);

            // Flatten the groups into one list of prefabs, one entry per enemy to spawn.
            var queue = new List<Enemy>();
            foreach (SpawnGroup g in wave.groups)
            {
                if (g.enemyPrefab == null) continue;
                for (int i = 0; i < g.count; i++) queue.Add(g.enemyPrefab);
            }

            if (wave.shuffle) Shuffle(queue);

            foreach (Enemy prefab in queue)
            {
                Instantiate(prefab, Path.Instance.Point(0), Quaternion.identity);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            yield return new WaitForSeconds(wave.delayAfterWave);
        }
    }

    // Fisher-Yates shuffle
    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Temporary readout so the demo works before the UI team's HUD exists. Delete once a real one is in.
    private void OnGUI()
    {
        if (hudPresent) return;
        GUI.Label(new Rect(10, 35, 300, 30), $"Wave: {CurrentWave} / {TotalWaves}");
    }
}
