using System.Collections.Generic;
using System.Linq;

namespace Base.ToolSDK.Editor
{
    /// <summary>Sắp xếp module theo phụ thuộc và tạo danh sách bước.</summary>
    public static class Planner
    {
        /// <summary>
        /// Mở rộng danh sách cần cài: tự thêm các module được Requires (nếu chưa cài đủ),
        /// trả về theo thứ tự phụ thuộc (module được cần đứng trước).
        /// Vòng phụ thuộc không làm treo (nhờ visited) nhưng được RequiresRule báo lỗi.
        /// </summary>
        public static List<ISdkModule> ExpandInstall(IEnumerable<ISdkModule> selected, out List<ISdkModule> autoAdded)
        {
            var selectedList = selected.ToList();
            var selectedIds = new HashSet<string>(selectedList.Select(m => m.Id));
            var visited = new HashSet<string>();
            var order = new List<ISdkModule>();
            var added = new List<ISdkModule>();

            void Visit(ISdkModule module)
            {
                if (!visited.Add(module.Id)) return;

                foreach (var reqId in module.Requires)
                {
                    if (!SdkRegistry.TryGet(reqId, out var req)) continue;
                    // Đã cài đủ thì bỏ qua; cài dở thì phải đưa vào kế hoạch để hoàn tất.
                    if (req.GetInstallState() == SdkInstallState.Installed) continue;
                    Visit(req);
                    if (!selectedIds.Contains(req.Id) && !added.Contains(req)) added.Add(req);
                }

                order.Add(module);
            }

            foreach (var m in selectedList) Visit(m);

            autoAdded = added;
            return order;
        }

        /// <summary>Thứ tự gỡ: module phụ thuộc gỡ trước, module được cần gỡ sau.</summary>
        public static List<ISdkModule> OrderForRemove(IEnumerable<ISdkModule> toRemove)
        {
            var list = toRemove.ToList();
            var ids = new HashSet<string>(list.Select(m => m.Id));
            var visited = new HashSet<string>();
            var order = new List<ISdkModule>();

            void Visit(ISdkModule module)
            {
                if (!visited.Add(module.Id)) return;
                foreach (var reqId in module.Requires)
                    if (ids.Contains(reqId) && SdkRegistry.TryGet(reqId, out var req))
                        Visit(req);
                order.Add(module);
            }

            foreach (var m in list) Visit(m);
            order.Reverse();
            return order;
        }

        public static List<StepData> BuildInstallSteps(IEnumerable<ISdkModule> ordered)
        {
            var steps = new List<StepData>();
            foreach (var m in ordered) steps.AddRange(m.BuildInstallSteps());
            return steps;
        }

        /// <summary>
        /// Tạo bước gỡ, BỎ các package/define mà module còn lại (keep) vẫn dùng,
        /// để gỡ module này không làm hỏng module khác. Các trường hợp bị giữ lại ghi vào notes.
        /// </summary>
        public static List<StepData> BuildRemoveSteps(
            IEnumerable<ISdkModule> ordered,
            IEnumerable<ISdkModule> keep,
            out List<string> notes)
        {
            notes = new List<string>();

            // Tài nguyên mà module được giữ lại sở hữu (suy ra từ bước cài của chúng).
            var ownedPackages = new Dictionary<string, string>();
            var ownedDefines = new Dictionary<string, string>();
            foreach (var k in keep)
            {
                foreach (var s in k.BuildInstallSteps())
                {
                    if (s.kind == StepKind.AddPackage)
                    {
                        var name = PackageUtil.NameOf(s.arg);
                        if (!ownedPackages.ContainsKey(name)) ownedPackages[name] = k.DisplayName;
                    }
                    else if (s.kind == StepKind.AddDefine)
                    {
                        if (!ownedDefines.ContainsKey(s.arg)) ownedDefines[s.arg] = k.DisplayName;
                    }
                }
            }

            var steps = new List<StepData>();
            var emitted = new HashSet<string>();
            var noted = new HashSet<string>();

            foreach (var m in ordered)
            {
                foreach (var s in m.BuildRemoveSteps())
                {
                    string key;
                    string owner = null;
                    string what = null;

                    switch (s.kind)
                    {
                        case StepKind.RemovePackage:
                        {
                            var name = PackageUtil.NameOf(s.arg);
                            key = "pkg:" + name;
                            what = "package " + name;
                            ownedPackages.TryGetValue(name, out owner);
                            break;
                        }
                        case StepKind.RemoveDefine:
                        {
                            key = "def:" + s.arg;
                            what = "define " + s.arg;
                            ownedDefines.TryGetValue(s.arg, out owner);
                            break;
                        }
                        default:
                            key = s.kind + ":" + s.arg;
                            break;
                    }

                    if (owner != null)
                    {
                        if (noted.Add(key))
                            notes.Add("Giữ lại " + what + " vì '" + owner + "' vẫn dùng.");
                        continue;
                    }

                    if (emitted.Add(key)) steps.Add(s);
                }
            }

            return steps;
        }
    }
}
