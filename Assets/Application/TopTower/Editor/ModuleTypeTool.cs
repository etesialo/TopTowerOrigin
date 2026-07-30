using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KS.TopTower.EditorTools
{
    /// <summary>
    /// 모듈 타입 속성 편집기. 레벨→그룹→모듈을 고르면 미리보기 + 6축 속성(Cube/Frame/Zone/Extend)을 편집·저장.
    /// Group/Module은 스프라이트 파일명에서 자동 파싱(읽기전용). 저장 대상: Assets/Application/TopTower/ModuleData/.
    /// 메뉴: Tools/Top Tower/Module Type Editor.
    /// </summary>
    public class ModuleTypeTool : EditorWindow
    {
        private const string ModuleImageRoot = "Assets/Application/TopTower/Image/Module";
        private const string ModuleDataFolder = "Assets/Application/TopTower/ModuleData";

        private string[] _stages = new string[0];
        private int _stageIdx;
        private ModuleGroup _group = ModuleGroup.Structural;

        private struct Entry { public string name; public string path; public Sprite sprite; }
        private List<Entry> _modules = new List<Entry>();
        private int _moduleIdx = -1;

        // 편집 중 값
        private CubeSize _cube;
        private Frame _frame;
        private bool _zoneAG, _zoneUG, _zoneRT;
        private ModuleExtend _extend;
        private string _moduleName = "";
        private Sprite _sprite;
        private ModuleData _loadedAsset;

        // 방 정보 (팝업 표시용)
        private bool _clickable;
        private string _roomName = "";
        private string _description = "";
        private long _dailyRent;
        private long _buildCost;
        private float _buildSeconds = 10f;

        [MenuItem("Tools/Top Tower/Module Type Editor")]
        public static void Open()
        {
            var w = GetWindow<ModuleTypeTool>("Module Type");
            w.minSize = new Vector2(470, 540);
            w.RefreshStages();
        }

        private void OnEnable() { RefreshStages(); }

        private void RefreshStages()
        {
            if (!AssetDatabase.IsValidFolder(ModuleImageRoot)) { _stages = new string[0]; return; }
            _stages = AssetDatabase.GetSubFolders(ModuleImageRoot).Select(Path.GetFileName).ToArray();
            if (_stageIdx >= _stages.Length) _stageIdx = 0;
            RefreshModules();
        }

        private void RefreshModules()
        {
            _modules.Clear();
            _moduleIdx = -1;
            _sprite = null; _moduleName = ""; _loadedAsset = null;
            if (_stages.Length == 0) return;

            string folder = ModuleImageRoot + "/" + _stages[_stageIdx];
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue; // 직속만
                string file = Path.GetFileNameWithoutExtension(path);
                if (!TryParse(file, out var g, out var name)) continue;
                if (g != _group) continue;
                _modules.Add(new Entry { name = name, path = path, sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path) });
            }
            _modules = _modules.OrderBy(m => m.name).ToList();
            if (_modules.Count > 0) { _moduleIdx = 0; LoadModule(_modules[0]); }
        }

        /// <summary>파일명 파싱. `nnn_Group_Name_nnn` 또는 `Group_Name_nnn`(공통 폴더) 형식 모두 지원.</summary>
        private static bool TryParse(string file, out ModuleGroup group, out string name)
        {
            group = default; name = null;
            var parts = file.Split('_');
            if (parts.Length < 3) return false;

            int start = 0;
            if (int.TryParse(parts[0], out _)) start = 1;               // 앞 stage id 있으면 스킵
            if (parts.Length - start < 2) return false;

            string groupToken = parts[start];
            if (int.TryParse(groupToken, out _)) return false;          // 숫자면 그룹 아님
            if (!System.Enum.TryParse<ModuleGroup>(groupToken, out group)) return false;
            if (!System.Enum.IsDefined(typeof(ModuleGroup), group)) return false;

            int end = parts.Length;
            if (int.TryParse(parts[parts.Length - 1], out _)) end = parts.Length - 1;  // 뒤 id 스킵
            int nameCount = end - (start + 1);
            if (nameCount < 1) return false;
            name = string.Join("_", parts.Skip(start + 1).Take(nameCount));
            return !string.IsNullOrEmpty(name);
        }

        private void LoadModule(Entry e)
        {
            _sprite = e.sprite;
            _moduleName = e.name;
            string assetPath = ModuleDataFolder + "/MD_" + _group + "_" + e.name + ".asset";
            _loadedAsset = AssetDatabase.LoadAssetAtPath<ModuleData>(assetPath);

            if (_loadedAsset != null)
            {
                _cube = _loadedAsset.cube; _frame = _loadedAsset.frame; _extend = _loadedAsset.extend;
                _zoneAG = _loadedAsset.allowedZones.Contains(Zone.Aboveground);
                _zoneUG = _loadedAsset.allowedZones.Contains(Zone.Underground);
                _zoneRT = _loadedAsset.allowedZones.Contains(Zone.Rooftop);
                _clickable = _loadedAsset.clickable;
                _roomName = _loadedAsset.roomName;
                _description = _loadedAsset.description;
                _dailyRent = _loadedAsset.dailyRent;
                _buildCost = _loadedAsset.buildCost;
                _buildSeconds = _loadedAsset.buildSeconds;
            }
            else
            {
                var d = ModuleDefaults.Get(_group, e.name);
                _cube = d.cube; _frame = d.frame; _extend = d.extend;
                _zoneAG = d.zones.Contains(Zone.Aboveground);
                _zoneUG = d.zones.Contains(Zone.Underground);
                _zoneRT = d.zones.Contains(Zone.Rooftop);
                _clickable = d.clickable;
                _roomName = e.name;   // 기본 방이름 = 모듈명 (편집 권장)
                _description = "";
                _dailyRent = 0;
                _buildCost = 0;
                _buildSeconds = 10f;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Top Tower — Module Type Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_stages.Length == 0)
            {
                EditorGUILayout.HelpBox("레벨 폴더 없음: " + ModuleImageRoot, MessageType.Warning);
                if (GUILayout.Button("새로고침")) RefreshStages();
                return;
            }

            EditorGUI.BeginChangeCheck();
            _stageIdx = EditorGUILayout.Popup("스테이지(Stage)", _stageIdx, _stages);
            _group = (ModuleGroup)EditorGUILayout.EnumPopup("그룹(Group)", _group);
            if (EditorGUI.EndChangeCheck()) RefreshModules();

            // 스테이지별 모듈 슬롯 폭 (선택한 스테이지의 StageData 편집. 엘베는 항상 1이라 예외)
            string stageName = _stages[_stageIdx];
            var stageData = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Application/TopTower/StageData/" + stageName + ".asset");
            if (stageData != null)
            {
                EditorGUI.BeginChangeCheck();
                int w = EditorGUILayout.IntField("모듈 슬롯 폭 (이 스테이지, 엘베 제외)", stageData.ModuleSlotWidth);
                if (EditorGUI.EndChangeCheck())
                {
                    stageData.ModuleSlotWidth = Mathf.Max(1, w);
                    EditorUtility.SetDirty(stageData);
                    AssetDatabase.SaveAssets();
                }
            }

            if (_modules.Count == 0)
            {
                EditorGUILayout.HelpBox("이 레벨/그룹에 해당하는 스프라이트가 없습니다.", MessageType.Info);
                return;
            }

            var names = _modules.Select(m => m.name).ToArray();
            int idx = Mathf.Clamp(_moduleIdx, 0, names.Length - 1);
            int newIdx = EditorGUILayout.Popup("모듈(Module)", idx, names);
            if (newIdx != _moduleIdx)
            {
                _moduleIdx = newIdx;
                LoadModule(_modules[_moduleIdx]);
            }

            EditorGUILayout.Space();
            DrawEditor();
        }

        private void DrawEditor()
        {
            EditorGUILayout.BeginHorizontal();

            Rect box = GUILayoutUtility.GetRect(150, 150, GUILayout.Width(150), GUILayout.Height(150));
            EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.15f));
            if (_sprite != null) DrawSprite(box, _sprite);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("그룹(Group)", _group.ToString());
            EditorGUILayout.LabelField("모듈(Module)", _moduleName);
            EditorGUILayout.Space(4);
            _cube = (CubeSize)EditorGUILayout.EnumPopup("큐브(Cube)", _cube);
            _frame = (Frame)EditorGUILayout.EnumPopup("뼈대(Frame)", _frame);
            EditorGUILayout.LabelField("존(Zone) — 다중 선택");
            _zoneAG = EditorGUILayout.ToggleLeft("   Aboveground 지상", _zoneAG);
            _zoneUG = EditorGUILayout.ToggleLeft("   Underground 지하", _zoneUG);
            _zoneRT = EditorGUILayout.ToggleLeft("   Rooftop 옥상", _zoneRT);
            _extend = (ModuleExtend)EditorGUILayout.EnumPopup("확장(Extend)", _extend);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 방 정보 (팝업 표시용)
            EditorGUILayout.LabelField("방 정보 (팝업 표시)", EditorStyles.boldLabel);
            _clickable = EditorGUILayout.ToggleLeft("클릭 가능 (방 정보 팝업 표시)", _clickable);
            _roomName = EditorGUILayout.TextField("방 이름(RoomName)", _roomName);
            EditorGUILayout.LabelField("방 설명(Description)");
            _description = EditorGUILayout.TextArea(_description, GUILayout.MinHeight(48));
            _dailyRent = EditorGUILayout.LongField("일일 임대료(Daily Rent)", _dailyRent);
            _buildCost = EditorGUILayout.LongField("공사비용(Build Cost)", _buildCost);
            _buildSeconds = EditorGUILayout.FloatField("건설시간 초(Build Seconds)", _buildSeconds);
            EditorGUILayout.Space();

            if (_loadedAsset != null)
                EditorGUILayout.HelpBox("기존 ModuleData 로드됨:\n" + AssetDatabase.GetAssetPath(_loadedAsset), MessageType.None);
            else
                EditorGUILayout.HelpBox("저장 시 새 ModuleData 생성 (기본값 미리 채워짐)", MessageType.Info);

            if (GUILayout.Button("저장 (Save)", GUILayout.Height(28)))
                Save();
        }

        private void Save()
        {
            EnsureFolder(ModuleDataFolder);
            string assetPath = ModuleDataFolder + "/MD_" + _group + "_" + _moduleName + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<ModuleData>(assetPath);
            bool isNew = data == null;
            if (isNew) data = CreateInstance<ModuleData>();

            data.group = _group;
            data.moduleName = _moduleName;
            data.cube = _cube;
            data.frame = _frame;
            data.extend = _extend;
            data.sprite = _sprite;
            data.allowedZones = new List<Zone>();
            if (_zoneAG) data.allowedZones.Add(Zone.Aboveground);
            if (_zoneUG) data.allowedZones.Add(Zone.Underground);
            if (_zoneRT) data.allowedZones.Add(Zone.Rooftop);
            data.clickable = _clickable;
            data.roomName = _roomName;
            data.description = _description;
            data.dailyRent = _dailyRent;
            data.buildCost = _buildCost;
            data.buildSeconds = _buildSeconds;

            if (isNew) AssetDatabase.CreateAsset(data, assetPath);
            else EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            _loadedAsset = data;
            TopTowerAddressablesSyncTool.Sync();   // 저장 즉시 Addressables 동기화 → 런타임 자동 인식
            Debug.Log("[ModuleTypeTool] 저장 + Sync 완료: " + assetPath);
        }

        private static void DrawSprite(Rect r, Sprite sp)
        {
            var tex = sp.texture;
            if (tex == null) return;
            var tr = sp.textureRect;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            float aspect = tr.width / tr.height;
            Rect fit = r;
            if (aspect >= 1f) { fit.height = r.width / aspect; fit.y = r.y + (r.height - fit.height) * 0.5f; }
            else { fit.width = r.height * aspect; fit.x = r.x + (r.width - fit.width) * 0.5f; }
            GUI.DrawTextureWithTexCoords(fit, tex, uv);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// 모듈 갱신: 모든 모듈 스프라이트를 스캔해 ModuleData가 없는 새 모듈을 자동 생성 + Sync.
        /// 기존 ModuleData는 건드리지 않음(스킵). 새 가게 이미지만 넣고 이걸 실행하면 됨.
        /// </summary>
        [MenuItem("Tools/Top Tower/Scan & Add Missing Modules")]
        public static void GenerateDefaults()
        {
            EnsureFolder(ModuleDataFolder);
            int created = 0, skipped = 0;
            var seen = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ModuleImageRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (!TryParse(file, out var g, out var name)) continue;

                string key = g + "_" + name;
                if (!seen.Add(key)) continue;                               // 스테이지 공통: 이름당 1개
                string assetPath = ModuleDataFolder + "/MD_" + key + ".asset";
                if (AssetDatabase.LoadAssetAtPath<ModuleData>(assetPath) != null) { skipped++; continue; }

                var d = ModuleDefaults.Get(g, name);
                var data = CreateInstance<ModuleData>();
                data.group = g; data.moduleName = name; data.cube = d.cube; data.frame = d.frame;
                data.extend = d.extend; data.allowedZones = new List<Zone>(d.zones);
                data.clickable = d.clickable;
                data.roomName = name;   // 기본 방이름 = 모듈명 (툴에서 편집)
                // 개발 중 통일값: 임대 업종(구조/시스템 제외)은 공사비/건설시간/임대료를 초밥집과 동일하게.
                if (g != ModuleGroup.Structural && g != ModuleGroup.System)
                {
                    data.buildCost = 10;
                    data.buildSeconds = 3f;
                    data.dailyRent = 5;
                }
                data.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                AssetDatabase.CreateAsset(data, assetPath);
                created++;
            }
            AssetDatabase.SaveAssets();
            TopTowerAddressablesSyncTool.Sync();   // 생성 즉시 Addressables 동기화
            Debug.Log("[ModuleTypeTool] 모듈 갱신 완료 + Sync. 신규 생성 " + created + " / 스킵(이미 있음) " + skipped);
        }
    }

    /// <summary>모듈명 기준 기본 타입 속성 프리셋 (Structural/Facility).</summary>
    internal static class ModuleDefaults
    {
        public struct Def { public CubeSize cube; public Frame frame; public Zone[] zones; public ModuleExtend extend; public bool clickable; }

        public static Def Get(ModuleGroup group, string name)
        {
            var def = GetTypeDef(group, name);
            // 클릭(방 정보 팝업) 기본값: 구조/시설은 false, 임대 업종 그룹은 true.
            // 구조(Structural)만 기본 비클릭. System(빈방·엘베)·임대업종은 상호작용 가능 → 기본 클릭.
            def.clickable = group != ModuleGroup.Structural;
            return def;
        }

        private static Def GetTypeDef(ModuleGroup group, string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "wall":
                case "gate":        return D(CubeSize.Long, Frame.Outdoor, Zone.Aboveground);
                case "underwall":   return D(CubeSize.Long, Frame.Outdoor, Zone.Underground);
                case "wallfr":      return D(CubeSize.Short, Frame.Outdoor, Zone.Aboveground);
                case "underwallfr": return D(CubeSize.Short, Frame.Outdoor, Zone.Underground);
                case "bottom":
                case "bottomfr":    return D(CubeSize.Short, Frame.Outdoor, Zone.Underground);
                case "floor":       return D(CubeSize.Short, Frame.Bonedoor, Zone.Aboveground, Zone.Underground);
                case "root":        return D(CubeSize.Big, Frame.Outdoor, Zone.Underground);
                case "cons":
                case "consroof":    return D(CubeSize.Big, Frame.Outdoor, Zone.Rooftop);
                case "elevator":
                case "empty":       return D(CubeSize.Long, Frame.Indoor, Zone.Aboveground, Zone.Underground);
            }
            if (group == ModuleGroup.Structural) return D(CubeSize.Long, Frame.Outdoor, Zone.Aboveground);
            if (group == ModuleGroup.Facility)   return D(CubeSize.Long, Frame.Indoor, Zone.Aboveground, Zone.Underground);
            return D(CubeSize.Long, Frame.Indoor, Zone.Aboveground);
        }

        private static Def D(CubeSize c, Frame f, params Zone[] z)
        {
            return new Def { cube = c, frame = f, zones = z, extend = ModuleExtend.Normal };
        }
    }
}
