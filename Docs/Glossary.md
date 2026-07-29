# Glossary / 용어 사전

> **본 문서 범위**: Top Tower **빌딩 시각 시스템**의 용어 + 명명체계. 향후 메커닉/UI 용어가 추가되면 별도 섹션으로.

표기: `영문 / 한글`

## 그리드/공간 단위

- **Cube / 큐브** — 빌딩 그리드의 1×1 한 칸. 가장 작은 단위.
- **Indoor / 실내** — 건물 안쪽 Cube. 일반 모듈 배치 영역.
- **Outdoor / 실외** — 건물 바깥쪽 Cube. 좌/우 외벽 + 옥상이 모두 Outdoor에 속함. 일반 모듈 배치 불가, 특수 속성 모듈만 점유.
- **Floor / 층** — Cube들의 한 행. 각 Floor는 자기 너비를 가질 수 있음(계단형 빌딩).
- **Building / 빌딩** — Floor들의 집합. 한 스테이지 = 한 빌딩.
- **Stage / 스테이지** — 게임 진행 단위. 한 빌딩 + 클리어 조건. **데이터/코드/prefab 이름에 사용**. (예: Stage_001.prefab, StageData, IStageContent)
- **Level / 레벨** — UI에 표시되는 단어. 사용자에게 보이는 텍스트로만 사용. (예: "Level.1 Normal" 버튼, "Stage 3" 텍스트는 사용 안 함 → "Level 3"으로 표시). **내부적으로는 Stage와 동일 개념**.

## 시각 요소 (구분선 / 외관)

- **OuterWall / 외벽** — 빌딩 좌/우 가장자리 Outdoor Cube의 시각 표현. 1 Cube 폭.
- **InnerWall / 내벽** — 별도 시스템 폐기. **각 모듈 sprite가 자기 영역의 좌/우 경계 벽까지 직접 그림**. 다칸 모듈은 sprite가 통째로 영역 차지하니까 내부 벽 자동 X, 다른 모듈 인접 시 두 벽이 겹쳐 한 줄로 보임. 별도 계산/갱신 로직 불필요.
- **FloorDivider / 층 구분줄 (천장)** — Floor 사이의 가로 줄. 한 층의 천장 역할 (= 위층의 바닥). 텍스트/아이콘이 들어갈 정도의 두께. (이미지에서 진파랑). **InnerWall과 별개의 시스템**으로 유지됨.
- **Background / 배경** — 빌딩 외부 (하늘 등). 화면 양옆/위.
- **Ground / 지면** — 화면 하단 (갈색). 지상/지하 경계.

## 분류축

- **Group / 그룹** — 모듈 분류축 #1. "이 모듈이 무슨 종류인가". 자세한 내용 → Groups.md
- **Zone / 존** — 모듈 분류축 #2. "이 모듈을 어디에 놓을 수 있는가". 자세한 내용 → Zones.md

## 모듈

- **Module / 모듈** — 개별 방 한 채. m×n Cube 점유. 자세한 내용 → Modules.md

## 모듈 속성

- **Terrace / 테라스** — 모듈에 붙는 속성. 인접 좌/우 외벽 Outdoor Cube 1개를 추가 점유.
- **Penthouse / 펜트하우스** — 모듈에 붙는 속성. 옥상 Outdoor Cube를 추가 점유.

## 액터 / 게임플레이

- **Tenant / 세입자** — 모듈을 임대해서 자동 월세 납부하는 액터.
- **Visitor / 방문객** — 모듈을 방문해서 모듈 안에 골드를 떨구는 액터.

## Structural Group 모듈 (시스템 자동 배치)

`Structural / 구조` 그룹에 속한 모듈들. 시스템이 자동으로 배치/관리하며 사용자가 직접 짓거나 임대하지 않음. 모듈 카탈로그는 `Modules.md` 참조.

- **EmptyModule / 빈 모듈** — 1×1 Indoor 빈 Cube 표시 (세입자 없는 빈 방).
- **WallModule / 외벽 모듈** — 좌/우 Outdoor 외벽 Cube 표시.
- **ConstructionModule / 공사 모듈** — 옥상존 평상시 자동 점유 (타워크레인+건설중 이미지).
- **RooftopModule / 옥상 모듈** — 깔끔한 옥상 마감(=지붕). 클리어 시 ConstructionModule을 대체할 수 있는 단순 마감 옵션.

## Penthouse 속성

`Penthouse`는 단일 모듈이 아니라 **모듈에 붙는 속성**이다.
- Penthouse 속성 모듈은 **항상 옥상존 전용** (allowedZones: [Rooftop])
- Group은 자유 — 어떤 업종이냐에 따라 결정 (주거/사무/식당/상업/시설 등 모두 가능)
- 옥상 Outdoor Cube를 점유 (이전 정의대로)

## 층 라벨 (Floor Label)

각 floor의 ceiling row(천장 영역)에 표시되는 텍스트.

**텍스트 형식**:
- 지상층 (FloorIndex ≥ 1): `▼ {N}F` — 예: `▼ 1F`, `▼ 2F`, `▼ 12F`
- 지하층 (FloorIndex < 0): `▼ B{N}` — 예: `▼ B1`, `▼ B2`, `▼ B3`
- prefix `▼ ` 고정 (디자이너가 원하면 변경 가능하지만 현재 컨벤션)

**시각 요소**:
- TMP (TextMeshPro) SDF 폰트
- 외곽선: TMP SDF 내장 (face dilate로 외부 기준 stroke)
- 받침박스: Image 배경. 텍스트 시각 중심에 위치 + 대칭 확장
- 위치: col 2 (Indoor 좌측 시작)부터 2칸 폭, 세로는 ceilingHeight

**Inspector 노출 (BuildingView "층 라벨" 헤더)**:
- Font (TMP_FontAsset)
- Color
- Font Size
- Use Stroke / Stroke Color / Stroke Thickness Range(0, 0.2)
- Use Backdrop / Backdrop Color (알파) / Backdrop Size (큐브 단위) / Backdrop Offset (픽셀)

## Sprite 이름 체계 (Stage 전용)

Stage 전용 sprite (Addressables 라벨 `Stage_{NNN}`)의 명명 규칙: `{NNN}_Structural_{Type}_{ID}`.
`{Type}` 부분이 sprite의 역할을 결정 — BuildingView가 이 키워드로 매칭.

| Type 키워드 | 한글명 | 위치 / 조건 |
|---|---|---|
| `_Elevator_` | 엘베 | StageData.ElevatorPosition 컬럼 |
| `_Gate_` | 빌딩입구 | 1F outdoor (있으면 _Wall_보다 우선) |
| `_Wall_` | 외벽 | 지상층(FloorIndex≥1) outdoor cube |
| `_Wallfr_` | 천장외벽 | 지상층 outdoor 구분줄 |
| `_Floor_` | 천장 | Indoor 구분줄 (층 라벨 표시 영역) |
| `_Underwall_` | 지하외벽 | 지하층(FloorIndex<0) outdoor cube |
| `_Underwallfr_` | 지하천장외벽 | 지하층 outdoor 구분줄 (1F↔B1 경계 포함) |
| `_Bottom_` | 최하바닥 | 최하 지하층 Bn 아래 indoor 바닥 |
| `_Root_` | 뿌리 | Bottom row 아래 단일 stretch sprite. 가로 7칸(좌우 외벽 포함), 세로 2×stackHeight. 지하층 존재 시에만 표시 |

좌측 sprite만 등록하면 우측은 런타임 미러로 자동 생성 (Wall/Wallfr/Underwall/Underwallfr/Gate).

공통 sprite (Addressables 라벨 `StageCommon`):
- `Structural_Empty_NNN` — 빈 모듈(EmptyModule). ID 001~020 기본, 021~040 특수 예약, 100~ 1×2.
