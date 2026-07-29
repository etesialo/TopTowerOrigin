---
name: 프로젝트 배경
description: Top Tower vanilla Unity 2D URP 프로젝트 (회사 RootBox 의존 X). 빌딩 시각 시스템 이식 완료 베이스.
type: project
---
이 프로젝트는 사용자가 회사 RootBox 베이스에서 만들었던 Top Tower 게임의 **빌딩 시각 시스템**을 vanilla Unity 2D URP로 독립시킨 새 프로젝트.

- **게임명**: Top Tower
- **장르**: 타워 방치형 (Sim Tower / Tiny Tower 계열)
- **플랫폼**: 모바일 세로 9:16 (참조 해상도 1080×1920)
- **패키지명**: (사용자가 결정)

**의존성 (vanilla 한정)**:
- 필수: TMP (vanilla), Addressables, UniTask
- 회사 SDK 의존 0건. RootBox/UIService/IngameBase/회사 fork TMP 등 일절 미사용.

**구현된 빌딩 시스템 범위**:
- 9칸 가로 그리드 (배경/외벽/Indoor 5칸/외벽/배경)
- 지상층 + 지하층 + Bottom row + Root sprite
- sprite 매핑 (Wall/Wallfr/Floor/Underwall/Underwallfr/Bottom/Gate/Elevator/Root/Empty)
- 우측 외벽 자동 미러 (RenderTexture + Blit)
- 배경 3종 (Sky/Main/Underground) 빌딩 정렬
- 드래그(고무줄), 휠/핀치 줌, HomeButton (한 몸 이동)
- TowerOrigin 단일 통제점 (OriginY/HomeY/Show Guide Lines/Zoom 한계)
- TMP 층 라벨 + 받침박스
- Edit Mode 전용 가이드 라인

**아직 미구현 (별도 단계)**:
- 메커닉 (세입자/방문객/골드)
- HUD/UI 패널
- 광고/IAP
- 저장/로드

**How to apply:**
- 빌딩 큐브 그리드 구조는 `Docs/TopTower_GameSpec.md`의 정본 사양 따라 정확히 유지.
- 코드 동작은 `Docs/TopTower_TechnicalReference.md`의 흐름 그대로.
- 새 기능 추가 시 `Docs/Groups.md`/`Modules.md`/`Zones.md`의 분류축 갱신.
