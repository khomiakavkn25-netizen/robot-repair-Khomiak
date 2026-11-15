using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Lab8Builder
{
    const string MainScenePath = "Assets/_2DAdventureGame/Secne/MainScene.unity";
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";
    const string SpeedBoostPrefabPath = "Assets/_2DAdventureGame/Prefabs/CollectibleSpeedBoost.prefab";
    [MenuItem("Robot Repair/Lab 8/Build MyLevel with Speed Boost")]
    public static void Build()
    {
        RestoreReferenceLayout();
        CreateSpeedBoostPrefab();

        Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
            throw new System.InvalidOperationException("MyLevel needs PlayerCharacter.");

        ConfigureWalkableGrate();
        foreach (SpeedBoostCollectible oldBoost in Object.FindObjectsByType<SpeedBoostCollectible>(FindObjectsSortMode.None))
            Object.DestroyImmediate(oldBoost.gameObject);
        PlaceSpeedBoosts(scene, player);
        ConfigureCamera(player);
        AddMyLevelToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Restored MyLevel with four repairable robots, animated damage zones, and six speed-boost collectibles.");
    }

    [MenuItem("Robot Repair/Lab 8/Add Speed Boosts to Existing MyLevel")]
    public static void AddSpeedBoostsToExistingLevel()
    {
        CreateSpeedBoostPrefab();
        if (EditorSceneManager.GetSceneByPath(LevelPath).isLoaded)
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
            throw new System.InvalidOperationException("MyLevel needs PlayerCharacter.");

        foreach (SpeedBoostCollectible oldBoost in Object.FindObjectsByType<SpeedBoostCollectible>(FindObjectsSortMode.None))
            Object.DestroyImmediate(oldBoost.gameObject);
        PlaceSpeedBoosts(scene, player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Placed six speed-boost collectibles on the existing MyLevel without rebuilding the map.");
    }
    static void RestoreReferenceLayout()
    {
        if (!File.Exists(MainScenePath))
            throw new FileNotFoundException("Reference MainScene was not found.", MainScenePath);
        if (EditorSceneManager.GetSceneByPath(LevelPath).isLoaded)
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        File.Copy(MainScenePath, LevelPath, true);
        AssetDatabase.ImportAsset(LevelPath, ImportAssetOptions.ForceUpdate);
    }

    static void AddMyLevelToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.path != LevelPath)
            .ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(LevelPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void ConfigureWalkableGrate()
    {
        GameObject grate = GameObject.Find("FloorWaterRoundCorners");
        if (grate == null)
            return;

        Vector3 position = grate.transform.position;
        position.y = 1.03f;
        grate.transform.position = position;

        BoxCollider2D collider = grate.GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.isTrigger = true;

        SpriteRenderer renderer = grate.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = -1;
    }

    static void CreateSpeedBoostPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SpeedBoostPrefabPath) != null)
            return;

        const string sourcePath = "Assets/_2DAdventureGame/Prefabs/CollectibleHealth.prefab";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
            throw new FileNotFoundException("CollectibleHealth prefab was not found.", sourcePath);

        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            throw new System.InvalidOperationException("Could not instantiate the collectible prefab.");
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        HealthCollectible health = instance.GetComponent<HealthCollectible>();
        if (health != null)
            Object.DestroyImmediate(health);
        Rigidbody2D body = instance.GetComponent<Rigidbody2D>();
        if (body == null)
            body = instance.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        BoxCollider2D trigger = instance.GetComponent<BoxCollider2D>();
        if (trigger == null)
            trigger = instance.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;

        SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = new Color(0.45f, 0.9f, 1f, 1f);
        instance.AddComponent<SpeedBoostCollectible>();
        instance.name = "CollectibleSpeedBoost";

        bool success;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, SpeedBoostPrefabPath, out success);
        Object.DestroyImmediate(instance);
        if (!success || prefab == null)
            throw new System.InvalidOperationException("CollectibleSpeedBoost prefab was not created.");
    }

    static void PlaceSpeedBoosts(Scene scene, PlayerController player)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpeedBoostPrefabPath);
        Vector3[] offsets =
        {
            new Vector3(4f, 1.5f, 0f),
            new Vector3(-4f, 3f, 0f),
            new Vector3(0f, -4f, 0f),
            new Vector3(8f, 0f, 0f),
            new Vector3(-8f, -1f, 0f),
            new Vector3(4f, -6f, 0f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                throw new System.InvalidOperationException("Could not place all speed-boost collectibles in MyLevel.");

            instance.name = $"Speed Boost {i + 1} (+50%, 5 seconds)";
            instance.transform.position = player.transform.position + offsets[i];
            SpeedBoostCollectible boost = instance.GetComponent<SpeedBoostCollectible>();
            boost.boostFactor = 1.5f;
            boost.boostTime = 5f;
        }
    }

    public static void ConfigureCamera(PlayerController player)
    {
        Camera camera = Camera.main;
        if (camera == null)
            throw new System.InvalidOperationException("MyLevel needs a Main Camera.");

        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        if (follow == null)
            follow = camera.gameObject.AddComponent<CameraFollow2D>();
        follow.target = player.transform;
        follow.useBounds = true;

        Tilemap tilemap = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.gameObject.name == "Tilemap");
        if (tilemap == null)
            return;

        BoundsInt cells = tilemap.cellBounds;
        Vector3 min = tilemap.CellToWorld(new Vector3Int(cells.xMin, cells.yMin, 0));
        Vector3 max = tilemap.CellToWorld(new Vector3Int(cells.xMax, cells.yMax, 0));
        follow.minBounds = min;
        follow.maxBounds = max;
    }

}

[InitializeOnLoad]
public static class MyLevelCameraFollowSetup
{
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";

    static MyLevelCameraFollowSetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += ConfigureIfMyLevelIsOpen;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && scene.path == LevelPath)
            Configure(scene);
    }

    static void ConfigureIfMyLevelIsOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == LevelPath)
            Configure(scene);
    }

    static void Configure(Scene scene)
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
            return;

        Lab8Builder.ConfigureCamera(player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

[InitializeOnLoad]
public static class Lab8AutoSetup
{
    const string SetupKey = "RobotRepair_Lab8_SpeedBoost_SetupCompleted_v4";

    static Lab8AutoSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        if (EditorPrefs.GetBool(SetupKey, false)) return;
        EditorPrefs.SetBool(SetupKey, true);
        try { Lab8Builder.Build(); }
        catch (System.Exception exception)
        {
            EditorPrefs.SetBool(SetupKey, false);
            Debug.LogException(exception);
        }
    }
}

[InitializeOnLoad]
public static class Lab8SpeedBoostAutoApply
{
    const string SetupKey = "RobotRepair_Lab8_ExistingMyLevel_SixSpeedBoosts_v1";

    static Lab8SpeedBoostAutoApply()
    {
        EditorApplication.delayCall += ApplyOnce;
    }

    static void ApplyOnce()
    {
        if (EditorPrefs.GetBool(SetupKey, false))
            return;

        EditorPrefs.SetBool(SetupKey, true);
        try
        {
            Lab8Builder.AddSpeedBoostsToExistingLevel();
        }
        catch (System.Exception exception)
        {
            EditorPrefs.SetBool(SetupKey, false);
            Debug.LogException(exception);
        }
    }
}
