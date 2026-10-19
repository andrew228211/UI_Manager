using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace Base.ToolSDK.Editor
{
    /// <summary>Quản lý Scripting Define Symbols cho Android, iOS và Standalone.</summary>
    public static class DefineUtil
    {
        static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone
        };

        // Cache ngắn hạn cho truy vấn (Window làm mới liên tục). Ghi luôn đọc trực tiếp.
        const double CacheSeconds = 0.3;
        static List<HashSet<string>> _cache;
        static double _cacheTime;

        public static void Invalidate()
        {
            _cache = null;
        }

        static List<string> ReadFresh(NamedBuildTarget target)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbols(target);
            var list = new List<string>();
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var s in raw.Split(';'))
            {
                var t = s.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list;
        }

        static List<HashSet<string>> Snapshot()
        {
            if (_cache != null && EditorApplication.timeSinceStartup - _cacheTime <= CacheSeconds)
                return _cache;

            var snapshot = new List<HashSet<string>>();
            foreach (var t in Targets) snapshot.Add(new HashSet<string>(ReadFresh(t)));
            _cache = snapshot;
            _cacheTime = EditorApplication.timeSinceStartup;
            return snapshot;
        }

        /// <summary>Có ở TẤT CẢ target.</summary>
        public static bool IsDefined(string symbol)
        {
            foreach (var set in Snapshot())
                if (!set.Contains(symbol)) return false;
            return true;
        }

        /// <summary>Có ở ít nhất một target.</summary>
        public static bool IsDefinedAnywhere(string symbol)
        {
            foreach (var set in Snapshot())
                if (set.Contains(symbol)) return true;
            return false;
        }

        public static void Add(string symbol)
        {
            foreach (var t in Targets)
            {
                var list = ReadFresh(t);
                if (list.Contains(symbol)) continue;
                list.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
            }
            Invalidate();
        }

        public static void Remove(string symbol)
        {
            foreach (var t in Targets)
            {
                var list = ReadFresh(t);
                if (!list.Remove(symbol)) continue;
                PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
            }
            Invalidate();
        }
    }
}
