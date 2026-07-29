# Zones / 존 정의

> **본 문서 범위**: Module 분류축 + StageBuilderTool의 자동 Zone 결정(최상층=Rooftop, 양수 FloorIndex=Aboveground, 음수=Underground)의 정본.

표기: `영문 / 한글`

Zone은 모듈의 분류축 #2 — "이 모듈을 어디에 놓을 수 있는가". 각 Module은 1개 이상의 Zone에 배치 가능 (allowedZones).

## 정의된 Zone 목록

- **Underground / 지하존**
- **Aboveground / 지상존**
- **Rooftop / 옥상존**

## 추가 규칙

- 새 Zone은 줄바꿈으로 위 목록에 추가
- 코드에서는 enum 하드코딩 금지, 데이터 주도형(ScriptableObject 등)으로 다룸
- 각 Cube는 자기가 속한 Zone을 가짐 (`cube.zone`)
- Module은 `allowedZones: Zone[]` 으로 어느 Zone에 배치 가능한지 정의

## Zone별 특수 규칙 메모

- **Rooftop**: 평상시 ConstructionModule이 자동 점유. 특정 조건(스테이지 클리어 등) 달성 시 RooftopModule 또는 PenthouseModule로 교체.
- **Underground / Aboveground**: 일반 모듈 자유 배치 (각 Module의 allowedZones에 포함된 경우)
