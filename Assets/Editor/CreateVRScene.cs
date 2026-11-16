using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using TMPro;

namespace VRExplorer.Editor
{
    public static class CreateVRScene
    {
        private const string ScenePath = "Assets/Scenes/MainVRScene.unity";
        private const string StarterAssetsPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets";

        [MenuItem("Tools/VR Explorer/Generate Main Scene", false, 10)]
        public static void GenerateMainSceneMenu()
        {
            GenerateMainScene();
        }

        public static void GenerateMainScene()
        {
            // Confirmación si la escena activa tiene cambios no guardados
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[VR Explorer] Generación cancelada por el usuario para proteger cambios.");
                    return;
                }
            }

            Debug.Log("[VR Explorer] Iniciando generación de 'VR Explorer: Magical Island'...");

            // 1. Crear nueva escena vacía
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Asegurar directorios
            EnsureDirectories();

            // 2. Iluminación y Skybox
            SetupLightingAndAtmosphere();

            // 3. Crear Materiales del Proyecto
            MaterialsLibrary mats = CreateOrLoadMaterials();

            // 4. Configurar Sistema VR (XR Origin, Interaction Manager, Controllers, Teleportation)
            GameObject xrOriginGO = SetupVROrigin();

            // 5. Construir Entorno 3D (Zonas 1 a 4)
            BuildIslandEnvironment(mats);

            // 6. Configurar Interfaz 3D
            SetupWorldSpaceUI(mats);

            // 7. Configurar Managers
            SetupGameManagers();

            // 8. Guardar Escena
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[VR Explorer] ¡Escena generada exitosamente en {ScenePath}!");

            // 9. Registrar escena en Build Settings si no está
            AddSceneToBuildSettings(ScenePath);
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/Scenes",
                "Assets/Materials/Environment",
                "Assets/Materials/Magical",
                "Assets/Materials/Water",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/InteractiveObjects",
                "Assets/Prefabs/UI",
                "Assets/Audio",
                "Assets/Particles"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                    Directory.CreateDirectory(d);
            }
        }

        private static void SetupLightingAndAtmosphere()
        {
            // Luz Direccional (Sol místico crepuscular)
            GameObject lightGO = new GameObject("Directional Light - Sun");
            Light sun = lightGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // Luz Ambiental
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.55f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.4f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.2f, 0.15f);
        }

        private class MaterialsLibrary
        {
            public Material grass;
            public Material pathway;
            public Material rock;
            public Material wood;
            public Material leaves;
            public Material water;
            public Material crystalBlue;
            public Material crystalGold;
            public Material runeStone;
            public Material glowingShroom;
            public Material sanctuaryWhite;
            public Material sanctuaryRune;
        }

        private static MaterialsLibrary CreateOrLoadMaterials()
        {
            MaterialsLibrary mats = new MaterialsLibrary();
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            // Función local para crear o actualizar material
            Material GetOrCreateMat(string path, Color albedo, float smoothness = 0.2f, Color? emission = null)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(urpShader);
                    AssetDatabase.CreateAsset(m, path);
                }
                m.color = albedo;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", albedo);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);

                if (emission.HasValue && emission.Value != Color.black)
                {
                    m.EnableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission.Value);
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            mats.grass = GetOrCreateMat("Assets/Materials/Environment/M_IslandGrass.mat", new Color(0.22f, 0.48f, 0.2f), 0.1f);
            mats.pathway = GetOrCreateMat("Assets/Materials/Environment/M_StonePathway.mat", new Color(0.55f, 0.52f, 0.45f), 0.2f);
            mats.rock = GetOrCreateMat("Assets/Materials/Environment/M_IslandRock.mat", new Color(0.35f, 0.37f, 0.4f), 0.15f);
            mats.wood = GetOrCreateMat("Assets/Materials/Environment/M_TreeTrunk.mat", new Color(0.36f, 0.24f, 0.15f), 0.1f);
            mats.leaves = GetOrCreateMat("Assets/Materials/Environment/M_TreeLeaves.mat", new Color(0.18f, 0.52f, 0.25f), 0.2f);
            
            // Agua mágica (translúcida/azul brillante)
            mats.water = GetOrCreateMat("Assets/Materials/Water/M_MagicalWater.mat", new Color(0.1f, 0.6f, 0.85f, 0.8f), 0.9f);
            
            // Cristales y magia
            mats.crystalBlue = GetOrCreateMat("Assets/Materials/Magical/M_CrystalBlue.mat", new Color(0.2f, 0.8f, 1f, 0.9f), 0.95f, new Color(0.1f, 0.6f, 1f) * 2f);
            mats.crystalGold = GetOrCreateMat("Assets/Materials/Magical/M_CrystalGold.mat", new Color(1f, 0.8f, 0.2f, 0.9f), 0.95f, new Color(1f, 0.7f, 0.1f) * 2f);
            mats.runeStone = GetOrCreateMat("Assets/Materials/Magical/M_RuneStone.mat", new Color(0.25f, 0.25f, 0.3f), 0.3f, new Color(0.7f, 0.2f, 1f) * 1.5f);
            mats.glowingShroom = GetOrCreateMat("Assets/Materials/Magical/M_GlowingShroom.mat", new Color(0.9f, 0.3f, 0.6f), 0.4f, new Color(1f, 0.2f, 0.5f) * 2.2f);
            mats.sanctuaryWhite = GetOrCreateMat("Assets/Materials/Environment/M_SanctuaryMarble.mat", new Color(0.9f, 0.92f, 0.95f), 0.7f);
            mats.sanctuaryRune = GetOrCreateMat("Assets/Materials/Magical/M_SanctuaryRune.mat", new Color(0.85f, 0.9f, 1f), 0.8f, new Color(0.3f, 0.7f, 1f) * 2.5f);

            AssetDatabase.SaveAssets();
            return mats;
        }

        private static GameObject SetupVROrigin()
        {
            // Intentar instanciar el prefab Starter Assets XR Origin si existe
            string prefabPath = $"{StarterAssetsPath}/Prefabs/XR Origin (XR Rig).prefab";
            GameObject originPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject xrOriginGO;

            if (originPrefab != null)
            {
                xrOriginGO = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
                xrOriginGO.name = "XR Origin (XR Rig)";
            }
            else
            {
                xrOriginGO = new GameObject("XR Origin (VR)");
                var origin = xrOriginGO.AddComponent<XROrigin>();
                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(xrOriginGO.transform, false);

                GameObject mainCam = new GameObject("Main Camera");
                mainCam.transform.SetParent(cameraOffset.transform, false);
                mainCam.tag = "MainCamera";
                var cam = mainCam.AddComponent<Camera>();
                cam.nearClipPlane = 0.05f;
                mainCam.AddComponent<AudioListener>();

                origin.CameraFloorOffsetObject = cameraOffset;
                origin.Camera = cam;
            }

            // Posicionar en Zona 1 (Entrada de la isla)
            xrOriginGO.transform.position = new Vector3(0f, 0.1f, -18f);
            xrOriginGO.transform.rotation = Quaternion.identity;

            // Asegurar XRInteractionManager en la escena
            var xriManager = Object.FindFirstObjectByType<XRInteractionManager>();
            if (xriManager == null)
            {
                GameObject managerGO = new GameObject("XR Interaction Manager");
                managerGO.AddComponent<XRInteractionManager>();
            }

            // Asegurar que Tunneling Vignette esté presente para confort anti-náuseas
            string vignettePath = $"{StarterAssetsPath}/TunnelingVignette/TunnelingVignette.prefab";
            GameObject vignettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vignettePath);
            if (vignettePrefab != null && xrOriginGO.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>() == null)
            {
                GameObject vig = (GameObject)PrefabUtility.InstantiatePrefab(vignettePrefab, xrOriginGO.transform);
                vig.name = "TunnelingVignette";
            }

            return xrOriginGO;
        }

        private static void BuildIslandEnvironment(MaterialsLibrary mats)
        {
            GameObject islandRoot = new GameObject("--- Island Environment ---");

            // Terreno Base (Gran meseta insular orgánica con bordes)
            GameObject islandBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            islandBase.name = "Island_Base_Terrain";
            islandBase.transform.SetParent(islandRoot.transform, false);
            islandBase.transform.position = new Vector3(0f, -1.5f, 0f);
            islandBase.transform.localScale = new Vector3(70f, 3f, 70f);
            islandBase.GetComponent<MeshRenderer>().sharedMaterial = mats.grass;
            
            // Configurar como área de teletransporte
            TeleportManager.EnsureTeleportArea(islandBase);

            // ZONA 1: Entrada de la isla (Z = -22 a -12)
            BuildZone1_Entrance(islandRoot.transform, mats);

            // ZONA 2: Bosque mágico (X = -20 a -5, Z = -10 a +15)
            BuildZone2_MagicForest(islandRoot.transform, mats);

            // ZONA 3: Lago mágico (X = 5 a 25, Z = -10 a +15)
            BuildZone3_MagicLake(islandRoot.transform, mats);

            // ZONA 4: Santuario mágico (Z = 16 a 28, X = -8 a +8)
            BuildZone4_Sanctuary(islandRoot.transform, mats);

            // Camino Principal que une las zonas
            BuildMainPathways(islandRoot.transform, mats);
        }

        private static void BuildZone1_Entrance(Transform root, MaterialsLibrary mats)
        {
            GameObject z1 = new GameObject("Zone1_Entrance");
            z1.transform.SetParent(root, false);

            // Plataforma de Bienvenida (Círculo de piedra elevada)
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "WelcomePlatform";
            platform.transform.SetParent(z1.transform, false);
            platform.transform.position = new Vector3(0f, 0.05f, -18f);
            platform.transform.localScale = new Vector3(8f, 0.1f, 8f);
            platform.GetComponent<MeshRenderer>().sharedMaterial = mats.pathway;
            TeleportManager.EnsureTeleportArea(platform);

            // Postes de bienvenida / Antorchas mágicas
            CreateMagicLantern(z1.transform, new Vector3(-3.5f, 0f, -18f), mats);
            CreateMagicLantern(z1.transform, new Vector3(3.5f, 0f, -18f), mats);

            // Árboles a los costados de la entrada
            CreateStylizedTree(z1.transform, new Vector3(-6f, 0f, -16f), 1.2f, mats);
            CreateStylizedTree(z1.transform, new Vector3(6f, 0f, -16f), 1.1f, mats);
            CreateStylizedTree(z1.transform, new Vector3(-8f, 0f, -20f), 0.9f, mats);
            CreateStylizedTree(z1.transform, new Vector3(8f, 0f, -20f), 1.0f, mats);

            // Objeto Coleccionable 1 en la entrada
            CreateCollectibleObject(z1.transform, new Vector3(-2.5f, 1f, -15f), "Amuleto de Bienvenida", mats.crystalGold);
        }

        private static void BuildZone2_MagicForest(Transform root, MaterialsLibrary mats)
        {
            GameObject z2 = new GameObject("Zone2_MagicForest");
            z2.transform.SetParent(root, false);

            // Distribución de árboles del bosque
            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-12f, 0f, -8f),
                new Vector3(-16f, 0f, -5f),
                new Vector3(-10f, 0f, 0f),
                new Vector3(-18f, 0f, 3f),
                new Vector3(-13f, 0f, 8f),
                new Vector3(-8f, 0f, 10f),
                new Vector3(-22f, 0f, -2f),
                new Vector3(-15f, 0f, 14f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                float scale = 0.8f + (i % 3) * 0.35f;
                CreateStylizedTree(z2.transform, treePositions[i], scale, mats);
            }

            // Rocas en el bosque
            CreateRock(z2.transform, new Vector3(-14f, 0f, -2f), new Vector3(2.5f, 1.8f, 2.2f), mats.rock);
            CreateRock(z2.transform, new Vector3(-19f, 0f, 6f), new Vector3(3f, 2.5f, 2.8f), mats.rock);
            CreateRock(z2.transform, new Vector3(-9f, 0f, 4f), new Vector3(1.5f, 1.2f, 1.4f), mats.rock);

            // Hongos Luminosos
            CreateGlowingMushroom(z2.transform, new Vector3(-11f, 0f, -3f), mats);
            CreateGlowingMushroom(z2.transform, new Vector3(-13f, 0f, 2f), mats);
            CreateGlowingMushroom(z2.transform, new Vector3(-17f, 0f, 5f), mats);
            CreateGlowingMushroom(z2.transform, new Vector3(-10f, 0f, 7f), mats);

            // Partículas ambientales del bosque (esporas mágicas)
            CreateMagicSporeParticles(z2.transform, new Vector3(-14f, 2.5f, 2f), new Color(0.4f, 1f, 0.6f));

            // INTERACCIÓN 1: Cristal Mágico del Bosque (Esmeralda Mística)
            CreateCrystalObject(z2.transform, new Vector3(-14f, 0.8f, 2f), "Cristal Silvano", mats.crystalBlue, new Color(0.2f, 1f, 0.5f));

            // INTERACCIÓN 2: Piedra Mágica Rúnica
            CreateMagicRunestone(z2.transform, new Vector3(-18f, 0.5f, -4f), mats);

            // Objeto Coleccionable 2 oculto entre los árboles del bosque
            CreateCollectibleObject(z2.transform, new Vector3(-16f, 0.8f, 9f), "Orbe del Bosque Antiguo", mats.crystalGold);
        }

        private static void BuildZone3_MagicLake(Transform root, MaterialsLibrary mats)
        {
            GameObject z3 = new GameObject("Zone3_MagicLake");
            z3.transform.SetParent(root, false);

            // Cuenca del Lago de agua mágica
            GameObject lake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lake.name = "MagicalLake_Water";
            lake.transform.SetParent(z3.transform, false);
            lake.transform.position = new Vector3(14f, 0.02f, 1f);
            lake.transform.localScale = new Vector3(18f, 0.05f, 22f);
            lake.GetComponent<MeshRenderer>().sharedMaterial = mats.water;
            // No teletransporte sobre agua profunda (teleport invalid)

            // Rocas bordeando el lago
            float radiusX = 9.5f;
            float radiusZ = 11.5f;
            int rockCount = 14;
            for (int i = 0; i < rockCount; i++)
            {
                float angle = (i * 360f / rockCount) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(14f + Mathf.Cos(angle) * radiusX, 0f, 1f + Mathf.Sin(angle) * radiusZ);
                CreateRock(z3.transform, pos, new Vector3(1.8f + (i % 2) * 0.8f, 1.2f + (i % 3) * 0.6f, 1.7f), mats.rock);
            }

            // Cascada pequeña y peñasco elevado
            GameObject cliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliff.name = "Waterfall_Cliff";
            cliff.transform.SetParent(z3.transform, false);
            cliff.transform.position = new Vector3(22f, 2.5f, 3f);
            cliff.transform.localScale = new Vector3(6f, 5f, 8f);
            cliff.GetComponent<MeshRenderer>().sharedMaterial = mats.rock;

            // Cascada (superficie azul claro vertical inclinada)
            GameObject waterFall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterFall.name = "Waterfall_Mesh";
            waterFall.transform.SetParent(z3.transform, false);
            waterFall.transform.position = new Vector3(19.5f, 2.2f, 2.5f);
            waterFall.transform.localScale = new Vector3(1.2f, 4f, 3.5f);
            waterFall.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
            waterFall.GetComponent<MeshRenderer>().sharedMaterial = mats.water;

            // Partículas de espuma de cascada
            CreateSplashParticles(z3.transform, new Vector3(18.5f, 0.3f, 2.5f));

            // Cristales luminosos del Lago (Interacción 1 alternativa)
            CreateCrystalObject(z3.transform, new Vector3(8.5f, 0.6f, 4f), "Cristal del Manantial", mats.crystalBlue, new Color(0.1f, 0.8f, 1f));
            CreateCrystalObject(z3.transform, new Vector3(10f, 0.7f, -6f), "Cristal de Escarcha", mats.crystalBlue, new Color(0.5f, 0.9f, 1f));
        }

        private static void BuildZone4_Sanctuary(Transform root, MaterialsLibrary mats)
        {
            GameObject z4 = new GameObject("Zone4_Sanctuary");
            z4.transform.SetParent(root, false);

            // Plataforma elevada del Santuario
            GameObject sanctPlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sanctPlatform.name = "Sanctuary_Dais";
            sanctPlatform.transform.SetParent(z4.transform, false);
            sanctPlatform.transform.position = new Vector3(0f, 0.3f, 22f);
            sanctPlatform.transform.localScale = new Vector3(14f, 0.5f, 14f);
            sanctPlatform.GetComponent<MeshRenderer>().sharedMaterial = mats.sanctuaryWhite;
            TeleportManager.EnsureTeleportArea(sanctPlatform);

            // Escalinatas de acceso
            GameObject stairs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stairs.name = "Sanctuary_Stairs";
            stairs.transform.SetParent(z4.transform, false);
            stairs.transform.position = new Vector3(0f, 0.15f, 14.5f);
            stairs.transform.localScale = new Vector3(6f, 0.3f, 3f);
            stairs.GetComponent<MeshRenderer>().sharedMaterial = mats.sanctuaryWhite;
            TeleportManager.EnsureTeleportArea(stairs);

            // Pilares Místicos en círculo
            List<MeshRenderer> pillarRenderers = new List<MeshRenderer>();
            List<Light> pillarLights = new List<Light>();

            int pillarCount = 6;
            float pRadius = 5.2f;
            for (int i = 0; i < pillarCount; i++)
            {
                float ang = (i * 360f / pillarCount) * Mathf.Deg2Rad;
                Vector3 pPos = new Vector3(Mathf.Cos(ang) * pRadius, 2.5f, 22f + Mathf.Sin(ang) * pRadius);

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Sanctuary_Pillar_{i + 1}";
                pillar.transform.SetParent(z4.transform, false);
                pillar.transform.position = pPos;
                pillar.transform.localScale = new Vector3(0.9f, 2.5f, 0.9f);
                var pRend = pillar.GetComponent<MeshRenderer>();
                pRend.sharedMaterial = mats.sanctuaryRune;
                pillarRenderers.Add(pRend);

                // Luz en la cima de cada pilar
                GameObject pLightGO = new GameObject($"PillarLight_{i + 1}");
                pLightGO.transform.SetParent(pillar.transform, false);
                pLightGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                Light pLight = pLightGO.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.color = new Color(0.3f, 0.7f, 1f);
                pLight.intensity = 1.5f;
                pLight.range = 6f;
                pLight.enabled = false; // Se activa con el santuario
                pillarLights.Add(pLight);
            }

            // Piedras Mágicas Flotantes alrededor del santuario
            List<Transform> floatingStones = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 fPos = new Vector3(Mathf.Cos(a) * 3f, 3f + (i % 2) * 0.5f, 22f + Mathf.Sin(a) * 3f);
                GameObject fStone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fStone.name = $"FloatingStone_{i + 1}";
                fStone.transform.SetParent(z4.transform, false);
                fStone.transform.position = fPos;
                fStone.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
                fStone.GetComponent<MeshRenderer>().sharedMaterial = mats.runeStone;
                floatingStones.Add(fStone.transform);
            }

            // Altar Central
            GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "Sanctuary_CentralAltar";
            altar.transform.SetParent(z4.transform, false);
            altar.transform.position = new Vector3(0f, 0.9f, 22f);
            altar.transform.localScale = new Vector3(2.5f, 0.6f, 2.5f);
            altar.GetComponent<MeshRenderer>().sharedMaterial = mats.sanctuaryWhite;

            // INTERACCIÓN 4: Gran Cristal Principal del Santuario
            GameObject mainCrystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainCrystal.name = "Sanctuary_MainCrystal";
            mainCrystal.transform.SetParent(z4.transform, false);
            mainCrystal.transform.position = new Vector3(0f, 2.2f, 22f);
            mainCrystal.transform.localScale = new Vector3(1.2f, 2.2f, 1.2f);
            var mcRend = mainCrystal.GetComponent<MeshRenderer>();
            mcRend.sharedMaterial = mats.crystalBlue;

            // Luz Cenital del Santuario
            GameObject spotGO = new GameObject("Sanctuary_BeamLight");
            spotGO.transform.SetParent(z4.transform, false);
            spotGO.transform.position = new Vector3(0f, 8f, 22f);
            spotGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Light spotLight = spotGO.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.spotAngle = 45f;
            spotLight.range = 15f;
            spotLight.color = new Color(0.4f, 0.8f, 1f);
            spotLight.intensity = 0f; // Sube al despertar

            // Partículas de Aura y Ráfaga
            ParticleSystem auraParticles = CreateSanctuaryAuraParticles(mainCrystal.transform);
            ParticleSystem burstParticles = CreateSanctuaryBurstParticles(mainCrystal.transform);

            // Controlador del Santuario
            var sanctController = mainCrystal.AddComponent<SanctuaryController>();
            // Configurar campos por reflexión o SerializedObject
            SerializedObject so = new SerializedObject(sanctController);
            so.FindProperty("mainCrystalRenderer").objectReferenceValue = mcRend;
            so.FindProperty("sanctuarySpotLight").objectReferenceValue = spotLight;
            so.FindProperty("sanctuaryAuraParticles").objectReferenceValue = auraParticles;
            so.FindProperty("sanctuaryBurstParticles").objectReferenceValue = burstParticles;
            
            SerializedProperty sPillarsProp = so.FindProperty("runePillars");
            sPillarsProp.ClearArray();
            for (int i = 0; i < pillarRenderers.Count; i++)
            {
                sPillarsProp.InsertArrayElementAtIndex(i);
                sPillarsProp.GetArrayElementAtIndex(i).objectReferenceValue = pillarRenderers[i];
            }

            SerializedProperty sLightsProp = so.FindProperty("pillarLights");
            sLightsProp.ClearArray();
            for (int i = 0; i < pillarLights.Count; i++)
            {
                sLightsProp.InsertArrayElementAtIndex(i);
                sLightsProp.GetArrayElementAtIndex(i).objectReferenceValue = pillarLights[i];
            }

            SerializedProperty sStonesProp = so.FindProperty("floatingStones");
            sStonesProp.ClearArray();
            for (int i = 0; i < floatingStones.Count; i++)
            {
                sStonesProp.InsertArrayElementAtIndex(i);
                sStonesProp.GetArrayElementAtIndex(i).objectReferenceValue = floatingStones[i];
            }

            // Asignar sonido
            AudioClip soundClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button Pop.wav");
            so.FindProperty("sanctuaryActivationSound").objectReferenceValue = soundClip;
            so.ApplyModifiedProperties();

            // Objeto Coleccionable 3 en el santuario (Cáliz Sagrado)
            CreateCollectibleObject(z4.transform, new Vector3(1.8f, 1f, 22f), "Reliquia del Santuario", mats.crystalGold);
        }

        private static void BuildMainPathways(Transform root, MaterialsLibrary mats)
        {
            GameObject pathways = new GameObject("Island_Pathways");
            pathways.transform.SetParent(root, false);

            // Segmentos de adoquines/camino conectando: Entrada -> Bosque -> Lago -> Santuario
            Vector3[] pathWaypoints = new Vector3[]
            {
                new Vector3(0f, 0.03f, -14f),
                new Vector3(0f, 0.03f, -10f),
                new Vector3(-4f, 0.03f, -6f),
                new Vector3(-7f, 0.03f, -2f), // Camino al bosque
                new Vector3(0f, 0.03f, 0f),   // Encrucijada central
                new Vector3(6f, 0.03f, 0f),   // Hacia el lago
                new Vector3(0f, 0.03f, 6f),
                new Vector3(0f, 0.03f, 10f),
                new Vector3(0f, 0.03f, 14f)   // Entrada al santuario
            };

            for (int i = 0; i < pathWaypoints.Length; i++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stone.name = $"PathStone_{i + 1}";
                stone.transform.SetParent(pathways.transform, false);
                stone.transform.position = pathWaypoints[i];
                stone.transform.localScale = new Vector3(3.2f, 0.04f, 3.2f);
                stone.GetComponent<MeshRenderer>().sharedMaterial = mats.pathway;
                TeleportManager.EnsureTeleportArea(stone);
            }
        }

        private static void SetupWorldSpaceUI(MaterialsLibrary mats)
        {
            GameObject uiRoot = new GameObject("--- World Space UI & Instructions ---");

            // Cartel 3D Principal en la Entrada
            GameObject signBoard = new GameObject("Welcome_SignBoard");
            signBoard.transform.SetParent(uiRoot.transform, false);
            signBoard.transform.position = new Vector3(0f, 2.2f, -13.5f);
            signBoard.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Orientado mirando hacia el jugador en Z=-18

            // Soporte físico del cartel (postes de madera)
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.transform.SetParent(signBoard.transform, false);
            postL.transform.localPosition = new Vector3(-1.8f, -1f, 0f);
            postL.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
            postL.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.transform.SetParent(signBoard.transform, false);
            postR.transform.localPosition = new Vector3(1.8f, -1f, 0f);
            postR.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
            postR.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            GameObject boardPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardPanel.transform.SetParent(signBoard.transform, false);
            boardPanel.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            boardPanel.transform.localScale = new Vector3(3.8f, 2.2f, 0.08f);
            boardPanel.GetComponent<MeshRenderer>().sharedMaterial = mats.rock;

            // Canvas en World Space
            GameObject canvasGO = new GameObject("VR_WorldCanvas");
            canvasGO.transform.SetParent(signBoard.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            canvasGO.transform.localRotation = Quaternion.identity;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(900f, 520f);
            canvasRT.localScale = new Vector3(0.004f, 0.004f, 0.004f);

            var uiController = canvasGO.AddComponent<VRUIController>();

            // 1. Título
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(canvasGO.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchoredPosition = new Vector2(0f, 190f);
            titleRT.sizeDelta = new Vector2(850f, 60f);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 42;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.85f, 0.3f);
            titleTMP.text = "BIENVENIDO A LA ISLA MÁGICA";

            // 2. Descripción
            GameObject descGO = new GameObject("DescText");
            descGO.transform.SetParent(canvasGO.transform, false);
            var descRT = descGO.AddComponent<RectTransform>();
            descRT.anchoredPosition = new Vector2(0f, 140f);
            descRT.sizeDelta = new Vector2(850f, 40f);
            var descTMP = descGO.AddComponent<TextMeshProUGUI>();
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.fontSize = 22;
            descTMP.color = new Color(0.9f, 0.95f, 1f);
            descTMP.text = "Explora el entorno, descubre los cristales y encuentra los objetos ocultos.";

            // 3. Instrucciones
            GameObject instGO = new GameObject("InstructionsText");
            instGO.transform.SetParent(canvasGO.transform, false);
            var instRT = instGO.AddComponent<RectTransform>();
            instRT.anchoredPosition = new Vector2(0f, 40f);
            instRT.sizeDelta = new Vector2(800f, 130f);
            var instTMP = instGO.AddComponent<TextMeshProUGUI>();
            instTMP.alignment = TextAlignmentOptions.Left;
            instTMP.fontSize = 20;
            instTMP.color = Color.white;
            instTMP.text = 
                "• Apunta hacia el terreno para teletransportarte.\n" +
                "• Confirma el destino con el controlador.\n" +
                "• Utiliza los controles para interactuar con los objetos.\n" +
                "• Descubre el santuario mágico.";

            // 4. Contador de Objetos
            GameObject countGO = new GameObject("CounterText");
            countGO.transform.SetParent(canvasGO.transform, false);
            var countRT = countGO.AddComponent<RectTransform>();
            countRT.anchoredPosition = new Vector2(-180f, -80f);
            countRT.sizeDelta = new Vector2(400f, 40f);
            var countTMP = countGO.AddComponent<TextMeshProUGUI>();
            countTMP.fontSize = 24;
            countTMP.fontStyle = FontStyles.Bold;
            countTMP.color = new Color(0.4f, 1f, 0.8f);
            countTMP.text = "Objetos Mágicos: 0 / 3";

            // 5. Progreso
            GameObject progGO = new GameObject("ProgressText");
            progGO.transform.SetParent(canvasGO.transform, false);
            var progRT = progGO.AddComponent<RectTransform>();
            progRT.anchoredPosition = new Vector2(180f, -80f);
            progRT.sizeDelta = new Vector2(400f, 40f);
            var progTMP = progGO.AddComponent<TextMeshProUGUI>();
            progTMP.fontSize = 22;
            progTMP.color = new Color(0.3f, 0.85f, 1f);
            progTMP.text = "Progreso: 0% - Comienza tu viaje";

            // 6. Notificación Dinámica
            GameObject notifGO = new GameObject("NotificationText");
            notifGO.transform.SetParent(canvasGO.transform, false);
            var notifRT = notifGO.AddComponent<RectTransform>();
            notifRT.anchoredPosition = new Vector2(0f, -160f);
            notifRT.sizeDelta = new Vector2(850f, 50f);
            var notifTMP = notifGO.AddComponent<TextMeshProUGUI>();
            notifTMP.alignment = TextAlignmentOptions.Center;
            notifTMP.fontSize = 22;
            notifTMP.fontStyle = FontStyles.Italic;
            notifTMP.color = new Color(1f, 0.95f, 0.5f);
            notifTMP.text = "Pista: Sigue el sendero empedrado hacia el bosque mágico.";

            // Conectar campos a VRUIController
            SerializedObject uiso = new SerializedObject(uiController);
            uiso.FindProperty("welcomeTitleText").objectReferenceValue = titleTMP;
            uiso.FindProperty("welcomeDescText").objectReferenceValue = descTMP;
            uiso.FindProperty("instructionsText").objectReferenceValue = instTMP;
            uiso.FindProperty("counterText").objectReferenceValue = countTMP;
            uiso.FindProperty("progressText").objectReferenceValue = progTMP;
            uiso.FindProperty("notificationText").objectReferenceValue = notifTMP;
            uiso.ApplyModifiedProperties();
        }

        private static void SetupGameManagers()
        {
            GameObject mgmtGO = new GameObject("--- Island Game Managers ---");
            mgmtGO.AddComponent<CollectibleManager>();
            mgmtGO.AddComponent<TeleportManager>();
            mgmtGO.AddComponent<VRSetupManager>();
        }

        // ==========================================
        // HELPERS PARA CREACIÓN DE ELEMENTOS 3D
        // ==========================================

        private static void CreateStylizedTree(Transform parent, Vector3 pos, float scale, MaterialsLibrary mats)
        {
            GameObject tree = new GameObject("StylizedTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * scale;

            // Tronco
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            trunk.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            // Copa de follaje (Cono o capas de esferas estilizadas)
            GameObject foliage1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage1.name = "Foliage_Bottom";
            foliage1.transform.SetParent(tree.transform, false);
            foliage1.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            foliage1.transform.localScale = new Vector3(3f, 1.8f, 3f);
            foliage1.GetComponent<MeshRenderer>().sharedMaterial = mats.leaves;

            GameObject foliage2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage2.name = "Foliage_Top";
            foliage2.transform.SetParent(tree.transform, false);
            foliage2.transform.localPosition = new Vector3(0f, 4.3f, 0f);
            foliage2.transform.localScale = new Vector3(2f, 1.5f, 2f);
            foliage2.GetComponent<MeshRenderer>().sharedMaterial = mats.leaves;
        }

        private static void CreateRock(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock_Boulder";
            rock.transform.SetParent(parent, false);
            rock.transform.position = pos + new Vector3(0f, scale.y * 0.4f, 0f);
            rock.transform.localScale = scale;
            rock.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0);
            rock.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateGlowingMushroom(Transform parent, Vector3 pos, MaterialsLibrary mats)
        {
            GameObject shroom = new GameObject("GlowingMushroom");
            shroom.transform.SetParent(parent, false);
            shroom.transform.position = pos;

            // Tallo
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(shroom.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            stem.transform.localScale = new Vector3(0.12f, 0.3f, 0.12f);
            stem.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            // Sombrerillo brillante
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.transform.SetParent(shroom.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cap.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = mats.glowingShroom;

            // Luz suave
            GameObject lightGO = new GameObject("ShroomLight");
            lightGO.transform.SetParent(shroom.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.25f, 0.6f);
            l.range = 3f;
            l.intensity = 1.2f;
        }

        private static void CreateMagicLantern(Transform parent, Vector3 pos, MaterialsLibrary mats)
        {
            GameObject lantern = new GameObject("MagicLantern");
            lantern.transform.SetParent(parent, false);
            lantern.transform.position = pos;

            // Pilar
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.transform.SetParent(lantern.transform, false);
            p.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            p.transform.localScale = new Vector3(0.2f, 1.1f, 0.2f);
            p.GetComponent<MeshRenderer>().sharedMaterial = mats.rock;

            // Foco de cristal
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.SetParent(lantern.transform, false);
            g.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            g.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            g.GetComponent<MeshRenderer>().sharedMaterial = mats.crystalBlue;

            GameObject lGO = new GameObject("LanternLight");
            lGO.transform.SetParent(lantern.transform, false);
            lGO.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            Light l = lGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.3f, 0.7f, 1f);
            l.intensity = 2f;
            l.range = 7f;
        }

        private static void CreateCrystalObject(Transform parent, Vector3 pos, string name, Material baseMat, Color glowColor)
        {
            GameObject cGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cGO.name = $"Crystal_{name.Replace(" ", "_")}";
            cGO.transform.SetParent(parent, false);
            cGO.transform.position = pos;
            cGO.transform.localScale = new Vector3(0.7f, 1.4f, 0.7f);
            var rend = cGO.GetComponent<MeshRenderer>();
            rend.sharedMaterial = baseMat;

            // Luz
            GameObject lGO = new GameObject("CrystalLight");
            lGO.transform.SetParent(cGO.transform, false);
            Light l = lGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = glowColor;
            l.intensity = 2f;
            l.range = 5f;
            l.enabled = false;

            // Partículas
            ParticleSystem ps = CreateCrystalParticles(cGO.transform, glowColor);

            // Componente CrystalInteraction
            var ci = cGO.AddComponent<CrystalInteraction>();
            SerializedObject so = new SerializedObject(ci);
            so.FindProperty("crystalRenderer").objectReferenceValue = rend;
            so.FindProperty("activeEmissionColor").colorValue = glowColor * 2.5f;
            so.FindProperty("magicParticles").objectReferenceValue = ps;
            so.FindProperty("crystalLight").objectReferenceValue = l;
            so.FindProperty("crystalName").stringValue = name;
            
            AudioClip soundClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button Pop.wav");
            so.FindProperty("interactionSound").objectReferenceValue = soundClip;
            so.ApplyModifiedProperties();
        }

        private static void CreateMagicRunestone(Transform parent, Vector3 pos, MaterialsLibrary mats)
        {
            GameObject stoneGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stoneGO.name = "Magic_Runestone";
            stoneGO.transform.SetParent(parent, false);
            stoneGO.transform.position = pos + new Vector3(0f, 0.8f, 0f);
            stoneGO.transform.localScale = new Vector3(0.9f, 1.6f, 0.6f);
            var rend = stoneGO.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mats.runeStone;

            // Luz
            GameObject lGO = new GameObject("RuneLight");
            lGO.transform.SetParent(stoneGO.transform, false);
            Light l = lGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 0.35f, 1f);
            l.intensity = 2.5f;
            l.range = 6f;
            l.enabled = false;

            // Polvo místico
            ParticleSystem ps = CreateCrystalParticles(stoneGO.transform, new Color(0.85f, 0.35f, 1f));

            var msi = stoneGO.AddComponent<MagicStoneInteraction>();
            SerializedObject so = new SerializedObject(msi);
            so.FindProperty("stoneRenderer").objectReferenceValue = rend;
            so.FindProperty("stoneLight").objectReferenceValue = l;
            so.FindProperty("magicDust").objectReferenceValue = ps;

            AudioClip soundClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button Pop.wav");
            so.FindProperty("humSound").objectReferenceValue = soundClip;
            so.ApplyModifiedProperties();
        }

        private static void CreateCollectibleObject(Transform parent, Vector3 pos, string itemName, Material mat)
        {
            GameObject colGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            colGO.name = $"Collectible_{itemName.Replace(" ", "_")}";
            colGO.transform.SetParent(parent, false);
            colGO.transform.position = pos;
            colGO.transform.localScale = Vector3.one * 0.35f;

            var rend = colGO.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;

            // Pedestal decorativo para sostener el coleccionable
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(parent, false);
            pedestal.transform.position = pos - new Vector3(0f, 0.55f, 0f);
            pedestal.transform.localScale = new Vector3(0.5f, 0.45f, 0.5f);
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Partículas
            ParticleSystem ps = CreateCrystalParticles(colGO.transform, new Color(1f, 0.85f, 0.2f));

            // Componente CollectibleItem (añade XRGrabInteractable y Rigidbody automáticamente)
            var itemComp = colGO.AddComponent<CollectibleItem>();
            SerializedObject so = new SerializedObject(itemComp);
            so.FindProperty("itemName").stringValue = itemName;
            so.FindProperty("collectParticles").objectReferenceValue = ps;
            so.FindProperty("itemRenderer").objectReferenceValue = rend;

            AudioClip soundClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button Pop.wav");
            so.FindProperty("grabSound").objectReferenceValue = soundClip;
            so.ApplyModifiedProperties();
        }

        // ==========================================
        // HELPERS PARA SISTEMAS DE PARTÍCULAS
        // ==========================================

        private static ParticleSystem CreateCrystalParticles(Transform parent, Color col)
        {
            GameObject psGO = new GameObject("MagicParticles");
            psGO.transform.SetParent(parent, false);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = col;
            main.startSize = 0.08f;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.5f;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            return ps;
        }

        private static ParticleSystem CreateSanctuaryAuraParticles(Transform parent)
        {
            GameObject psGO = new GameObject("SanctuaryAura");
            psGO.transform.SetParent(parent, false);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.3f, 0.7f, 1f, 0.6f);
            main.startSize = 0.15f;
            main.startLifetime = 2.5f;
            main.startSpeed = 0.8f;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 1.2f;

            return ps;
        }

        private static ParticleSystem CreateSanctuaryBurstParticles(Transform parent)
        {
            GameObject psGO = new GameObject("SanctuaryBurst");
            psGO.transform.SetParent(parent, false);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.9f, 0.95f, 1f, 0.9f);
            main.startSize = 0.25f;
            main.startLifetime = 1.5f;
            main.startSpeed = 4f;
            main.playOnAwake = false;
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            return ps;
        }

        private static void CreateMagicSporeParticles(Transform parent, Vector3 pos, Color col)
        {
            GameObject psGO = new GameObject("AmbientMagicSpores");
            psGO.transform.SetParent(parent, false);
            psGO.transform.position = pos;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = col;
            main.startSize = 0.06f;
            main.startLifetime = 4f;
            main.startSpeed = 0.2f;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 4f, 12f);
        }

        private static void CreateSplashParticles(Transform parent, Vector3 pos)
        {
            GameObject psGO = new GameObject("WaterfallSplash");
            psGO.transform.SetParent(parent, false);
            psGO.transform.position = pos;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.85f, 0.95f, 1f, 0.7f);
            main.startSize = 0.12f;
            main.startLifetime = 0.8f;
            main.startSpeed = 1.5f;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 30f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.8f;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
