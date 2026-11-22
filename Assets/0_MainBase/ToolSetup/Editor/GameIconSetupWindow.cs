using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Base.Editor
{
    // Gán nhanh icon game: Default icon + Android (Legacy/Round/Adaptive).
    public class GameIconSetupWindow : EditorWindow
    {
        Texture2D _icon;        // icon gốc (Default, Android Legacy/Round)
        Texture2D _background;  // Android Adaptive - layer background
        Texture2D _foreground;  // Android Adaptive - layer foreground

        [MenuItem("Base/Tool Setup/Game Icon Setup")]
        static void Open() => GetWindow<GameIconSetupWindow>("Game Icon Setup");

        void OnGUI()
        {
            _icon = Field("Icon gốc", _icon);
            _background = Field("Background (Adaptive)", _background);
            _foreground = Field("Foreground (Adaptive)", _foreground);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_icon == null))
                if (GUILayout.Button("Setup", GUILayout.Height(30))) Setup();

            if (_icon != null && (_background == null || _foreground == null))
                EditorGUILayout.HelpBox("Thiếu Background/Foreground: Adaptive icon sẽ không được set.", MessageType.Info);
        }

        static Texture2D Field(string label, Texture2D tex) =>
            (Texture2D)EditorGUILayout.ObjectField(label, tex, typeof(Texture2D), false, GUILayout.Height(64));

        void Setup()
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { _icon }, IconKind.Any);

            // ponytail: match kind theo tên để không phụ thuộc module UnityEditor.Android; thêm iOS khi cần override riêng.
            var android = NamedBuildTarget.Android;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(android))
            {
                var name = kind.ToString();
                bool adaptive = name.Contains("Adaptive");
                if (adaptive && (_background == null || _foreground == null)) continue;
                if (!adaptive && !name.Contains("Legacy") && !name.Contains("Round")) continue;

                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var icon in icons)
                {
                    if (adaptive)
                    {
                        icon.SetTexture(_background, 0);
                        icon.SetTexture(_foreground, 1);
                    }
                    else icon.SetTexture(_icon);
                }
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[GameIconSetup] Đã cập nhật icon trong Player Settings.");
        }
    }
}
