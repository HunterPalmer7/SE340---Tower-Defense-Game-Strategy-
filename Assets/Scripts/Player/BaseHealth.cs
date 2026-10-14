using System;
using UnityEngine;

/// <summary>
/// The player's base. Loses health whenever an enemy reaches the end of the path.
/// Put this on one GameObject in the scene (e.g. at the last waypoint).
/// UI scripts can subscribe to OnHealthChanged / OnBaseDestroyed instead of polling.
/// </summary>
public class BaseHealth : MonoBehaviour
{
    public static BaseHealth Instance { get; private set; }

    [SerializeField] private int maxHealth = 20;

    /// <summary>(current, max)</summary>
    public event Action<int, int> OnHealthChanged;
    public event Action OnBaseDestroyed;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => CurrentHealth <= 0;

    private void Awake()
    {
        Instance = this;
        CurrentHealth = maxHealth;
    }

    private void OnEnable()  { Enemy.OnEnemyReachedEnd += HandleEnemyReachedEnd; }
    private void OnDisable() { Enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd; }

    private bool hudPresent;

    private void Start()
    {
        hudPresent = FindFirstObjectByType<HudUI>() != null; // real HUD exists: hide the temporary text below
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth); // lets UI show the starting value
    }

    private void HandleEnemyReachedEnd(Enemy enemy)
    {
        TakeDamage(enemy.Damage);
    }

    public void TakeDamage(int amount)
    {
        if (IsDestroyed) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDestroyed)
        {
            OnBaseDestroyed?.Invoke();
            Time.timeScale = 0f; // simple game over: freeze the game
            Debug.Log("Base destroyed - game over");
        }
    }

    // Temporary readout so the demo works before the UI team's HUD exists. Delete once a real one is in.
    private void OnGUI()
    {
        if (hudPresent) return;
        GUI.Label(new Rect(10, 10, 300, 30), $"Base Health: {CurrentHealth} / {maxHealth}");
        if (IsDestroyed)
            GUI.Label(new Rect(10, 60, 300, 30), "GAME OVER");
    }
}
