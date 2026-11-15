using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BossCampaignBuilder
{
    const string LevelPath = "Assets/_2DAdventureGame/Scenes/MyLevel.unity";
    const string BossScenePath = "Assets/_2DAdventureGame/Scenes/BossArena.unity";
    const string PortalPrefabPath = "Assets/_2DAdventureGame/Prefabs/RepairPortal.prefab";
    const string EnemyPrefabPath = "Assets/_2DAdventureGame/Prefabs/Enemy.prefab";
    const string PortalFramesFolder = "Assets/_2DAdventureGame/Animation/Art/Ducko/VFX/Portal";
    const string PlayerPrefabPath = "Assets/_2DAdventureGame/Prefabs/PlayerCharacter.prefab";
    const string ProjectilePrefabPath = "Assets/_2DAdventureGame/Prefabs/Projectile.prefab";
    const string BossProjectilePrefabPath = "Assets/_2DAdventureGame/Prefabs/BossProjectile.prefab";
    const string BossArenaFloorPath = "Assets/_2DAdventureGame/Animation/Art/Ducko/BossArenaFloor.png";
    const string BossRabbitSpritePath = "Assets/_2DAdventureGame/Animation/Art/Ducko/BossRabbit.png";
    const string BossRabbitAttackSpritePath = "Assets/_2DAdventureGame/Animation/Art/Ducko/BossRabbit_Attack.png";
    const string BossCarrotSpritePath = "Assets/_2DAdventureGame/Animation/Art/Ducko/BossCarrotProjectile.png";

    [MenuItem("Robot Repair/Campaign/Build Boss Portal and Arena")]
    public static void BuildCampaign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before building the boss campaign.");
            return;
        }
        if (EditorSceneManager.GetSceneByPath(LevelPath).isLoaded)
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        CreatePortalPrefab();
        CreateBossProjectilePrefab();
        ConfigureCampaignLevel();
        BuildBossArena();
        AddScenesToBuildSettings();
        EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        Debug.Log("Boss campaign ready: repair four robots, enter the portal, then repair the boss with 15 projectile hits.");
    }

    static void CreateBossProjectilePrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        if (source == null)
            throw new FileNotFoundException("Projectile prefab was not found.", ProjectilePrefabPath);
        GameObject shot = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (shot == null)
            throw new System.InvalidOperationException("Could not create the boss projectile.");
        PrefabUtility.UnpackPrefabInstance(shot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Projectile playerProjectile = shot.GetComponent<Projectile>();
        if (playerProjectile != null)
            Object.DestroyImmediate(playerProjectile);
        Rigidbody2D body = shot.GetComponent<Rigidbody2D>();
        if (body == null)
            body = shot.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        BoxCollider2D collider = shot.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = shot.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        SpriteRenderer renderer = shot.GetComponent<SpriteRenderer>();
        Sprite carrotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossCarrotSpritePath);
        if (renderer == null || carrotSprite == null)
            throw new System.InvalidOperationException("Boss carrot artwork was not imported correctly.");
        renderer.sprite = carrotSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 8;
        shot.transform.localScale = Vector3.one * 0.45f;
        shot.name = "BossCarrotProjectile";
        BossProjectile controller = shot.AddComponent<BossProjectile>();
        controller.speed = 5f;
        controller.damage = 1;
        bool success;
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(shot, BossProjectilePrefabPath, out success);
        Object.DestroyImmediate(shot);
        if (!success || saved == null)
            throw new System.InvalidOperationException("Boss projectile prefab could not be saved.");
    }

    [MenuItem("Robot Repair/Campaign/Add Portal to MyLevel")]
    public static void AddPortalToMyLevel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before placing the portal in MyLevel.");
            return;
        }
        if (EditorSceneManager.GetSceneByPath(LevelPath).isLoaded)
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        CreatePortalPrefab();
        ConfigureCampaignLevel();
        AddScenesToBuildSettings();
        Debug.Log("The inactive boss portal is now in MyLevel and will open after four repairs.");
    }
    static void CreatePortalPrefab()
    {
        Sprite[] frames = LoadPortalAnimationFrames();
        GameObject portal;
        bool loadedPrefabContents = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath) != null;

        if (loadedPrefabContents)
        {
            portal = PrefabUtility.LoadPrefabContents(PortalPrefabPath);
        }
        else
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_2DAdventureGame/Prefabs/CollectibleHealth.prefab");
            if (source == null)
                throw new FileNotFoundException("CollectibleHealth prefab was not found.");
            portal = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (portal == null)
                throw new System.InvalidOperationException("Could not create the portal prefab.");
            PrefabUtility.UnpackPrefabInstance(portal, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        HealthCollectible health = portal.GetComponent<HealthCollectible>();
        if (health != null)
            Object.DestroyImmediate(health);
        Rigidbody2D body = portal.GetComponent<Rigidbody2D>();
        if (body == null)
            body = portal.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        foreach (Collider2D collider in portal.GetComponents<Collider2D>())
            Object.DestroyImmediate(collider);
        CircleCollider2D trigger = portal.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.35f;

        SpriteRenderer renderer = portal.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = portal.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.color = Color.white;
        renderer.sortingOrder = 8;
        portal.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        PortalController controller = portal.GetComponent<PortalController>();
        if (controller == null)
            controller = portal.AddComponent<PortalController>();
        controller.destinationScene = "BossArena";
        controller.animationFrames = frames;
        controller.frameRate = 9f;
        portal.name = "RepairPortal";

        bool success;
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(portal, PortalPrefabPath, out success);
        if (loadedPrefabContents)
            PrefabUtility.UnloadPrefabContents(portal);
        else
            Object.DestroyImmediate(portal);
        if (!success || saved == null)
            throw new System.InvalidOperationException("RepairPortal prefab could not be saved.");
    }

    static Sprite[] LoadPortalAnimationFrames()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string[] paths = Directory.GetFiles(PortalFramesFolder, "Portal_*.png")
            .OrderBy(path => path, System.StringComparer.Ordinal)
            .ToArray();
        if (paths.Length == 0)
            throw new FileNotFoundException("Animated portal frames were not found.", PortalFramesFolder);

        Sprite[] frames = new Sprite[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            string assetPath = paths[i].Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (frames[i] == null)
                throw new System.InvalidOperationException("Could not import portal animation frame: " + assetPath);
        }
        return frames;
    }

    static void ConfigureCampaignLevel()
    {
        Scene level = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (player == null || manager == null)
            throw new System.InvalidOperationException("MyLevel needs a player and GameManager.");

        manager.requiredRepairsBeforePortal = 4;
        manager.portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
        manager.portalSpawnPosition = (Vector2)player.transform.position + new Vector2(0f, 2.5f);
        foreach (PortalController oldPortal in Object.FindObjectsByType<PortalController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(oldPortal.gameObject);
        manager.portalInstance = PrefabUtility.InstantiatePrefab(manager.portalPrefab, level) as GameObject;
        if (manager.portalInstance == null)
            throw new System.InvalidOperationException("Could not place the portal in MyLevel.");
        manager.portalInstance.name = "Boss Portal (opens after four repairs)";
        manager.portalInstance.transform.position = manager.portalSpawnPosition;
        manager.portalInstance.SetActive(false);
        
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(level);
        EditorSceneManager.SaveScene(level);
    }

    static void BuildBossArena()
    {
        if (!File.Exists(LevelPath))
            throw new FileNotFoundException("MyLevel scene was not found.", LevelPath);

        File.Copy(LevelPath, BossScenePath, true);
        AssetDatabase.ImportAsset(BossScenePath, ImportAssetOptions.ForceUpdate);
        Scene arena = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);

        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            Object.DestroyImmediate(enemy.gameObject);
        foreach (SpeedBoostCollectible boost in Object.FindObjectsByType<SpeedBoostCollectible>(FindObjectsSortMode.None))
            Object.DestroyImmediate(boost.gameObject);
        foreach (PortalController portal in Object.FindObjectsByType<PortalController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(portal.gameObject);
        foreach (NonPlayerCharacter npc in Object.FindObjectsByType<NonPlayerCharacter>(FindObjectsSortMode.None))
            Object.DestroyImmediate(npc.gameObject);
        foreach (DamageZone zone in Object.FindObjectsByType<DamageZone>(FindObjectsSortMode.None))
            Object.DestroyImmediate(zone.gameObject);
        foreach (string inheritedPropName in new[] { "tree2", "houses", "damageZone" })
        {
            GameObject inheritedProp = GameObject.Find(inheritedPropName);
            if (inheritedProp != null)
                Object.DestroyImmediate(inheritedProp);
        }
        GameObject waterGate = GameObject.Find("FloorWaterRoundCorners");
        if (waterGate != null)
            Object.DestroyImmediate(waterGate);
        foreach (GameObject decoration in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (decoration.name.StartsWith("Decoration_") && !decoration.name.StartsWith("Decoration_11"))
                Object.DestroyImmediate(decoration);

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
            throw new System.InvalidOperationException("BossArena needs PlayerCharacter.");

        GameManager arenaManager = Object.FindFirstObjectByType<GameManager>();
        if (arenaManager != null)
        {
            arenaManager.portalInstance = null;
            BossArenaSpawn spawn = arenaManager.GetComponent<BossArenaSpawn>();
            if (spawn == null)
                spawn = arenaManager.gameObject.AddComponent<BossArenaSpawn>();
            spawn.entryPosition = player.transform.position;
        }

        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            tilemap.color = new Color(0.48f, 0.62f, 0.92f, 1f);
        GameObject arenaBoundary = GameObject.Find("Decoration_11 Map Boundary");
        if (arenaBoundary != null)
            Object.DestroyImmediate(arenaBoundary);
        Camera arenaCamera = Camera.main;
        if (arenaCamera != null)
            arenaCamera.backgroundColor = new Color(0.025f, 0.04f, 0.12f, 1f);
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject boss = PrefabUtility.InstantiatePrefab(bossPrefab, arena) as GameObject;
        if (boss == null)
            throw new System.InvalidOperationException("Could not place the boss in BossArena.");

        EnemyController regularEnemy = boss.GetComponent<EnemyController>();
        if (regularEnemy != null)
            Object.DestroyImmediate(regularEnemy);
        SpriteRenderer bossRenderer = boss.GetComponent<SpriteRenderer>();
        Sprite bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossRabbitSpritePath);
        if (bossRenderer == null || bossSprite == null)
            throw new System.InvalidOperationException("Boss rabbit artwork was not imported correctly.");
        bossRenderer.sprite = bossSprite;
        bossRenderer.color = Color.white;
        Animator enemyAnimator = boss.GetComponent<Animator>();
        if (enemyAnimator != null)
            enemyAnimator.enabled = false;
        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
        }
        boss.name = "Repair Boss";
        Vector2 arenaCenter = new Vector2(-1.5f, -5f);
        boss.transform.position = arenaCenter + new Vector2(4f, 0f);
        boss.transform.localScale *= 1.7f;
        BossController bossController = boss.AddComponent<BossController>();
        bossController.requiredHits = 15;
        bossController.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossProjectilePrefabPath);
        bossController.movementSpeed = 1.2f;
        bossController.preferredDistance = 4f;
        bossController.minimumDistance = 2.5f;
        bossController.fireRate = 1.5f;
        bossController.strafeSpeed = 0.65f;
        bossController.strafeChangeInterval = 2.5f;
        bossController.walkingFrames = Enumerable.Range(0, 8)
            .Select(index => AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/_2DAdventureGame/Animation/Art/Ducko/BossRabbit_Walk_{index:00}.png"))
            .ToArray();
        bossController.attackFrame = AssetDatabase.LoadAssetAtPath<Sprite>(BossRabbitAttackSpritePath);
        if (bossController.walkingFrames.Any(frame => frame == null) || bossController.attackFrame == null)
            throw new System.InvalidOperationException("Boss rabbit animation frames were not imported correctly.");
        bossController.animationFrameRate = 8f;
        bossController.attackPoseDuration = 0.3f;
        bossController.defeatAnimationDuration = 1.1f;
        bossController.arenaMin = arenaCenter + new Vector2(-21.4f, -14.9f);
        bossController.arenaMax = arenaCenter + new Vector2(21.4f, 14.9f);
        CreateBossArenaFloor(arena, arenaCenter);
        CreateBossArenaWalls(arena, arenaCenter);

        CameraFollow2D cameraFollow = arenaCamera != null ? arenaCamera.GetComponent<CameraFollow2D>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.target = player.transform;
            cameraFollow.useBounds = false;
        }

        EditorSceneManager.MarkSceneDirty(arena);
        EditorSceneManager.SaveScene(arena);
        AssetDatabase.SaveAssets();
    }

    static void CreateBossArenaFloor(Scene arena, Vector2 center)
    {
        Sprite floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossArenaFloorPath);
        if (floorSprite == null)
            throw new FileNotFoundException("Boss arena floor artwork was not imported.", BossArenaFloorPath);

        GameObject floor = new GameObject("Boss Arena - Reactor Floor");
        SceneManager.MoveGameObjectToScene(floor, arena);
        floor.transform.position = new Vector3(center.x, center.y, 0.12f);

        SpriteRenderer renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = floorSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -9;
    }

    static void CreateBossArenaWalls(Scene arena, Vector2 center)
    {
        CreateArenaWall(arena, "Boss Arena North Barrier", center + Vector2.up * 15.5f, new Vector2(44f, 0.65f));
        CreateArenaWall(arena, "Boss Arena South Barrier", center + Vector2.down * 15.5f, new Vector2(44f, 0.65f));
        CreateArenaWall(arena, "Boss Arena West Barrier", center + Vector2.left * 22f, new Vector2(31f, 0.65f), 90f);
        CreateArenaWall(arena, "Boss Arena East Barrier", center + Vector2.right * 22f, new Vector2(31f, 0.65f), 90f);
    }

    static void CreateArenaWall(Scene arena, string name, Vector2 position, Vector2 size, float rotation = 0f)
    {
        GameObject wall = new GameObject(name);
        SceneManager.MoveGameObjectToScene(wall, arena);
        wall.transform.position = position;
        wall.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    static void AddScenesToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(scene =>
            scene.path != LevelPath && scene.path != BossScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(BossScenePath, true));
        scenes.Insert(0, new EditorBuildSettingsScene(LevelPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

[InitializeOnLoad]
public static class BossCampaignAutoSetup
{
    const string SetupKey = "RobotRepair_BossCampaign_Setup_v1";

    static BossCampaignAutoSetup()
    {
        EditorApplication.delayCall += RunWhenEditorIsReady;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += RunWhenEditorIsReady;
    }

    static void RunWhenEditorIsReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorPrefs.GetBool(SetupKey, false))
            return;

        EditorPrefs.SetBool(SetupKey, true);
        try
        {
            BossCampaignBuilder.BuildCampaign();
        }
        catch (System.Exception exception)
        {
            EditorPrefs.SetBool(SetupKey, false);
            Debug.LogException(exception);
        }
    }
}
[InitializeOnLoad]
public static class BossPortalSceneAutoSetup
{
    const string SetupKey = "RobotRepair_BossPortal_InMyLevel_v1";

    static BossPortalSceneAutoSetup()
    {
        EditorApplication.delayCall += RunWhenEditorIsReady;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += RunWhenEditorIsReady;
    }

    static void RunWhenEditorIsReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(SetupKey, false))
            return;
        EditorPrefs.SetBool(SetupKey, true);
        try
        {
            BossCampaignBuilder.AddPortalToMyLevel();
        }
        catch (System.Exception exception)
        {
            EditorPrefs.SetBool(SetupKey, false);
            Debug.LogException(exception);
        }
    }
}
[InitializeOnLoad]
public static class BossArenaVisualTransferAutoSetup
{
    const string SetupKey = "RobotRepair_BossArena_VisualTransfer_v1";

    static BossArenaVisualTransferAutoSetup()
    {
        EditorApplication.delayCall += RunWhenEditorIsReady;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += RunWhenEditorIsReady;
    }

    static void RunWhenEditorIsReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(SetupKey, false))
            return;
        EditorPrefs.SetBool(SetupKey, true);
        try
        {
            BossCampaignBuilder.BuildCampaign();
        }
        catch (System.Exception exception)
        {
            EditorPrefs.SetBool(SetupKey, false);
            Debug.LogException(exception);
        }
    }
}
