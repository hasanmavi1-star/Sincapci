using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using SquirrelGame.Player;
using SquirrelGame.Companion;
using SquirrelGame.Gameplay;
using SquirrelGame.Camera;

namespace SquirrelGame.Editor
{
    public static class SceneSetupHelper
    {
        private const string SquirrelFbxPath = "Assets/Models/squirrel.fbx";
        private const string AcornFbxPath = "Assets/Models/acorn.fbx";
        private const string GroundFbxPath = "Assets/Models/Ground_4.fbx";
        private const string PowerAcornFbxPath = "Assets/Models/Power_01_Acorn.fbx";
        private const string HomeFbxPath = "Assets/Models/Home.fbx";
        private const string TreeFbxPath = "Assets/Models/Tree_1.fbx";

        private const string SquirrelControllerPath = "Assets/Animations/SquirrelAnimatorController.controller";
        private const string AcornControllerPath = "Assets/Animations/AcornAnimatorController.controller";

        public const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player_Squirrel.prefab";
        public const string CompanionPrefabPath = "Assets/Prefabs/Characters/Companion_Acorn.prefab";
        public const string IslandPrefabPath = "Assets/Prefabs/Environment/Platform_Island.prefab";
        public const string TreePrefabPath = "Assets/Prefabs/Environment/Tree_Decor.prefab";
        public const string HomePrefabPath = "Assets/Prefabs/Environment/Home_Cave.prefab";
        public const string PowerAcornPrefabPath = "Assets/Prefabs/Gameplay/Collectible_PowerAcorn.prefab";

        [MenuItem("Tools/Setup Platformer Scene & Animators")]
        public static void SetupAll()
        {
            SetupSquirrelAnimator();
            SetupAcornAnimator();
            CreateOrUpdatePrefabs();
            BuildScene();
            Debug.Log("<b>[Sincapci]</b> Sahne, Prefab altyapısı ve Animator Controller kurulumu başarıyla tamamlandı!");
        }

        public static AnimatorController SetupSquirrelAnimator()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(SquirrelControllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsGliding", AnimatorControllerParameterType.Bool);
            controller.AddParameter("JumpTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("VictoryTrigger", AnimatorControllerParameterType.Trigger);

            var clips = LoadClips(SquirrelFbxPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = clips.GetValueOrDefault("squirrel_idle");
            rootStateMachine.defaultState = idleState;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = clips.GetValueOrDefault("squirrel_run");
            runState.speed = 1.35f;

            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = clips.GetValueOrDefault("squirrel_jump-short");

            var glideStartState = rootStateMachine.AddState("GlideStart");
            glideStartState.motion = clips.GetValueOrDefault("squirrel_jump-hold");

            var glideCycleState = rootStateMachine.AddState("GlideCycle");
            glideCycleState.motion = clips.GetValueOrDefault("squirrel_jump-hold_cycle");

            var victoryState = rootStateMachine.AddState("Victory");
            victoryState.motion = clips.GetValueOrDefault("squirrel_idle");

            // Transitions: Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.08f;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.08f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

            // Transitions: AnyState -> Jump
            var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            anyToJump.hasExitTime = false;
            anyToJump.duration = 0.05f;
            anyToJump.canTransitionToSelf = false;
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "JumpTrigger");

            var jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.hasExitTime = true;
            jumpToIdle.exitTime = 0.4f;
            jumpToIdle.duration = 0.1f;
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // Transitions: Jump → GlideStart  (havadayken süzülme başlatır)
            var jumpToGlide = jumpState.AddTransition(glideStartState);
            jumpToGlide.hasExitTime = false;
            jumpToGlide.duration = 0.05f;
            jumpToGlide.AddCondition(AnimatorConditionMode.If, 0, "IsGliding");

            // Transitions: Idle → GlideStart  (yerde space uzun basarsa)
            var idleToGlide = idleState.AddTransition(glideStartState);
            idleToGlide.hasExitTime = false;
            idleToGlide.duration = 0.05f;
            idleToGlide.AddCondition(AnimatorConditionMode.If, 0, "IsGliding");

            // Transitions: Run → GlideStart
            var runToGlide = runState.AddTransition(glideStartState);
            runToGlide.hasExitTime = false;
            runToGlide.duration = 0.05f;
            runToGlide.AddCondition(AnimatorConditionMode.If, 0, "IsGliding");

            var glideStartToCycle = glideStartState.AddTransition(glideCycleState);
            glideStartToCycle.hasExitTime = true;
            glideStartToCycle.exitTime = 0.45f;
            glideStartToCycle.duration = 0.08f;

            // Return to Idle when glide ends
            var glideStartToIdle = glideStartState.AddTransition(idleState);
            glideStartToIdle.hasExitTime = false;
            glideStartToIdle.duration = 0.1f;
            glideStartToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGliding");

            var glideCycleToIdle = glideCycleState.AddTransition(idleState);
            glideCycleToIdle.hasExitTime = false;
            glideCycleToIdle.duration = 0.1f;
            glideCycleToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGliding");

            var anyToVictory = rootStateMachine.AddAnyStateTransition(victoryState);
            anyToVictory.hasExitTime = false;
            anyToVictory.duration = 0.15f;
            anyToVictory.canTransitionToSelf = false;
            anyToVictory.AddCondition(AnimatorConditionMode.If, 0, "VictoryTrigger");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static AnimatorController SetupAcornAnimator()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(AcornControllerPath);

            controller.AddParameter("IsFlying", AnimatorControllerParameterType.Bool);

            var clips = LoadClips(AcornFbxPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = clips.GetValueOrDefault("acorn_idle");
            rootStateMachine.defaultState = idleState;

            var flyState = rootStateMachine.AddState("Fly");
            flyState.motion = clips.GetValueOrDefault("acorn_fly");

            var idleToFly = idleState.AddTransition(flyState);
            idleToFly.hasExitTime = false;
            idleToFly.duration = 0.15f;
            idleToFly.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");

            var flyToIdle = flyState.AddTransition(idleState);
            flyToIdle.hasExitTime = false;
            flyToIdle.duration = 0.2f;
            flyToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static void CreateOrUpdatePrefabs()
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Characters");
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Environment");
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Gameplay");

            var squirrelCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SquirrelControllerPath);
            var acornCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AcornControllerPath);

            // 1. Player_Squirrel Prefab
            var squirrelFbx = AssetDatabase.LoadAssetAtPath<GameObject>(SquirrelFbxPath);
            var playerTemp = (GameObject)PrefabUtility.InstantiatePrefab(squirrelFbx);
            playerTemp.name = "Player_Squirrel";
            playerTemp.tag = "Player";

            var cc = playerTemp.GetComponent<CharacterController>();
            if (cc == null) cc = playerTemp.AddComponent<CharacterController>();
            cc.height = 0.7f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.35f, 0f);
            cc.skinWidth = 0.015f;
            cc.stepOffset = 0.15f;
            cc.minMoveDistance = 0f;

            if (playerTemp.GetComponent<PlayerAnimator>() == null) playerTemp.AddComponent<PlayerAnimator>();
            if (playerTemp.GetComponent<PlayerController>() == null) playerTemp.AddComponent<PlayerController>();

            // FBX bağlantısını kır – aksi halde Animator ayarları prefab'a kaydedilmez
            PrefabUtility.UnpackPrefabInstance(playerTemp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var playerAnim = playerTemp.GetComponent<Animator>();
            if (playerAnim != null && squirrelCtrl != null) playerAnim.runtimeAnimatorController = squirrelCtrl;

            PrefabUtility.SaveAsPrefabAsset(playerTemp, PlayerPrefabPath);
            Object.DestroyImmediate(playerTemp);

            // 2. Companion_Acorn Prefab
            var acornFbx = AssetDatabase.LoadAssetAtPath<GameObject>(AcornFbxPath);
            var acornTemp = (GameObject)PrefabUtility.InstantiatePrefab(acornFbx);
            acornTemp.name = "Companion_Acorn";

            if (acornTemp.GetComponent<CompanionAnimator>() == null) acornTemp.AddComponent<CompanionAnimator>();
            if (acornTemp.GetComponent<AcornCompanion>() == null) acornTemp.AddComponent<AcornCompanion>();

            // FBX bağlantısını kır – aksi halde Animator controller kaybolur
            PrefabUtility.UnpackPrefabInstance(acornTemp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var acornAnim = acornTemp.GetComponent<Animator>();
            if (acornAnim != null && acornCtrl != null) acornAnim.runtimeAnimatorController = acornCtrl;

            var grabHandle = acornTemp.transform.Find("GrabHandle");
            if (grabHandle == null)
            {
                var handleGo = new GameObject("GrabHandle");
                handleGo.transform.SetParent(acornTemp.transform);
                handleGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            }

            PrefabUtility.SaveAsPrefabAsset(acornTemp, CompanionPrefabPath);
            Object.DestroyImmediate(acornTemp);

            // 3. Platform_Island Prefab
            var groundFbx = AssetDatabase.LoadAssetAtPath<GameObject>(GroundFbxPath);
            var islandTemp = (GameObject)PrefabUtility.InstantiatePrefab(groundFbx);
            islandTemp.name = "Platform_Island";

            var boxCol = islandTemp.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = islandTemp.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, -0.5f, 0f);
            boxCol.size = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(islandTemp, IslandPrefabPath);
            Object.DestroyImmediate(islandTemp);

            // 4. Tree_Decor Prefab
            var treeFbx = AssetDatabase.LoadAssetAtPath<GameObject>(TreeFbxPath);
            var treeTemp = (GameObject)PrefabUtility.InstantiatePrefab(treeFbx);
            treeTemp.name = "Tree_Decor";
            PrefabUtility.SaveAsPrefabAsset(treeTemp, TreePrefabPath);
            Object.DestroyImmediate(treeTemp);

            // 5. Collectible_PowerAcorn Prefab
            var powerAcornFbx = AssetDatabase.LoadAssetAtPath<GameObject>(PowerAcornFbxPath);
            var powerTemp = (GameObject)PrefabUtility.InstantiatePrefab(powerAcornFbx);
            powerTemp.name = "Collectible_PowerAcorn";

            var sphereCol = powerTemp.GetComponent<SphereCollider>();
            if (sphereCol == null) sphereCol = powerTemp.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.5f;
            sphereCol.center = new Vector3(0f, 0.3f, 0f);

            if (powerTemp.GetComponent<CollectibleAcorn>() == null) powerTemp.AddComponent<CollectibleAcorn>();

            PrefabUtility.SaveAsPrefabAsset(powerTemp, PowerAcornPrefabPath);
            Object.DestroyImmediate(powerTemp);

            // 6. Home_Cave Prefab
            var homeFbx = AssetDatabase.LoadAssetAtPath<GameObject>(HomeFbxPath);
            var homeTemp = (GameObject)PrefabUtility.InstantiatePrefab(homeFbx);
            homeTemp.name = "Home_Cave";

            var existingGoal = homeTemp.transform.Find("GoalTrigger");
            if (existingGoal == null)
            {
                var goalTrigger = new GameObject("GoalTrigger");
                goalTrigger.transform.SetParent(homeTemp.transform);
                goalTrigger.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                var goalBox = goalTrigger.AddComponent<BoxCollider>();
                goalBox.isTrigger = true;
                goalBox.size = new Vector3(2.5f, 2.5f, 2.5f);
                goalTrigger.AddComponent<LevelGoal>();
            }

            PrefabUtility.SaveAsPrefabAsset(homeTemp, HomePrefabPath);
            Object.DestroyImmediate(homeTemp);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildScene()
        {
            var existingLevel = GameObject.Find("Level_Root");
            if (existingLevel != null) Object.DestroyImmediate(existingLevel);

            var levelRoot = new GameObject("Level_Root");

            var islandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IslandPrefabPath);
            var powerAcornPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PowerAcornPrefabPath);
            var homePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath);
            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var companionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CompanionPrefabPath);

            // --- 1. ISLAND 1 (Start Island: X = -3 to 3) ---
            var island1 = new GameObject("Island_1_Start");
            island1.transform.SetParent(levelRoot.transform);
            CreateIslandInstance(island1, islandPrefab, new Vector3(0f, 0f, 0f), new Vector3(6f, 1.5f, 3.5f));

            if (treePrefab != null)
            {
                var tree1 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, island1.transform);
                tree1.transform.position = new Vector3(-2f, 0f, 1f);
                tree1.transform.localScale = Vector3.one * 1.1f;
            }

            // --- 2. ISLAND 2 (Middle Island: X = 7 to 11) ---
            var island2 = new GameObject("Island_2_Middle");
            island2.transform.SetParent(levelRoot.transform);
            CreateIslandInstance(island2, islandPrefab, new Vector3(8.5f, 0f, 0f), new Vector3(4f, 1.5f, 3.5f));

            if (treePrefab != null)
            {
                var tree2 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, island2.transform);
                tree2.transform.position = new Vector3(9f, 0f, 1.1f);
                tree2.transform.localScale = Vector3.one * 0.95f;
            }

            // --- 3. ISLAND 3 (Goal Island: X = 19 to 27) ---
            var island3 = new GameObject("Island_3_Goal");
            island3.transform.SetParent(levelRoot.transform);
            CreateIslandInstance(island3, islandPrefab, new Vector3(23f, -0.6f, 0f), new Vector3(8.5f, 1.5f, 4f));

            if (treePrefab != null)
            {
                var tree3 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, island3.transform);
                tree3.transform.position = new Vector3(22.5f, -0.6f, 1.2f);
                tree3.transform.localScale = Vector3.one * 1.2f;
            }

            // Background Parallax Islands
            var bgRoot = new GameObject("Background_Decor");
            bgRoot.transform.SetParent(levelRoot.transform);
            CreateBgIslandInstance(bgRoot, islandPrefab, treePrefab, new Vector3(4f, 2.5f, 9f), 0.6f);
            CreateBgIslandInstance(bgRoot, islandPrefab, treePrefab, new Vector3(15f, 4f, 14f), 0.8f);
            CreateBgIslandInstance(bgRoot, islandPrefab, treePrefab, new Vector3(28f, 3f, 11f), 0.7f);

            // Collectible Acorn Prefab Instance
            if (powerAcornPrefab != null)
            {
                var powerAcorn = (GameObject)PrefabUtility.InstantiatePrefab(powerAcornPrefab, island3.transform);
                powerAcorn.name = "Power_Acorn";
                powerAcorn.transform.position = new Vector3(20.5f, 0.7f, 0f);
                powerAcorn.transform.localScale = Vector3.one * 1.3f;
            }

            // Home Cave Prefab Instance
            if (homePrefab != null)
            {
                var home = (GameObject)PrefabUtility.InstantiatePrefab(homePrefab, island3.transform);
                home.name = "Home_Cave";
                home.transform.position = new Vector3(25.5f, -0.6f, 0.2f);
                home.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                home.transform.localScale = Vector3.one * 1.3f;
            }

            // --- 4. PLAYER PREFAB INSTANCE ---
            GameObject playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, levelRoot.transform);
            playerGo.name = "Player_Squirrel";
            playerGo.transform.position = new Vector3(-1.5f, 0.05f, 0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var playerCtrl = playerGo.GetComponent<PlayerController>();

            // --- 5. COMPANION PREFAB INSTANCE ---
            GameObject acornGo = (GameObject)PrefabUtility.InstantiatePrefab(companionPrefab, levelRoot.transform);
            acornGo.name = "Companion_Acorn";
            acornGo.transform.position = new Vector3(-2.2f, 1.3f, 0.2f);
            acornGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var companionComp = acornGo.GetComponent<AcornCompanion>();
            if (playerCtrl != null && companionComp != null)
            {
                playerCtrl.SetCompanion(companionComp);
            }

            // --- 6. CAMERA ---
            var mainCam = UnityEngine.Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 2.0f, -7.0f);
                mainCam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
                mainCam.fieldOfView = 50f;

                var camFollow = mainCam.GetComponent<CameraFollow2D>();
                if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow2D>();
                camFollow.SetTarget(playerGo.transform);
            }

            // --- 7. EVENT SYSTEM ---
            var existingEventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // Directional Light tuning
            var dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                dirLight.transform.rotation = Quaternion.Euler(38f, -40f, 0f);
                var l = dirLight.GetComponent<Light>();
                if (l != null)
                {
                    l.color = new Color(1f, 0.96f, 0.88f);
                    l.intensity = 1.3f;
                    l.shadows = LightShadows.Soft;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        private static void CreateIslandInstance(GameObject parent, GameObject islandPrefab, Vector3 pos, Vector3 size)
        {
            var islandVisual = (GameObject)PrefabUtility.InstantiatePrefab(islandPrefab, parent.transform);
            islandVisual.name = "Platform_Mesh";
            islandVisual.transform.position = pos;
            islandVisual.transform.localScale = size;
        }

        private static void CreateBgIslandInstance(GameObject parent, GameObject islandPrefab, GameObject treePrefab, Vector3 pos, float scale)
        {
            var island = new GameObject("Bg_Island");
            island.transform.SetParent(parent.transform);
            island.transform.position = pos;

            if (islandPrefab != null)
            {
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(islandPrefab, island.transform);
                mesh.name = "Platform_Mesh";
                mesh.transform.localPosition = Vector3.zero;
                mesh.transform.localScale = new Vector3(4f * scale, 1.2f * scale, 3f * scale);
            }

            if (treePrefab != null)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, island.transform);
                tree.name = "Tree";
                tree.transform.localPosition = new Vector3(0f, 0f, 0.2f);
                tree.transform.localScale = Vector3.one * scale;
            }
        }

        private static Dictionary<string, AnimationClip> LoadClips(string fbxPath)
        {
            var dict = new Dictionary<string, AnimationClip>();
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    dict[clip.name] = clip;
                }
            }
            return dict;
        }
    }
}
