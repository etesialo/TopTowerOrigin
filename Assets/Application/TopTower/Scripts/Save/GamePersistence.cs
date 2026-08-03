using System;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 저장 오케스트레이션. 부팅 시 로드→복원(→오프라인 정산), 백그라운드/종료/주기 저장.
    /// 씬 배치 불필요 — LoadIntoGame() 최초 호출 시 자동 생성(DontDestroyOnLoad).
    /// TowerStage.Start()가 스테이지 프리팹 인스턴스화 '전에' LoadIntoGame()을 호출해야
    /// BuildingView가 복원된 슬롯 상태 그대로 렌더한다.
    /// </summary>
    public class GamePersistence : MonoBehaviour
    {
        private const float AutosaveInterval = 20f;
        private const long OfflineCapSeconds = 7200;   // 오프라인 수익 상한 2시간
        private const long StartingReward = 110;       // 신규 시작 보상: 관리동 100 + 첫 식당 10

        private static GamePersistence _inst;
        private float _autosaveTimer;

        /// <summary>부팅 로드: 세이브 적용(골드·슬롯) + 수입률 재계산 (+ 오프라인 정산은 Step3).</summary>
        public static void LoadIntoGame()
        {
            EnsureInstance();

            var data = SaveSystem.Load();
            if (data == null)
            {
                // 신규 게임: 시작 보상 자동 지급
                if (GoldManager.Instance != null) GoldManager.Instance.SetGold(StartingReward);
                return;
            }

            if (GoldManager.Instance != null) GoldManager.Instance.SetGold(data.gold);
            if (BuildManager.Instance != null) BuildManager.Instance.ImportSlots(data.slots);
            if (IncomeManager.Instance != null) IncomeManager.Instance.Recalculate();

            GrantOfflineEarnings(data.lastSaveUnixUtc);
        }

        /// <summary>마지막 저장 이후 경과시간 × 수입률(상한)을 지급 + 복귀 팝업.</summary>
        private static void GrantOfflineEarnings(long lastSaveUnixUtc)
        {
            if (lastSaveUnixUtc <= 0) return;
            if (GoldManager.Instance == null || IncomeManager.Instance == null) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - lastSaveUnixUtc;
            if (elapsed <= 0) return;                        // 시계 되돌림/동시각 → 지급 없음
            long capped = Math.Min(elapsed, OfflineCapSeconds);

            double rate = IncomeManager.Instance.TotalRatePerSec;
            long offlineGold = (long)(rate * capped);
            if (offlineGold <= 0) return;

            GoldManager.Instance.Add(offlineGold);
            OfflineRewardPopup.Show(offlineGold, capped);
        }

        /// <summary>현재 상태를 파일로 저장.</summary>
        public static void SaveNow()
        {
            if (GoldManager.Instance == null || BuildManager.Instance == null) return;
            var data = new SaveData
            {
                gold = GoldManager.Instance.Gold,
                lastSaveUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                slots = BuildManager.Instance.ExportSlots(),
            };
            SaveSystem.Save(data);
        }

        private static void EnsureInstance()
        {
            if (_inst != null) return;
            var go = new GameObject("GamePersistence");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<GamePersistence>();
        }

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
        }

        private void OnDestroy() { if (_inst == this) _inst = null; }

        private void Update()
        {
            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= AutosaveInterval)
            {
                _autosaveTimer = 0f;
                SaveNow();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();   // 모바일 백그라운드 진입
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }
    }
}
