using System.Collections.Generic;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 층별 방 슬롯 상태 + 건설 진행. InGameScene에 1개.
    /// - StartBuild: 재화 소모 → 해당 층 슬롯을 '공사중'으로 → 화면 갱신 + 카메라 이동.
    /// - Update: 공사 시간이 끝난 슬롯을 '입주완료'로 전환 → 화면 갱신.
    /// BuildingView가 각 층 모듈을 렌더할 때 GetSlot으로 상태를 조회한다.
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        public enum SlotStatus { Empty, Constructing, Built }

        public class Slot
        {
            public SlotStatus status = SlotStatus.Empty;
            public ModuleData module;   // 공사중/입주완료 모듈
            public float endTime;       // 공사 완료 시각(Time.time)
        }

        private readonly Dictionary<int, Slot> _slots = new Dictionary<int, Slot>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>해당 층 슬롯. 없으면 null(=빈방).</summary>
        public Slot GetSlot(int floorIndex)
        {
            _slots.TryGetValue(floorIndex, out var s);
            return s;
        }

        /// <summary>남은 공사 시간(초). 공사중 아니면 0.</summary>
        public float RemainingSeconds(int floorIndex)
        {
            var s = GetSlot(floorIndex);
            if (s == null || s.status != SlotStatus.Constructing) return 0f;
            return Mathf.Max(0f, s.endTime - Time.time);
        }

        /// <summary>건설 시작. 빈 슬롯 + 재화 충분해야 성공. 성공 시 공사중 전환 + 갱신 + 카메라 이동.</summary>
        public bool StartBuild(int floorIndex, ModuleData m)
        {
            if (m == null) return false;
            var s = GetSlot(floorIndex);
            if (s != null && s.status != SlotStatus.Empty) return false; // 이미 점유

            var gm = GoldManager.Instance;
            if (gm == null || !gm.TrySpend(m.buildCost)) return false;    // 재화 부족

            _slots[floorIndex] = new Slot
            {
                status = SlotStatus.Constructing,
                module = m,
                endTime = Time.time + Mathf.Max(0.1f, m.buildSeconds),
            };

            var bv = Object.FindObjectOfType<BuildingView>();
            if (bv != null)
            {
                bv.RenderBuilding();
                bv.ScrollToFloor(floorIndex);
            }
            return true;
        }

        private void Update()
        {
            List<int> finished = null;
            foreach (var kv in _slots)
            {
                var s = kv.Value;
                if (s.status == SlotStatus.Constructing && Time.time >= s.endTime)
                {
                    if (finished == null) finished = new List<int>();
                    finished.Add(kv.Key);
                }
            }
            if (finished != null)
            {
                foreach (var f in finished) _slots[f].status = SlotStatus.Built;
                var bv = Object.FindObjectOfType<BuildingView>();
                if (bv != null) bv.RenderBuilding();
            }
        }
    }
}
