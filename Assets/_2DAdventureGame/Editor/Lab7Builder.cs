using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Lab7Builder
{
    const string MainScenePath = "Assets/_2DAdventureGame/Secne/MainScene.unity";
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";
    const string PrefabsPath = "Assets/_2DAdventureGame/Prefabs/";
    const string VariantPath = PrefabsPath + "EnemyFast.prefab";
    const string TileRoot = "Assets/_2DAdventureGame/Tutorial_Demo/Demo_Tiles/Ducko/";

    public static void Build()
    {
        CreateEnemyVariant();
        CreateLevelScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Lab 7 MyLevel setup completed.");
    }

    static void CreateEnemyVariant()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath) != null) return;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "Enemy.prefab");
        if (source == null) throw new FileNotFoundException("Enemy.prefab not found.");
        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null) throw new System.InvalidOperationException("Could not instantiate Enemy prefab.");
        instance.name = "EnemyFast";
        EnemyController controller = instance.GetComponent<EnemyController>();
        controller.speed = 2f;
        controller.changeTime = 1.5f;
        SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = new Color(1f, 0.62f, 0.2f, 1f);
        bool success;
        GameObject variant = PrefabUtility.SaveAsPrefabAssetAndConnect(
            instance, VariantPath, InteractionMode.AutomatedAction, out success);
        if (!success || variant == null || PrefabUtility.GetPrefabAssetType(variant) != PrefabAssetType.Variant)
            throw new System.InvalidOperationException("EnemyFast prefab variant was not created.");
        Object.DestroyImmediate(instance);
    }

    static void CreateLevelScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelPath) == null)
        {
            Directory.CreateDirectory("Assets/_2DAdventureGame/Scenes");
            AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(MainScenePath, LevelPath))
                throw new System.InvalidOperationException("Could not copy MainScene to MyLevel.");
        }

        Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        Tilemap tilemap = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.gameObject.name == "Tilemap");
        if (player == null || tilemap == null)
            throw new System.InvalidOperationException("MyLevel needs PlayerCharacter and the main Tilemap.");

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            Object.DestroyImmediate(enemy.gameObject);
        foreach (HealthCollectible item in Object.FindObjectsByType<HealthCollectible>(FindObjectsSortMode.None))
            Object.DestroyImmediate(item.gameObject);
        foreach (DamageZone zone in Object.FindObjectsByType<DamageZone>(FindObjectsSortMode.None))
            Object.DestroyImmediate(zone.gameObject);

        TileBase grass = LoadTile("FloorGrass_7.asset");
        TileBase[] grassVariants = { grass, LoadTile("FloorGrass_4.asset"), LoadTile("FloorGrass_5.asset"), LoadTile("FloorGrass_6.asset"), LoadTile("FloorGrass_8.asset") };
        TileBase water = LoadTile("FloorWaterRoundCorners4.asset");
        if (grass == null || water == null) throw new System.InvalidOperationException("Required SugarLand/Ducko tiles not found.");

        Vector3Int oldStart = tilemap.WorldToCell(player.transform.position);
        int startX = oldStart.x;
        int centerY = oldStart.y;
        tilemap.ClearAllTiles();
        for (int x = startX - 2; x <= startX + 39; x++)
            for (int y = centerY - 7; y <= centerY + 7; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), water);

        Random.InitState(7);
        PaintIsland(tilemap, grassVariants, startX, centerY, 0, 10, -3, 3);
        PaintIsland(tilemap, grassVariants, startX, centerY, 13, 24, -2, 2);
        PaintIsland(tilemap, grassVariants, startX, centerY, 27, 37, -3, 3);
        PaintIsland(tilemap, grassVariants, startX, centerY, 10, 12, -1, 1);
        PaintIsland(tilemap, grassVariants, startX, centerY, 25, 26, -1, 1);
        tilemap.RefreshAllTiles();

        Vector3 start = tilemap.GetCellCenterWorld(new Vector3Int(startX + 1, centerY, 0));
        player.transform.position = start;
        NonPlayerCharacter npc = Object.FindFirstObjectByType<NonPlayerCharacter>();
        if (npc != null) npc.transform.position = start + Vector3.right * 2f;

        InstantiateEnemy(PrefabsPath + "Enemy.prefab", scene, tilemap, startX + 7, centerY, true, 1f, 3f, Color.white, "Zone 1 Sentinel");
        InstantiateEnemy(PrefabsPath + "Enemy.prefab", scene, tilemap, startX + 18, centerY, false, 1f, 3f, Color.white, "Zone 2 Sentinel");
        InstantiateEnemy(VariantPath, scene, tilemap, startX + 32, centerY, false, 2f, 1.5f, new Color(1f, 0.62f, 0.2f), "Zone 3 Fast Sentinel");

        InstantiatePickup(scene, start + Vector3.right * 11f, 2);
        InstantiatePickup(scene, start + Vector3.right * 27f, 1);
        InstantiateDamageZone(scene, start + Vector3.right * 19f, 1);

        Camera camera = Camera.main;
        if (camera != null)
        {
            CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow2D>();
            follow.target = player.transform;
            follow.minBounds = tilemap.GetCellCenterWorld(new Vector3Int(startX - 1, centerY - 5, 0));
            follow.maxBounds = tilemap.GetCellCenterWorld(new Vector3Int(startX + 38, centerY + 5, 0));
        }

        SetDialogueText();
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> scenes = current.Where(s => s.path != LevelPath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(LevelPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static TileBase LoadTile(string name) => AssetDatabase.LoadAssetAtPath<TileBase>(TileRoot + name);

    static void PaintIsland(Tilemap tilemap, TileBase[] variants, int sx, int cy, int x0, int x1, int y0, int y1)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                tilemap.SetTile(new Vector3Int(sx + x, cy + y, 0), variants[Random.Range(0, variants.Length)]);
    }

    static GameObject SpawnPrefab(string path, Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new FileNotFoundException(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null) throw new System.InvalidOperationException("Could not instantiate " + path);
        return instance;
    }

    static void InstantiateEnemy(string path, Scene scene, Tilemap map, int x, int y, bool vertical,
        float speed, float changeTime, Color color, string name)
    {
        GameObject go = SpawnPrefab(path, scene);
        go.name = name;
        go.transform.position = map.GetCellCenterWorld(new Vector3Int(x, y, 0));
        EnemyController enemy = go.GetComponent<EnemyController>();
        enemy.vertical = vertical;
        enemy.speed = speed;
        enemy.changeTime = changeTime;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = color;
    }

    static void InstantiatePickup(Scene scene, Vector3 position, int amount)
    {
        GameObject go = SpawnPrefab(PrefabsPath + "CollectibleHealth.prefab", scene);
        go.name = amount > 1 ? "Health Cache (+2)" : "Health Pickup (+1)";
        go.transform.position = position;
        HealthCollectible pickup = go.GetComponent<HealthCollectible>();
        pickup.amount = amount;
    }

    static void InstantiateDamageZone(Scene scene, Vector3 position, int damage)
    {
        GameObject go = SpawnPrefab(PrefabsPath + "DamageZone.prefab", scene);
        go.name = "Repair Hazard";
        go.transform.position = position;
        go.GetComponent<DamageZone>().damage = damage;
    }

    static void SetDialogueText()
    {
        const string path = "Assets/_2DAdventureGame/GameUI.uxml";
        string uxml = File.ReadAllText(path);
        const string oldText = "Тисни C, щоб стріляти шестернями, і допоможи полагодити зламаних роботів! Повернись, коли закінчиш.";
        const string newText = "Відремонтуй трьох вартових! Натискай C, коли ворог повертається спиною, та оминай червоні зони.";
        if (uxml.Contains(oldText)) File.WriteAllText(path, uxml.Replace(oldText, newText));
        AssetDatabase.ImportAsset(path);
    }
}

[InitializeOnLoad]
public static class Lab7AutoSetup
{
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";
    const string SetupKey = "RobotRepair_Lab7_SetupCompleted_v1";

    static Lab7AutoSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Robot Repair/Lab 7/Create MyLevel")]
    static void RunFromMenu() { Lab7Builder.Build(); }

    static void RunOnce()
    {
        if (EditorPrefs.GetBool(SetupKey, false)) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_2DAdventureGame/Prefabs/EnemyFast.prefab") != null)
        {
            EditorPrefs.SetBool(SetupKey, true);
            return;
        }
        EditorPrefs.SetBool(SetupKey, true);
        try { Lab7Builder.Build(); }
        catch (System.Exception exception)
        {
            EditorPrefs.SetBool(SetupKey, false);
            Debug.LogException(exception);
        }
    }
}
