using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds two bits of on-screen help at runtime, so there is nothing to wire in the Inspector:
///   1. A hint banner telling the player what to do. It disappears once they place a tower.
///   2. An "i" button that opens a panel listing every targeting strategy and what it shoots.
///      Opening it pauses the game; closing it resumes.
///
/// The list is read from StrategyLibrary, and the text comes from each strategy's own
/// DisplayName and Description - so adding a fourth strategy updates this screen by itself.
///
/// Put this component on the same GameObject as BuildManager.
/// </summary>
public class StrategyInfoUI : MonoBehaviour
{
    [SerializeField]
    private string hintMessage = "Click a tower spot, then pick how that tower aims.";

    [SerializeField] private bool pauseWhileInfoIsOpen = true;
    [SerializeField] private Color panelColor = new Color(1f, 1f, 1f, 0.97f);
    [SerializeField] private Color inkColor = new Color(0.11f, 0.11f, 0.13f, 1f);

    private GameObject hintObject;
    private GameObject infoPanel;
    private Font font;
    private float timeScaleBeforePause = 1f;

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
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("StrategyInfoUI: no Canvas in the scene, nothing to attach to.");
            return;
        }

        BuildHint(canvas.transform);
        BuildInfoButton(canvas.transform);
        BuildInfoPanel(canvas.transform);
    }

    private void HandleTowerBuilt(Tower tower)
    {
        if (hintObject != null) hintObject.SetActive(false);
    }

    // ---------- open / close ----------

    private void ToggleInfoPanel()
    {
        if (infoPanel == null) return;

        if (infoPanel.activeSelf) CloseInfoPanel();
        else OpenInfoPanel();
    }

    private void OpenInfoPanel()
    {
        infoPanel.SetActive(true);

        if (!pauseWhileInfoIsOpen) return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void CloseInfoPanel()
    {
        infoPanel.SetActive(false);

        if (!pauseWhileInfoIsOpen) return;

        // Put back whatever it was. If the base was already destroyed the game stays frozen.
        Time.timeScale = timeScaleBeforePause;
    }

    // ---------- pieces ----------

    private void BuildHint(Transform parent)
    {
        RectTransform rect = MakeRect("Hint", parent,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -100f), size: new Vector2(820f, 64f));

        AddImage(rect, panelColor);
        MakeText("HintText", rect, hintMessage, 28, TextAnchor.MiddleCenter, FontStyle.Normal);

        hintObject = rect.gameObject;
    }

    private void BuildInfoButton(Transform parent)
    {
        RectTransform rect = MakeRect("InfoButton", parent,
            anchor: new Vector2(0f, 0f), pivot: new Vector2(0f, 0f),
            anchoredPos: new Vector2(28f, 28f), size: new Vector2(60f, 60f));

        Image image = AddImage(rect, panelColor);
        MakeText("InfoLabel", rect, "i", 36, TextAnchor.MiddleCenter, FontStyle.Bold);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ToggleInfoPanel);
    }

    private void BuildInfoPanel(Transform parent)
    {
        RectTransform rect = MakeRect("InfoPanel", parent,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: new Vector2(820f, 520f));

        AddImage(rect, panelColor);

        RectTransform titleRect = MakeRect("Title", rect,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -24f), size: new Vector2(740f, 48f));
        MakeText("TitleText", titleRect, "TARGETING STRATEGIES", 34, TextAnchor.MiddleCenter, FontStyle.Bold);

        RectTransform bodyRect = MakeRect("Body", rect,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -84f), size: new Vector2(740f, 330f));
        MakeText("BodyText", bodyRect, BuildStrategyText(), 27, TextAnchor.UpperLeft, FontStyle.Normal);

        RectTransform closeRect = MakeRect("Close", rect,
            anchor: new Vector2(0.5f, 0f), pivot: new Vector2(0.5f, 0f),
            anchoredPos: new Vector2(0f, 28f), size: new Vector2(200f, 56f));

        Image closeImage = AddImage(closeRect, inkColor);
        MakeText("CloseLabel", closeRect, "Close", 28, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

        Button closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(CloseInfoPanel);

        infoPanel = rect.gameObject;
        infoPanel.SetActive(false);
    }

    private string BuildStrategyText()
    {
        if (StrategyLibrary.Instance == null)
            return "No StrategyLibrary found in the scene.";

        StringBuilder sb = new StringBuilder();
        foreach (ITargetingStrategy strategy in StrategyLibrary.Instance.Options)
        {
            sb.Append("<b>");
            sb.Append(strategy.DisplayName);
            sb.Append("</b>\n");
            sb.Append("     ");
            sb.Append(strategy.Description);
            sb.Append("\n\n");
        }

        return sb.ToString().TrimEnd();
    }

    // ---------- tiny UI helpers ----------

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
                          TextAnchor alignment, FontStyle style)
    {
        return MakeText(name, parent, content, size, alignment, style, inkColor);
    }

    private Text MakeText(string name, Transform parent, string content, int size,
                          TextAnchor alignment, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 10f);
        rect.offsetMax = new Vector2(-16f, -10f);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.fontStyle = style;
        text.color = color;
        text.supportRichText = true;
        text.lineSpacing = 1.15f;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
