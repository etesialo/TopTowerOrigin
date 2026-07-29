---
name: 게임 컨셉 (Top Tower)
description: Top Tower 게임의 코어 컨셉 - 타워 방치형, 9:16 모바일 세로, 단면도 비주얼, 그리드 기반 모듈 배치. 핵심 용어 = Cube/Module/Group.
type: project
originSessionId: 93ff8962-d236-4105-9ce1-30c9289c2e19
---

## 핵심 용어 (Glossary) — 코드/대화 모두 이 용어로 통일
- **Cube (큐브)**: 빌딩 그리드의 1×1 한 칸. 가장 작은 단위.
- **Indoor (실내)**: 건물 안쪽 Cube. 일반 모듈 배치 영역.
- **Outdoor (실외)**: 건물 바깥쪽 Cube. **좌/우 외벽 + 옥상**이 모두 Outdoor에 속함. 일반 모듈 배치 불가, 특수 속성 모듈만 점유.
- **Module (모듈)**: 개별 방 한 채. (예: "초밥집")
- **Group (그룹)**: 모듈의 분류축 #1 — "이 모듈이 무슨 종류인가". 현재 6가지 = 구조(Structural)/시설(Facility)/식당(Restaurant)/상업(Commercial)/사무(Office)/주거(Residential). 추가 가능.
  - **Structural**: 시스템이 자동 배치하는 빈 모듈, 외벽, 공사/옥상 모듈 등 건물 구조의 기본 요소
  - **Facility**: 건물 운영 시설 (청소방, 화장실 등) — 임대용 아님
  - 나머지 4종 (Restaurant/Commercial/Office/Residential): 임대 가능 업종 모듈
- **Zone (존)**: 모듈의 분류축 #2 — "이 모듈을 어디에 놓을 수 있는가". 현재 3가지 = Underground(지하존)/Aboveground(지상존)/Rooftop(옥상존). 추가 가능.
- **Terrace (테라스)**: 모듈에 붙는 **속성**. 인접 좌/우 외벽 Outdoor Cube 1개를 추가 점유. (외벽으로 튀어나온 발코니 표현)
- **Penthouse (펜트하우스)**: 모듈에 붙는 **속성**. 옥상 Outdoor Cube를 추가 점유.
- **Tenant (세입자)**: 모듈을 임대해서 자동 월세 납부하는 액터.
- **Visitor (방문객)**: 모듈을 방문해서 모듈 안에 골드를 떨구는 액터.
- **Stage (스테이지)**: 게임 진행 단위 = 한 빌딩 + 클리어 조건. **데이터/코드/prefab 이름에 사용** (예: Stage_001.prefab, StageData, IStageContent).
- **Level (레벨)**: UI 표시 텍스트로만 사용 (예: "Level.1 Normal"). 내부적으론 Stage와 동일 개념. 코드/데이터엔 사용하지 않음.

→ 코드의 클래스/필드 이름도 이 용어 그대로 사용 (`Cube`, `Indoor`, `Outdoor`, `Module`, `Group`, `Zone`, `Terrace`, `Penthouse`, `Tenant`, `Visitor` 등).

## 클래스 계층
- **Group**과 **Zone**은 동격 — 둘 다 Module을 분류하는 독립된 축. 둘 다 데이터 주도형(ScriptableObject 등) 권장. enum 하드코딩 금지.
- **Module**:
  - `group: Group` — 정확히 1개 (필수)
  - `allowedZones: Zone[]` — 1개 이상 (어디 놓을 수 있는지)
  - 그 외: `size (m×n)`, `levels[]`, `outdoorAttachment`, `image` 등
- **Cube**:
  - `zone: Zone` — 자기가 속한 영역
  - `kind: Indoor | Outdoor` — 실내/실외 속성
  - `occupiedBy: Module?` — 점유 중인 모듈 (앵커 셀이거나 앵커 가리키는 참조)

## 장르 / 레퍼런스
- Sim Tower / Tiny Tower / Project Highrise 계열의 **타워 방치형** 게임
- 모바일 세로 화면 (**9:16**)
- 진행 단위: **스테이지 형식** (한 스테이지가 한 빌딩)

## 씬/로딩 구조
- 빈 씬(시스템 설정 오브젝트만 있는 베이스 씬)에서 시작
- 스테이지 프리팹을 호출/로드해서 스테이지를 띄움
- → Addressables 기반 동적 로드에 적합

## 비주얼
- 평면 단면도 스타일 (원근감 없음)
- 건물 내부 구획이 그대로 보이는 도면 형식

## 폴더 구조 + 라벨 시스템 (2026-05-12 정리)

### 폴더 구조
```
Assets/Application/TopTower/Image/
├─ Background/                                        [라벨: Background]
│   └─ Stage_{NNN}_Background_{Type}_{ID}.png
│       Type ∈ {Main, Sky, Under}
└─ Module/
    ├─ StageCommon/                                   [라벨: StageCommon]
    │   └─ {Group}_{Type}_{ID}.png
    │       예: Structural_Empty_001
    └─ Stage_{NNN}/                                   [라벨: Stage_{NNN}]
        └─ Stage_{NNN}_{Type}_{ID}.png
            예: Stage_001_Wall_001
```

### 명명 규칙 (2026-05-12 갱신)
- **공통 sprite** (StageCommon 폴더): `{Group}_{Type}_{ID}` — stage 무관
  - 예: `Structural_Empty_001`
- **Stage 전용 sprite** (Stage_NNN 폴더): `{NNN}_{Group}_{Type}_{ID}` — stage prefix + 그룹 포함 (NNN 뒤 underscore)
  - 예: `001_Structural_Wall_001`, `001_Structural_Elevator_001`, `001_Structural_Wallfr_001`, `001_Structural_Fr_001`
- **Background**: `{NNN}_Background_{Main|Sky|Under}_{ID}` — Group 없이 stage prefix + Type
  - 예: `001_Background_Main_001`, `001_Background_Sky_001`, `001_Background_Under_001`

**코드 매칭 방식**:
- BuildingView 모듈/벽/엘베/구분줄: type 부분(`_Wall_`, `_Elevator_`, `_Wallfr_`, `_Fr_`)만 `Contains`로 매칭 → prefix 변경에 자동 호환
- BackgroundView: stage 매칭을 위해 `{NNN}_Background_{Type}_` prefix `StartsWith`로 매칭 → stage 단위 분기

### Addressables 라벨 규칙
- Sync 도구가 **직속 부모 폴더명을 라벨로 자동 부여**
- 한 stage에 들어가면 두 라벨 일괄 로드:
  - `StageCommon` — 공통 모듈 (Empty 등)
  - `Stage_{NNN}` — 그 stage 전용 (Wall, 추후 모듈 등)
  - `Background` — 배경 (코드가 stage name prefix로 필터)
- 코드는 sprite name으로 종류/stage 구분

### Sync 도구
- 메뉴: `Tools > Top Tower > Sync Addressables` (`Assets/Application/TopTower/Editor/StageSpritesSyncTool.cs`)
- 폴더 재귀 스캔 → 미등록 asset Default Group에 추가 + 폴더명 라벨 부여
- **Address는 풀 경로 그대로** (Addressables 기본값). 사용자 단축 entry는 보존.
- 이미 등록된 entry는 위치/Address 보존, 라벨만 누적 추가. 재실행 안전.
- 제외: `Editor/` 폴더, `.cs/.asmdef/.meta` 파일.

## 빈 모듈(EmptyModule) sprite 사양
세입자 입주 전 Indoor 영역을 자동으로 채우는 디폴트 sprite.
- **위치/명명**: `Image/Module/StageCommon/Structural_Empty_{ID:D3}.png` — stage 공통
- **ID 분류 (기획)**:
  - **001~020**: 기본 1×4 모듈 (엘베 C/G용)
  - **021~040**: 특수 1×4 모듈 (용도 미정, 현재 코드는 미사용)
  - **100~**: 1×2 모듈 (엘베 E용, 추후 지원)
- **비율**:
  - 1×4: 가로 4 : 세로 1.3 (480×156 px 또는 960×312 px)
  - 1×2: 가로 2 : 세로 1.3 (240×156 px 또는 480×312 px)
- **Addressables 라벨**: `StageCommon` (폴더명 그대로)
- **로드/배치**: `BuildingView.LoadAndPlaceEmptyModulesAsync` — `StageCommon` 라벨 일괄 로드 → `Structural_Empty_` prefix 필터 → ID 001~020 범위 필터 → 각 층 1×4 영역에 랜덤 1장 배치
- 라벨/sprite 미등록 시 silent skip → Indoor cube 흰색 단색 유지

## 외벽(Wall) sprite 사양
- **위치/명명**: `Image/Module/Stage_{NNN}/{NNN}_Structural_Wall_{ID:D3}.png` — stage 전용 (예: `001_Structural_Wall_001`)
- **비율**: 1×1 cube = 가로 1 : 세로 1.3 (120×156 px 또는 240×312 px)
- **사용자 작업량**: **좌측 1장만 디자인** — 우측은 코드가 자동 미러 생성
- **Addressables 라벨**: `Stage_{NNN}` (폴더명 그대로)
- **로드/배치**: `BuildingView.LoadAndPlaceWallsAsync`
  - `Stage_{NNN}` 라벨 일괄 로드 → name에 `_Wall_` 포함하는 첫 sprite를 좌측 sprite로 사용
  - **우측 sprite는 `Sprite.Create`로 UV 반전된 미러 sprite 런타임 생성**
    - 부모 RectTransform의 localScale은 (1,1,1) 그대로 → 음수 scale 회피 → RectMask2D 충돌 회피 (이전 시도 실패 원인)
  - 각 Outdoor cube의 col이 그리드 중앙보다 우측이면 미러 sprite, 좌측이면 원본 sprite 적용

## 구분줄(Ceiling / 천장) sprite 사양
층 사이에 별도 row로 자기 영역을 점유. cube 위에 덮는 게 아니라 cube 행과 cube 행 사이에 새 row 삽입.
```
[옥상 cube row    높이 156]
[구분줄 row       높이 52]   ← 별도 점유
[2층 cube row     높이 156]
[구분줄 row       높이 52]
[1층 cube row     높이 156]
```
- **세로 길이**: `cubeHeight × _ceilingHeightRatio` (기본 1/3) — 1080 기준 ≈ 52 px (2배수 104 px)
- **위치/명명** (사용자 결정 — 짧은 약자):
  - `Image/Module/Stage_{NNN}/{NNN}_Structural_Fr_{ID:D3}.png` — Indoor cube 위 (Fr = floor divider)
  - `Image/Module/Stage_{NNN}/{NNN}_Structural_Wallfr_{ID:D3}.png` — Outdoor(외벽) cube 위 (Wallfr = wall floor divider)
  - 예: `001_Structural_Fr_001`, `001_Structural_Wallfr_001`
- **권장 sprite 크기**: 240 × 104 px (2배수)
- **Addressables 라벨**: `Stage_{NNN}` (외벽/엘베와 공유)
- **배치 규칙**:
  - 옥상(최상층) 위는 안 그림
  - Background cube 위는 **항상 투명** (안 그림)
  - Indoor cube 위: `_Fr_` sprite 있으면 sprite, **없으면 단색 진파랑 fallback** (코드 상수 `CeilingFallbackColor`)
  - Outdoor cube 위: `_Wallfr_` sprite 있으면 sprite, 없으면 안 그림
  - 매칭 시 `_Wallfr_`가 `_Fr_`를 포함하지 않도록 분리 매칭
- **좌표 계산**: 한 층 점유 = `stackHeight = cubeHeight + ceilingHeight`. floorIdx N의 cube 위치 y = `-N × stackHeight`. 그 사이 ceiling row가 자연스럽게 끼어듦.
- **로드/배치**: `BuildingView.LoadAndPlaceCeilingsAsync`

## 엘리베이터(Elevator) sprite 사양
- **위치/명명**: `Image/Module/Stage_{NNN}/{NNN}_Structural_Elevator_{ID:D3}.png` — stage 전용 (예: `001_Structural_Elevator_001`)
- **비율**: 1×1 cube = 가로 1 : 세로 1.3 (120×156 px 또는 240×312 px)
- **Addressables 라벨**: `Stage_{NNN}` (외벽과 공유)
- **배치**: 모든 floor의 `StageData.ElevatorPosition` col에 자동 배치
  - Left → col 2 (C), Center → col 4 (E), Right → col 6 (G)
- **로드/배치**: `BuildingView.LoadAndPlaceElevatorAsync`
  - 라벨 로드 후 name에 `_Elevator_` 포함하는 첫 sprite 사용
  - 엘베 col이 Indoor인 floor에만 배치 (예외 cube 회피)

## 배경 sprite 사양
- **위치**: `Assets/Application/TopTower/Image/Background/`
- **네이밍**: `Background_{종류}_{StageID:D3}.png` (예: `Background_main_001`, `Background_sky_001`, `Background_under_001`)
- **종류 3개**:
  - **main**: 1:1 정사각형. 1920×1920 권장 (SafeArea 풀스크린 + 1:1 비율 대응). Image Type = Simple + Preserve Aspect.
  - **sky**: 1:1 비율 sprite. **1080×1080 권장** (RectTransform 가로 1080과 정확히 일치 시 Tiled 깔끔). Image Type = Tiled, 세로로 무한 반복.
  - **under**: sky와 동일 사양.
- **Texture import 필수 설정**: Wrap Mode = **Repeat**, Mesh Type = Full Rect, Texture Type = Sprite (2D and UI)
- **로드 방식**: Addressables. Address 형식 = `Background_main_001` 같이 파일명 그대로 (확장자/경로 없이). BackgroundView.cs가 StageID로 자동 로드.

## 화면 레이아웃 (9:16 모바일 세로)
사용자가 임시 이미지로 확정한 화면 구성. 시작 메인 화면 기준.

### 시각 레이어 (위에서 아래)
1. **Background (하늘)**: 빌딩 외부 양옆/위. 보통 하늘색.
2. **Building (빌딩)**: 화면 중앙. Floor들이 세로로 쌓여 있음.
3. **Ground (지면)**: 화면 하단. 갈색. 지상/지하 경계.

### 빌딩 한 층의 구성 (가로 방향)
```
[Background] [OuterWall(좌)] [Indoor 5 Cube] [OuterWall(우)] [Background]
                ↑ 1 Cube 폭     ↑ 4개 InnerWall로  ↑ 1 Cube 폭
                                   구분된 5칸
```

### 빌딩 한 층의 구성 (세로 방향)
```
─── FloorDivider (진파란 가로줄) ───
   한 층의 Cube들 (Indoor + Outdoor)
─── FloorDivider ───
```

### 시각 구분 요소 명명
- **OuterWall (외벽)**: 좌/우 Outdoor Cube의 시각 표현 (검은색). 1 Cube 폭.
- **InnerWall (내벽)**: **별도 시스템 폐기**. 각 모듈 sprite가 자기 좌/우 경계 벽까지 직접 그림.
- **FloorDivider (층 구분줄, 천장)**: Floor 사이 가로 줄 (진파랑). 한 층의 천장 (= 위층의 바닥) 역할. 텍스트/아이콘 들어갈 두께. **InnerWall과 별개의 시스템**으로 유지.

### InnerWall 처리 방식 (단순화 결정)
별도 sprite/계산 시스템 없음. 모듈 sprite가 자체적으로 좌/우 벽 포함하도록 디자인.
- **다칸 모듈** (1×2 초밥집 등): sprite가 통째로 영역 차지 → 내부 벽 자동 X
- **다른 모듈 인접**: 두 sprite의 경계 벽이 겹쳐서 한 줄로 보임
- 데이터 계산 없이 sprite만으로 시각 처리 완료

### 데이터 모델 영향
- OuterWall = Outdoor Cube의 sprite로 통합 가능 (기존 WallModule이 그 역할)
- **InnerWall = 모듈 sprite의 일부**. 별도 갱신 로직 불필요.
- FloorDivider = Floor 단위로 위/아래 보더로 처리 (각 Floor 시각의 일부)

## 한 층(Floor)의 구조 및 빌딩 확장 메커니즘
한 Floor = **Cube들 + 그 위의 천장(FloorDivider)** 한 묶음.

### "한 층 추가" 흐름
1. 기존 빌딩의 위쪽 끝은 항상 `[기존 마지막 천장] → [옥상존(공사모듈)]` 구조
2. 한 층 추가 시: 기존 천장 위와 공사모듈 사이에 **새 층(Cube들 + 새 천장)** 1개 삽입
3. 공사모듈은 새 천장 위로 자동 이동 → **옥상존이 항상 빌딩 최상단을 점유**한다는 불변식 유지

```
이전:                          한 층 추가 후:
[공사모듈 옥상존]               [공사모듈 옥상존]    ← 위로 이동
                                [새 천장]            ← 추가
                                [새 층 큐브들]       ← 추가
[기존 마지막 천장]              [기존 마지막 천장]
[기존 마지막 층 큐브]           [기존 마지막 층 큐브]
...                             ...
```

### 데이터 모델 영향
- **Floor**가 자기 Cube 배열 + 자기 천장(FloorDivider) 정보 보유
- **Building.AddFloor()** 같은 확장 메서드: 새 Floor 삽입 후 옥상존 GameObject를 새 천장 위로 위치 갱신
- **불변식**: 스테이지 클리어 전까지 옥상존엔 ConstructionModule이 점유 중

### 미정/추후 결정
- 층 추가 트리거 (자동? 골드 비용? 시간?)
- 카메라 자동 추적 여부 (새 층 추가 시 화면 어떻게 보일지)

## 빌딩 그리드 (Cube 기반)
- **1 Cube** = 직사각형 1칸. **가로:세로 = 1:1.3** (세로가 약간 더 김)
- **한 층 = 9 Cube 가로 고정**, 각 Cube가 **자유 타입**:
  - **Background (배경)**: 하늘 등. 모듈 배치 불가.
  - **Outdoor (외벽)**: 검정. 일반 모듈 배치 불가, 특수 속성(Terrace/Penthouse) 모듈만.
  - **Indoor (실내)**: 흰색. 일반 모듈 배치 가능.
- **외벽 위치 고정 X**. 한 층 안에서 Background/Outdoor/Indoor가 자유롭게 배치되어 다양한 빌딩 형태 가능 (예: 한 층에 두 동의 빌딩, 비대칭, 구멍 등).
- 예시 한 층: `배경-외벽-실내-외벽-배경-외벽-실내-외벽-배경` (두 동의 미니 빌딩이 한 층에)
- **기본 빌딩 레이아웃**: `배경-외벽-실내×5-외벽-배경` (대칭 단일 빌딩)
- **양옆 배경 영역 = 각 1 Cube씩** (배경 그룹). 빌딩 + 배경 모두 동일 Cube 그리드 단위로 표현.
- **전체 그리드 가로 = 9 Cube** (배경좌1 + 빌딩7 + 배경우1)
- **SafeArea = 전체 그리드 영역** (Cube 설치 영역 + 배경 영역 모두 SafeArea 안)

### 9:16 모바일 화면에서 Cube 크기 계산
- 화면 가로 9 단위 / 9 Cube → Cube 가로 = 1 단위 (정확히 일치)
- Cube 세로 = 1 × 1.3 = 1.3 단위
- 화면 세로 16 단위 / 1.3 → 세로로 약 12 Cube 표시 가능 (16/1.3 ≈ 12.3)
- **화면 그리드 = 9 × 12 Cube**
- 1080×1920 픽셀 기준: Cube = 120 × 156 픽셀 (비율 1.30 ✓)
- 빌딩이 12층보다 높아지면 카메라 스크롤 필요
- **빌딩 세로 높이**: 스테이지마다 가변
- **지하 구역 / 지상 구역** 분리. 각 구역에만 지을 수 있는 Module Group 존재
- 층 사이에 가로 구분줄. 텍스트/아이콘이 들어갈 정도로 너무 얇으면 안 됨
- **데이터 모델 권장**: `Building`이 `floors[]`를 가지고, 각 `Floor`가 자기 `IndoorWidthInCubes`를 가지는 형태 + 좌/우 Outdoor Cube 각 1개. 빌딩 외형은 모든 층 너비의 합집합 윤곽으로 결정.

## Outdoor sprite 처리 (Auto-tiling)
외벽 sprite는 컨텍스트별 자동 선택 (런타임 계산).
- **데이터(StageData)에는 Cube 타입만 저장** (Background/Outdoor/Indoor)
- **sprite 선택은 런타임 자동 계산** — `WallSpriteResolver`가 외벽 Cube의 **8방향 인접 Cube 타입**을 보고 매칭
- **좌측 sprite 1장**만 디자인하고, **우측은 flipX로 자동 반전**

### 강제 룰 (StageBuilderTool에서 자동 검증)
- **룰 1: 실내↔배경 인접 금지 — 해제됨** (사용자 결정). 자유 배치 허용.
- **룰 2: 3연속 외벽 금지 — 해제됨** (사용자 결정). 자유 배치 허용.
- **룰 3: Zone 자동 결정** — Floor의 Zone은 사용자가 변경 불가. 룰: **최상층(가장 큰 FloorIndex) = Rooftop**, **양수 FloorIndex = Aboveground**, **음수 FloorIndex = Underground**. StageBuilderTool에서 그리기 전에 자동 갱신.

**현재 자유 배치 정책**: Cube 타입(Background/Outdoor/Indoor)은 사용자가 자유롭게 클릭으로 순환 변경. 인접 제약 없음. sprite 매핑 시스템이 모든 케이스(룰 1/2 위반 포함) 대응해야 함.

### sprite 카탈로그 (현재 총 1장 — 게임 단순화 후)

엘베 위치에 따라 한 층 패턴 3가지 — 셋 다 **외벽은 항상 `L=BG, R=IN` 동일 패턴**:
```
Left(C):   [BG] [OUT] [엘베 1×1] [모듈 1×4]              [OUT] [BG]
Right(G):  [BG] [OUT] [모듈 1×4]              [엘베 1×1] [OUT] [BG]
Center(E): [BG] [OUT] [모듈 1×2] [엘베 1×1] [모듈 1×2] [OUT] [BG]
```
세 경우 모두 외벽 좌/우 컨텍스트가 동일하므로 외벽 sprite는 **1장으로 충분** (좌측 sprite + flipX로 우측 재사용). 다른 sprite는 특수 케이스용 (일반 게임플레이에서 등장 X).

| 카테고리 | sprite | 좌/우 패턴 | 비고 |
|---|---|---|---|
| 1 | **기본 좌측 외벽** | L=BG, R=IN | flipX로 우측 외벽 재사용. **일반 케이스 전용 1장으로 충분** |

**특수 케이스용 (미정 — 등장 시 그때 디자인)**:
- 포위된 외벽 시리즈 (실내 내부 외벽) — 분리벽이 있는 특수 모듈
- 코너 ㄱ/┘자 — 계단형 빌딩 (층마다 너비 다른 경우)
- 2단 깊이 ⊃자 — 2단 움푹 파인 빌딩
- 단독 외벽, 외벽 그룹 끝 등 — 등장 케이스 정해지면 추가

**제거된 케이스 (게임 단순화로 등장 X)**:
- ❌ 모든 OUT-OUT 인접 (1×4 고정 = OUT-OUT 발생 X)
- ❌ BG-OUT-BG (단독 외벽), BG-OUT-OUT-BG (외벽 그룹) 등

### 메모
- 추후 더 케이스 추가 가능성 있음 (코너/T-junction/지붕 부근 등)
- WallSpriteResolver는 8방향 검사 + 패턴 매칭 룰셋으로 구현 예정

## Outdoor Cube 상세
**외벽과 옥상은 모두 그리드의 Cube다** (별도 facade 레이어 X). `Indoor` 대신 `Outdoor` 속성을 가진 Cube로 분류.

### Outdoor 영역 구성
- **좌측 외벽**: 모든 층의 가장 왼쪽에 1 Cube씩
- **우측 외벽**: 모든 층의 가장 오른쪽에 1 Cube씩
- **옥상**: 빌딩 최상층 위 1줄 (가로 폭은 최상층 Indoor 너비 + 좌우 외벽 만큼)

### Outdoor 점유 규칙
- **일반 모듈**: Outdoor Cube를 점유할 수 없음. Indoor에만 배치.
- **Terrace 속성 모듈**: **인접 좌/우 외벽 Outdoor Cube 1개**를 추가 점유. 모듈은 외벽에 붙은 Indoor 위치(가장 좌측 또는 가장 우측)에만 배치 가능.
- **Penthouse 속성 모듈**: **옥상 Outdoor Cube**를 추가 점유. 빌딩 최상층 근처에만 배치 가능.

### 모듈 속성 데이터 모델 (일반화)
모듈에 `OutdoorAttachment` 옵션:
- `None`: Indoor만 점유 (일반 모듈)
- `LeftWall`: Indoor + 좌측 외벽 1 Cube (Terrace)
- `RightWall`: Indoor + 우측 외벽 1 Cube (Terrace)
- `Rooftop`: Indoor + 옥상 Cube (Penthouse)

→ Terrace와 Penthouse를 **하나의 메커니즘**으로 일반화. 추후 다른 외부 점유 패턴이 추가돼도 enum 확장만으로 대응 가능.

### Outdoor Cube의 시각 처리
- **좌/우 외벽 빈 Cube**: 외벽 이미지 (빈 Indoor의 빈방 이미지와 다름)
- **옥상 Cube**: 별도 상태 enum 없이 **점유 중인 모듈로 표현** (아래 옥상 시스템 참조)

## 옥상 (Rooftop) 시스템 — 모듈 통합 방식
**옥상은 별도의 상태 enum을 두지 않는다.** 대신 옥상 영역(Outdoor Cube들)에 어떤 모듈이 점유 중인지로 모든 상태를 표현한다.

### 옥상존에 들어가는 모듈
- **`ConstructionModule`** — Group: Structural, allowedZones: [Rooftop]. 스테이지 시작 시 옥상 자동 점유. 타워크레인 + 건설중 이미지. 가로 폭 = 옥상 전체.
- **`RooftopModule`** — Group: Structural, allowedZones: [Rooftop]. 깔끔한 옥상 마감(=지붕). 단순 클리어 옵션.
- **옥상 시설 모듈** — Group은 가변 (식당/상업/주거 등 다양). 루프탑 와인바(식당), 루프탑 카페(상업), 펜트하우스 속성 모듈(주거 등). allowedZones: [Rooftop].

### 클리어 메커니즘
- **클리어 조건 = 옥상에 ConstructionModule이 아닌 다른 모듈을 건설**
- **스테이지마다 옥상 클리어 후보 모듈 리스트가 다름** (StageData에 정의):
  - 스테이지 A: 후보 = [RooftopModule] — 단순 마감만 허용
  - 스테이지 B: 후보 = [RooftopModule, RooftopWineBar, Penthouse_X, ...] — 사용자 선택권
- 사용자가 후보 중 하나 선택 → ConstructionModule 제거 → 선택 모듈 배치 → 클리어 트리거
- 옥상 가로 폭 일부만 차지하는 모듈을 골랐을 경우 나머지 옥상 영역은 RooftopModule이 채울지 빈 채로 둘지 미정

### 데이터 모델 권장
- 옥상 영역 = Outdoor Cube들 (1 Cube 높이 × 빌딩 가로 폭)
- 일반 모듈 점유 시스템과 **동일한 메커니즘**으로 처리 (앵커 셀 패턴 등 그대로 적용)
- ConstructionModule, RooftopModule은 모듈 카탈로그(Structural 그룹)에 등록된 일반 데이터 — 시스템적 특별 취급은 "스테이지 시작 시 자동 배치", "클리어 시 교체" 트리거 로직에서만 발생
- Penthouse 속성 모듈은 어느 그룹에든 분포 가능 (Structural 제외, 임대 가능 업종 그룹에 분포). 모듈 정의에 `attribute: Penthouse` 표시.
- StageData에 `rooftopClearOptions: Module[]` (옥상 클리어 후보 리스트)

## Module 규격
- **기본 크기 = 엘리베이터 위치에 따라 달라짐**:
  - 엘베 **Left(C)** 또는 **Right(G)** 스테이지: 모듈 = **1×4 Cube 고정**.
  - 엘베 **Center(E)** 스테이지: 모듈 = **1×2 Cube 고정** (엘베 좌/우에 한 개씩, 총 두 개). 이 스테이지엔 **1×2 전용 모듈 카탈로그**만 배치 가능 — 1×4 모듈은 진입 불가.
- 한 층 표준 패턴:
  - Left:   `[BG][OUT][엘베 1×1][모듈 1×4]            [OUT][BG]`
  - Right:  `[BG][OUT][모듈 1×4]            [엘베 1×1][OUT][BG]`
  - Center: `[BG][OUT][모듈 1×2][엘베 1×1][모듈 1×2][OUT][BG]`
- **엘리베이터(ElevatorModule)**: Structural Group, 1×1, 모든 층 자동 배치. **위치는 StageData.ElevatorPosition enum** (Left/Center/Right, 기본 Left)으로 스테이지마다 결정. Indoor 5칸(C/D/E/F/G) 중 **C / E / G만 허용**. 하드코딩 금지. 추후 Visitor 이동 시각화에 활용 예정.
- **특수 케이스 (위 기본 크기에서 벗어나는 모듈)** — 미정. 후보: Penthouse, Terrace 속성 모듈, 시스템 모듈 등
- 사용자에게 보이는 결정 = "이 층에 어떤 종류 시설 지을까?" — Tiny Tower 스타일
- 모듈 sprite 가로 비율 = m × Cube. 1×4 → 4:1, 1×2 → 2:1 (1 cube 120×156 px 기준)

### 미정 항목
- 1×4 / 1×2 외 크기 예외 모듈 카탈로그
- Visitor 이동 시각화 구현 (엘베 통해 위/아래로 움직임)
- E 스테이지 두 영역(C-D, F-G)에 같은 모듈 강제인지 자유인지 — 현재 자유로 가정

## Module 상세 규칙
- **Group 소속**: 모든 모듈은 정확히 1개 Group에 소속 (다중 소속 없음)
- **Group은 현재 5종**, 추후 추가 가능 → enum 하드코딩 금지, 데이터 주도형 설계
  1. 시설 (Facility)
  2. 식당 (Restaurant)
  3. 상업 (Commercial)
  4. 사무 (Office)
  5. 주거 (Residential)
- **개별 고유 이미지**: 모듈마다 고유 이미지 1장. (예: 초밥집 = 식당 Group, 1×2, 1:2 비율 이미지)
- **이미지 비율**: 점유하는 m×n Cube 비율과 정확히 일치
- **빈 Cube 표현**: 세입자(Tenant) 입주 전 빌딩 Cube들은 **1×1 빈방 이미지**로 채워짐
- **빈 Cube → Module 전환 표현**: 미정. 권장 = 교체 방식 (아래 기술 노트)

## Module 레벨 시스템
- **기본 구조 = Lv1 ~ Lv4** (4단계). 단 **모듈마다 레벨 수가 다를 수 있음** → 데이터 구조에서 레벨 수를 모듈별 가변값으로 둘 것 (4 하드코딩 금지)
- Tenant 입주 직후 **Lv1**
- 유저가 재화로 **개별 모듈별 업그레이드** 가능
- 레벨 ↑ → **월세 ↑ + 더 부유한 Visitor**
- **레벨업해도 점유 Cube 크기는 변화 없음** (1×2 초밥집은 어떤 레벨에서도 1×2). 이미지/이펙트/수익만 변경.
- Tenant ↔ Visitor ↔ 레벨의 정확한 연계 공식은 미정 (추후 기획)

## 방치 메커닉 (코어 골드 수급)
- **Tenant**: 모듈을 임대 → 자동으로 골드를 유저에게 납부
- **Visitor**: 모듈을 방문 → 모듈 내부에 골드를 떨굼
- 떨궈진 골드는 유저가 **버튼 한 번으로 일괄 수거** 가능
- 자세한 수급 공식, 빈도, 캐파 룰 등은 미정

## 기술 노트 — 빈 Cube/Module 표현 권장안
효율 관점에서 다음 방식 권장:
- **데이터 모델**: 각 Cube가 "현재 어떤 Module을 가리키는지" 참조. 빈 Cube도 EmptyModule이라는 Module 타입으로 일관 처리.
- **다칸 Module 표현 — 앵커 Cube 패턴**: 모듈의 좌상단(또는 좌하단) Cube가 앵커이고 그 Cube에 모듈 정보(크기/Group/Module ID/Lv) 저장. 나머지 점유 Cube들은 앵커를 가리키는 참조만 가짐.
- **시각적 처리는 교체 방식 권장** (오버레이 X):
  - 1×2 모듈 입주 → 그 자리 빈 Cube GameObject 2개 비활성화/제거 → 1×2 모듈 GameObject 1개 생성
  - 모듈 빠지면 빈 Cube GameObject 복원
  - 이유: GameObject 수 적고, z-order/레이어 관리 단순, 자료구조와 1:1 매핑
- **이미지 정합**: 모듈 GameObject의 Sprite/Image size를 (Cube 크기 × m, Cube 크기 × n)으로 강제 → 비율 일치 보장

## 시점 / 카메라
- **기본 시작 시점 = 빌딩 2층 구간** (FloorIndex == 2 cube의 세로 중심이 viewport 세로 중앙). `BuildingView.FocusHome()`이 ScrollRect.verticalNormalizedPosition을 계산해 정렬. RenderBuilding 끝에서 자동 호출.
- **빌딩 홈 버튼 (UI) — 추후 구현 예정**. 유저가 위/아래로 많이 스크롤한 뒤 누르면 위 기본 시작 시점으로 복귀. 구현 시 버튼 onClick → `BuildingView.FocusHome()` 호출만 연결하면 됨 (API 이미 마련됨).
- FloorIndex=2가 없는 스테이지 폴백: 가장 작은 양수 FloorIndex → 그것도 없으면 첫 floor.
- BackgroundScrollSync가 ScrollRect.content를 따라가므로 시점 이동 시 배경도 자동 동기화 — 별도 처리 불필요.

## 미정 / 추후 확정 항목
- Cube의 실제 크기 (Unity Unit / 픽셀)
- 스테이지 클리어 조건
- 골드 수급 공식 / 밸런싱
- 모듈 카탈로그 (각 Zone × Group별 어떤 모듈이 존재하는지)
- 시간 흐름 모델 (실시간 / 가속)
- 오프라인 보상 처리
- Tenant/Visitor/Level 연계 공식
- **옥상(Rooftop) 해금 조건**: 평상시 ConstructionModule이 점유 → 어떤 조건에 unlock 되어 옥상 모듈 배치 가능해지는지 (스테이지 클리어가 유력하지만 미확정)
- **옥상 보상이 자동 배치인지 사용자 선택인지**: StageData의 RooftopReward로 자동 vs 클리어 후 사용자 선택권

## How to apply
- 새 시스템 설계 시 **Cube/Module/Group 용어를 그대로 사용**. 다른 단어 쓰면 혼동 발생.
- 미정 항목을 건드리는 작업이면 사용자에게 먼저 물어볼 것
- 빌딩 데이터는 가변 크기 그리드 + 지상/지하 구분이 핵심 — 단순화해서 깎지 말 것
- Visitor의 "모듈 안 골드 드롭" + "버튼 일괄 수거" 패턴은 코어 인터랙션 — 일찍 확정 권장
- Group 추가 가능성을 항상 염두 (5종 enum 하드코딩 금지, ScriptableObject 또는 데이터 테이블 기반)
