using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Base.ToolSDK.Editor
{
    /// <summary>Tự tìm mọi module trong các assembly Editor bằng TypeCache.</summary>
    public static class SdkRegistry
    {
        static List<ISdkModule> _all;

        public static IReadOnlyList<ISdkModule> All
        {
            get
            {
                if (_all == null) Build();
                return _all;
            }
        }

        public static bool TryGet(string id, out ISdkModule module)
        {
            module = All.FirstOrDefault(m => m.Id == id);
            return module != null;
        }

        static void Build()
        {
            _all = new List<ISdkModule>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ISdkModule>())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                try
                {
                    _all.Add((ISdkModule)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _all = _all.OrderBy(m => m.Group).ThenBy(m => m.DisplayName).ToList();
        }
    }
}
