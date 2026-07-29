# Top Tower — 게임 데이터 / 용어 문서

이 폴더는 Top Tower의 **게임 데이터와 용어를 관리**하는 곳입니다.

## 표기 규칙

- **영문 우선, 슬래시 구분**: `Cube / 큐브`, `Restaurant / 식당`, `SushiBar / 초밥집`
- 코드의 클래스/필드 이름은 **영문**을 그대로 사용
- 줄바꿈으로 항목 추가 가능 → 클로드가 자동 인식

## 파일 구성

### 데이터/용어 (분류·명명 마스터)

| 파일 | 내용 |
|---|---|
| `README.md` | 이 폴더 안내, 표기 규칙 |
| `Glossary.md` | 게임 핵심 용어 사전 + sprite naming 마스터 + 층 라벨 표기 |
| `Groups.md` | Group 정의 (모듈의 분류축 #1) |
| `Zones.md` | Zone 정의 (모듈의 분류축 #2) |
| `Modules.md` | Module 카탈로그 (Group별 모듈 목록) |

### 이식/구현 가이드 (vanilla 이식용 3종)

| 파일 | 관점 | 용도 |
|---|---|---|
| `TopTower_GameSpec.md` | 사양 — "무엇을" | 픽셀/규칙/매핑/수치의 정본 |
| `TopTower_TechnicalReference.md` | 동작 — "어떻게" | 호출 흐름·공식 유도·함정·자주 빠뜨리는 점 |
| `TopTower_BuildPlan.md` | 절차 — "순서대로" | 새 프로젝트에서 단계별 Claude 명령용 |

### Claude Code 환경 번들

| 폴더 | 내용 |
|---|---|
| `ClaudeCode/` | Claude Code 권한·커스텀 도구·메모리 번들. `ClaudeCode/README.md`의 절차대로 새 프로젝트에 복원하면 같은 작업 방식 그대로 사용. |

**vanilla 이식 시**: 위 8개 .md + `ClaudeCode/` 폴더 전부 새 프로젝트의 `Docs/`에 복사. 분류축(Group/Zone)·명명체계·작업 원칙이 모두 일관되게 유지되어야 함.

## 클로드 협업 시 참고

- 클로드는 작업 시작 시 항상 이 폴더를 먼저 확인합니다.
- 새 모듈/그룹/존 추가 시 해당 .md 파일에 줄바꿈으로 추가하면 자동 반영됩니다.
- 양이 많아지면 .md를 .csv로 변환해도 클로드가 동일하게 읽습니다.
