using System;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 재화(Gold) 중앙 관리. InGameScene에 배치.
    /// - 아이들 골드: 방치 시 초당 자동 적립 (기본 10/초).
    /// - 상한: MaxGold 도달 시 더 오르지 않고 초과분 무시.
    /// - 다른 수입원(임대료·보상 등)은 GoldManager.Instance.Add(amount) 호출로 합류 예정.
    /// </summary>
    public class GoldManager : MonoBehaviour
    {
        // 골드 상한 (추후 확장 가능).
        public const long MaxGold = 999_999_999L;

        [Tooltip("시작 골드.")]
        [SerializeField] private long _startGold = 0;

        [Tooltip("아이들 골드: 한 번에 오르는 양(덩어리).")]
        [SerializeField] private long _idleGoldPerTick = 10;

        [Tooltip("아이들 골드: 지급 간격(초). 이 간격마다 위 양만큼 '한 번에' 오름.")]
        [SerializeField] private float _idleTickSeconds = 1f;

        private long _gold;
        private float _idleTimer;

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
            if (_idleGoldPerTick <= 0 || _idleTickSeconds <= 0f || _gold >= MaxGold) return;

            // 간격마다 덩어리로 '한 번에' 지급 (예: 1초마다 +10 → 0, 10, 20, 30 …)
            _idleTimer += Time.deltaTime;
            while (_idleTimer >= _idleTickSeconds)
            {
                _idleTimer -= _idleTickSeconds;
                Add(_idleGoldPerTick);
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
