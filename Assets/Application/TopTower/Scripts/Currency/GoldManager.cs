using System;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 재화(Gold) 중앙 관리. InGameScene에 배치.
    /// - 방치 수입: IncomeManager.TotalRatePerSec(입주 세입자 초당 수입 합)을 매 프레임 누적.
    /// - 상한: MaxGold 도달 시 더 오르지 않고 초과분 무시.
    /// - 다른 수입원(오프라인 정산·보상 등)은 GoldManager.Instance.Add(amount) 호출로 합류.
    /// </summary>
    public class GoldManager : MonoBehaviour
    {
        // 골드 상한 (추후 확장 가능).
        public const long MaxGold = 999_999_999L;

        [Tooltip("시작 골드.")]
        [SerializeField] private long _startGold = 0;

        [Tooltip("수입 지급 간격(초). 이 간격마다 그동안 번 만큼을 '한 번에' 올린다(1씩 스르륵 X).")]
        [SerializeField] private float _payoutIntervalSeconds = 1f;

        private long _gold;
        private double _carry;    // 초당 수입 누적 버퍼(소수부 포함)
        private float _payoutTimer;

        public static GoldManager Instance { get; private set; }

        /// <summary>현재 골드.</summary>
        public long Gold => _gold;

        /// <summary>골드 변동 시 발생 (신규 값 전달). UI 구독용.</summary>
        public event Action<long> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _gold = Clamp(_startGold);
        }

        private void Start()
        {
            // 초기 UI 갱신 (구독자들이 준비된 뒤 1회 통지)
            OnGoldChanged?.Invoke(_gold);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_gold >= MaxGold) return;

            // 수입은 매 프레임 내부 버퍼에만 계속 누적 (아직 화면엔 반영 X)
            var im = IncomeManager.Instance;
            double rate = im != null ? im.TotalRatePerSec : 0.0;
            if (rate > 0.0) _carry += rate * Time.deltaTime;

            // 간격마다 그동안 쌓인 정수분을 '한 번에' 지급 → 눈에 보이는 덩어리 상승
            if (_payoutIntervalSeconds <= 0f) _payoutIntervalSeconds = 1f;
            _payoutTimer += Time.deltaTime;
            if (_payoutTimer >= _payoutIntervalSeconds)
            {
                _payoutTimer -= _payoutIntervalSeconds;
                if (_carry >= 1.0)
                {
                    long whole = (long)_carry;
                    _carry -= whole;
                    Add(whole);   // 1회 지급 = 1회 OnGoldChanged → 한 번에 점프
                }
            }
        }

        /// <summary>골드 획득. 상한 초과분은 무시. (임대료·보상 등 모든 수입원이 이걸 호출)</summary>
        public void Add(long amount)
        {
            if (amount <= 0) return;
            long next = _gold + amount;
            if (next > MaxGold) next = MaxGold;   // 상한 도달 → 초과 무시
            if (next != _gold)
            {
                _gold = next;
                OnGoldChanged?.Invoke(_gold);
            }
        }

        /// <summary>골드 절대값 설정 (세이브 로드 시). 상한 클램프 + UI 통지.</summary>
        public void SetGold(long value)
        {
            _gold = Clamp(value);
            _carry = 0.0;
            OnGoldChanged?.Invoke(_gold);
        }

        /// <summary>골드 사용. 부족하면 false (추후 상점/건설 등에서 사용).</summary>
        public bool TrySpend(long amount)
        {
            if (amount <= 0) return true;
            if (_gold < amount) return false;
            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            return true;
        }

        private static long Clamp(long v)
        {
            if (v < 0) return 0;
            if (v > MaxGold) return MaxGold;
            return v;
        }
    }
}
