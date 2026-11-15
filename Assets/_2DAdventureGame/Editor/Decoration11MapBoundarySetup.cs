using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class Decoration11MapBoundarySetup
{
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";
    const string DecorationSpritePath = "Assets/_2DAdventureGame/Animation/Art/Ruby/Environment/Decoration_11.png";
    const string BoundaryRootName = "Decoration_11 Map Boundary";

    static Decoration11MapBoundarySetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += BuildForActiveScene;
    }

    [MenuItem("Robot Repair/Map/Build Decoration_11 perimeter")]
    static void BuildFromMenu() => BuildForActiveScene();

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && scene.path == LevelPath)
            Build(scene);
    }

    static void BuildForActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == LevelPath)
            Build(scene);
    }

    static void Build(Scene scene)
    {
        Tilemap tilemap = UnityEngine.Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null || tilemap.gameObject.scene != scene)
            return;

        Sprite decoration = AssetDatabase.LoadAssetAtPath<Sprite>(DecorationSpritePath);
        if (decoration == null)
        {
            Debug.LogError("MyLevel needs the Ruby Decoration_11 sprite.");
            return;
        }

        GameObject oldRoot = GameObject.Find(BoundaryRootName);
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot);

        GameObject root = new GameObject(BoundaryRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        BoundsInt cells = tilemap.cellBounds;
        Vector3 min = tilemap.CellToWorld(new Vector3Int(cells.xMin, cells.yMin, cells.zMin));
        Vector3 max = tilemap.CellToWorld(new Vector3Int(cells.xMax, cells.yMax, cells.zMax));
        Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
        float wallThickness = Mathf.Max(decoration.bounds.size.y, 0.5f);
        float preferredSegmentLength = Mathf.Max(decoration.bounds.size.x, 1f);
        const float topOverlap = 0.9f;
        const float leftOverlap = 0.9f;
        const float otherEdgeOverlap = 0.12f;

        CreateHorizontalEdge(root.transform, decoration, bounds.min.x - wallThickness,
            bounds.max.x + wallThickness, bounds.min.y - wallThickness * 0.5f + otherEdgeOverlap,
            wallThickness, preferredSegmentLength, "Bottom");
        CreateHorizontalEdge(root.transform, decoration, bounds.min.x - wallThickness,
            bounds.max.x + wallThickness, bounds.max.y + wallThickness * 0.5f - topOverlap,
            wallThickness, preferredSegmentLength, "Top");
        CreateVerticalEdge(root.transform, decoration, bounds.min.y - wallThickness,
            bounds.max.y + wallThickness, bounds.min.x - wallThickness * 0.5f + leftOverlap,
            wallThickness, preferredSegmentLength, "Left");
        CreateVerticalEdge(root.transform, decoration, bounds.min.y - wallThickness,
            bounds.max.y + wallThickness, bounds.max.x + wallThickness * 0.5f - otherEdgeOverlap,
            wallThickness, preferredSegmentLength, "Right");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Built a solid Decoration_11 perimeter around MyLevel.");
    }

    static void CreateHorizontalEdge(Transform root, Sprite sprite, float startX, float endX,
        float y, float thickness, float preferredLength, string side)
    {
        int count = Mathf.Max(1, Mathf.CeilToInt((endX - startX) / preferredLength));
        float segmentLength = (endX - startX) / count;
        for (int i = 0; i < count; i++)
        {
            float x = startX + segmentLength * (i + 0.5f);
            CreateSegment(root, sprite, new Vector3(x, y, 0), segmentLength, thickness,
                0f, $"Decoration_11 {side} {i + 1}");
        }
    }

    static void CreateVerticalEdge(Transform root, Sprite sprite, float startY, float endY,
        float x, float thickness, float preferredLength, string side)
    {
        int count = Mathf.Max(1, Mathf.CeilToInt((endY - startY) / preferredLength));
        float segmentLength = (endY - startY) / count;
        for (int i = 0; i < count; i++)
        {
            float y = startY + segmentLength * (i + 0.5f);
            CreateSegment(root, sprite, new Vector3(x, y, 0), segmentLength, thickness,
                90f, $"Decoration_11 {side} {i + 1}");
        }
    }

    static void CreateSegment(Transform root, Sprite sprite, Vector3 position,
        float length, float thickness, float rotation, string name)
    {
        GameObject segment = new GameObject(name);
        segment.transform.SetParent(root, true);
        segment.transform.position = position;
        segment.transform.rotation = Quaternion.Euler(0, 0, rotation);
        segment.transform.localScale = new Vector3(
            length / sprite.bounds.size.x,
            thickness / sprite.bounds.size.y,
            1f);

        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;

        BoxCollider2D collider = segment.AddComponent<BoxCollider2D>();
        collider.size = sprite.bounds.size;
        collider.offset = sprite.bounds.center;
        collider.isTrigger = false;
    }
}
