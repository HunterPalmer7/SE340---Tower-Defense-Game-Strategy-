using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Connects the HUD (health bar, wave counter, game over panel) to the game scripts.
/// Put this on the HUD Canvas. "Tools > Build HUD" creates the Canvas and fills these slots for you.
/// BaseHealth and EnemySpawner raise events for the status HUD; this script also owns the
/// per-tower targeting panel.
/// </summary>
public class HudUI : MonoBehaviour
{
    [Header("Base health")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthText;

    [Header("Waves")]
    [SerializeField] private Text waveText;

    [Header("Game over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverResultText;
    [SerializeField] private Button restartButton;

    [Header("Tower targeting")]
    [SerializeField] private GameObject targetingPanel;
    [SerializeField] private Toggle firstTargetToggle;
    [SerializeField] private Toggle closestTargetToggle;
    [SerializeField] private Toggle strongestTargetToggle;

    private BaseHealth baseHealth;
    private EnemySpawner spawner;
    private Tower selectedTower;

    // Start (not OnEnable) so every other script has finished its Awake and has valid values.
    private void Start()
    {
        baseHealth = FindFirstObjectByType<BaseHealth>();
        spawner = FindFirstObjectByType<EnemySpawner>();

        if (baseHealth != null)
        {
            baseHealth.OnHealthChanged += UpdateHealth;
            baseHealth.OnBaseDestroyed += ShowGameOver;
            UpdateHealth(baseHealth.CurrentHealth, baseHealth.MaxHealth); // starting value
        }
        else Debug.LogWarning("HudUI: no BaseHealth found in the scene.");

        if (spawner != null)
        {
            spawner.OnWaveStarted += UpdateWave;
            UpdateWave(spawner.CurrentWave, spawner.TotalWaves); // the first wave may already have started
        }
        else Debug.LogWarning("HudUI: no EnemySpawner found in the scene.");

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);

        if (firstTargetToggle != null) firstTargetToggle.onValueChanged.AddListener(OnFirstTargetSelected);
        if (closestTargetToggle != null) closestTargetToggle.onValueChanged.AddListener(OnClosestTargetSelected);
        if (strongestTargetToggle != null) strongestTargetToggle.onValueChanged.AddListener(OnStrongestTargetSelected);
        if (targetingPanel != null) targetingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (baseHealth != null)
        {
            baseHealth.OnHealthChanged -= UpdateHealth;
            baseHealth.OnBaseDestroyed -= ShowGameOver;
        }
        if (spawner != null) spawner.OnWaveStarted -= UpdateWave;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (firstTargetToggle != null) firstTargetToggle.onValueChanged.RemoveListener(OnFirstTargetSelected);
        if (closestTargetToggle != null) closestTargetToggle.onValueChanged.RemoveListener(OnClosestTargetSelected);
        if (strongestTargetToggle != null) strongestTargetToggle.onValueChanged.RemoveListener(OnStrongestTargetSelected);
    }

    private void UpdateHealth(int current, int max)
    {
        if (healthBar != null) healthBar.value = max > 0 ? (float)current / max : 0f;
        if (healthText != null) healthText.text = $"{current} / {max}";
    }

    private void UpdateWave(int current, int total)
    {
        if (waveText != null) waveText.text = $"WAVE {current} / {total}";
    }

    private void ShowGameOver()
    {
        if (gameOverResultText != null && spawner != null)
            gameOverResultText.text = $"You reached wave {spawner.CurrentWave} of {spawner.TotalWaves}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void ShowTargeting(Tower tower)
    {
        if (tower == null || targetingPanel == null) return;

        selectedTower = tower;
        targetingPanel.SetActive(true);
        SetSelectedToggle(tower.Strategy);
    }

    public void HideTargeting()
    {
        selectedTower = null;
        if (targetingPanel != null) targetingPanel.SetActive(false);
    }

    private void OnFirstTargetSelected(bool selected)
    {
        if (selected) ApplyStrategy(new FirstTarget());
    }

    private void OnClosestTargetSelected(bool selected)
    {
        if (selected) ApplyStrategy(new ClosestTarget());
    }

    private void OnStrongestTargetSelected(bool selected)
    {
        if (selected) ApplyStrategy(new StrongestTarget());
    }

    private void ApplyStrategy(ITargetingStrategy strategy)
    {
        if (selectedTower != null) selectedTower.SetStrategy(strategy);
    }

    private void SetSelectedToggle(ITargetingStrategy strategy)
    {
        if (firstTargetToggle != null) firstTargetToggle.SetIsOnWithoutNotify(strategy is FirstTarget);
        if (closestTargetToggle != null) closestTargetToggle.SetIsOnWithoutNotify(strategy is ClosestTarget);
        if (strongestTargetToggle != null) strongestTargetToggle.SetIsOnWithoutNotify(strategy is StrongestTarget);
    }

    private void Restart()
    {
        Time.timeScale = 1f; // BaseHealth freezes time on game over; unfreeze before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
