# Claude Code 환경 이식 번들

새 vanilla 프로젝트로 옮길 때 **Claude Code의 작업 방식·권한·메모리**도 같이 가져가기 위한 번들.

## 폴더 구성

```
Docs/ClaudeCode/
├── README.md                              (본 문서)
├── settings.local.json                    ← 새 프로젝트의 .claude/ 에 복사
├── agents/
│   └── code-reviewer.md                   ← 새 프로젝트의 .claude/agents/ 에 복사
├── commands/
│   ├── fix-bug.md                         ← 새 프로젝트의 .claude/commands/ 에 복사
│   └── new-feature.md                     ← 동일
└── memory/
    ├── MEMORY.md                          ← 새 프로젝트의 Claude 메모리에 1대1 복원
    ├── feedback_debug_workflow.md
    ├── feedback_git.md
    ├── feedback_problem_solving.md
    ├── feedback_reuse_pattern.md
    ├── feedback_shared_logic.md
    ├── game_concept.md
    ├── project_context.md                 ← (구) 회사 베이스 — vanilla에선 미사용
    ├── project_context_VANILLA.md         ← 새 프로젝트용. 위 이름을 project_context.md로 바꿔 사용
    ├── reference_docs_folder.md
    ├── reference_unity_editor_log.md
    └── user_preferences.md
```

## 이식 절차

### 1) settings.local.json — 권한/Auto 모드

새 프로젝트 루트에 `.claude/` 폴더를 만들고 `settings.local.json`을 복사:

```
새프로젝트/
└── .claude/
    └── settings.local.json    ← Docs/ClaudeCode/settings.local.json 복사
```

내용 요약:
- `defaultMode: "auto"` — Edit/Read/Write/Glob/Grep은 매번 묻지 않고 자동 허용
- Bash 화이트리스트: git, cd, ls, find, cat, adb — 묻지 않고 실행
- PowerShell, Skill(update-config) 허용

→ 새 프로젝트에서도 처음부터 "bash 묻지 마" 가 적용된 상태로 시작.

### 2) agents/, commands/ — 커스텀 도구

새 프로젝트의 `.claude/agents/`, `.claude/commands/` 폴더를 만들고 .md 파일 그대로 복사.

- `code-reviewer`: 코드 리뷰 전용 서브에이전트
- `/fix-bug`, `/new-feature`: 슬래시 커맨드

### 3) memory/ — Claude 메모리 복원

⚠ **메모리는 사용자 홈 폴더(`~/.claude/projects/<project-hash>/memory/`)에 저장됨**. 프로젝트 경로가 바뀌면 hash도 바뀌므로 새 프로젝트에서는 빈 메모리부터 시작.

**복원 방법**: 새 프로젝트의 Claude에게 다음 명령:

> `Docs/ClaudeCode/memory/` 안의 모든 .md 파일을 읽고, 각 파일의 frontmatter + 본문을 보고 똑같은 내용으로 메모리에 저장해줘. 단:
> - `project_context.md`는 **무시**하고 대신 `project_context_VANILLA.md`의 내용을 `project_context.md`라는 이름으로 저장
> - `MEMORY.md`는 마지막에 인덱스로 갱신
> - 저장 후 어떤 메모리가 복원됐는지 한 줄씩 요약 보고

→ 복원 완료 시 모든 작업 원칙(feedback_*)·사용자 선호(user_preferences)·디버깅 워크플로(feedback_debug_workflow)·Docs 자동 참조(reference_docs_folder) 등이 새 프로젝트에서도 그대로 활성화.

### 4) project_context_VANILLA.md → project_context.md 이름 변경

vanilla용 프로젝트 배경. 새 프로젝트의 의존성·범위가 정리되어 있음. 메모리 복원 시 이 내용을 `project_context.md`로 저장하면 새 Claude가 회사 RootBox 무관 vanilla 프로젝트임을 인지.

## 핵심 메모리 한 줄 요약

새 프로젝트에서 무엇이 활성화되는지:

| 메모리 | 효과 |
|---|---|
| `user_preferences.md` | 한국어 답변, 자동 수행 선호, 이모지 금지 |
| `feedback_git.md` | git 작업은 사용자가 직접. Claude는 커밋 권유/실행 금지 |
| `feedback_reuse_pattern.md` | 작동하는 코드 패턴 재사용, 다른 방식 시도 금지 |
| `feedback_shared_logic.md` | 동일 로직 중복 금지, 공용 함수 하나로 |
| `feedback_problem_solving.md` | 문제 즉시 수정 X. 원인분석 → 방안제시 → 허가 → 수정. + cs 코드는 자동, GUI 작업은 안내만 |
| `feedback_debug_workflow.md` | 에러 발생 시 5단계 자동 (로그 추가 → Play → Editor.log 읽기 → 솔루션 → 정리) |
| `reference_docs_folder.md` | Docs/ 자동 참조. 모듈/그룹/존 작업 전 필수 확인 |
| `reference_unity_editor_log.md` | Console 캡처 없이도 Unity Editor.log 자동 읽기 |
| `game_concept.md` | Top Tower 컨셉 — 타워 방치형, 9:16, 단면도 등 |
| `project_context.md` (VANILLA) | vanilla Unity 2D URP, 회사 의존 0 |

## 검증

새 프로젝트에서 위 단계 완료 후, Claude에게 다음 질문:

> 현재 메모리 목록 알려줘. 그리고 "bash 묻지 마" 같은 권한이 적용되어 있는지 확인해줘.

기대 응답:
- 메모리 10개 모두 인덱스에 표시
- settings.local.json의 Bash(git:*) 등 권한이 자동 허용됨을 확인
