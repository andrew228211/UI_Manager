using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;

namespace Base.ToolSDK.Editor
{
    /// <summary>
    /// Truy vấn package UPM. Chỉ tính package CÀI TRỰC TIẾP (có trong manifest.json):
    /// package chỉ có mặt vì là dependency của package khác không được tính là đã cài,
    /// nếu không bước gỡ sẽ không bao giờ hoàn tất.
    /// Lưu ý: bản demo chưa hỗ trợ nhận biết package cài bằng git URL.
    /// </summary>
    public static class PackageUtil
    {
        struct Entry
        {
            public string Version;
            public bool Direct;
        }

        // Cache ngắn hạn: Window làm mới liên tục, không nên duyệt toàn bộ package mỗi lần.
        const double CacheSeconds = 0.3;
        static Dictionary<string, Entry> _cache;
        static double _cacheTime;

        static PackageUtil()
        {
            Events.registeredPackages += _ => Invalidate();
        }

        /// <summary>Xóa cache. Gọi khi vừa có thay đổi package.</summary>
        public static void Invalidate()
        {
            _cache = null;
        }

        static bool TryGet(string name, out Entry entry)
        {
            if (_cache == null || EditorApplication.timeSinceStartup - _cacheTime > CacheSeconds)
            {
                var map = new Dictionary<string, Entry>();
                foreach (var p in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                    map[p.name] = new Entry { Version = p.version, Direct = p.isDirectDependency };
                _cache = map;
                _cacheTime = EditorApplication.timeSinceStartup;
            }

            return _cache.TryGetValue(name, out entry);
        }

        /// <summary>Lấy tên package từ id (bỏ phần @version).</summary>
        public static string NameOf(string id)
        {
            var i = id.IndexOf('@');
            return i < 0 ? id : id.Substring(0, i);
        }

        static string VersionOf(string id)
        {
            var i = id.IndexOf('@');
            return i < 0 ? null : id.Substring(i + 1);
        }

        // Phần sau @ là phiên bản thường (1.2.3), không phải URL/hash git.
        static bool IsPlainVersion(string v)
        {
            return !string.IsNullOrEmpty(v) && v.IndexOf('/') < 0 && v.IndexOf(':') < 0 && v.IndexOf('#') < 0;
        }

        /// <summary>Package có trong manifest.json (cài trực tiếp), bất kể phiên bản.</summary>
        public static bool IsDirect(string name)
        {
            return TryGet(name, out var e) && e.Direct;
        }

        /// <summary>
        /// Đã cài trực tiếp, và nếu id có @version thì phải đúng phiên bản đó
        /// (sai phiên bản → chưa đạt, bước thêm sẽ cập nhật).
        /// </summary>
        public static bool IsInstalled(string id)
        {
            if (!TryGet(NameOf(id), out var e) || !e.Direct) return false;
            var version = VersionOf(id);
            return !IsPlainVersion(version) || e.Version == version;
        }
    }
}
