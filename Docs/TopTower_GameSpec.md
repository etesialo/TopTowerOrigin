# Top Tower — 게임 기획서 / 이식 사양서

**문서 목적**: 이 프로젝트(회사 RootBox 의존)를 vanilla Unity 2D URP 빈 프로젝트로 옮길 때 **빌딩 큐브 구조와 시각/입력 시스템을 픽셀 단위로 동일하게 재현**할 수 있는 자기충족형 사양서.

읽는 사람은 현재 코드에 접근 못해도 이 문서 + Unity + 우리 sprite 자산만 있으면 재구현 가능해야 함.

---

> **본 문서 범위**: Top Tower의 **빌딩 시각 시스템** 전용 — 큐브 그리드, 모듈/외벽/구분줄/배경/입력/라벨. 메커닉(세입자/방문객/골드), HUD, UI 패널, 카드 매칭 등은 별도 사양.

## 1. 게임 개요

| 항목 | 값 |
|---|---|
| 게임명 | Top Tower |
| 장르 | 타워 방치형 (Sim Tower / Tiny Tower / Project Highrise 계열) |
| 플랫폼 | 모바일 세로 |
| 해상도(참조) | **1080 × 1920 (9:16)** |
| 비주얼 | 평면 단면도 (원근감 없음, 건물 내부가 그대로 보임) |

### 1.1 Canvas / CanvasScaler 표준 설정 (Stage prefab 루트)

이 설정이 틀리면 다양한 종횡비 디바이스에서 빌딩 가로 위치가 어긋남.

| 컴포넌트 | 속성 | 값 |
|---|---|---|
| Canvas | renderMode | `ScreenSpaceOverlay` |
| CanvasScaler | uiScaleMode | `ScaleWithScreenSize` |
| CanvasScaler | referenceResolution | (1080, 1920) |
| CanvasScaler | screenMatchMode | `MatchWidthOrHeight` |
| CanvasScaler | **matchWidthOrHeight** | **0 (가로 우선)** |
| GraphicRaycaster | (있어야 클릭/드래그 동작) | 기본값 |

Stage_001 prefab의 root RT는 anchor 중앙, **sizeDelta (1080, 1920) 고정**. (Anchor stretch + sizeDelta 0이면 다른 화면비에서 늘어남.)

**핵심 메커닉 (구현 예정, 본 문서 범위 밖)**:
- 세입자(Tenant): 방 임대 → 자동 골드 납부
- 방문객(Visitor): 방 방문 → 골드 드롭, 일괄 수거

---

## 2. 빌딩 그리드 구조 (CORE — 픽셀 단위 정확 재현 대상)

### 2.1 가로 그리드 — 9칸 고정

모든 스테이지 공통. col 인덱스 0~8.

```
col   0       1       2  3  4  5  6       7       8
     [BG]   [Wall]  [I][I][I][I][I]    [Wall]   [BG]
     배경    좌측외벽    실내 5칸 (C D E F G)   우측외벽   배경
```

- **col 0, 8**: 배경 (Background) — 하늘/지면 sprite 보이는 영역. 어떤 sprite도 그리지 않음.
- **col 1**: 좌측 외벽 (Outdoor) — `_Wall_` 또는 지하층은 `_Underwall_`, 1F는 `_Gate_` 우선.
- **col 2~6**: 실내 (Indoor) — Empty 모듈, Elevator, 기타 모듈. 한글 표기 C/D/E/F/G.
- **col 7**: 우측 외벽 — col 1의 미러.

### 2.2 세로 그리드 — 가변

스테이지마다 다름. 각 Floor는 `FloorIndex`로 식별.

| FloorIndex | 의미 |
|---|---|
| 1, 2, 3, ... | 지상층 (1F, 2F, 3F, ...) |
| -1, -2, -3, ... | 지하층 (B1, B2, B3, ...) |
| **0** | **사용 안 함** |

### 2.3 큐브 픽셀 크기 (참조 해상도 1080 기준)

| 변수 | 계산식 | 값 (gridWidth=9, cubeAspectRatio=1.3, ceilingHeightRatio=1/3) |
|---|---|---|
| `cubeWidth` | containerWidth / gridWidth | 1080 / 9 = **120 px** |
| `cubeHeight` | cubeWidth × cubeAspectRatio | 120 × 1.3 = **156 px** |
| `ceilingHeight` | cubeHeight × ceilingHeightRatio | 156 / 3 ≈ **52 px** |
| `stackHeight` | cubeHeight + ceilingHeight | ≈ **208 px** (한 층이 점유하는 총 세로) |

**디자이너 노출 파라미터** (Inspector에서 조정):
- `_gridWidth` = 9 (고정)
- `_cubeAspectRatio` Range(0.5, 3.0), default 1.3
- `_ceilingHeightRatio` Range(0.0, 1.0), default 1/3

### 2.4 한 층의 구조 (위에서 아래)

각 floor는 다음 두 row가 위아래로 점유:

```
┌─────────────────────────────────────────┐  ← ceilingTopY (이 floor의 위 경계)
│  ceiling row  (높이 = ceilingHeight)    │  ← 천장/구분줄 영역. 층 라벨 텍스트 위치.
├─────────────────────────────────────────┤  ← ceilingTopY - ceilingHeight
│                                         │
│  cube row     (높이 = cubeHeight)       │  ← 실제 모듈/외벽/엘베가 그려지는 영역
│                                         │
└─────────────────────────────────────────┘  ← ceilingTopY - stackHeight (다음 floor 위 경계)
```

floorIdx (위에서 0부터 내림차순 정렬된 index)에 따른 anchored y:
- ceiling row top y = `-floorIdx * stackHeight + currentYOffset`
- cube row top y = `-floorIdx * stackHeight - ceilingHeight + currentYOffset`
- cube row bottom y = `-(floorIdx + 1) * stackHeight + currentYOffset`

### 2.5 1F 바닥 정렬 (Origin Y)

빌딩 layout 계산 시 가장 중요한 기준:
- **1F cube row의 BOTTOM**이 cubeContainer 좌표상 절대 Y = `_originY` 위치에 오도록 yOffset 자동 계산.
- 공식:
  ```
  currentYOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight
  ```
  - `halfHeight` = cubeContainer.rect.height / 2
  - `floor1Idx` = 정렬된 floors 배열에서 FloorIndex=1인 위치 (없으면 가장 작은 양수 fallback)

이렇게 하면 stage가 어떤 floor 분포를 가지든 1F 바닥은 항상 같은 화면 Y에 위치 → 스테이지 간 시각 일관성.

### 2.6 Bottom Row (최하 지하층 아래)

`FloorIndex < 0`인 floor가 존재하면, 그 중 가장 깊은 Bn의 cube row **바로 아래**에 ceilingHeight 높이의 "Bottom row"가 추가로 그려짐.

- top y = `-(deepestIdx + 1) * stackHeight + currentYOffset`
- 각 cell type별 sprite:
  - Indoor → `_Bottom_`
  - Outdoor → `_Underwallfr_`
  - Background → 안 그림

### 2.7 Root 모듈 (Bottom Row 아래)

지하층이 있을 때만 추가로 그려지는 단일 stretch sprite.

- 위치: Bottom row 바로 아래
- 가로: 7칸 (col 1~7, 좌측 외벽~우측 외벽 포함)
- 세로: **2 × stackHeight** (= 큐브+반큐브+큐브+반큐브)
- sprite 매칭: name에 `_Root_` 포함
- 단일 이미지가 통째로 stretch — 4개 row가 sprite 안에 그려진 형태로 디자인

좌표:
- top y = `-(deepestIdx + 1) * stackHeight - ceilingHeight + currentYOffset`
- x = `1 × cubeWidth`
- width = `7 × cubeWidth`
- height = `2 × stackHeight`

---

## 3. 데이터 모델

### 3.1 CubeType (enum)

```csharp
public enum CubeType
{
    Background = 0,  // 배경 (하늘/지면 sprite 보임 — 셀에 아무 sprite 안 그림)
    Outdoor    = 1,  // 외벽
    Indoor     = 2,  // 실내 — 모듈/엘베 배치 가능
}
```

### 3.2 ElevatorPosition (enum)

엘리베이터는 Indoor 5칸 중 C, E, G(col 2, 4, 6) 세 위치만 허용.

```csharp
public enum ElevatorPosition
{
    Left,    // col 2 (C). 같은 층 모듈 = 1×4 (D-E-F-G = col 3~6)
    Center,  // col 4 (E). 같은 층 모듈 = 1×2 두 개 (C-D + F-G). 1×4 모듈 입장 불가.
    Right,   // col 6 (G). 같은 층 모듈 = 1×4 (C-D-E-F = col 2~5)
}
```

### 3.3 FloorData

```csharp
[System.Serializable]
public class FloorData
{
    public int FloorIndex;       // 음수=지하, 양수=지상, 0은 사용 안 함
    public Zone Zone;             // Underground/Aboveground/Rooftop (게임 로직용)
    public CubeType[] Cubes;     // 길이 = 9. 각 칸 타입.
}
```

기본값 (`FloorData.DefaultCubes()`):
```
[Background, Outdoor, Indoor, Indoor, Indoor, Indoor, Indoor, Outdoor, Background]
   col 0      col 1   col 2   col 3   col 4   col 5   col 6   col 7    col 8
```

### 3.4 StageData (ScriptableObject)

```csharp
public class StageData : ScriptableObject
{
    public int StageID;
    public string StageName;
    public ElevatorPosition ElevatorPosition;
    public List<FloorData> Floors;
}
```

`CreateAssetMenu`: "TopTower/Stage Data".

---

## 4. Sprite 명명 + 매핑 규칙 (CORE)

### 4.1 Addressables 라벨 시스템

| 라벨 | 폴더 | 용도 |
|---|---|---|
| `Stage_{NNN}` | `Assets/.../Image/Module/Stage_{NNN}/` | 해당 스테이지 전용 sprite |
| `StageCommon` | `Assets/.../Image/Module/StageCommon/` | 모든 스테이지 공통 (Empty 모듈 등) |

폴더명 = 라벨. Editor 도구가 자동으로 entry 등록 + 라벨 부여 (StageSpritesSyncTool 참조).

### 4.2 Sprite 명명 규칙

스테이지 전용: `{NNN}_Structural_{Type}_{ID}` (예: `001_Structural_Wall_001`)
공통: `Structural_{Type}_{ID}` (예: `Structural_Empty_005`)

`{Type}` 키워드가 sprite의 역할을 결정 — 코드가 name에 substring으로 매칭.

### 4.3 Type 키워드 표

| Type | 한글명 | 조건 / 위치 | 미러 자동 생성 |
|---|---|---|---|
| `_Empty_` | 빈 모듈 | StageCommon. Indoor 1×4 모듈 영역. ID 001~020 기본, 021~040 특수, 100~ 1×2 예약. | X |
| `_Wall_` | 외벽 | 지상층(FloorIndex ≥ 1) outdoor cube | O (우측 자동) |
| `_Wallfr_` | 천장외벽 | 지상층 outdoor ceiling row | O |
| `_Floor_` | 천장 | Indoor ceiling row (1F/2F 등 라벨 표시 영역) | X |
| `_Underwall_` | 지하외벽 | 지하층(FloorIndex < 0) outdoor cube | O |
| `_Underwallfr_` | 지하천장외벽 | 지하층 outdoor ceiling row (1F↔B1 경계 포함) + Bottom row outdoor | O |
| `_Bottom_` | 최하바닥 | 최하 Bn 아래 Bottom row indoor | X |
| `_Gate_` | 빌딩입구 | 1F outdoor (있으면 `_Wall_`보다 우선) | O |
| `_Elevator_` | 엘베 | StageData.ElevatorPosition 컬럼의 indoor cube. 모든 층. | X |
| `_Root_` | 뿌리 | Bottom row 아래 단일 stretch (7칸 × 2×stackHeight). 지하층 있을 때만. | X |

### 4.4 매칭 규칙 상세

| Floor의 cube row outdoor 셀 | Sprite |
|---|---|
| FloorIndex == 1 AND `_Gate_` 존재 | `_Gate_` (좌/우 미러) |
| FloorIndex ≥ 1 (Gate 없음) | `_Wall_` (좌/우 미러) |
| FloorIndex < 0 | `_Underwall_` (좌/우 미러) |

| Floor의 ceiling row 셀 | Indoor sprite | Outdoor sprite |
|---|---|---|
| FloorIndex ≥ 1 | `_Floor_` (없으면 진파랑 단색 fallback) | `_Wallfr_` |
| FloorIndex < 0 | `_Floor_` | `_Underwallfr_` |

| Bottom Row 셀 (최하 지하층 아래만) | Indoor | Outdoor |
|---|---|---|
|  | `_Bottom_` | `_Underwallfr_` |

| Background cube/ceiling 셀 | 항상 안 그림 (배경 sprite가 보임) |

### 4.5 1F↔B1 경계 처리

B1은 FloorIndex=-1이므로 지하층 분기를 탐. → B1의 ceiling row outdoor = `_Underwallfr_`. 1F의 바닥 = B1의 ceiling이므로 자연스럽게 지하 스타일 구분줄로 보임.

### 4.6 우측 미러 생성

코드가 좌측 sprite 1장만 받아서 우측은 런타임에 자동 생성. 방법:
- RenderTexture 임시 생성 → Graphics.Blit으로 UV X 반전 → Texture2D.ReadPixels로 추출 → 새 Sprite.Create
- `HideAndDontSave` flag 부여
- 음수 scale 방식이 prefab edit mode에서 불안정해서 채택한 방식

미러 대상: `_Wall_`, `_Wallfr_`, `_Underwall_`, `_Underwallfr_`, `_Gate_`. 미러 미대상: `_Floor_`, `_Bottom_`, `_Elevator_`, `_Empty_`, `_Root_`.

### 4.7 Sprite 권장 픽셀 크기 (2x 해상도)

| Type | 표시 크기 (1x) | 권장 PNG (2x) |
|---|---|---|
| `_Wall_`, `_Gate_`, `_Underwall_`, `_Elevator_`, `_Empty_` | 120 × 156 | **240 × 312** |
| `_Wallfr_`, `_Underwallfr_`, `_Floor_`, `_Bottom_` | 120 × 52 | **240 × 104** |
| `_Empty_` (1×4) | 4 × 120 × 156 = 480 × 156 | **960 × 312** |
| `_Root_` | 7 × 120 × 2 × 208 ≈ 840 × 416 | **1680 × 832** |

---

## 5. 배경 시스템

### 5.1 BackgroundGroup 구조 (Hierarchy)

```
SafeArea (or RootStage)
├── BackgroundGroup           ← 빌딩과 한 몸으로 이동/줌
│   ├── Background_Sky        (anchor 0.5, 0.5; pivot 0.5, 0)
│   ├── Background_Main       (anchor 0.5, 0.5; pivot 0.5, 0.5)
│   └── Background_Underground (anchor 0.5, 0.5; pivot 0.5, 1)
├── BuildingContainer         ← 빌딩 sprite들의 부모 = cubeContainer
└── ...
```

### 5.2 Main 정렬

`Background_Main` bottom edge = 빌딩 1F cube bottom (= Origin Y 라인).
구현: `BackgroundView.SetOriginY(originY + mainHalfHeight)` — Main pivot center이므로 bottom = anchoredY - halfHeight = originY.

### 5.3 Sky/Underground 자동 스냅

`BuildingView.RenderBuilding()` 끝에 `BackgroundView.AdjustHeights(totalBuildingHeight)` 호출:
1. Sky/Under sizeDelta.y = `max(buildingHeight × heightMargin, minHeight)` — 빌딩보다 더 길게
2. Sky.anchoredPosition.y = Main top edge (pivot bottom이라 그 자리가 sky 바닥)
3. Under.anchoredPosition.y = Main bottom edge (pivot top이라 그 자리가 under 윗면)

기본값:
- `_heightMargin` = 1.5
- `_minHeight` = 5000

### 5.4 배경 sprite 로드

라벨 `Background` (별도 폴더) 사용. sprite name 매칭:
- `{NNN}_Background_Main_{ID}`
- `{NNN}_Background_Sky_{ID}`
- `{NNN}_Background_Under_{ID}`

stage 번호 + 종류 일치하는 첫 sprite를 각 Image에 할당.

---

## 6. 좌표/렌더링 시스템

### 6.1 cubeContainer

- 부모: SafeArea/Stage 루트 안의 한 RT
- anchor (0, 0) ~ (1, 1) stretch, sizeDelta (0, 0) → 부모 채움
- pivot (0.5, 0.5) — 중앙 pivot이라 localScale로 줌 시 중앙 기준 확장

### 6.2 Shift Y (스크롤)

- `cubeContainer.anchoredPosition.y = shift` → 빌딩+모든 sprite가 한 몸으로 이동
- 동시에 `BackgroundView.SetShiftY(shift)` 호출 → BackgroundGroup도 같이 이동 (한 몸)

### 6.3 Origin Y vs Home Y

| | 의미 | 영향 |
|---|---|---|
| **Origin Y** | 1F 바닥의 cubeContainer 좌표 절대 Y. 빌딩 layout 렌더 전용. | RenderBuilding 시 cube들이 이 라인 기준으로 쌓임. |
| **Home Y** | HomeButton 누르면 빌딩이 시프트되어 도착할 위치. **OriginY와 무관.** | MoveToHomeY()는 단순히 `SetShiftY(_homeY)` 호출. |

HomeButton 동작:
```csharp
SetShiftY(_homeY);  // OriginY 안 끼움 — _homeY 자체가 시프트 목표값
```

### 6.4 Zoom

- `cubeContainer.localScale = (zoom, zoom, 1)` + `BackgroundGroup.localScale = (zoom, zoom, 1)`
- 화면 중앙 기준 확대/축소 (각 RT의 pivot 0.5, 0.5)
- 한계: `_zoomMin` ~ `_zoomMax` (TowerOrigin에서 조정)

---

## 7. 입력 시스템

### 7.1 입력 매핑

| 입력 | 동작 | 비고 |
|---|---|---|
| 마우스 드래그 / 1손가락 스와이프 | 빌딩 세로 스크롤 | 가로 무시. 한계 시 고무줄. |
| 마우스 휠 | 줌 인/아웃 (화면 중앙) | 1 노치당 ±10% (`_wheelZoomStep`) |
| 두 손가락 핀치 | 줌 인/아웃 (모바일) | 두 손가락 거리 비율 |

### 7.2 고무줄 (Rubber-band)

드래그가 한계 초과 시:
```
over = newShift - maxShift  (또는 minShift - newShift)
clampedShift = limit ± over × _rubberBandResistance  // 기본 0.3
```

손 떼면 (`OnEndDrag`) 한계 안으로 ease-out cubic 보간으로 복귀 (`_reboundDuration` = 0.25s).

### 7.3 드래그 한계 (CalculateLimits)

cubeContainer 좌표계 + zoom 반영:
```
scaledOriginY    = originY × zoom
scaledTotalHeight = totalBuildingHeight × zoom
halfViewport     = viewport.rect.height / 2

maxShift = halfViewport - scaledOriginY              // 1F 바닥이 viewport top까지
minShift = halfViewport - scaledOriginY - scaledTotalHeight   // 빌딩 top이 viewport bottom까지
```

빌딩 전체가 viewport보다 작으면 (maxShift < minShift) → 중간점 고정.

### 7.4 핀치 충돌 회피

- `Input.touchCount == 2`이면 드래그(IDragHandler) early return
- 휠도 드래그 중엔 무시
- 핀치 종료 시 `_previousPinchDistance = 0`으로 reset

### 7.5 HomeButton

- Stage prefab 내부의 Button 컴포넌트
- `OnClick` → 코드에서 `_homeButton.onClick.AddListener(MoveToHomeY)` 등록 (Inspector에 Button 드래그)
- MoveToHomeY: 현재 ShiftY → `_homeY`로 ease-out cubic 보간 (0.3s)

---

## 8. 층 라벨

### 8.1 위치/크기

| 항목 | 값 |
|---|---|
| 좌측 시작 col | 2 (Indoor 왼쪽 끝) |
| 폭 | 2칸 (`labelWidthInCubes = 2f`) |
| 높이 | ceilingHeight |
| anchor / pivot | (0, 1) / (0, 1) — 좌상단 |
| 부모 | cubeContainer |
| 위치 | `(2 * cubeWidth, ceilingTopY)` — 각 floor의 ceiling row 좌측 |

### 8.2 텍스트 형식

`"▼ {N}F"` (지상층) 또는 `"▼ B{N}"` (지하층). prefix `"▼ "` 고정.

### 8.3 폰트/외곽선 — TMP SDF

Inspector 노출 6개:
- `Font` (TMP_FontAsset) — null이면 `TMP_Settings.defaultFontAsset` fallback
- `Color`
- `Font Size` (TMP 단위)
- `Use Stroke` (bool)
- `Stroke Color`
- `Stroke Thickness` Range(0, 0.2) default 0.1

외곽선 구현 (외부 기준 stroke):
```
mat.SetColor(ShaderUtilities.ID_OutlineColor, _labelStrokeColor);
mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
mat.SetFloat(ShaderUtilities.ID_FaceDilate, width);  // 같은 양 dilate해서 외곽선이 글자 안쪽 침범 X
text.UpdateMeshPadding();
```

내부 고정 상수:
- `alignment = TextAlignmentOptions.MidlineLeft`
- `fontStyle = Bold`
- `enableAutoSizing = false`

### 8.4 받침박스 (Backdrop)

라벨 텍스트 뒤에 그려지는 Image. Inspector 노출 4개:
- `Use Backdrop` (bool)
- `Backdrop Color` (알파 포함)
- `Backdrop Size` (Vector2, 큐브 단위 배수) — 기본 (2, 1) = 라벨 rect 동일
- `Backdrop Offset` (Vector2, 픽셀)

구현 포인트:
- pivot (0.5, 0.5) — 중심 pivot이라 size 변경 시 대칭 확장
- **X 중심은 라벨 rect 중심이 아닌 실제 텍스트 중심** (`labelX + GetPreferredValues(text).x * 0.5`)
- Y 중심 = 라벨 rect 세로 중심
- sibling order: 라벨보다 먼저 (renders behind text)

⚠ **금지**: 라벨 생성 직후 `ForceMeshUpdate()` 호출 — Canvas 초기화 전이라 NRE 위험. `GetPreferredValues(string)`로 폭만 계산.

---

## 9. 가이드 라인 (Edit Mode 전용)

디자이너가 빌딩 위치 잡을 때 보조선. **Play 모드에선 항상 숨김.**

| 색 | 의미 | 부모 | 동작 |
|---|---|---|---|
| 핑크 (RGB 1, 0.4, 0.8) | Origin Y (1F 바닥 라인) | cubeContainer | 빌딩과 같이 이동 |
| 연두 (RGB 0.6, 1, 0.4) | Home Y (HomeButton 도착점) | **cubeContainer의 부모** | 월드 고정 |

TowerOrigin 컴포넌트의 `Show Guide Lines` 체크박스로 토글.

토글 시 라인 GameObject 캐시 재사용 (재생성 X) → 빌딩 sprite(특히 random Empty 모듈) 영향 없음.

---

## 10. 컴포넌트 / 씬 구조

### 10.1 IngameScene

```
IngameScene
├── TopTower (GameObject)
│   └── TowerOrigin (컴포넌트)
└── (Addressables로 Stage_001 prefab 동적 로드)
```

### 10.2 Stage_001 Prefab (vanilla 재구성 가이드)

```
Stage_001 (RectTransform)              ← Canvas + GraphicRaycaster + CanvasScaler + BuildingView + BackgroundView
├── Canvas Scaler (ScaleWithScreenSize, ref 1080×1920, match 0=가로 우선)
├── (Canvas: Overlay)
├── (GraphicRaycaster — 이거 없으면 클릭/드래그 안 됨!)
└── SafeArea (RT, 화면 채움)
    ├── BackgroundGroup
    │   ├── Background_Sky        (pivot 0.5, 0)
    │   ├── Background_Main       (pivot 0.5, 0.5)
    │   └── Background_Underground (pivot 0.5, 1)
    ├── BuildingContainer (= cubeContainer, pivot 0.5, 0.5, stretch anchor)
    └── BuildingSkill
        └── Home
            └── HomeButton (Button)
```

`Stage_001` 루트에 두 컴포넌트:
- **BuildingView**: stageData, cubeContainer, backgroundView, homeButton 참조 필드
- **BackgroundView**: stageData, mainImage, skyImage, undergroundImage 참조 필드

SafeArea에 별도 컴포넌트:
- **BuildingDragController** (RequireComponent Image — 투명 raycast용)
- 같은 GameObject에 투명 Image (raycastTarget=true) + CanvasRenderer

### 10.3 TowerOrigin Inspector 필드

```
고정점 Y축
  Origin Y Range(-960, 960) default -400
  Home Y Range(-960, 960) default 0
  Show Guide Lines bool default true (출시 시 끄기)

줌 한계
  Zoom Min Range(0.1, 1) default 0.5
  Zoom Max Range(1, 5) default 2
```

`[ExecuteAlways]` + `OnEnable` + `OnValidate(EditorApplication.delayCall)`로 BuildingView/BackgroundView에 값 push.

### 10.4 BuildingView 주요 메서드

```
public void SetStageData(StageData)
public void SetOriginY(float)                    // Mathf.Approximately로 변경 감지 — 동일값이면 RenderBuilding 안 함
public void SetHomeY(float)
public void SetShowOriginGuideLine(bool)
public void SetShowHomeGuideLine(bool)
public void SetZoomLimits(float min, float max)
public void SetZoom(float)                       // 한계 clamp
public void SetShiftY(float)                     // cubeContainer.anchoredPosition.y + BackgroundView.SetShiftY
public void MoveToHomeY()                        // SetShiftY(_homeY) — 애니메이션
public float ShiftY { get; }                     // cubeContainer.anchoredPosition.y
public float OriginY, HomeY, CurrentZoom, ZoomMin, ZoomMax
public float GetTotalBuildingHeight()            // Floors.Count × stackHeight
public void RenderBuilding()                     // 전체 재생성. 호출 시점:
                                                 //   - Start, SetStageData, SetOriginY(값 변경)
```

### 10.5 BuildingView Inspector (전체 필드)

```
데이터
  Stage Data: StageData (ScriptableObject)

참조 (비워두면 자동 탐색)
  Cube Container: RectTransform
  Background View: BackgroundView
  Home Button: Button

빌딩 크기 설정
  Grid Width: 9 (고정 권장)
  Cube Aspect Ratio: Range(0.5, 3.0) default 1.3
  Ceiling Height Ratio: Range(0.0, 1.0) default 0.333

층 라벨
  Font: TMP_FontAsset
  Color: Color
  Font Size: float default 32
  Use Stroke: bool
  Stroke Color: Color
  Stroke Thickness: Range(0, 0.2) default 0.1

층 라벨 - 받침박스
  Use Backdrop: bool
  Backdrop Color: Color (알파 포함)
  Backdrop Size: Vector2 default (2, 1)
  Backdrop Offset: Vector2 default (0, 0)
```

---

## 11. Addressables 운영

### 11.1 폴더 구조 → 라벨

```
Assets/Application/TopTower/Image/Module/Stage_001/    ← 라벨: Stage_001
  001_Structural_Wall_001.png
  001_Structural_Wallfr_001.png
  001_Structural_Gate_001.png
  001_Structural_Elevator_001.png
  001_Structural_Underwall_001.png
  001_Structural_Underwallfr_001.png
  001_Structural_Bottom_001.png
  001_Structural_Root_001.png
  001_Structural_Floor_001.png  (선택 — 없으면 진파랑 fallback)

Assets/Application/TopTower/Image/Module/StageCommon/  ← 라벨: StageCommon
  Structural_Empty_001.png
  Structural_Empty_002.png
  ...
```

### 11.2 자동 등록 도구 (StageSpritesSyncTool)

메뉴: `Tools > Top Tower > Sync Addressables`. 동작:
1. `Assets/Application/TopTower/` 전체 재귀 스캔
2. 각 asset을 Default Group에 entry 등록 (Address = full path)
3. 직속 폴더명을 단일 라벨로 부여 (옛 라벨 모두 제거)

신규 sprite 추가 시 이 메뉴 한 번 클릭으로 등록 + 라벨링 완료.

vanilla로 옮길 때 이 에디터 스크립트는 그대로 가져가도 동작 (UnityEditor만 의존).

---

## 12. 마이그레이션 체크리스트 (vanilla 2D URP)

### 12.1 새 프로젝트 패키지

- Unity 6.x or 2022 LTS (현 프로젝트 버전 맞추기)
- `com.unity.textmeshpro` (vanilla)
- `com.unity.addressables`
- VContainer (선택, 우리 게임 코드는 미사용)
- UniTask (필수, 우리 코드 사용)
- UniRx (선택)

### 12.2 코드 복사

`.meta` 파일까지 같이 복사 (GUID 보존).

- `Scripts/Building/BuildingView.cs` (vanilla 옮긴 후 **`EnsureTextAnimatorFieldsInitialized` 관련 코드 모두 제거** — vanilla TMP라 불필요)
- `Scripts/Building/BackgroundView.cs`
- `Scripts/Building/BuildingDragController.cs`
- `Scripts/Ingame/TowerOrigin.cs`
- `Scripts/Data/StageData.cs`
- `Editor/StageSpritesSyncTool.cs`

### 12.3 자산 복사

- `Image/Module/Stage_001/` 전체 (PNG + meta)
- `Image/Module/StageCommon/` 전체
- `LevelData/Stage_001.asset` + meta
- `Docs/` 전체 (본 문서 포함)
- `CLAUDE.md`

### 12.4 새로 만들 것

- **IngameScene** — 빈 씬 + EventSystem + (필요 시 ProjectLifetimeScope)
- **TopTowerIngame.cs** — vanilla 버전. 예시 골격:
  ```csharp
  public class TopTowerIngame : MonoBehaviour {
      [SerializeField] string _stageAddress = "...Stage_001.prefab";
      async UniTaskVoid Start() {
          var handle = Addressables.LoadAssetAsync<GameObject>(_stageAddress);
          await handle.Task;
          if (handle.Status == AsyncOperationStatus.Succeeded)
              Instantiate(handle.Result);
      }
  }
  ```
- **Stage_001.prefab** — 본 문서 10.2 구조대로 vanilla로 재구성. Canvas + GraphicRaycaster 필수.
- **panel_Ingame.prefab** — 새 기획에 맞춰 처음부터.

### 12.5 안 가져가는 것

- `Assets/RootBox*/` 전부
- `Assets/RootBoxResource/`
- `Assets/Application/Scripts/UI/` (회사 UIInjected... 시스템)
- `Assets/Resources/__RootCanvas__.prefab`
- `Packages/manifest.json`의 회사 패키지들 (`com.doubleugames.*` 등)
- 회사 커스텀 TMP fork → vanilla TMP로 교체

### 12.6 이식 후 코드 정리 (vanilla에서)

`BuildingView.cs`에서 회사 fork 우회 코드 제거:
- `s_tmpDefaultAppearancesTagsField` 등 static 필드
- `EnsureTextAnimatorFieldsInitialized()` 메서드
- `AssignEmptyIfNull()` 메서드
- `ApplyLabelProperties` 첫 줄의 `EnsureTextAnimatorFieldsInitialized(text)` 호출

→ 한 30줄 정도 제거. vanilla TMP는 SerializeField 빈 배열로 정상 동작.

---

## 13. 검증 순서 (vanilla 이식 후)

1. **단독 BuildingView 테스트**: 빈 씬에 Canvas+SafeArea+BuildingContainer 직접 만들고 BuildingView 부착 → Stage_001 ScriptableObject 연결 → Play로 큐브들이 렌더되는지 확인
2. **Sprite 로드 확인**: Wall/Floor 등 sprite가 자리에 배치되는지. 안 보이면 Addressables Sync 실행.
3. **BackgroundView 연결**: Main이 1F 바닥에 붙는지
4. **TowerOrigin 연동**: Origin Y / Home Y 슬라이더 변경 시 빌딩 위치 즉시 반영
5. **드래그**: Stage 루트에 GraphicRaycaster 있는지 확인 → 세로 드래그 동작
6. **줌**: 휠/핀치 → 빌딩+배경 한 몸 확대 축소, 한계 clamp
7. **HomeButton**: OnClick에 직접 연결 OR Inspector Button 필드 연결 → 빌딩이 Home Y로 이동
8. **층 라벨 + 받침박스**: TMP 폰트 연결 시 정상 렌더, 외곽선/박스 적용

---

## 14. 알려진 hack / 주의사항 (현 프로젝트만)

다음은 회사 fork 환경 때문에 들어간 우회 코드. **vanilla 이식 시 모두 제거.**

| 위치 | 코드 | 이유 | vanilla에서 |
|---|---|---|---|
| `BuildingView.ApplyLabelProperties` 시작부 | `EnsureTextAnimatorFieldsInitialized(text)` | 회사 fork TMP가 `defaultAppearancesTags` 등 SerializeField를 추가, 런타임 생성 시 null이라 GenerateTextMesh NRE | 제거 |
| `BuildingView` 후반부 | 리플렉션 FieldInfo 캐시 + 헬퍼 메서드 | 위와 동일 | 제거 |
| (없는 게 정상) | EventSystem 중복 경고 | IngameScene + `__RootCanvas__` 양쪽에 EventSystem | vanilla는 IngameScene EventSystem 1개만 |

---

## 부록 A: 수치 파라미터 모음

| 파라미터 | 위치 | 기본값 | 비고 |
|---|---|---|---|
| `_gridWidth` | BuildingView | 9 | 고정 권장 |
| `_cubeAspectRatio` | BuildingView | 1.3 | 큐브 세로 = 가로 × 이 값 |
| `_ceilingHeightRatio` | BuildingView | 0.333 | 구분줄 높이 = 큐브 세로 × 이 값 |
| `_heightMargin` | BackgroundView | 1.5 | Sky/Under 영역 빌딩 높이 × 이 값 |
| `_minHeight` | BackgroundView | 5000 | Sky/Under 최소 높이 (px) |
| `_originY` | TowerOrigin | -400 | 1F 바닥 Y (cubeContainer 좌표) |
| `_homeY` | TowerOrigin | 0 | HomeButton 도착 Y |
| `_zoomMin` | TowerOrigin | 0.5 | 최소 zoom |
| `_zoomMax` | TowerOrigin | 2.0 | 최대 zoom |
| `_wheelZoomStep` | BuildingDragController | 0.1 | 휠 1 노치당 ±10% |
| `_pinchZoomSensitivity` | BuildingDragController | 1.0 | 핀치 거리 비율 직접 |
| `_rubberBandResistance` | BuildingDragController | 0.3 | 한계 초과 시 감쇠율 |
| `_reboundDuration` | BuildingDragController | 0.25 | 복귀 애니 시간 (초) |
| `HomeMoveDuration` | BuildingView (const) | 0.3 | HomeButton 이동 애니 시간 |
| `EmptyBasicIdMin/Max` | BuildingView (const) | 1 / 20 | 기본 Empty 모듈 ID 범위 |

---

## 부록 B: Floors 권장 정렬

`StageData.Floors`는 입력 순서 자유. 코드는 사용 직전 `OrderByDescending(f => f.FloorIndex)` 적용 → 위에서 아래 순서 = floorIdx 0이 최상층.

---

## 부록 C: Sprite name 끝 ID 파싱

`Structural_Empty_005` → 5. 코드: `name.LastIndexOf('_') + 1` 뒤를 `int.TryParse`. ID 범위 필터로 sprite 분류 (기본/특수/1×2 등).

---

이 문서가 vanilla 이식의 기준 사양서. 큐브 구조 관련 모든 수치/공식/매칭 규칙은 본 문서를 정본으로 봐도 됨. 새 프로젝트의 코드는 본 문서를 보고 검증 가능.
