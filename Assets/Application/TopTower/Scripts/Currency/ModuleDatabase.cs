using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace KS.TopTower
{
    /// <summary>
    /// 런타임 ModuleData 조회소. Awake에서 Addressables 라벨 "ModuleData"를 preload해 (group,name) 키로 매핑.
    /// 모듈 클릭 시 방 정보를 즉시 찾기 위함. InGameScene에 1개 배치.
    /// </summary>
    public class ModuleDatabase : MonoBehaviour
    {
        private static ModuleDatabase _inst;
        private readonly Dictionary<string, ModuleData> _map = new Dictionary<string, ModuleData>();

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(this); return; }
            _inst = this;
            LoadAll();
        }

        private void OnDestroy() { if (_inst == this) _inst = null; }

        private void LoadAll()
        {
            var h = Addressables.LoadAssetsAsync<ModuleData>("ModuleData", null);
            var list = h.WaitForCompletion();
            if (list != null)
                foreach (var m in list)
                    if (m != null) _map[Key(m.group, m.moduleName)] = m;
        }

        private static string Key(ModuleGroup g, string n) => g + "_" + n;

        public static ModuleData Get(ModuleGroup g, string n)
        {
            if (_inst == null) return null;
            _inst._map.TryGetValue(Key(g, n), out var d);
            return d;
        }

        /// <summary>해당 그룹의 모든 ModuleData (임차인 찾기 목록용). 로드 안 됐으면 빈 리스트.</summary>
        public static System.Collections.Generic.List<ModuleData> GetByGroup(ModuleGroup g)
        {
            var result = new System.Collections.Generic.List<ModuleData>();
            if (_inst == null) return result;
            foreach (var kv in _inst._map)
                if (kv.Value != null && kv.Value.group == g) result.Add(kv.Value);
            result.Sort((a, b) => string.Compare(a.moduleName, b.moduleName, System.StringComparison.Ordinal));
            return result;
        }

        /// <summary>스프라이트 파일명(예: "Facility_Empty_002")으로 ModuleData 조회.</summary>
        public static ModuleData GetBySpriteName(string spriteName)
        {
            if (!TryParse(spriteName, out var g, out var n)) return null;
            return Get(g, n);
        }

        /// <summary>`nnn_Group_Name_nnn` 또는 `Group_Name_nnn` 파싱.</summary>
        private static bool TryParse(string file, out ModuleGroup group, out string name)
        {
            group = default; name = null;
            if (string.IsNullOrEmpty(file)) return false;
            var parts = file.Split('_');
            if (parts.Length < 3) return false;

            int start = 0;
            if (int.TryParse(parts[0], out _)) start = 1;
            if (parts.Length - start < 2) return false;

            string gt = parts[start];
            if (int.TryParse(gt, out _)) return false;
            if (!System.Enum.TryParse<ModuleGroup>(gt, out group)) return false;
            if (!System.Enum.IsDefined(typeof(ModuleGroup), group)) return false;

            int end = parts.Length;
            if (int.TryParse(parts[parts.Length - 1], out _)) end = parts.Length - 1;
            int cnt = end - (start + 1);
            if (cnt < 1) return false;

            var arr = new string[cnt];
            System.Array.Copy(parts, start + 1, arr, 0, cnt);
            name = string.Join("_", arr);
            return !string.IsNullOrEmpty(name);
        }
    }
}
