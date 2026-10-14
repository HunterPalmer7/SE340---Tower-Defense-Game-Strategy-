using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Apply Wireframe Look.
/// Restyles the map to match the wireframes: pale background, wide grey path with a dashed center line,
/// dashed "+" tower spots, dark base marker and grey enemies. Creates a TowerSpot prefab and places
/// example spots beside the path (only the first time). Safe to run again.
/// </summary>
public static class ApplyWireframeLook
{
    private const string Dir = "Assets/Art/Wireframe";
    private static readonly Color Ink = new Color(0.23f, 0.22f, 0.21f, 1f);

    [MenuItem("Tools/Apply Wireframe Look")]
    private static void Apply()
    {
        System.IO.Directory.CreateDirectory(Dir);

        Sprite dashedSquare = SaveSprite(Dir + "/TowerSpotDashed.png", MakeDashedSquare());
        Sprite solidSquare = SaveSprite(Dir + "/SolidSquare.png", MakeSolid(64, new Color(0.16f, 0.16f, 0.15f, 1f)));
        Texture2D dashTex = SaveTexture(Dir + "/PathDash.png", MakeDash());

        Material pathMat = GetOrCreateMaterial(Dir + "/PathMat.mat", null);
        Material dashMat = GetOrCreateMaterial(Dir + "/PathDashMat.mat", dashTex);

        // Camera: pale grey background like the wireframe
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Wireframe look");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.855f, 0.851f, 0.827f, 1f);
        }

        // Path: wide grey line + dashed center line
        Path path = Object.FindFirstObjectByType<Path>();
        if (path != null)
        {
            Undo.RecordObject(path, "Wireframe look");
            path.width = 0.8f;
            EditorUtility.SetDirty(path);

            var lr = path.GetComponent<LineRenderer>();
            lr.sharedMaterial = pathMat;
            lr.startColor = lr.endColor = new Color(0.74f, 0.74f, 0.71f, 1f);
            lr.numCornerVertices = 6;
            lr.numCapVertices = 6;
            lr.sortingOrder = -10;

            PathCenterLine center = Object.FindFirstObjectByType<PathCenterLine>();
            if (center == null)
            {
                var go = new GameObject("PathCenterLine", typeof(LineRenderer), typeof(PathCenterLine));
                Undo.RegisterCreatedObjectUndo(go, "Wireframe look");
                center = go.GetComponent<PathCenterLine>();
            }
            var clr = center.GetComponent<LineRenderer>();
            clr.sharedMaterial = dashMat;
            clr.startColor = clr.endColor = new Color(0.56f, 0.55f, 0.52f, 1f);
            clr.widthMultiplier = 0.07f;
            clr.numCornerVertices = 2;
            clr.textureMode = LineTextureMode.Tile;
            clr.textureScale = new Vector2(2.5f, 1f);
            clr.sortingOrder = -9;
        }
        else Debug.LogWarning("Wireframe look: no Path found in the scene.");

        // Base marker: dark square
        BaseHealth baseHealth = Object.FindFirstObjectByType<BaseHealth>();
        if (baseHealth != null)
        {
            var sr = baseHealth.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(baseHealth.gameObject);
            sr.sprite = solidSquare;
            sr.sortingOrder = -4;
            Undo.RecordObject(baseHealth.transform, "Wireframe look");
            baseHealth.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        }

        // Tower spot prefab
        string prefabPath = "Assets/prefabs/TowerSpot.prefab";
        GameObject spotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (spotPrefab == null)
        {
            var go = new GameObject("TowerSpot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dashedSquare;
            sr.sortingOrder = -5;
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            go.AddComponent<TowerSpot>();
            spotPrefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        // Example spots beside the path (first run only)
        if (path != null && GameObject.Find("Tower Spots") == null)
            PlaceSpots(path, spotPrefab);

        // Enemy prefabs: grey circles, darker = tougher, drawn above the path
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/prefabs/Enemies" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(p);
            var sr = contents.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                string n = contents.name;
                sr.color = n.Contains("Small") ? new Color(0.61f, 0.61f, 0.58f, 1f)
                         : n.Contains("Mid") ? new Color(0.42f, 0.42f, 0.39f, 1f)
                         : new Color(0.23f, 0.22f, 0.21f, 1f);
                sr.sortingOrder = 5;
                PrefabUtility.SaveAsPrefabAsset(contents, p);
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Wireframe look applied. Save the scene (Ctrl+S).");
    }

    // ---------- tower spots ----------
    private static void PlaceSpots(Path path, GameObject prefab)
    {
        var parent = new GameObject("Tower Spots");
        Undo.RegisterCreatedObjectUndo(parent, "Wireframe look");

        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 a = path.Point(i);
            Vector2 b = path.Point(i + 1);
            float len = Vector2.Distance(a, b);
            if (len < 4f) continue;

            Vector2 dir = (b - a) / len;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            for (int k = 1; k <= 2; k++)
            {
                float side = ((i + k) % 2 == 0) ? 1f : -1f;
                Vector2 p = a + dir * (len * k / 3f) + normal * side * 1.3f;
                if (Mathf.Abs(p.x) > 8f || Mathf.Abs(p.y) > 4.4f) continue;
                if (DistanceToPath(path, p) < 1.1f) continue;

                var spot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                spot.transform.SetParent(parent.transform);
                spot.transform.position = new Vector3(p.x, p.y, 0f);
                Undo.RegisterCreatedObjectUndo(spot, "Wireframe look");
            }
        }
    }

    private static float DistanceToPath(Path path, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 a = path.Point(i);
            Vector2 b = path.Point(i + 1);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
        }
        return best;
    }

    // ---------- generated images ----------
    private static Texture2D MakeSolid(int size, Color c)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        t.SetPixels(px);
        return t;
    }

    private static Texture2D MakeDashedSquare()
    {
        const int s = 64;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                bool onBorder = x < 3 || x >= s - 3 || y < 3 || y >= s - 3;
                bool horizontalEdge = y < 3 || y >= s - 3;
                int along = horizontalEdge ? x : y;
                bool dash = (along / 8) % 2 == 0;
                bool plus = (Mathf.Abs(x - 31.5f) <= 10f && Mathf.Abs(y - 31.5f) <= 1.5f)
                         || (Mathf.Abs(y - 31.5f) <= 10f && Mathf.Abs(x - 31.5f) <= 1.5f);
                t.SetPixel(x, y, (onBorder && dash) || plus ? Ink : clear);
            }
        }
        return t;
    }

    private static Texture2D MakeDash()
    {
        var t = new Texture2D(16, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 16; x++)
                t.SetPixel(x, y, x < 9 ? Color.white : new Color(1, 1, 1, 0));
        return t;
    }

    private static Sprite SaveSprite(string path, Texture2D tex)
    {
        WritePng(path, tex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 64;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D SaveTexture(string path, Texture2D tex)
    {
        WritePng(path, tex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void WritePng(string path, Texture2D tex)
    {
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static Material GetOrCreateMaterial(string path, Texture2D tex)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.mainTexture = tex;
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
