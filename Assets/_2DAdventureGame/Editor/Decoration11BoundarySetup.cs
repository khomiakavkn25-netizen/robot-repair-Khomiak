using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Decoration11BoundarySetup
{
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";

    static Decoration11BoundarySetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += ApplyToActiveMyLevel;
    }

    [MenuItem("Robot Repair/Collision/Apply Decoration_11 boundaries to MyLevel")]
    static void ApplyFromMenu() => ApplyToActiveMyLevel();

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && scene.path == LevelPath)
            ApplyBoundaries(scene);
    }

    static void ApplyToActiveMyLevel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == LevelPath)
            ApplyBoundaries(scene);
    }

    static void ApplyBoundaries(Scene scene)
    {
        bool changed = false;
        foreach (SpriteRenderer spriteRenderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (spriteRenderer.gameObject.scene != scene)
                continue;

            Sprite sprite = spriteRenderer.sprite;
            bool isDecoration11 = spriteRenderer.gameObject.name.IndexOf("Decoration_11", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (sprite != null && sprite.name.IndexOf("Decoration_11", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!isDecoration11)
                continue;

            BoxCollider2D boundary = spriteRenderer.GetComponent<BoxCollider2D>();
            if (boundary == null)
                boundary = Undo.AddComponent<BoxCollider2D>(spriteRenderer.gameObject);

            boundary.isTrigger = false;
            if (sprite != null)
            {
                boundary.size = sprite.bounds.size;
                boundary.offset = sprite.bounds.center;
            }
            changed = true;
        }

        if (!changed)
            return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Added solid BoxCollider2D boundaries to Decoration_11 objects in MyLevel.");
    }
}
