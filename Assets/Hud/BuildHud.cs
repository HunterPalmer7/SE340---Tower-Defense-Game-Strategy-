using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > Build HUD. Builds the HUD Canvas from the wireframes in the open scene and
/// connects it to HudUI. Safe to run once; it refuses if a HudUI already exists.
/// Uses Unity's built-in UI Text so nothing needs importing. The targeting panel and build bar are
/// created and wired to HudUI, so clicking a built tower can change its targeting strategy.
/// </summary>
public static class BuildHud
{
    private static readonly Color Panel = new Color(1f, 1f, 1f, 0.92f);
    private static readonly Color Ink = new Color(0.12f, 0.12f, 0.11f, 1f);
    private static readonly Color Button = new Color(0.95f, 0.95f, 0.93f, 1f);

    [MenuItem("Tools/Build HUD")]
    private static void Build()
    {
        if (Object.FindFirstObjectByType<HudUI>() != null)
        {
            EditorUtility.DisplayDialog("Build HUD", "A HudUI already exists in this scene. Delete the old HUD Canvas first to rebuild.", "OK");
            return;
        }

        BuildManager buildManager = Object.FindFirstObjectByType<BuildManager>();
        if (buildManager == null)
        {
            Debug.LogError("Build HUD could not find a BuildManager in the open scene. Build menu buttons cannot be wired.");
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---- Canvas ----
        var canvasGO = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build HUD");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var hud = canvasGO.AddComponent<HudUI>();
        Transform root = canvasGO.transform;

        // ---- 1. Base health (top-left) ----
        var healthPanel = Rect("BaseHealthPanel", root);
        Anchor(healthPanel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(480, 100));
        Img(healthPanel, Panel);

        var healthLabel = Rect("HealthLabel", healthPanel);
        Stretch(healthLabel, 16, 10, 16, 10);
        Txt(healthLabel, font, "BASE HEALTH", 22, TextAnchor.UpperLeft, Ink, FontStyle.Bold);

        var healthValue = Rect("HealthText", healthPanel);
        Stretch(healthValue, 16, 8, 16, 10);
        Text healthText = Txt(healthValue, font, "20 / 20", 28, TextAnchor.UpperRight, Ink, FontStyle.Bold);

        var sliderRT = Rect("HealthBar", healthPanel);
        sliderRT.anchorMin = new Vector2(0, 0);
        sliderRT.anchorMax = new Vector2(1, 0);
        sliderRT.pivot = new Vector2(0.5f, 0);
        sliderRT.anchoredPosition = new Vector2(0, 14);
        sliderRT.sizeDelta = new Vector2(-32, 26);
        Img(sliderRT, new Color(0.25f, 0.25f, 0.24f, 1f));
        var fillArea = Rect("Fill Area", sliderRT);
        Stretch(fillArea, 0, 0, 0, 0);
        var fill = Rect("Fill", fillArea);
        fill.sizeDelta = Vector2.zero;
        Img(fill, new Color(0.80f, 0.22f, 0.20f, 1f));
        var slider = sliderRT.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.targetGraphic = null;
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 1f;

        // ---- 2. Wave counter (top-right) ----
        var wavePanel = Rect("WavePanel", root);
        Anchor(wavePanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(320, 100));
        Img(wavePanel, Panel);
        var waveRT = Rect("WaveText", wavePanel);
        Stretch(waveRT, 8, 8, 8, 8);
        Text waveText = Txt(waveRT, font, "WAVE 0 / 0", 40, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);

        // ---- 3. Build bar (right edge, vertical) ----
        var buildBar = Rect("BuildBar", root);
        Img(buildBar, Panel);
        LayoutBuildBar(buildBar);
        // Button firstButton = MakeButton("First", buildBar, font, "First", 26, Button, Ink);
        // Button closestButton = MakeButton("Closest", buildBar, font, "Closest", 26, Button, Ink);
        // Button strongestButton = MakeButton("Strongest", buildBar, font, "Strongest", 26, Button, Ink);
        // WireBuildButton(firstButton, buildManager.SelectFirst);
        // WireBuildButton(closestButton, buildManager.SelectClosest);
        // WireBuildButton(strongestButton, buildManager.SelectStrongest);

        // ---- 4. Targeting panel (hidden until a tower is clicked) ----
        var targeting = Rect("TargetingPanel", root);
        Anchor(targeting, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 360));
        Img(targeting, Panel);
        var v = targeting.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 20, 20);
        v.spacing = 12;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var group = targeting.gameObject.AddComponent<ToggleGroup>();
        group.allowSwitchOff = false;

        var targetTitle = Rect("Title", targeting);
        Txt(targetTitle, font, "TARGETING", 34, TextAnchor.MiddleLeft, Ink, FontStyle.Bold);
        targetTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;

        MakeToggle("FirstToggle", targeting, font, "First", group, true);
        MakeToggle("ClosestToggle", targeting, font, "Closest", group, false);
        MakeToggle("StrongestToggle", targeting, font, "Strongest", group, false);
        Button closeTargetingButton = MakeButton("CloseTargeting", targeting, font, "Close", 24, Button, Ink);
        closeTargetingButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
        WireBuildButton(closeTargetingButton, hud.HideTargeting);
        targeting.gameObject.SetActive(false);

        // ---- 5. Game over panel (hidden) ----
        var gameOver = Rect("GameOverPanel", root);
        Stretch(gameOver, 0, 0, 0, 0);
        Img(gameOver, new Color(0f, 0f, 0f, 0.65f));

        var box = Rect("Box", gameOver);
        Anchor(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 440));
        Img(box, Color.white);

        var title = Rect("TitleText", box);
        Anchor(title, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(760, 100));
        Txt(title, font, "BASE DESTROYED", 64, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);

        var result = Rect("ResultText", box);
        Anchor(result, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(760, 80));
        Text resultText = Txt(result, font, "You reached wave 0 of 0", 36, TextAnchor.MiddleCenter, Ink, FontStyle.Normal);

        var restartRT = Rect("RestartButton", box);
        Anchor(restartRT, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(320, 96));
        Img(restartRT, Ink);
        var restartButton = restartRT.gameObject.AddComponent<UnityEngine.UI.Button>();
        var restartLabel = Rect("Label", restartRT);
        Stretch(restartLabel, 0, 0, 0, 0);
        Txt(restartLabel, font, "Restart", 36, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        gameOver.gameObject.SetActive(false);

        // ---- Wire HudUI ----
        var so = new SerializedObject(hud);
        so.FindProperty("healthBar").objectReferenceValue = slider;
        so.FindProperty("healthText").objectReferenceValue = healthText;
        so.FindProperty("waveText").objectReferenceValue = waveText;
        so.FindProperty("gameOverPanel").objectReferenceValue = gameOver.gameObject;
        so.FindProperty("gameOverResultText").objectReferenceValue = resultText;
        so.FindProperty("restartButton").objectReferenceValue = restartButton;
        so.FindProperty("targetingPanel").objectReferenceValue = targeting.gameObject;
        so.FindProperty("firstTargetToggle").objectReferenceValue =
            targeting.Find("FirstToggle").GetComponent<Toggle>();
        so.FindProperty("closestTargetToggle").objectReferenceValue =
            targeting.Find("ClosestToggle").GetComponent<Toggle>();
        so.FindProperty("strongestTargetToggle").objectReferenceValue =
            targeting.Find("StrongestToggle").GetComponent<Toggle>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- EventSystem (buttons need one) ----
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Build HUD");
        }

        // ---- Restart reloads the scene by index, so it must be in Build Settings ----
        var scene = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path))
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == scene.path))
            {
                list.Add(new EditorBuildSettingsScene(scene.path, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasGO;
        Debug.Log("HUD built. Save the scene (Ctrl+S). Targeting panel and Game Over panel start hidden.");
    }

    [MenuItem("Tools/Wire Tower Sprites")]
    private static void WireTowerSprites()
    {
        BuildManager buildManager = Object.FindFirstObjectByType<BuildManager>();
        if (buildManager == null)
        {
            EditorUtility.DisplayDialog("Wire Tower Sprites", "No BuildManager was found in the open scene.", "OK");
            return;
        }

        Sprite turbo = LoadSprite("Assets/prefabs/TowerS/Turbo.png", "Turbo_0");
        Sprite spongebob = LoadSprite("Assets/prefabs/TowerS/Spongebob.png", "Spongebob_0");
        Sprite patrick = LoadSprite("Assets/prefabs/TowerS/Patrick.png", "Patrick_0");

        if (turbo == null || spongebob == null || patrick == null)
        {
            EditorUtility.DisplayDialog(
                "Wire Tower Sprites",
                "One or more tower sprites could not be found. Check the PNG paths and import settings.",
                "OK");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(buildManager);
        serializedManager.FindProperty("firstTowerSprite").objectReferenceValue = turbo;
        serializedManager.FindProperty("closestTowerSprite").objectReferenceValue = spongebob;
        serializedManager.FindProperty("strongestTowerSprite").objectReferenceValue = patrick;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Tower sprites wired: Turbo = First, Spongebob = Closest, Patrick = Strongest. Save the scene.");
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null && sprite.name == spriteName) return sprite;
        }

        Debug.LogError($"Could not find sprite '{spriteName}' at '{path}'.");
        return null;
    }

    /// <summary>Puts the build bar in a vertical column centered on the right edge (keeps the lanes clear).</summary>
    private static void LayoutBuildBar(RectTransform bar)
    {
        Anchor(bar, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(220, 440));

        var oldH = bar.GetComponent<HorizontalLayoutGroup>();
        if (oldH != null) Object.DestroyImmediate(oldH);

        var v = bar.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = bar.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 16, 16);
        v.spacing = 16;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = true;
    }

    [MenuItem("Tools/Move Build Bar To Right")]
    private static void MoveBuildBarToRight()
    {
        GameObject go = GameObject.Find("BuildBar");
        if (go == null)
        {
            EditorUtility.DisplayDialog("Move Build Bar", "No object named BuildBar found in the open scene.", "OK");
            return;
        }
        var rt = go.GetComponent<RectTransform>();
        Undo.RegisterFullObjectHierarchyUndo(go, "Move Build Bar");
        LayoutBuildBar(rt);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Build bar moved to the right edge. Save the scene (Ctrl+S).");
    }

    // ---------- helpers ----------
    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static Image Img(RectTransform rt, Color c)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.color = c;
        return i;
    }

    private static Text Txt(RectTransform rt, Font font, string s, int size, TextAnchor anchor, Color c, FontStyle style)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = c;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static Button MakeButton(string name, Transform parent, Font font, string label, int size, Color bg, Color ink)
    {
        var rt = Rect(name, parent);
        Img(rt, bg);
        Button button = rt.gameObject.AddComponent<Button>();
        var l = Rect("Label", rt);
        Stretch(l, 0, 0, 0, 0);
        Txt(l, font, label, size, TextAnchor.MiddleCenter, ink, FontStyle.Bold);
        return button;
    }

    private static void WireBuildButton(Button button, UnityAction action)
    {
        UnityEventTools.AddVoidPersistentListener(button.onClick, action);
    }

    private static void MakeToggle(string name, Transform parent, Font font, string label, ToggleGroup group, bool on)
    {
        var rt = Rect(name, parent);
        var bg = Img(rt, new Color(0.93f, 0.93f, 0.91f, 1f));
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;

        var check = Rect("Checkmark", rt);
        Anchor(check, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(28, 28));
        var checkImg = Img(check, new Color(0.14f, 0.28f, 0.84f, 1f));

        var l = Rect("Label", rt);
        Stretch(l, 64, 0, 12, 0);
        Txt(l, font, label, 28, TextAnchor.MiddleLeft, new Color(0.12f, 0.12f, 0.11f, 1f), FontStyle.Bold);

        var toggle = rt.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic = checkImg;
        toggle.group = group;
        toggle.isOn = on;
    }
}
