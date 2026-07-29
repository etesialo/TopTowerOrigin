# 프로젝트 개요

**게임 이름**: Top Tower
**패키지명**: com.kyeongsoo.toptower (본인 게임 패키지명으로 조정)
**장르**: 타워 방치형 (Sim Tower / Tiny Tower / Project Highrise 계열)
**플랫폼**: 모바일 세로 (9:16)
**구조**: 빈 베이스 씬 + 스테이지 프리팹 동적 로드(Addressables) 방식
**기반**: vanilla Unity 2D URP. **회사 RootBox/SDK 의존 없음.**

**핵심 컨셉**
- 평면 단면도 스타일 비주얼 (원근감 없음, 건물 내부가 그대로 보임)
- 1칸 = 정사각형 그리드. 가로 고정, 세로는 스테이지마다 가변
- 지하/지상 구역 구분. 각 구역 전용 방 종류 존재
- 세입자(tenant): 방 임대 → 골드 자동 납부 / 방문객(visitor): 방 방문 → 골드 드롭, 일괄 수거

**namespace**: 코어 코드는 `KS.TopTower` (Editor 도구는 `KS.TopTower.EditorTools`).

## 기본 사항

- 답변은 한국어로. 코드에 이모지 사용 금지.
- 프레임워크: **UniTask, Addressables, TMP** (vanilla). VContainer/UniRx는 미사용 — 저장/자동납부 등 별도 시스템 도입 시 그때 검토.

## 주의사항

- git 작업(커밋, 푸시 등)은 반드시 사전 확인 후 진행.
- 프로젝트 수정(코드, 에셋) 시 파일 수정 행위 자체는 매번 허가 불필요. 단, 접근 방식/계획은 반드시 사전 합의.

## 작업 원칙

- 기능 구현, 버그 수정, 기능 수정, 개선사항 등 **모든 작업**에 동일 적용:
  1. 원인/요구사항 분석
  2. 기존 시스템·코드를 충분히 활용한 합리적 방안 모색 (중복 코딩, 불필요한 복잡성 금지)
  3. 사용자에게 방안 제시 → 실행 허가 후 수정
- 분석 없이 코드를 바꾸거나, 추측으로 수정하는 것을 금지한다.

## 사양 정본 (작업 시 매번 참조)

- 빌딩 시스템 전반 + 용어/분류: `Docs/` 폴더의 8개 .md
  - `Docs/Glossary.md`, `Docs/Groups.md`, `Docs/Zones.md`, `Docs/Modules.md`
  - `Docs/TopTower_GameSpec.md` (사양), `Docs/TopTower_TechnicalReference.md` (코드 동작)
  - `Docs/TopTower_BuildPlan.md` (단계별 구현 절차)
- 코어 이식 경위/드롭인 절차: 프로젝트 루트 `README_이식.md` (있는 경우)
