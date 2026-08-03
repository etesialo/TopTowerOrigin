using System;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 초당 총수입률 관리. 입주(Built) 세입자들의 incomePerSecond 합 × globalMultiplier.
    /// 이벤트 기반 재계산(Recalculate) — 슬롯 변경/프레스티지 시에만 호출(매 프레임 X).
    /// GoldManager가 TotalRatePerSec를 읽어 골드를 누적한다.
    /// 씬에 없으면 최초 접근 시 자동 생성(수동 배치 불필요).
    /// </summary>
    public class IncomeManager : MonoBehaviour
    {
        private static IncomeManager _inst;

        public static IncomeManager Instance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = FindObjectOfType<IncomeManager>();
                    if (_inst == null)
                    {
                        var go = new GameObject("IncomeManager");
                        _inst = go.AddComponent<IncomeManager>();
                    }
                }
                return _inst;
            }
        }

        /// <summary>프레스티지·업그레이드 등 전역 배수. Phase 0에선 1.0 고정.</summary>
        public double GlobalMultiplier { get; set; } = 1.0;

        /// <summary>현재 초당 총수입(골드/초).</summary>
        public double TotalRatePerSec { get; private set; }

        /// <summary>수입률 변동 시 (UI 표시용).</summary>
        public event Action<double> OnRateChanged;

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(this); return; }
            _inst = this;
        }

        private void OnDestroy()
        {
            if (_inst == this) _inst = null;
        }

        /// <summary>입주 세입자 합산 × 배수로 총수입률 재계산. 슬롯 변경/프레스티지 시 호출.</summary>
        public void Recalculate()
        {
            long baseSum = BuildManager.Instance != null ? BuildManager.Instance.SumBuiltIncomePerSecond() : 0;
            double rate = baseSum * GlobalMultiplier;
            if (rate != TotalRatePerSec)
            {
                TotalRatePerSec = rate;
                OnRateChanged?.Invoke(rate);
            }
        }
    }
}
