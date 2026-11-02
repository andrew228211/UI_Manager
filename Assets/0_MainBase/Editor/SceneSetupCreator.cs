using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Base.Editor
{
    public enum SceneType
    {
        Scene2D,
        Scene3D
    }

    [System.Serializable]
    public class SceneSetupOptions
    {
        public string sceneName = "GameScene";
        public SceneType type = SceneType.Scene2D;

        // Camera
        public Color backgroundColor = new Color(0.19f, 0.30f, 0.47f);
        public float orthographicSize = 5f;   // 2D
        public float fieldOfView = 60f;       // 3D

        // Thành phần thêm
        public bool addLight = true;          // 3D: Directional Light
        public bool addGround = false;        // 3D: Plane
        public bool addCanvas = true;         // Canvas + EventSystem
        public Vector2 referenceResolution = new Vector2(1080, 1920);
        public float matchWidthOrHeight = 0.5f;

        public bool addToBuildSettings = true;
        public bool openAfterCreate = true;
    }

    /// <summary>Tạo scene 2D/3D đã setup sẵn Camera, Light, Canvas, EventSystem...</summary>
    public static class SceneSetupCreator
    {
        [MenuItem("Assets/Game Folders/New Scene 2D", false, 20)]
        private static void CreateScene2DHere() => CreateQuick(SceneType.Scene2D);

        [MenuItem("Assets/Game Folders/New Scene 3D", false, 21)]
        private static void CreateScene3DHere() => CreateQuick(SceneType.Scene3D);

        private static void CreateQuick(SceneType type)
        {
            string folder = FolderStructureCreator.GetSelectedFolderPath();
            var options = new SceneSetupOptions
            {
                type = type,
                sceneName = type == SceneType.Scene2D ? "NewScene2D" : "NewScene3D",
                addLight = type == SceneType.Scene3D,
            };
            Create(folder, options);
        }

        /// <summary>Tạo scene trong folder. Trả về path scene hoặc null nếu bị huỷ.</summary>
        public static string Create(string folder, SceneSetupOptions o)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return null;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            string name = string.IsNullOrWhiteSpace(o.sceneName) ? "NewScene" : o.sceneName.Trim();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.unity");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (o.type == SceneType.Scene2D) Setup2D(o);
            else Setup3D(o);

            if (o.addCanvas)
            {
                CreateCanvas(o);
                CreateEventSystem();
            }

            EditorSceneManager.SaveScene(scene, path);

            if (o.addToBuildSettings) AddToBuildSettings(path);

            if (!o.openAfterCreate)
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[SceneSetup] Đã tạo scene {o.type}: {path}");
            return path;
        }

        // ---------------- 2D ----------------
        private static void Setup2D(SceneSetupOptions o)
        {
            var cam = CreateCamera(o);
            cam.orthographic = true;
            cam.orthographicSize = o.orthographicSize;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            // Scene view chuyển sang chế độ 2D
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.in2DMode = true;
        }

        // ---------------- 3D ----------------
        private static void Setup3D(SceneSetupOptions o)
        {
            var cam = CreateCamera(o);
            cam.orthographic = false;
            cam.fieldOfView = o.fieldOfView;
            cam.transform.position = new Vector3(0, 1, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.Skybox;

            if (o.addLight)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.956f, 0.839f);
                light.intensity = 1f;
                light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            if (o.addGround)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(5, 1, 5);
            }

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.in2DMode = false;
        }

        // ---------------- Common ----------------
        private static Camera CreateCamera(SceneSetupOptions o)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = o.backgroundColor;
            return cam;
        }

        private static void CreateCanvas(SceneSetupOptions o)
        {
            var go = new GameObject("Canvas") { layer = LayerMask.NameToLayer("UI") };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = o.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = o.matchWidthOrHeight;

            go.AddComponent<GraphicRaycaster>();
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
