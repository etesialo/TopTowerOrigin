using System.Collections.Generic;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 층×셀 방 슬롯 상태 + 건설 진행. InGameScene에 1개.
    /// - 셀 구조: 지하층 = 2셀(2슬롯씩, 시설 나란히), 지상층 = 1셀(4슬롯).
    /// - StartBuild: 존/게이트/재화 검증 → 셀을 '공사중'으로 → 화면 갱신 + 카메라 이동.
    /// - Update: 공사 완료 셀을 '입주완료'로 전환 → 화면 갱신.
    /// - 게이트: 관리동(Facility/Admin) 건설 전에는 관리동만 건설 가능.
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        public const string ManagementCoreName = "Admin";   // 관리동 moduleName

        public enum SlotStatus { Empty, Constructing, Built }

        public class Slot
        {
            public SlotStatus status = SlotStatus.Empty;
            public ModuleData module;   // 공사중/입주완료 모듈
            public float endTime;       // 공사 완료 시각(Time.time)
        }

        private readonly Dictionary<(int floor, int cell), Slot> _slots = new Dictionary<(int, int), Slot>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>층의 셀 개수. 지하=2, 지상=1.</summary>
        public static int CellCountForFloor(int floor) => floor < 0 ? 2 : 1;

        /// <summary>층의 존. 음수=지하, 그 외=지상.</summary>
        public static Zone ZoneForFloor(int floor) => floor < 0 ? Zone.Underground : Zone.Aboveground;

        /// <summary>관리동(핵심 시설)인지.</summary>
        public static bool IsManagementCore(ModuleData m)
            => m != null && m.group == ModuleGroup.Facility && m.moduleName == ManagementCoreName;

        /// <summary>관리동이 입주완료 상태인지 (전체 모듈 해금 여부). 슬롯에서 파생 → 세이브에 자동 반영.</summary>
        public bool ManagementBuilt
        {
            get
            {
                foreach (var kv in _slots)
                    if (kv.Value.status == SlotStatus.Built && IsManagementCore(kv.Value.module)) return true;
                return false;
            }
        }

        /// <summary>해당 셀 슬롯. 없으면 null(=빈방).</summary>
        public Slot GetSlot(int floor, int cell)
        {
            _slots.TryGetValue((floor, cell), out var s);
            return s;
        }

        /// <summary>세이브용: 빈방 제외한 슬롯들을 직렬화 구조로 내보냄.</summary>
        public List<SlotSave> ExportSlots()
        {
            var list = new List<SlotSave>();
            foreach (var kv in _slots)
            {
                var s = kv.Value;
                if (s.status == SlotStatus.Empty || s.module == null) continue;
                list.Add(new SlotSave
                {
                    floorIndex = kv.Key.floor,
                    cellIndex = kv.Key.cell,
                    group = (int)s.module.group,
                    moduleName = s.module.moduleName,
                    status = (int)s.status,
                    remainingBuildSeconds = s.status == SlotStatus.Constructing ? Mathf.Max(0f, s.endTime - Time.time) : 0f,
                });
            }
            return list;
        }

        /// <summary>세이브 로드 복원. 렌더 전에 호출하면 BuildingView가 복원 상태로 그림.</summary>
        public void ImportSlots(List<SlotSave> slots)
        {
            _slots.Clear();
            if (slots == null) return;
            foreach (var ss in slots)
            {
                if (ss.status == (int)SlotStatus.Empty) continue;
                var module = ModuleDatabase.Get((ModuleGroup)ss.group, ss.moduleName);
                if (module == null) continue;
                var slot = new Slot { status = (SlotStatus)ss.status, module = module };
                if (slot.status == SlotStatus.Constructing)
                    slot.endTime = Time.time + Mathf.Max(0.1f, ss.remainingBuildSeconds);
                _slots[(ss.floorIndex, ss.cellIndex)] = slot;
            }
        }

        /// <summary>입주(Built) 세입자들의 초당 수입 합.</summary>
        public long SumBuiltIncomePerSecond()
        {
            long sum = 0;
            foreach (var kv in _slots)
            {
                var s = kv.Value;
                if (s.status == SlotStatus.Built && s.module != null)
                    sum += s.module.incomePerSecond;
            }
            return sum;
        }

        /// <summary>남은 공사 시간(초). 공사중 아니면 0.</summary>
        public float RemainingSeconds(int floor, int cell)
        {
            var s = GetSlot(floor, cell);
            if (s == null || s.status != SlotStatus.Constructing) return 0f;
            return Mathf.Max(0f, s.endTime - Time.time);
        }

        /// <summary>건설 시작. 존/게이트/점유/재화 검증 통과해야 성공.</summary>
        public bool StartBuild(int floor, int cell, ModuleData m)
        {
            if (m == null) return false;

            // 존 검사: 모듈 allowedZones에 이 층 존이 포함돼야 (시설=지하, 업종=지상)
            Zone zone = ZoneForFloor(floor);
            if (m.allowedZones != null && m.allowedZones.Count > 0 && !m.allowedZones.Contains(zone)) return false;

            // 게이트: 관리동 건설 전엔 관리동만
            if (!ManagementBuilt && !IsManagementCore(m)) return false;

            var key = (floor, cell);
            if (_slots.TryGetValue(key, out var s) && s.status != SlotStatus.Empty) return false; // 점유

            var gm = GoldManager.Instance;
            if (gm == null || !gm.TrySpend(m.buildCost)) return false; // 재화 부족

            _slots[key] = new Slot
            {
                status = SlotStatus.Constructing,
                module = m,
                endTime = Time.time + Mathf.Max(0.1f, m.buildSeconds),
            };

            var bv = Object.FindObjectOfType<BuildingView>();
            if (bv != null)
            {
                bv.RefreshCell(floor, cell);
                bv.ScrollToFloor(floor);
            }
            return true;
        }

        private void Update()
        {
            List<(int floor, int cell)> finished = null;
            foreach (var kv in _slots)
            {
                var s = kv.Value;
                if (s.status == SlotStatus.Constructing && Time.time >= s.endTime)
                {
                    if (finished == null) finished = new List<(int, int)>();
                    finished.Add(kv.Key);
                }
            }
            if (finished != null)
            {
                var bv = Object.FindObjectOfType<BuildingView>();
                foreach (var key in finished)
                {
                    _slots[key].status = SlotStatus.Built;
                    if (bv != null) bv.RefreshCell(key.floor, key.cell);
                }
                if (IncomeManager.Instance != null) IncomeManager.Instance.Recalculate();
            }
        }
    }
}
