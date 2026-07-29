---
name: Docs 폴더 자동 참조
description: 프로젝트 루트의 Docs/ 폴더에 있는 게임 데이터/용어 문서를 작업 시 항상 먼저 확인할 것
type: reference
originSessionId: 93ff8962-d236-4105-9ce1-30c9289c2e19
---
프로젝트 루트의 `Docs/` 폴더에 Top Tower 게임의 **마스터 데이터/용어 문서**가 있다. 모든 작업 시작 시 관련 파일을 먼저 확인할 것.

## 폴더 구조

| 파일 | 내용 | 언제 참조? |
|---|---|---|
| `Docs/README.md` | 폴더 안내, 표기 규칙 | 새 .md 파일 만들 때 |
| `Docs/Glossary.md` | 게임 핵심 용어 사전 | 용어 헷갈릴 때, 새 클래스/필드 명명 시 |
| `Docs/Groups.md` | Group 정의 (5종 + 확장) | Module 다룰 때, Group 검증 시 |
| `Docs/Zones.md` | Zone 정의 (3종 + 확장) | Module 배치 시, Cube Zone 다룰 때 |
| `Docs/Modules.md` | Module 카탈로그 (Group별 모듈 목록) | 모듈 시스템 작업 시 |

## 표기 규칙

- 영문 우선, 슬래시 구분: `Cube / 큐브`, `Restaurant / 식당`, `SushiBar / 초밥집`
- 코드 클래스/필드는 영문 그대로 사용
- 줄바꿈으로 항목 추가됨 — 텍스트만 보고 항목 인식할 것

## How to apply

- **모듈/그룹/존 관련 작업 전에는 반드시 해당 .md 파일을 Read** — 사용자가 추가/변경했을 수 있음
- 코드 작성 시 클래스/필드 이름은 Glossary.md 영문명을 그대로 사용
- 사용자가 Modules.md에 새 모듈을 줄바꿈으로 추가하면 그게 게임 데이터의 정의 — 별도 확인 없이 인식
- 양이 늘어나서 `Modules.csv` 등으로 포맷이 바뀌어도 동일하게 참조 (CSV 읽기 가능)
- Docs 파일이 game_concept.md 메모리와 충돌하면 Docs를 우선 (Docs가 사용자가 직접 관리하는 마스터)
