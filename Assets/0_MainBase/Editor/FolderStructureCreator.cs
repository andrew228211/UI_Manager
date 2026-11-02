using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.Editor
{
    /// <summary>
    /// Tạo nhanh cấu trúc folder chuẩn cho game.
    /// Chuột phải trong Project window -> Game Folders
    /// </summary>
    public class FolderStructureCreator : EditorWindow
    {
        [System.Serializable]
        private class FolderEntry
        {
            public string path;
            public bool enabled;

            public FolderEntry(string path, bool enabled = true)
            {
                this.path = path;
                this.enabled = enabled;
            }
        }

        private static readonly string[] DefaultFolders =
        {
            "Scenes",
            "Scripts",
            "Scripts/Core",
            "Scripts/UI",
            "Scripts/Gameplay",
            "Scripts/Data",
            "Prefabs",
            "Prefabs/UI",
            "Sprites",
            "Sprites/UI",
            "Textures",
            "Materials",
            "Shaders",
            "Models",
            "Animations",
            "Animations/Clips",
            "Animations/Controllers",
            "Audio",
            "Audio/Music",
            "Audio/SFX",
            "Fonts",
            "VFX",
            "ScriptableObjects",
            "Resources",
            "Editor",
            "Plugins",
        };

        private const string PrefKeyRoot = "Base.FolderCreator.Root";

        private string _parentFolder = "Assets";
        private string _rootFolder = "_Game";
        private bool _addGitKeep = true;
        private string _newFolder = "";
        private List<FolderEntry> _folders;
        private Vector2 _scroll;

        // Scene setup
        private bool _createScene = true;
        private bool _sceneFoldout = true;
        private SceneSetupOptions _sceneOptions = new SceneSetupOptions();

        [MenuItem("Assets/Game Folders/Open Creator...", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderStructureCreator>("Folder Creator");
            window.minSize = new Vector2(380, 640);
            window._parentFolder = GetSelectedFolderPath();
            window.Repaint();
        }

        [MenuItem("Assets/Game Folders/Create Default Folders Here", false, 1)]
        private static void CreateInSelectedFolder()
        {
            string parent = GetSelectedFolderPath();
            int count = CreateFolders(parent, DefaultFolders, true);
            Debug.Log($"[FolderCreator] Đã tạo {count} folder trong '{parent}'.");
        }

        private void OnEnable()
        {
            _rootFolder = EditorPrefs.GetString(PrefKeyRoot, _rootFolder);
            ResetToDefault();
        }

        private void ResetToDefault()
        {
            _folders = new List<FolderEntry>();
            foreach (var f in DefaultFolders)
                _folders.Add(new FolderEntry(f));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Cấu hình", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Parent Folder", _parentFolder);
            if (GUILayout.Button("Use Selected", GUILayout.Width(95)))
                _parentFolder = GetSelectedFolderPath();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _rootFolder = EditorGUILayout.TextField(
                new GUIContent("Root Folder", "Để trống để tạo trực tiếp trong Parent Folder"), _rootFolder);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(PrefKeyRoot, _rootFolder);

            _addGitKeep = EditorGUILayout.Toggle(
                new GUIContent("Add .gitkeep", "Thêm file .gitkeep để git giữ lại folder rỗng"), _addGitKeep);

            EditorGUILayout.HelpBox($"Đích: {GetRootPath()}", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);
            if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                _folders.ForEach(f => f.enabled = true);
            if (GUILayout.Button("None", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                _folders.ForEach(f => f.enabled = false);
            if (GUILayout.Button("Reset", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                ResetToDefault();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, EditorStyles.helpBox, GUILayout.MinHeight(150));
            for (int i = 0; i < _folders.Count; i++)
            {
                var entry = _folders[i];
                EditorGUILayout.BeginHorizontal();
                int depth = entry.path.Split('/').Length - 1;
                GUILayout.Space(depth * 15);
                entry.enabled = EditorGUILayout.ToggleLeft(entry.path, entry.enabled);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _folders.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            _newFolder = EditorGUILayout.TextField(_newFolder);
            GUI.enabled = !string.IsNullOrWhiteSpace(_newFolder);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                string clean = _newFolder.Trim().Replace('\\', '/').Trim('/');
                if (!_folders.Exists(f => f.path == clean))
                {
                    _folders.Add(new FolderEntry(clean));
                    _folders.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
                }
                _newFolder = "";
                GUI.FocusControl(null);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawSceneSection();

            EditorGUILayout.Space();
            if (GUILayout.Button(_createScene ? "Create Folders + Scene" : "Create Folders", GUILayout.Height(32)))
            {
                var selected = _folders.FindAll(f => f.enabled).ConvertAll(f => f.path);
                int count = CreateFolders(GetRootPath(), selected, _addGitKeep);
                Debug.Log($"[FolderCreator] Đã tạo {count} folder mới trong '{GetRootPath()}'.");

                if (_createScene)
                {
                    SceneSetupCreator.Create($"{GetRootPath()}/Scenes", _sceneOptions);
                    GUIUtility.ExitGUI(); // scene mới được mở -> thoát GUI để tránh lỗi layout
                }
            }

            if (GUILayout.Button("Create Scene Only"))
            {
                SceneSetupCreator.Create($"{GetRootPath()}/Scenes", _sceneOptions);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSceneSection()
        {
            var o = _sceneOptions;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _sceneFoldout = EditorGUILayout.Foldout(_sceneFoldout, "Scene Setup", true, EditorStyles.foldoutHeader);
            _createScene = EditorGUILayout.ToggleLeft("Tạo scene", _createScene, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (_sceneFoldout)
            {
                EditorGUI.indentLevel++;
                o.sceneName = EditorGUILayout.TextField("Scene Name", o.sceneName);

                EditorGUI.BeginChangeCheck();
                int typeIdx = GUILayout.Toolbar((int)o.type, new[] { "2D", "3D" });
                if (EditorGUI.EndChangeCheck())
                {
                    o.type = (SceneType)typeIdx;
                    o.addLight = o.type == SceneType.Scene3D;
                }

                EditorGUILayout.LabelField("Camera", EditorStyles.miniBoldLabel);
                o.backgroundColor = EditorGUILayout.ColorField("Background", o.backgroundColor);
                if (o.type == SceneType.Scene2D)
                {
                    o.orthographicSize = EditorGUILayout.FloatField("Orthographic Size", o.orthographicSize);
                }
                else
                {
                    o.fieldOfView = EditorGUILayout.Slider("Field Of View", o.fieldOfView, 20f, 120f);
                    o.addLight = EditorGUILayout.Toggle("Directional Light", o.addLight);
                    o.addGround = EditorGUILayout.Toggle("Ground Plane", o.addGround);
                }

                EditorGUILayout.LabelField("UI", EditorStyles.miniBoldLabel);
                o.addCanvas = EditorGUILayout.Toggle("Canvas + EventSystem", o.addCanvas);
                if (o.addCanvas)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Reference Res");
                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    if (GUILayout.Button("Portrait", EditorStyles.miniButtonLeft)) o.referenceResolution = new Vector2(1080, 1920);
                    if (GUILayout.Button("Landscape", EditorStyles.miniButtonRight)) o.referenceResolution = new Vector2(1920, 1080);
                    EditorGUI.indentLevel = indent;
                    EditorGUILayout.EndHorizontal();
                    o.referenceResolution = EditorGUILayout.Vector2Field(" ", o.referenceResolution);
                    o.matchWidthOrHeight = EditorGUILayout.Slider("Match (W - H)", o.matchWidthOrHeight, 0f, 1f);
                }

                EditorGUILayout.LabelField("Khác", EditorStyles.miniBoldLabel);
                o.addToBuildSettings = EditorGUILayout.Toggle("Add To Build Settings", o.addToBuildSettings);
                o.openAfterCreate = EditorGUILayout.Toggle("Open After Create", o.openAfterCreate);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private string GetRootPath()
        {
            string root = (_rootFolder ?? "").Trim().Replace('\\', '/').Trim('/');
            string parent = string.IsNullOrEmpty(_parentFolder) ? "Assets" : _parentFolder;
            return string.IsNullOrEmpty(root) ? parent : $"{parent}/{root}";
        }

        public static string GetSelectedFolderPath()
        {
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path)) return path;
                return Path.GetDirectoryName(path)?.Replace('\\', '/');
            }
            return "Assets";
        }

        /// <summary>Tạo các folder (kể cả folder cha) dưới parentPath. Trả về số folder mới tạo.</summary>
        private static int CreateFolders(string parentPath, IEnumerable<string> relativePaths, bool addGitKeep)
        {
            int created = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                created += EnsureFolder(parentPath);

                foreach (var rel in relativePaths)
                {
                    string full = $"{parentPath}/{rel}";
                    created += EnsureFolder(full);

                    if (addGitKeep)
                    {
                        string keep = Path.Combine(full, ".gitkeep");
                        if (!File.Exists(keep)) File.WriteAllText(keep, string.Empty);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            return created;
        }

        private static int EnsureFolder(string assetPath)
        {
            // Dùng Directory.CreateDirectory để an toàn khi đang StartAssetEditing;
            // Unity sẽ tự sinh .meta khi Refresh.
            if (Directory.Exists(assetPath)) return 0;
            Directory.CreateDirectory(assetPath);
            return 1;
        }
    }
}
