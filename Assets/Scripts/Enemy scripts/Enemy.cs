using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Walks along the scene's Path (WP0 -> WP5...). Health, move speed, and damage are set in the
/// Inspector, so each enemy type is just a prefab with different numbers.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Stats (change per prefab)")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int damage = 1; // dealt to the player's base if it reaches the end

    [Header("Options")]
    [SerializeField] private bool rotateToFaceDirection = false;

    // Other scripts (UI, money, base health) can subscribe to these.
    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemyReachedEnd;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public int Damage => damage;

    /// <summary>Total distance walked along the path. FirstTarget can pick the highest value.</summary>
    public float DistanceTraveled { get; private set; }

    private int targetIndex = 1;
    private bool finished;
    private readonly Dictionary<Tower, GameObject> targetingIndicators = new Dictionary<Tower, GameObject>();

    private void Start()
    {
        CurrentHealth = maxHealth;

        if (Path.Instance == null || Path.Instance.Count < 2)
        {
            Debug.LogError("Enemy needs a Path in the scene with at least 2 waypoint children.");
            enabled = false;
            return;
        }

        transform.position = Path.Instance.Point(0);
    }

    private void Update()
    {
        if (finished) return;

        Vector3 target = Path.Instance.Point(targetIndex);
        Vector3 toTarget = target - transform.position;
        float step = moveSpeed * Time.deltaTime;

        if (rotateToFaceDirection && toTarget.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (toTarget.magnitude <= step)
        {
            DistanceTraveled += toTarget.magnitude;
            transform.position = target;
            targetIndex++;

            if (targetIndex >= Path.Instance.Count)
                ReachEnd();
        }
        else
        {
            transform.position += toTarget.normalized * step;
            DistanceTraveled += step;
        }
    }

    public void TakeDamage(float amount)
    {
        if (finished) return;

        CurrentHealth -= amount;
        if (CurrentHealth <= 0f)
            Die();
    }

    public void SetTargetedBy(Tower tower, bool targeted)
    {
        if (tower == null) return;

        if (targeted)
        {
            if (!targetingIndicators.ContainsKey(tower))
                targetingIndicators.Add(tower, CreateTargetingIndicator());
        }
        else if (targetingIndicators.TryGetValue(tower, out GameObject indicator))
        {
            Destroy(indicator);
            targetingIndicators.Remove(tower);
        }

        ArrangeTargetingIndicators();
    }

    private GameObject CreateTargetingIndicator()
    {
        GameObject indicator = new GameObject("TargetingIndicator");
        indicator.transform.SetParent(transform, false);
        indicator.transform.localPosition = new Vector3(0f, 0f, -0.1f);

        TextMesh label = indicator.AddComponent<TextMesh>();
        label.text = "X";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 96;
        label.characterSize = 0.2f;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.red;

        MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 20;
        return indicator;
    }

    private void ArrangeTargetingIndicators()
    {
        int indicatorCount = targetingIndicators.Count;
        if (indicatorCount == 0) return;

        int index = 0;
        foreach (GameObject indicator in targetingIndicators.Values)
        {
            float centeredIndex = index - (indicatorCount - 1) * 0.5f;
            indicator.transform.localPosition = new Vector3(centeredIndex * 0.32f, 0f, -0.1f);
            index++;
        }
    }

    private void Die()
    {
        finished = true;
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void ReachEnd()
    {
        finished = true;
        OnEnemyReachedEnd?.Invoke(this); // base health script should subtract enemy.Damage
        Destroy(gameObject);
    }
}
