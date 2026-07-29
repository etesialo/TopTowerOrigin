---
description: 새 기능 구현을 위한 체계적 워크플로우. Plan → Implement → Verify 단계를 따른다.
model: sonnet
---

# New Feature Workflow

새로운 게임 기능을 체계적으로 구현하는 워크플로우.

## Step 1: Plan (계획)

기능 요구사항을 분석하고 구현 계획을 세운다.
- 영향받는 기존 시스템 파악
- 필요한 새 스크립트 목록
- 수정이 필요한 기존 스크립트 목록
- 테스트 방법 정의

**plan mode로 시작하여 사용자 확인 후 진행.**

## Step 2: Implement (구현)

계획에 따라 한 시스템씩 구현한다.
1. 데이터 클래스 / ScriptableObject 먼저
2. 핵심 로직 구현
3. UI 연결
4. 기존 시스템과 통합

**각 단계마다 Debug.Log 마커로 컴파일 확인.**

## Step 3: Verify (검증)

- 에디터 콘솔 에러 0개 확인
- 새로 추가한 씬이 Build Settings에 등록되었는지 확인
- PlayerPrefs 키 충돌 없는지 확인
- 기존 기능 영향도 확인

## Critical Rules
1. 한 번에 하나의 시스템만 수정
2. 각 파일 수정 후 별도 커밋
3. 50% 컨텍스트 넘기 전에 /compact
4. 에러 수정 2회 실패 시 /clear 후 재접근
