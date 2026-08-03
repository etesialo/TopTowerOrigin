# Top Tower — Phase 0 상세 설계안

> 범위: **저장/로드 · 세입자 기반 수입(초 단위) · 오프라인 수익 · 성장 곡선 v1**
> 목적: 방치겜 기반 인프라 구축. 코드 구현 전 합의용 문서. (상위: [TopTower_BusinessPlan.md](TopTower_BusinessPlan.md))
> 결정사항: **수입 단위 = 초(gold/sec)**.

---

## 0. 요약

세 시스템은 서로 물려 있어 하나의 마일스톤으로 간다:

1. **IncomeManager** — 입주 세입자들의 초당 수입률 합산(× 배수)을 관리, GoldManager에 rate 공급.
2. **SaveSystem** — 골드·슬롯·마지막 저장시각을 JSON으로 저장/로드.
3. **OfflineEarnings** — 로드 시 경과시간 × 수입률(상한)로 정산 + 팝업.

구현 순서: `IncomeManager & GoldManager 개편` → `SaveSystem` → `OfflineEarnings + 팝업`.

---

## 1. 수입 모델 (초 단위)

### 1.1 개념
- 각 입주(Built) 세입자는 **초당 수입률**을 가진다.
- **총 수입률(totalRate) = Σ(입주 세입자 rate) × globalMultiplier**
  - `globalMultiplier` = 프레스티지 배수 × 업그레이드 배수 (Phase 0에선 1.0 고정, Phase 1에서 실제 값).
- 골드는 매 프레임 `totalRate × deltaTime` 만큼 누적(소수부는 버퍼링 후 정수 지급).

### 1.2 ModuleData 필드 처리
- 기존 `public long dailyRent;` 를 **초당 수입률**로 해석.
- 리네임 권장: `dailyRent` → `incomePerSecond` (의미 명확화).
  - 값 보존 위해 `[UnityEngine.Serialization.FormerlySerializedAs("dailyRent")]` 부착 → 기존 에셋 값 유지.
  - 현재 값 전부 임시 5라 데이터 손실 위험 사실상 없음.
- UI 문구 수정: "일일 임대 / 임대" → **"수입 /초"** (RoomInfoPopup, TenantFinderPopup).

### 1.3 IncomeManager (신규)
```
class IncomeManager (MonoBehaviour, singleton)
    double TotalRatePerSec { get; }        // 현재 초당 총수입
    event Action<double> OnRateChanged;    // UI 표시용
    void Recalculate();                    // 슬롯 변경/프레스티지 시 호출 (매 프레임 X)
    // BuildManager의 입주 슬롯을 순회하며 rate 합산 × globalMultiplier
```
- **이벤트 기반 재계산**: 건설 완료/철거/프레스티지 때만 `Recalculate()`. 매 프레임 합산 금지(성능).
- GoldManager는 IncomeManager.TotalRatePerSec를 읽어 누적.

### 1.4 GoldManager 개편
- 기존 고정 `_idleGoldPerTick`(초당 10) **제거**.
- Update에서:
```
_carry += IncomeManager.TotalRatePerSec * Time.deltaTime;   // double 버퍼
if (_carry >= 1.0) { long whole=(long)_carry; _carry-=whole; Add(whole); }
```
- 빈 타워 기본 수입 = 0 (건물을 지어야 번다). *FTUE에서 첫 세입자를 빠르게 쥐여줌.*
- `Add/TrySpend/OnGoldChanged/MaxGold`는 유지.

---

## 2. SaveSystem (신규)

### 2.1 저장 데이터 구조 (JsonUtility 호환 — Dictionary 불가, List 사용)
```
[Serializable] class SaveData
    int    version = 1;
    long   gold;
    long   lastSaveUnixUtc;          // DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    List<SlotSave> slots;
    // (Phase1+) prestige, upgrades, collection, settings

[Serializable] class SlotSave
    int    floorIndex;
    string moduleKey;                // "Group_Name" (예: "Restaurant_Sushi") → ModuleDatabase 조회
    int    status;                   // 0 Empty(저장 안 함) / 1 Constructing / 2 Built
    float  remainingBuildSeconds;    // Constructing일 때 남은 공사시간
```
- 빈 슬롯(Empty)은 저장하지 않음(용량↓, 로드 시 기본 빈방).

### 2.2 저장/로드 위치
- 경로: `Application.persistentDataPath + "/toptower_save.json"`.
- 직렬화: `JsonUtility.ToJson` (간단·빠름). 추후 암호화/버전 마이그레이션 여지.

### 2.3 저장 트리거
- `OnApplicationPause(true)` — 모바일 백그라운드 진입(가장 중요).
- `OnApplicationQuit`.
- **주기적 오토세이브**(예: 20초).
- 주요 상태 변화 직후(건설 시작/완료). — 과도 저장 방지 위해 dirty 플래그 + 최소 간격.

### 2.4 로드 플로우 (부팅 시)
1. 파일 있으면 파싱, 없으면 신규 세이브 생성(기본값).
2. `GoldManager`에 gold 주입.
3. `BuildManager`에 slots 복원(Constructing은 remaining으로, Built는 즉시 입주).
4. `IncomeManager.Recalculate()`.
5. **OfflineEarnings 정산**(§3) → gold 추가 + 팝업.
6. `BuildingView` 렌더(기존 경로).

### 2.5 저장 versioning
- `version` 필드로 향후 구조 변경 시 마이그레이션. v1에서 시작.

---

## 3. 오프라인 수익 (신규)

### 3.1 정산 계산
```
now      = 현재 UnixUtc
elapsed  = clamp(now - lastSaveUnixUtc, 0, OfflineCapSeconds)
offlineGold = floor(TotalRatePerSec * elapsed)   // 저장 시점 입주 세입자 기준
```
- `OfflineCapSeconds` 기본 **7200초(2시간)** — 조정 가능(추후 광고/업그레이드로 연장).

### 3.2 오프라인 중 공사 처리 (v1 단순화)
- 각 Constructing 슬롯: `remaining -= elapsed`.
  - `remaining <= 0` → Built로 전환(입주 완료).
  - 그 외 → 남은 remaining으로 재개.
- **단순화**: 오프라인 중 완공된 세입자의 "완공 후~복귀"까지의 부분 수입은 v1에서 미반영(저장시점 입주분만 정산). 추후 정밀화.

### 3.3 복귀 팝업 (OfflineRewardPopup)
- 내용: "오프라인 {시간} 동안 **{offlineGold}** 골드 획득".
- 버튼: `[받기]` (정산 확정), `[2배 받기 (광고)]` — Phase 3에서 광고 연결, Phase 0에선 자리만/비활성.
- 오프라인 시간 0 또는 수입 0이면 팝업 생략.

---

## 4. 성장 곡선 v1

> 실제 수치는 별도 엑셀에서 확정 후 **데이터로 주입**(하드코딩 금지). 여기선 형태와 초기값만.

### 4.1 공식
- **건설 비용**: `cost(k) = ceil(baseCost × r_cost^(k))`, `r_cost ≈ 1.15`.
  - k = 해당 업종의 보유/건설 순번 또는 층 높이(택1 — 엑셀에서 결정).
- **세입자 수입률**: `rate = baseRate × r_income^(tier)`, `r_income < r_cost` (수입이 비용보다 완만 → "하나 더" 트레드밀).
- **업그레이드(Phase1)**: 세입자 Lv업당 rate × 배수.
- **프레스티지(Phase1)**: 누적 실적 기반 영구 globalMultiplier.

### 4.2 Phase 0 잠정값
- 개발 중이므로 현재 통일값(공사 10 / 수입 5·초) 유지한 채 **시스템만 먼저 연결**.
- 곡선 엑셀 확정 후 ModuleData/설정에 실제 값 주입(별도 작업).

---

## 5. 변경/신규 파일 정리

### 신규
| 파일 | 역할 |
|---|---|
| `Scripts/Currency/IncomeManager.cs` | 초당 총수입률 산출·캐시·이벤트 |
| `Scripts/Save/SaveSystem.cs` | SaveData 구조 + 저장/로드(JSON) |
| `Scripts/Save/GamePersistence.cs` (또는 SaveSystem에 통합) | 저장 트리거(Pause/Quit/오토세이브)·부팅 로드 오케스트레이션 |
| `Scripts/Currency/OfflineEarnings.cs` | 경과시간 정산 계산 |
| `Scripts/UI/OfflineRewardPopup.cs` | 복귀 보상 팝업 |

### 수정
| 파일 | 변경 |
|---|---|
| `Scripts/Data/ModuleData.cs` | `dailyRent` → `incomePerSecond`(FormerlySerializedAs) |
| `Scripts/Currency/GoldManager.cs` | 고정 idle 제거 → IncomeManager rate 기반 누적 |
| `Scripts/Currency/BuildManager.cs` | 입주 슬롯 열거 API, 슬롯 직렬화(save)·복원(load), 상태변화 시 IncomeManager.Recalculate + Save dirty |
| `Scripts/UI/RoomInfoPopup.cs`, `TenantFinderPopup.cs` | "임대" 문구 → "수입 /초" |
| 부팅 지점(`TowerStage`/InGameScene 부트스트랩) | 로드 → 복원 → 오프라인 정산 순서 삽입 |

---

## 6. 엣지 케이스 / 주의

- **시계 조작 방지**: `now < lastSave`(시간 되돌림)면 elapsed=0 처리. (정밀 방지는 서버 필요 — v1은 클라 클램프만.)
- **MaxGold 상한**: 오프라인 정산도 상한 클램프(기존 Add 로직 재사용).
- **저장 손상/버전 불일치**: 파싱 실패 시 백업 후 신규 세이브로 복구(진행 손실 최소화 로깅).
- **에디터 재생 프레임 정지(MCP)**: Play 검증은 유저가 Game 뷰 포커스로 직접. (참고: 기존 MCP 한계)
- **double 누적 정밀도**: 초당 수입 누적은 double 버퍼로, 골드는 long 유지.

---

## 7. 완료 기준 (Definition of Done)

1. 앱 종료/백그라운드 후 재실행 시 **골드·건물이 유지**된다.
2. 세입자를 지을수록 **초당 수입이 실제로 증가**한다(수입률 UI로 확인).
3. 앱을 꺼둔 시간만큼 **오프라인 골드가 정산**되고 복귀 팝업이 뜬다(상한 2시간).
4. 저장/로드/정산에 예외·데이터손실 없음.

---

## 8. 다음 단계 (승인 후)

1. IncomeManager + GoldManager 개편 → 검증.
2. SaveSystem + BuildManager 직렬화/복원 → 검증.
3. OfflineEarnings + 팝업 → 검증.
4. (별도) 성장 곡선 엑셀 → 실제 수치 주입.

> 미결 파라미터(구현 중 조정 가능): 오프라인 상한(기본 2h), 오토세이브 간격(기본 20s), 빈 타워 기본수입(기본 0).
