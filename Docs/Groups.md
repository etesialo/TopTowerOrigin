# Groups / 그룹 정의

> **본 문서 범위**: Module 분류축. 게임 메커닉 기획용 + Sprite name 매핑에 참고.

표기: `영문 / 한글`

Group은 모듈의 분류축 #1 — "이 모듈이 무슨 종류인가". 모든 Module은 정확히 1개 Group에 속함.

## 정의된 Group 목록

- **Structural / 구조** — 빈 모듈, 외벽, 공사/옥상 모듈 등 건물 구조의 기본 요소. 시스템이 자동 배치/관리하는 모듈 영역.
- **Facility / 시설** — 건물 운영을 위한 시설 모듈 (청소방, 화장실 등)
- **Restaurant / 식당**
- **Commercial / 상업**
- **Office / 사무**
- **Residential / 주거**

## 추가 규칙

- 새 Group은 줄바꿈으로 위 목록에 추가
- 코드에서는 enum 하드코딩 금지, 데이터 주도형(ScriptableObject 등)으로 다룸
- Group 추가 시 어떤 Module들이 이 Group에 속하는지는 `Modules.md`에서 관리
