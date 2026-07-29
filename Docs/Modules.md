# Modules / 모듈 카탈로그

> **본 문서 범위**: 게임에 등장할 모듈의 기획 카탈로그. 현재 빌딩 시각 시스템에서는 Structural 그룹의 EmptyModule/WallModule/ElevatorModule이 구현되어 있고, 나머지 그룹(Restaurant/Commercial/Office/Residential 등)은 향후 메커닉 단계에서 데이터 추가.

표기: `영문 / 한글`

각 Group 아래에 그 Group에 속한 모듈을 줄바꿈으로 추가. 빈 항목은 비워두면 됨.

---

## Group: Structural / 구조

건물의 구조적 기본 요소. 시스템이 자동으로 배치/관리하는 모듈들. 사용자가 직접 짓거나 임대하는 게 아님.

- **EmptyModule / 빈 모듈** — allowedZones: [Underground, Aboveground, Rooftop], 1×1, Indoor 빈 Cube 표시 (세입자 없는 빈 방 이미지)
- **WallModule / 외벽 모듈** — allowedZones: [Underground, Aboveground], 좌/우 Outdoor 외벽 Cube 표시
- **ConstructionModule / 공사 모듈** — allowedZones: [Rooftop], 평상시 옥상 자동 점유 (타워크레인+건설중 이미지)
- **RooftopModule / 옥상 모듈** — allowedZones: [Rooftop], 깔끔한 옥상 마감(=지붕). 클리어 시 ConstructionModule을 대체할 수 있는 단순 마감 옵션
- **ElevatorModule / 엘리베이터 모듈** — allowedZones: [Underground, Aboveground, Rooftop], 1×1, 모든 층에 자동 배치. **위치는 StageData.ElevatorPosition으로 스테이지마다 결정** — Indoor 5칸(C, D, E, F, G) 중 **C / E / G 세 위치만 허용** (Left/Center/Right, 기본 Left).
  - **Left (C)**: 엘베가 Indoor 좌측 끝. 같은 층 모듈 = 1×4 (D-E-F-G) 한 개.
  - **Right (G)**: 엘베가 Indoor 우측 끝. 같은 층 모듈 = 1×4 (C-D-E-F) 한 개.
  - **Center (E)**: 엘베가 Indoor 가운데. 같은 층 모듈 = **1×2 두 개** (C-D, F-G). 이 스테이지에는 **1×2 전용 모듈 카탈로그**만 배치 가능 (1×4 모듈은 입장 불가).
  - 추후 방문객(Visitor) 이동 시각화에 활용 예정.

## Group: Facility / 시설

건물 운영을 위한 시설 모듈 (청소방, 화장실 등). 임대용 모듈은 아님.

(여기에 시설 모듈 추가 — 예: CleaningRoom / 청소방, Restroom / 화장실)

## Group: Restaurant / 식당

(여기에 식당 모듈 추가)
- 예시) SushiBar / 초밥집

## Group: Commercial / 상업

(여기에 상업 모듈 추가)

## Group: Office / 사무

(여기에 사무 모듈 추가)

## Group: Residential / 주거

(여기에 주거 모듈 추가)

---

## Penthouse 속성 모듈 안내

Penthouse는 단일 모듈이 아니라 **모듈에 붙는 속성**이다. Penthouse 속성을 가진 모듈은:
- **allowedZones: [Rooftop]** 으로 옥상존 전용
- **Group은 자유** — 모듈의 업종에 따라 결정 (주거 펜트하우스, 사무 펜트하우스, 식당 루프탑 레스토랑 등 다양)

Penthouse 속성 모듈은 위 일반 Group 섹션에 그대로 등록하면 되고, 메모란에 `Penthouse 속성` 표시. 예시:
- `PenthouseSuite / 펜트하우스 스위트` (Residential 그룹, Penthouse 속성, allowedZones: [Rooftop])

---

## 모듈 추가 시 적을 정보 (양 늘어나면 표 형식으로 전환 권장)

각 모듈은 다음 정보를 가집니다:
- 영문명 / 한글명
- Group (1개)
- allowedZones (1개 이상)
- 크기 (m × n Cube)
- 레벨 수 (기본 4, 모듈마다 다를 수 있음)
- 속성 (Terrace/Penthouse 등, 옵션)
- 이미지 (1:n 또는 m:n 비율)

양이 많아지면 `Modules.csv`로 전환 — 클로드는 동일하게 읽음.
