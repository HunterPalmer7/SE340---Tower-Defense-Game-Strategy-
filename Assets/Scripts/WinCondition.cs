using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Watches for the player clearing the game: the last wave has started, every enemy is gone,
/// and the base survived. Shows a win panel and freezes the game.
///
/// The panel is built at runtime, so there is nothing to wire in the Inspector.
/// Put this component on the same GameObject as BuildManager.
/// </summary>
public class WinCondition : MonoBehaviour
{
    [Tooltip("How long the map must stay empty on the final wave before it counts as cleared. " +
             "Set this a little above the spawner's Spawn Interval so a lull mid-wave isn't mistaken for a win.")]
    [SerializeField] private float secondsClearToWin = 3f;

    [SerializeField] private Color panelColor = new Color(1f, 1f, 1f, 0.97f);
    [SerializeField] private Color inkColor = new Color(0.11f, 0.11f, 0.13f, 1f);

    private EnemySpawner spawner;
    private BaseHealth baseHealth;
    private GameObject winPanel;
    private Text winDetailText;
    private Font font;

    private float clearTimer;
    private bool hasWon;

    private void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        baseHealth = FindFirstObjectByType<BaseHealth>();

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) BuildWinPanel(canvas.transform);
    }

    private void Update()
    {
        if (hasWon || spawner == null) return;

        // Lost already - the game over panel owns the screen.
        if (baseHealth != null && baseHealth.IsDestroyed) return;

        // Waves haven't finished starting yet.
        if (spawner.TotalWaves == 0 || spawner.CurrentWave < spawner.TotalWaves)
        {
            clearTimer = 0f;
            return;
        }

        // Still something alive on the map.
        if (FindFirstObjectByType<Enemy>() != null)
        {
            clearTimer = 0f;
            return;
        }

        clearTimer += Time.deltaTime;
        if (clearTimer >= secondsClearToWin) Win();
    }

    private void Win()
    {
        hasWon = true;

        if (winDetailText != null && spawner != null)
            winDetailText.text = $"You cleared all {spawner.TotalWaves} waves.";

        if (winPanel != null) winPanel.SetActive(true);

        Time.timeScale = 0f;
        Debug.Log("All waves cleared - you win.");
    }

    // ---------- runtime UI ----------

    private void BuildWinPanel(Transform parent)
    {
        RectTransform rect = MakeRect("WinPanel", parent,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: new Vector2(700f, 360f));

        AddImage(rect, panelColor);

        RectTransform titleRect = MakeRect("Title", rect,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -48f), size: new Vector2(620f, 70f));
        MakeText("TitleText", titleRect, "BASE DEFENDED", 48, TextAnchor.MiddleCenter, FontStyle.Bold, inkColor);

        RectTransform detailRect = MakeRect("Detail", rect,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -140f), size: new Vector2(620f, 60f));
        winDetailText = MakeText("DetailText", detailRect, "You cleared every wave.", 28,
                                 TextAnchor.MiddleCenter, FontStyle.Normal, inkColor);

        RectTransform buttonRect = MakeRect("Restart", rect,
            anchor: new Vector2(0.5f, 0f), pivot: new Vector2(0.5f, 0f),
            anchoredPos: new Vector2(0f, 44f), size: new Vector2(220f, 60f));

        Image buttonImage = AddImage(buttonRect, inkColor);
        MakeText("RestartLabel", buttonRect, "Play Again", 28, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Restart);

        winPanel = rect.gameObject;
        winPanel.SetActive(false);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
                                   Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return rect;
    }

    private Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text MakeText(string name, Transform parent, string content, int size,
                          TextAnchor alignment, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 8f);
        rect.offsetMax = new Vector2(-16f, -8f);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.fontStyle = style;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
