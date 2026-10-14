using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Context in the Strategy pattern.
/// Holds one ITargetingStrategy and delegates the "which enemy do I shoot" decision to it.
/// There is no if/else about targeting anywhere in this class - swapping the strategy
/// changes the tower's behaviour without changing a line of this file.
///
/// A freshly placed tower has no strategy yet. It holds fire until the player picks one.
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float range = 2.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float shotsPerSecond = 2f;
    [SerializeField] private float targetingPreviewSeconds = 0.15f;

    [Header("Shot line (optional)")]
    [SerializeField] private LineRenderer shotLine;
    [SerializeField] private float shotFlashSeconds = 0.06f;

    private const float TargetSpriteHeight = 0.75f;

    private ITargetingStrategy strategy;   // null until the player chooses

    private readonly List<Enemy> enemiesInRange = new List<Enemy>();
    private Enemy currentTarget;
    private float targetingPreviewTimer;
    private float cooldown;
    private float flashTimer;

    public float Range => range;
    public ITargetingStrategy Strategy => strategy;
    public bool HasStrategy => strategy != null;

    /// <summary>
    /// Swap this tower's targeting behaviour. Called by the targeting panel.
    /// The sprite is updated here too, so art can never drift out of sync with behaviour.
    /// </summary>
    public void SetStrategy(ITargetingStrategy newStrategy)
    {
        if (newStrategy == null) return;

        strategy = newStrategy;

        if (StrategyLibrary.Instance != null)
            SetSprite(StrategyLibrary.Instance.SpriteFor(newStrategy));
    }

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null) return;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;

        // Scale from the sprite's own unscaled height so repeated swaps don't compound.
        float spriteHeight = sprite.bounds.size.y;
        if (spriteHeight > 0f)
            transform.localScale = Vector3.one * (TargetSpriteHeight / spriteHeight);
    }

    private void OnMouseDown()
    {
        if (BuildManager.Instance != null)
            BuildManager.Instance.SelectTower(this);
    }

    private void Awake()
    {
        if (shotLine != null) shotLine.enabled = false;
    }

    private void OnDestroy()
    {
        SetCurrentTarget(null);
    }

    private void Update()
    {
        TickShotFlash();

        // No strategy picked yet: the tower just sits there.
        if (strategy == null)
        {
            SetCurrentTarget(null);
            return;
        }

        cooldown -= Time.deltaTime;
        GatherEnemiesInRange();

        // The whole pattern, in one line: ask the strategy, don't decide here.
        Enemy target = strategy.SelectTarget(this, enemiesInRange);
        SetCurrentTarget(target);
        if (target == null) return;

        if (targetingPreviewTimer > 0f)
        {
            targetingPreviewTimer -= Time.deltaTime;
            return;
        }

        if (cooldown > 0f) return;

        target.TakeDamage(damage);
        cooldown = 1f / Mathf.Max(0.01f, shotsPerSecond);
        ShowShot(target.transform.position);
    }

    private void SetCurrentTarget(Enemy target)
    {
        if (currentTarget == target) return;

        if (currentTarget != null)
            currentTarget.SetTargetedBy(this, false);

        currentTarget = target;
        targetingPreviewTimer = target != null ? targetingPreviewSeconds : 0f;

        if (currentTarget != null)
            currentTarget.SetTargetedBy(this, true);
    }

    private void GatherEnemiesInRange()
    {
        enemiesInRange.Clear();

        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in all)
        {
            if (enemy == null) continue;
            if (Vector2.Distance(transform.position, enemy.transform.position) <= range)
                enemiesInRange.Add(enemy);
        }
    }

    private void ShowShot(Vector3 targetPosition)
    {
        if (shotLine == null) return;

        shotLine.useWorldSpace = true;
        shotLine.positionCount = 2;
        shotLine.SetPosition(0, transform.position);
        shotLine.SetPosition(1, targetPosition);
        shotLine.enabled = true;
        flashTimer = shotFlashSeconds;
    }

    private void TickShotFlash()
    {
        if (shotLine == null || !shotLine.enabled) return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) shotLine.enabled = false;
    }

    // Shows the range circle in the Scene view when the tower is selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
