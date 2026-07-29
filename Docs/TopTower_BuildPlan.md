# Top Tower — 새 프로젝트 단계별 빌드 플랜

**문서 목적**: 새 vanilla 2D URP Unity 프로젝트에서 본 문서를 위에서 아래로 따라가며 **각 단계를 그대로 Claude에게 명령**하면 같은 결과가 나오도록.

각 단계는 **독립적으로 실행 가능**하며 끝에 검증 조건이 있음.

이 문서를 새 프로젝트의 `Docs/` 에도 같이 복사할 것. 그리고 `TopTower_GameSpec.md` + `TopTower_TechnicalReference.md` 도 같이 가져갈 것.

---

## 사전 준비 (한 번만)

### 0-A. 새 프로젝트 생성

Unity Hub → **2D (URP)** 템플릿으로 새 프로젝트 생성. 버전은 현 프로젝트와 동일(Unity 2022 LTS 또는 6.x).

### 0-B. 필수 패키지 설치 (Package Manager)

Window > Package Manager → Install:
- `com.unity.textmeshpro` (vanilla, 3.0.x 이상)
- `com.unity.addressables`

Git URL로:
- **UniTask** (**필수** — 빌딩 시스템 전체에서 사용): `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

> **현 빌딩 코어 코드는 UniTask만 사용.** VContainer/UniRx는 빌딩 시스템과 무관 — 향후 별도 시스템(저장, 자동 골드 납부 등)을 도입할 때 그 시점에 따로 검토. 이식 초기엔 설치하지 말 것 (의존성 단순화).

### 0-C. TMP Essential Resources 임포트

메뉴: **Window > TextMeshPro > Import TMP Essential Resources** 클릭. `Assets/TextMesh Pro/` 폴더 생성 + LiberationSans SDF 등 기본 자산 설치.

### 0-D. 폴더 구조 생성

```
Assets/
└── Application/
    └── TopTower/
        ├── Editor/
        ├── Image/
        │   └── Module/
        │       ├── Stage_001/        ← sprite PNG 복사 대상
        │       └── StageCommon/      ← Empty 모듈 PNG 대상
        ├── LevelData/                ← Stage_001.asset 대상
        └── Scripts/
            ├── Building/
            ├── Data/
            └── Ingame/
Docs/                                  ← 본 문서 8개 전부 복사
```

> **namespace 컨벤션**: 현 프로젝트는 `NGFE.TopTower` 사용. 새 프로젝트에선 **본인이 정한 게임명 namespace**로 모든 .cs 파일 통일 (예: `MyGame.TopTower`, `KS.TopTower` 등). Phase 1부터 일관 적용. 사양·동작 로직은 namespace와 무관.

### 0-E. 자산 복사 (구 프로젝트 → 새 프로젝트)

`.meta` 파일까지 같이 복사 (GUID 보존).

**자산 파일** (각 .png에 짝이 되는 .meta까지 같이 복사):

`Assets/Application/TopTower/Image/Module/Stage_001/` (9개 PNG):
- 001_Structural_Wall_001.png
- 001_Structural_Wallfr_001.png
- 001_Structural_Floor_001.png
- 001_Structural_Gate_001.png
- 001_Structural_Elevator_001.png
- 001_Structural_Underwall_001.png
- 001_Structural_Underwallfr_001.png
- 001_Structural_Bottom_001.png
- 001_Structural_Root_001.png

`Assets/Application/TopTower/Image/Module/StageCommon/` (4개 PNG, 추후 증가):
- Structural_Empty_001.png
- Structural_Empty_002.png
- Structural_Empty_003.png
- Structural_Empty_004.png

`Assets/Application/TopTower/LevelData/`:
- Stage_001.asset (+ meta)

**문서 (Docs 폴더 전체 8개 — 모두 필수)**:

분류·명명 마스터 (작업 시 Claude가 매번 참조):
- `Docs/README.md` (안내/표기 규칙)
- `Docs/Glossary.md` (용어 사전 + sprite naming + 층 라벨 표기)
- `Docs/Groups.md` (Group 분류축 #1 — Structural, Facility, Restaurant, Commercial, Office, Residential)
- `Docs/Zones.md` (Zone 분류축 #2 — Underground, Aboveground, Rooftop)
- `Docs/Modules.md` (Group별 모듈 카탈로그)

이식·구현 가이드:
- `Docs/TopTower_GameSpec.md` (사양 — 픽셀/규칙/매핑/수치)
- `Docs/TopTower_TechnicalReference.md` (동작 — 호출 흐름·공식·함정)
- `Docs/TopTower_BuildPlan.md` (본 문서 — 단계별 절차)

`CLAUDE.md` (프로젝트 루트, 새 프로젝트엔 vanilla 사양에 맞게 수정 필요)도 같이 복사.

### 0-F. CLAUDE.md 신규 작성 (vanilla 전용)

현 프로젝트의 `CLAUDE.md`는 회사 RootBox 베이스를 전제로 쓰여 있음. **그대로 옮기면 새 프로젝트 Claude가 혼란.** 새 프로젝트 루트에 **신규** `CLAUDE.md`를 다음 골격으로:

```markdown
# 프로젝트 개요

게임명: Top Tower
패키지: (본인 게임 패키지명)
장르: 타워 방치형 (Sim Tower / Tiny Tower 계열)
플랫폼: 모바일 세로 9:16
구조: vanilla Unity 2D URP, 회사 SDK 의존 X

## 기본
- 답변 한국어
- 코드 이모지 금지
- 프레임워크: UniTask, Addressables, TMP (vanilla)

## 작업 원칙
- 원인/요구사항 분석 → 기존 코드 재사용 모색 → 사용자에게 방안 제시 → 허가 후 수정
- 추측으로 수정 금지
- git 작업은 사전 확인 후

## 사양 정본
- 빌딩 시스템 전반: Docs/ 폴더의 8개 .md
- 코드 동작: Docs/TopTower_TechnicalReference.md
- 단계별 구현 절차: Docs/TopTower_BuildPlan.md
```

> 현 프로젝트 CLAUDE.md는 vanilla로 복사 **X**. 위 골격 기반 신규 작성.

### 0-G. Claude Code 환경 이식 (선택 — 강력 권장)

새 프로젝트에서도 같은 작업 방식·권한·메모리로 협업하려면.

**원본 위치**: `Docs/ClaudeCode/` (본 문서 옆 폴더)

**복원할 것 3종**:

1. **`.claude/settings.local.json`** (권한/Auto)
   - 새 프로젝트 루트의 `.claude/` 폴더 만들고 `Docs/ClaudeCode/settings.local.json` 복사
   - 효과: Bash(git/cd/ls/find/cat/adb), PowerShell, 기본 도구(Edit/Read/Write/Glob/Grep) 모두 자동 허용. "bash 묻지 마" 적용됨

2. **`.claude/agents/`, `.claude/commands/`** (커스텀 도구)
   - `Docs/ClaudeCode/agents/code-reviewer.md` → 새 프로젝트 `.claude/agents/`
   - `Docs/ClaudeCode/commands/fix-bug.md`, `new-feature.md` → 새 프로젝트 `.claude/commands/`

3. **Claude 메모리 복원** (작업 원칙·선호·워크플로우)
   - 메모리는 사용자 홈 폴더에 저장되어 새 프로젝트에선 빈 상태
   - 새 프로젝트의 Claude에게:
     > `Docs/ClaudeCode/memory/`의 모든 .md를 읽고 동일 내용으로 메모리에 저장해. 단 `project_context.md`는 무시하고 `project_context_VANILLA.md`의 내용을 `project_context.md`로 저장. 저장 완료 후 복원된 메모리 목록 보고.

**복원되는 작업 원칙** (요약):
- 한국어 답변, 이모지 금지, 자동 수행 선호
- git 작업은 사용자가 직접 (Claude는 커밋 권유 금지)
- 문제 발생 시: 원인분석 → 방안제시 → 허가 → 수정 (즉시 수정 금지)
- cs 코드 작업은 Claude 자동, Unity GUI 작업은 사용자 안내만
- 디버깅: 로그 추가 → Play → Editor.log 자동 읽기 → 솔루션 → 정리
- 작업 시작 시 `Docs/` 자동 참조

자세한 내용 + 검증 방법: `Docs/ClaudeCode/README.md` 참조.

### 0-H. Addressables 초기화

메뉴: **Window > Asset Management > Addressables > Groups** → Create Addressables Settings.

⚠ **검증 조건**: 새 프로젝트 시작 후 Console에 컴파일 에러 0개. TMP 폰트 임포트 완료. 빈 씬에서 Play 가능.

> **본 문서 적용 범위**: Top Tower의 **빌딩 시각 시스템**(큐브 그리드/모듈/배경/입력/라벨)에 한정. 카드 매칭, 세입자/방문객 메커닉, HUD, 광고, 인앱 결제 등은 본 문서 범위 외 — 별도 기획/구현.

---

## Phase 0.7: 분류축/용어 검토 (코딩 전 합의)

새 프로젝트에서 코드 짜기 전에 Claude와 한 번 같이 읽고 확인:

**Claude 명령 예시**:
> `Docs/Glossary.md`, `Docs/Groups.md`, `Docs/Zones.md`, `Docs/Modules.md` 를 모두 읽고 다음을 확인 후 보고:
> 1. CubeType enum 3종 (Background/Outdoor/Indoor)이 sprite 매핑 규칙(`Docs/TopTower_GameSpec.md` §4)과 일치하는지
> 2. Zone enum 3종 (Underground/Aboveground/Rooftop)이 StageBuilderTool의 자동 Zone 결정 로직(최상층=RT, 양수=AG, 음수=UG)과 정합하는지
> 3. ElevatorPosition (Left=C/Center=E/Right=G)이 col 2/4/6과 일치하는지
> 4. Module 카탈로그(`Modules.md`)의 EmptyModule/ElevatorModule 사양이 실제 구현(`Empty_` 라벨, `Elevator_` sprite)과 정합하는지
> 5. 층 라벨 표기 ("▼ 1F", "▼ B1")가 모든 위치에서 일관된지
> 
> 불일치 발견 시 무엇을 정정해야 할지 제안 후 멈추기.

**검증**: Claude가 모든 명명/분류가 일관됨을 확인 후 진행 OK.

이 단계의 의미 — 사양은 여러 문서에 분산되어 있어서 코딩 들어가기 전 한 번에 정합성을 잡아두면 후속 단계가 흔들리지 않음.

---

## Phase 1: 데이터 레이어

### 1-1. enum + StageData ScriptableObject

**Claude 명령 예시**:
> `Docs/TopTower_GameSpec.md`의 §3을 참고해서 `Assets/Application/TopTower/Scripts/Data/StageData.cs` 작성. namespace는 `MyGame.TopTower` (또는 원하는 이름). enum 3개(CubeType, ElevatorPosition, Zone) + FloorData class + StageData ScriptableObject. CreateAssetMenu도 포함.

**검증**:
- 컴파일 성공
- Project 창에서 우클릭 > Create > TopTower > Stage Data 가능
- 생성된 .asset 파일이 Inspector에서 Floors 리스트, ElevatorPosition 드롭다운 표시

---

### 1-2. (선택) StageBuilderTool — 그리드 시각 편집 윈도우

**Claude 명령 예시**:
> StageData를 시각적으로 편집할 EditorWindow를 `Assets/Application/TopTower/Editor/StageBuilderTool.cs`로 작성. 메뉴 `Tools/Top Tower/Stage Builder`. 9칸 가로 그리드 × 가변 세로. 좌클릭=외벽↔실내 토글, 우클릭=배경 변경. 행 라벨에 층 번호 + Zone 약자(UG/AG/RT). Zone은 자동 계산(최상층=RT, 양수=AG, 음수=UG). 엘리베이터 위치 드롭다운(C/E/G). 층 추가/삭제 버튼. New Stage Data 생성 버튼.

**검증**:
- 메뉴에서 윈도우 열림
- Stage_001.asset을 드래그하면 그리드 표시
- 클릭으로 셀 토글 가능

---

## Phase 2: 기본 렌더링 (sprite 없이 placeholder)

### 2-1. BuildingView 기본 골격

**Claude 명령 예시**:
> `Assets/Application/TopTower/Scripts/Building/BuildingView.cs` 작성. `Docs/TopTower_TechnicalReference.md` §4, §5 참고.
> 
> 핵심 기능:
> - `[ExecuteAlways]` MonoBehaviour
> - SerializeField: `_stageData`, `_cubeContainer` (RectTransform), `_gridWidth=9`, `_cubeAspectRatio=1.3` Range(0.5,3), `_ceilingHeightRatio=1/3` Range(0,1)
> - `Awake`: EnsureCanvasSetup + EnsureScales
> - `Start`: RenderBuilding 호출
> - `RenderBuilding`: §5의 yOffset 공식, ClearCubes, cube placeholder 배치 (Color.clear, raycastTarget=false)
> - 모든 cube는 anchor(0,1) pivot(0,1) 좌상단 기준
> - Edit Mode 생성물은 hideFlags = HideAndDontSave (저장 제외)
> - cubeContainer 폭 0이면 retry (최대 10번)
> 
> 이 단계엔 sprite 로드, ShiftY, Zoom, 라벨 등 모두 미포함. 큐브 placeholder만.

### 2-2. IngameScene + Stage_001 prefab 골격

**Claude 명령 예시**:
> `Assets/Application/Bundles/UI/Prefab/Stage/Stage_001.prefab` 생성. `Docs/TopTower_TechnicalReference.md` §10.2 + §11 트러블슈팅 참고.
> 
> Hierarchy 구조:
> - Stage_001 (root RT, anchor 중앙, sizeDelta 1080×1920)
>   - Canvas (Overlay), CanvasScaler (ScaleWithScreenSize, ref 1080×1920, match=0)
>   - **GraphicRaycaster** (필수)
>   - BuildingView 컴포넌트 부착
>   - 자식: SafeArea (anchor stretch, fill 부모)
>     - 자식: BuildingContainer (anchor stretch, fill, pivot center 0.5,0.5)
> 
> Stage_001의 BuildingView 필드:
> - StageData: Stage_001.asset 드래그
> - CubeContainer: BuildingContainer RT 드래그

> 빈 IngameScene도 생성 (그냥 EventSystem만 있는 씬). Stage_001 prefab을 IngameScene Hierarchy에 임시로 드래그해 디자이너 편의용 미리보기 가능하게.

**검증**:
- Edit Mode에서 Stage_001 prefab을 IngameScene에 드래그하면 cube placeholder들이 화면에 (투명이지만 Hierarchy에선 보임)
- Stage_001.asset의 Floors에 한 층 추가하고 Cubes 배열 9개 채우면 그리드가 갱신됨

---

## Phase 3: TowerOrigin (1F 바닥 정렬)

### 3-1. TowerOrigin 컴포넌트

**Claude 명령 예시**:
> `Assets/Application/TopTower/Scripts/Ingame/TowerOrigin.cs` 작성. `Docs/TopTower_TechnicalReference.md` §13 참고.
> 
> `[ExecuteAlways]` MonoBehaviour. SerializeField: `_originY` Range(-960, 960) default -400, `_homeY` Range(-960, 960) default 0, `_showGuideLines` bool default true. 추후 Zoom 한계 필드도 들어옴.
> 
> public 속성: OriginY, HomeY, ShowOriginGuideLine, ShowHomeGuideLine.
> 
> OnEnable + OnValidate(EditorApplication.delayCall) → ApplyToBuildingView 호출. FindObjectOfType<BuildingView>로 찾아 SetOriginY/SetHomeY/SetShowOriginGuideLine/SetShowHomeGuideLine 호출. (BuildingView에 메서드는 다음 단계에서 추가)

### 3-2. BuildingView에 SetOriginY 등 추가

**Claude 명령 예시**:
> `BuildingView.cs`에 다음 추가. `Docs/TopTower_TechnicalReference.md` §4.1, §5.2 참고.
> 
> private 필드: `_originY = 0`, `_homeY = 0`, `_showOriginGuideLine`, `_showHomeGuideLine`, `_originGuideLineGO`, `_homeGuideLineGO`, `_currentYOffset`.
> 
> public 메서드:
> - `SetOriginY(float)` — Mathf.Approximately 변경 감지, 다르면 _originY 저장 + RenderBuilding 재호출
> - `SetHomeY(float)` — _homeY 저장 + UpdateHomeGuideLine
> - `SetShowOriginGuideLine(bool)`, `SetShowHomeGuideLine(bool)` — 토글 + 가이드 갱신
> - public property: OriginY, HomeY
> 
> RenderBuilding에 yOffset 공식 적용:
> `_currentYOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight`
> 모든 cube placeholder의 anchoredPosition.y에 _currentYOffset 적용.

### 3-3. 가이드 라인 (Edit Mode 전용)

**Claude 명령 예시**:
> BuildingView에 가이드 라인 메서드 추가. `Docs/TopTower_TechnicalReference.md` §9 참고.
> 
> `UpdateOriginGuideLine()` (private):
> - cubeContainer 자식으로 캐시된 핑크 가로선 GameObject 재사용
> - Play 모드면 항상 hide
> - _showOriginGuideLine == false면 hide
> - 활성 시 anchoredPosition.y = _originY
> - 색 RGB (1, 0.4, 0.8, 0.9), sizeDelta (0, 8), anchor (0, 0.5) (1, 0.5), pivot (0.5, 0.5)
> 
> `UpdateHomeGuideLine()`:
> - 위와 동일하지만 부모는 `_cubeContainer.parent` (월드 고정)
> - 색 RGB (0.6, 1, 0.4, 0.9)
> - anchoredPosition.y = _homeY
> 
> Edit Mode 생성 시 hideFlags = DontSaveInEditor | DontSaveInBuild.
> 
> RenderBuilding 끝에 두 메서드 호출.

### 3-4. IngameScene에 TowerOrigin GameObject 추가

**Claude 명령 예시**:
> IngameScene 안에 빈 GameObject "TopTower" 만들고 TowerOrigin 컴포넌트 부착. 슬라이더 값 만지면 Stage_001 prefab(씬에 드래그된)의 빌딩이 즉시 이동하는지 확인.

**검증**:
- TowerOrigin의 Origin Y 슬라이더 움직이면 빌딩 1F 바닥이 그 라인에 따라 이동
- Show Guide Lines 체크하면 핑크/연두 라인 Edit Scene view에 표시
- Play 모드에선 가이드 라인 숨김

---

## Phase 4: Sprite 로드 + 표시

### 4-1. StageSpritesSyncTool (Addressables 자동 등록)

**Claude 명령 예시**:
> `Assets/Application/TopTower/Editor/StageSpritesSyncTool.cs` 작성. `Docs/TopTower_TechnicalReference.md` §15 참고.
> 
> 메뉴 `Tools/Top Tower/Sync Addressables`. `Assets/Application/TopTower/` 폴더 재귀 스캔. 각 asset을 Default Group에 등록 + 직속 폴더명을 단일 라벨로 부여. Editor 폴더는 skip. .cs/.asmdef/.meta 등은 skip.

> 메뉴 실행 후 Addressables Groups 창에서 Stage_001 라벨의 sprite들이 등록됐는지 확인.

### 4-2. LoadSpritesByLabelAsync 헬퍼

**Claude 명령 예시**:
> BuildingView에 sprite 로드 헬퍼 추가. `Docs/TopTower_TechnicalReference.md` §6 참고.
> 
> `LoadSpritesByLabelAsync(string label)`:
> - Play 모드: `Addressables.LoadResourceLocationsAsync` 먼저 → 등록 확인 → `LoadAssetsAsync<Sprite>` → 결과 반환
> - Edit 모드: `UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings`에서 entry 순회 → 라벨 매칭 → `AssetDatabase.LoadAssetAtPath` 동기 로드 (#if UNITY_EDITOR 안에서)
> - 없으면 null 반환 (silent skip)

### 4-3. CreateSpriteImage / CreateColorImage / CreateCubeImage

**Claude 명령 예시**:
> BuildingView에 sprite GameObject 생성 헬퍼 3개. 공통:
> - `new GameObject(name, typeof(RectTransform), typeof(Image))`
> - Edit Mode면 hideFlags = HideAndDontSave
> - 부모 = _cubeContainer
> - anchor (0, 1), pivot (0, 1) — 좌상단
> - sizeDelta는 인자
> - raycastTarget = false
> - CreateSpriteImage는 sprite 할당, CreateColorImage는 단색
> - CreateCubeImage는 GetCubeColor 호출(현재는 Color.clear 반환)

### 4-4. CreateMirroredSprite

**Claude 명령 예시**:
> BuildingView에 우측 미러 sprite 생성 메서드 추가. `Docs/TopTower_TechnicalReference.md` §7의 RenderTexture + Graphics.Blit 방식 정확히 그대로. 결과 sprite는 hideFlags = HideAndDontSave.

### 4-5. LoadAndPlaceWallsAsync (외벽)

**Claude 명령 예시**:
> BuildingView에 LoadAndPlaceWallsAsync. `Docs/TopTower_TechnicalReference.md` §6.2 참고.
> 
> Stage_{NNN} 라벨 로드. `_Wall_`, `_Underwall_`, `_Gate_` 매칭. 우측은 미러 생성. 각 floor의 outdoor cube마다 분기:
> - useGate (FloorIndex==1 + gate 존재) → gate
> - FloorIndex < 0 → underwall
> - else → wall
> 
> sprite null이면 skip.

### 4-6. LoadAndPlaceCeilingsAsync (구분줄 + Bottom row)

**Claude 명령 예시**:
> BuildingView에 LoadAndPlaceCeilingsAsync. `Docs/TopTower_TechnicalReference.md` §6.4 참고.
> 
> `_Floor_`, `_Wallfr_`, `_Underwallfr_`, `_Bottom_` 매칭. 각 floor의 ceiling row 배치:
> - Indoor → floorSprite (없으면 진파랑 RGB(0.1, 0.15, 0.4) fallback)
> - Outdoor 지상 → wallfr (우측 미러)
> - Outdoor 지하 → underwallfr (우측 미러)
> - Background → 안 그림
> 
> 최하 지하층 아래 Bottom row 추가:
> - Indoor → _Bottom_
> - Outdoor → _Underwallfr_

### 4-7. LoadAndPlaceElevatorAsync

**Claude 명령 예시**:
> BuildingView에 LoadAndPlaceElevatorAsync. `_Elevator_` 매칭. StageData.ElevatorPosition에 따라 col (Left=2, Center=4, Right=6). 모든 floor에서 해당 col이 Indoor면 배치.

### 4-8. LoadAndPlaceEmptyModulesAsync (1×4 빈 모듈)

**Claude 명령 예시**:
> BuildingView에 LoadAndPlaceEmptyModulesAsync. `Docs/TopTower_TechnicalReference.md` §6.1 참고.
> 
> StageCommon 라벨 로드. name이 "Structural_Empty_"로 시작 + 끝 ID 파싱(LastIndexOf('_'))이 1~20 범위인 것만 필터. ElevatorPosition별 모듈 영역(Left=col 3~6, Right=col 2~5, Center=skip). 해당 영역이 전부 Indoor인 floor만 배치. Random.Range로 매번 다른 sprite 선택. 폭 = 4 × cubeWidth.

### 4-9. LoadAndPlaceRootAsync (뿌리)

**Claude 명령 예시**:
> BuildingView에 LoadAndPlaceRootAsync. `Docs/TopTower_TechnicalReference.md` §6.5 참고.
> 
> 최하 지하층이 있을 때만 (deepest.FloorIndex < 0). _Root_ 매칭. 1장의 sprite를 7칸 × 2×stackHeight 영역에 stretch. 위치: Bottom row 바로 아래.

### 4-10. LoadSpritesThenLabelsAsync 통합

**Claude 명령 예시**:
> RenderBuilding 끝에서 호출할 LoadSpritesThenLabelsAsync 메서드. UniTask.WhenAll로 위 5개 메서드 병렬 await. (라벨 메서드는 다음 phase에서 추가하므로 일단 sprite만.)

### 4-11. 메뉴로 sync 실행 후 확인

**Claude 명령 예시**:
> 사용자에게 `Tools > Top Tower > Sync Addressables` 실행 안내. 그 후 Edit 모드에서 Stage_001 prefab을 Hierarchy에 다시 드래그하면 wall/elevator/empty 등 sprite가 표시되어야 함.

**검증**:
- 지상층 외벽이 좌우에 sprite로 표시
- 1F에 Gate sprite (있으면)
- 엘베가 한 column에 모든 층 표시
- Empty 모듈이 indoor 영역에 random 표시
- 지하층은 underwall/underwallfr로 표시
- 최하 지하층 아래 Bottom row + Root sprite

---

## Phase 5: 배경 시스템

### 5-1. BackgroundView 기본

**Claude 명령 예시**:
> `Assets/Application/TopTower/Scripts/Building/BackgroundView.cs` 작성. `Docs/TopTower_TechnicalReference.md` §12 참고.
> 
> SerializeField: _stageData, _mainImage, _skyImage, _undergroundImage (Image), _heightMargin=1.5, _minHeight=5000.
> 
> 메서드:
> - SetOriginY(mainAnchoredY) — Main 위치 + SnapSkyAndUnderToMain
> - AdjustHeights(buildingHeight) — Sky/Under sizeDelta.y 갱신 + Snap
> - SetShiftY(shift) — BackgroundGroup(= Main 부모) anchoredPosition.y
> - SetZoom(zoom) — BackgroundGroup localScale
> - GetMainImageHalfHeight()
> - AutoFindReferences()
> 
> Awake에 AutoFindReferences. Start에 LoadBackgroundsAsync(아래 5-2).

### 5-2. LoadBackgroundsAsync

**Claude 명령 예시**:
> BackgroundView에 LoadBackgroundsAsync. 라벨 "Background"로 sprite 일괄 로드. sprite name 매칭:
> - `{StageID:D3}_Background_Main_` → mainImage.sprite
> - `{StageID:D3}_Background_Sky_` → skyImage.sprite
> - `{StageID:D3}_Background_Under_` → undergroundImage.sprite

### 5-3. Stage_001 prefab에 BackgroundGroup 추가

**Claude 명령 예시**:
> Stage_001 prefab에 BackgroundGroup GameObject 추가 (SafeArea 자식, BuildingContainer 형제). 그 안에 Background_Sky / Background_Main / Background_Underground 3 GameObject (각 Image 컴포넌트). pivot:
> - Sky: (0.5, 0)
> - Main: (0.5, 0.5)
> - Underground: (0.5, 1)
> 
> Stage_001 root에 BackgroundView 컴포넌트 부착. 필드 자동 연결되도록 BackgroundView Inspector에서 자식 Image 드래그(또는 AutoFindReferences 활용).

### 5-4. BuildingView에서 BackgroundView 연동

**Claude 명령 예시**:
> BuildingView에 `_backgroundView` SerializeField 추가. RenderBuilding 끝에 `_backgroundView.AdjustHeights(totalBuildingHeight)` 호출. Awake/PullValuesFromTowerOrigin에 backgroundView.SetOriginY(originY + bg.GetMainImageHalfHeight()) 호출.

### 5-5. TowerOrigin → BackgroundView 연동

**Claude 명령 예시**:
> TowerOrigin.ApplyToBuildingView 끝에 BackgroundView도 같이 찾아 SetOriginY 호출 (originY + mainHalfHeight).

**검증**:
- Background sprite가 빌딩 뒤에 표시
- Main bottom이 1F 바닥(Origin Y 라인)에 정렬
- Sky가 Main 위, Underground가 Main 아래
- Origin Y 슬라이더 움직이면 빌딩 + 배경이 같이 움직임 (한 몸)

---

## Phase 6: ShiftY 시스템 + 드래그

### 6-1. BuildingView ShiftY

**Claude 명령 예시**:
> BuildingView에 ShiftY 시스템 추가:
> - public float ShiftY => _cubeContainer.anchoredPosition.y
> - public SetShiftY(float shift) — cubeContainer.anchoredPosition.y = shift, BackgroundView.SetShiftY(shift)
> - public CurrentBuildingOriginY => _originY + ShiftY
> - public GetTotalBuildingHeight() — Floors.Count × stackHeight (또는 정확한 계산)

### 6-2. BuildingDragController

**Claude 명령 예시**:
> `Assets/Application/TopTower/Scripts/Building/BuildingDragController.cs` 작성. `Docs/TopTower_TechnicalReference.md` §11 참고.
> 
> MonoBehaviour with IBeginDragHandler, IDragHandler, IEndDragHandler. RequireComponent(RectTransform).
> 
> SerializeField: _buildingView, _wheelZoomStep=0.1, _pinchZoomSensitivity=1, _rubberBandResistance=0.3 Range(0.05,1), _reboundDuration=0.25.
> 
> Awake: viewportRt = transform as RT; buildingView 자동 탐색.
> 
> OnDrag: 가로 무시. delta.y만 사용. CalculateLimits로 한계 산출. 한계 초과 시 고무줄 감쇠. _buildingView.SetShiftY 호출.
> 
> OnEndDrag: ReboundIfOutOfLimitsAsync (ease-out cubic, 0.25s).
> 
> CalculateLimits: zoom 반영. (zoom은 아직 1 고정이지만 코드는 미리 작성.)

### 6-3. Stage_001 prefab에 SafeArea Image + DragController

**Claude 명령 예시**:
> Stage_001 prefab의 SafeArea GameObject에 다음 추가:
> - CanvasRenderer
> - Image (color alpha=0 투명, raycastTarget=true)
> - BuildingDragController 컴포넌트 (BuildingView 필드는 Stage_001 root의 BuildingView로 연결)

**검증**:
- Play 모드에서 마우스 드래그 / 모바일 1손가락 드래그로 빌딩 + 배경이 같이 세로 이동
- 한계 도달 시 끈적이는 고무줄 효과
- 손 떼면 한계 안으로 부드럽게 복귀

---

## Phase 7: HomeButton

### 7-1. BuildingView에 HomeY + MoveToHomeY

**Claude 명령 예시**:
> BuildingView에 추가:
> - SerializeField `_homeButton` (Button)
> - Awake에 `_homeButton?.onClick.AddListener(MoveToHomeY)`, OnDestroy에 RemoveListener
> - public MoveToHomeY: 현재 ShiftY → _homeY로 0.3초 ease-out cubic 보간 (AnimateShiftAsync 헬퍼)
> - `Docs/TopTower_TechnicalReference.md` §4.4 참고. **OriginY 끼우지 말 것** — _homeY 자체가 시프트 목표.

### 7-2. Stage_001 prefab에 HomeButton 추가

**Claude 명령 예시**:
> Stage_001 prefab의 SafeArea 자식에 BuildingSkill > Home > HomeButton 계층 생성. HomeButton GameObject에 RectTransform(150×150, anchored 우상단) + Image(아이콘 sprite) + Button 컴포넌트. **Image의 raycastTarget=true 필수** (안 그러면 클릭 안 됨).
> 
> Stage_001 root의 BuildingView 컴포넌트의 Home Button 필드에 HomeButton 드래그 연결.

**검증**:
- HomeButton 클릭하면 빌딩이 Home Y 위치로 부드럽게 이동
- Home Y 슬라이더 변경 시 도착점 즉시 갱신

---

## Phase 8: Zoom

### 8-1. TowerOrigin에 Zoom 한계 추가

**Claude 명령 예시**:
> TowerOrigin에 SerializeField 추가: _zoomMin Range(0.1, 1) default 0.5, _zoomMax Range(1, 5) default 2. public 속성 ZoomMin, ZoomMax. ApplyToBuildingView에서 buildingView.SetZoomLimits(min, max) 호출.

### 8-2. BuildingView Zoom

**Claude 명령 예시**:
> BuildingView에 추가:
> - private _zoomMin, _zoomMax, _currentZoom = 1
> - public SetZoomLimits(min, max) — clamp 후 SetZoom 재호출 가능
> - public SetZoom(zoom) — Clamp + cubeContainer.localScale + BackgroundView.SetZoom
> - public property CurrentZoom, ZoomMin, ZoomMax

### 8-3. BuildingDragController Zoom 입력

**Claude 명령 예시**:
> BuildingDragController.Update에 추가:
> - Input.touchCount == 2이면 HandlePinchZoom (`Docs/TopTower_TechnicalReference.md` §11.5)
> - 드래그 중 아니면 Input.mouseScrollDelta.y 폴링 → ApplyZoomMultiplier(1 + wheel * _wheelZoomStep)
> - ApplyZoomMultiplier(factor) → buildingView.SetZoom(currentZoom * factor)
> 
> OnDrag 진입 시 Input.touchCount >= 2이면 early return (핀치 우선).
> 
> CalculateLimits에 zoom 반영: scaledOriginY = originY × zoom, scaledTotalHeight = totalHeight × zoom.

**검증**:
- PC 마우스 휠 → 빌딩 + 배경 확대/축소 (화면 중앙 기준)
- 모바일 두 손가락 핀치 → 동일
- 한계값에서 clamp
- 줌 인 상태에서 드래그하면 빌딩+배경 확대된 채로 이동

---

## Phase 9: 층 라벨 (TMP)

### 9-1. PlaceFloorLabels 기본

**Claude 명령 예시**:
> BuildingView에 PlaceFloorLabels. `Docs/TopTower_TechnicalReference.md` §5 + Phase 후반 추가 참고.
> 
> LoadSpritesThenLabelsAsync의 await 완료 후 호출. 각 floor에 라벨 GameObject 생성:
> - 이름: "FloorLabel_F{FloorIndex}"
> - typeof(RectTransform) + typeof(TextMeshProUGUI)
> - 부모: cubeContainer
> - anchor (0, 1), pivot (0, 1)
> - anchoredPosition (labelCol(=2) × cubeWidth, ceilingTopY)
> - sizeDelta (cubeWidth × 2, ceilingHeight)
> - text: "▼ {N}F" 또는 "▼ B{N}"
> 
> Edit Mode 생성 시 hideFlags = HideAndDontSave.
> 
> ApplyLabelProperties 헬퍼로 TMP 속성 적용.

### 9-2. ApplyLabelProperties (vanilla 단순화)

**Claude 명령 예시**:
> ApplyLabelProperties(TextMeshProUGUI text, string label):
> - text.text, text.font(_labelFont ?? TMP_Settings.defaultFontAsset), text.color, text.fontSize, text.alignment=MidlineLeft, enableAutoSizing=false, fontStyle 처리는 font asset에 맡김
> - polyfont 없으면 text.enabled=false (안전책)
> - text.raycastTarget = false
> 
> **이전 프로젝트의 EnsureTextAnimatorFieldsInitialized + 리플렉션 코드는 모두 제외** — vanilla TMP에서는 불필요. `Docs/TopTower_TechnicalReference.md` §17 + §18 참고.

### 9-3. BuildingView Inspector 노출 — 라벨 설정

**Claude 명령 예시**:
> BuildingView에 SerializeField 추가:
> 
> [Header("층 라벨")]
> - _labelFont: TMP_FontAsset
> - _labelColor: Color (default white)
> - _labelFontSize: float (default 32)
> - _labelUseStroke: bool (default false)
> - _labelStrokeColor: Color (default black)
> - _labelStrokeThickness: Range(0, 0.2) float (default 0.1)
> 
> ApplyLabelProperties 내부에서 외곽선 처리:
> ```
> var mat = text.fontMaterial;
> if (mat != null) {
>   float width = _labelUseStroke ? _labelStrokeThickness : 0f;
>   mat.SetColor(ShaderUtilities.ID_OutlineColor, _labelStrokeColor);
>   mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
>   mat.SetFloat(ShaderUtilities.ID_FaceDilate, width);  // 외부 기준 stroke
>   text.UpdateMeshPadding();
> }
> text.SetAllDirty();
> ```

**검증**:
- 각 floor 위에 "▼ 1F" 같은 라벨 표시
- BuildingView Inspector의 Font 필드에 TMP_FontAsset 드래그하면 폰트 변경
- Color, Font Size 만지면 즉시 반영
- Use Stroke 토글하면 외곽선 on/off

---

## Phase 10: 받침박스 (Backdrop)

### 10-1. SerializeField 추가

**Claude 명령 예시**:
> BuildingView에 SerializeField 추가:
> 
> [Header("층 라벨 - 받침박스")]
> - _labelBackdrop: bool default false
> - _labelBackdropColor: Color default (0, 0, 0, 0.5)
> - _labelBackdropSize: Vector2 default (2, 1)
> - _labelBackdropOffset: Vector2 default (0, 0)

### 10-2. CreateFloorLabelBackdrop + ApplyBackdropTransform

**Claude 명령 예시**:
> BuildingView에 메서드 추가. `Docs/TopTower_TechnicalReference.md` 받침박스 관련 부분 참고.
> 
> CreateFloorLabelBackdrop(floorIndex, labelX, ceilingTopY, cubeWidth, ceilingHeight, labelText):
> - GameObject "FloorLabelBackdrop_F{N}" 생성 (RT + Image)
> - 부모 cubeContainer, hideFlags Edit 모드 처리
> - anchor (0, 1), pivot (0.5, 0.5) — **중심 pivot 필수**
> - ApplyBackdropTransform 호출
> - Image color = _labelBackdropColor, raycastTarget=false
> 
> ApplyBackdropTransform(rt, labelText, labelX, ceilingTopY, cubeWidth, ceilingHeight):
> - pivot이 (0.5, 0.5) 아니면 강제 동기화
> - 텍스트 X 중심 = labelText.GetPreferredValues(labelText.text).x × 0.5 + labelX (텍스트 폰트 null이면 라벨 rect 중심 fallback)
>   ⚠ **`ForceMeshUpdate()` 호출 금지 — `GetPreferredValues`만 사용**
> - 텍스트 Y 중심 = ceilingTopY - ceilingHeight × 0.5
> - anchoredPosition = (textCenterX + offset.x, textCenterY + offset.y)
> - sizeDelta = (cubeWidth × _labelBackdropSize.x, ceilingHeight × _labelBackdropSize.y)
> 
> PlaceFloorLabels에서 라벨 GameObject 생성 후 _labelBackdrop이면 CreateFloorLabelBackdrop 호출. 그 후 backdrop의 sibling index를 라벨 직전으로 이동 (라벨이 위에 렌더되도록).

**검증**:
- Use Backdrop 체크하면 라벨 텍스트 뒤에 어두운 박스 표시
- Backdrop Color 알파 조절로 투명도 변경
- Backdrop Size 변경 시 라벨 텍스트 중심 기준으로 대칭 확장
- Backdrop Offset으로 미세 위치 조정

---

## Phase 11: OnValidate 패턴 (in-place 갱신)

### 11-1. BuildingView.OnValidate

**Claude 명령 예시**:
> BuildingView에 #if UNITY_EDITOR OnValidate 추가. `Docs/TopTower_TechnicalReference.md` §8 참고.
> 
> EditorApplication.delayCall로 UpdateAllFloorLabels 예약.
> 
> UpdateAllFloorLabels:
> - cubeContainer 자식 순회
> - FloorLabel_* 이름이면 TMP 컴포넌트 ApplyLabelProperties로 갱신
> - 같은 라벨의 받침박스 SyncBackdropForLabel로 동기화
> 
> SyncBackdropForLabel(labelChild, cubeWidth, ceilingHeight):
> - 같은 floor index의 FloorLabelBackdrop_F{N} 찾기
> - _labelBackdrop true + 없으면 생성, 있으면 색/위치/크기 갱신
> - _labelBackdrop false + 있으면 destroy

**검증**:
- Inspector에서 라벨 색/크기/외곽선/받침박스 옵션 만질 때마다 즉시 반영
- 빌딩 sprite는 재생성되지 않음 (Empty 모듈 random 결과 보존)

---

## Phase 12: TopTowerIngame 진입점

### 12-1. vanilla 단순 버전

**Claude 명령 예시**:
> `Assets/Application/TopTower/Scripts/Ingame/TopTowerIngame.cs` 작성. `Docs/TopTower_TechnicalReference.md` §14 + `Docs/TopTower_GameSpec.md` §12.4 골격 참고. **회사 IngameBase 상속 X, 단순 MonoBehaviour.**
> 
> ```csharp
> public class TopTowerIngame : MonoBehaviour {
>     [SerializeField] string _stageAddress = "Assets/Application/Bundles/UI/Prefab/Stage/Stage_001.prefab";
>     async UniTaskVoid Start() {
>         var handle = Addressables.LoadAssetAsync<GameObject>(_stageAddress);
>         await handle.Task;
>         if (handle.Status == AsyncOperationStatus.Succeeded)
>             Instantiate(handle.Result);
>     }
> }
> ```
> 
> Stage_001.prefab을 Addressables에 등록 (StageSpritesSyncTool 또는 수동).
> 
> IngameScene에 TopTowerIngame GameObject 추가 (또는 기존 TopTower 오브젝트에 같이 부착).

### 12-2. PullValuesFromTowerOrigin (BuildingView.Awake)

**Claude 명령 예시**:
> BuildingView.Awake 끝에 PullValuesFromTowerOrigin 호출. `Docs/TopTower_TechnicalReference.md` §3의 Awake 흐름 참고. FindObjectOfType<TowerOrigin>으로 찾아 SetOriginY/SetHomeY/SetShowOriginGuideLine/SetShowHomeGuideLine/SetZoomLimits 호출. BackgroundView도 같이 SetOriginY(originY + mainHalfHeight).

**검증**:
- Play 시 IngameScene이 자동으로 Stage_001 prefab 인스턴스화
- TowerOrigin 값이 BuildingView/BackgroundView에 즉시 적용
- 빈 씬에서도 동작

---

## Phase 13: 마무리/검증

### 13-1. EventSystem 정리

IngameScene에 EventSystem이 **정확히 1개**만 있는지 확인. Hierarchy에서 EventSystem GameObject가 있어야 클릭/드래그 이벤트 작동.

vanilla는 회사 `__RootCanvas__` 같은 전역 prefab이 없으므로, IngameScene에 GameObject > UI > Event System 한 번 추가하면 끝 (StandaloneInputModule 자동 부착).

### 13-2. 모바일 touch 입력 검증

- Unity Editor의 **Device Simulator** 또는 실기기 빌드에서 핀치 줌 동작 확인
- `Input.touchCount`, `Input.GetTouch` 사용 — old Input Manager 기반
- 만약 **Active Input Handling**이 "Input System Package (New)"로 설정되어 있다면 → Project Settings > Player > Configuration > Active Input Handling을 **"Both"** 또는 "Input Manager (Old)"로 변경 (현 코드는 Old 사용)

### 13-3. Sync Addressables 한 번 더 실행

새 sprite 추가/이름 변경 후엔 `Tools > Top Tower > Sync Addressables` 매번 실행.

### 13-4. 검증 체크리스트

`Docs/TopTower_TechnicalReference.md` §17의 함정 표 + `Docs/TopTower_GameSpec.md` §13의 검증 순서 참고. 모든 항목 확인.

---

## 부록 A: 단계별 의존 그래프

```
0. 환경 준비 (패키지, 폴더, 자산 복사)
   │
1. 데이터 (StageData)
   │
2. 기본 렌더링 (cube placeholder)
   │
3. TowerOrigin + 가이드 라인
   │  ┌──────────────────┐
   ├─►│ 4. Sprite 로드   │
   │  └──────────────────┘
   │
   ▼
5. 배경 시스템 ←──┐
   │             │
6. ShiftY + 드래그 ─┤  (배경도 같이 이동해야 의미 있음)
   │             │
7. HomeButton    │
   │             │
8. Zoom ────────┘  (배경도 같이 줌)
   │
9. 층 라벨 (TMP)
   │
10. 받침박스
   │
11. OnValidate 패턴
   │
12. TopTowerIngame 진입점
   │
13. 마무리/검증
```

각 단계는 위 의존 그래프 순서로. 5는 4 끝나면 평행 진행 가능.

---

## 부록 B: 단계별 Claude 명령 템플릿

새 프로젝트의 Claude에게 명령할 때 공통 prefix:

> 이 프로젝트는 vanilla Unity 2D URP. RootBox/회사 커스텀 의존 X.
> 
> 사양/분류 정본 — 작업 시 매번 참조:
> - `Docs/Glossary.md` (용어, sprite naming, 층 라벨 표기)
> - `Docs/Groups.md`, `Docs/Zones.md`, `Docs/Modules.md` (분류축 + 모듈 카탈로그)
> - `Docs/TopTower_GameSpec.md` (사양)
> - `Docs/TopTower_TechnicalReference.md` (동작)
> - `Docs/TopTower_BuildPlan.md` (절차)
> 
> 다음 단계 진행: [Phase N-M 또는 단계 번호].
> 해당 단계의 검증 조건 충족 후 멈춰서 보고.

권장: 한 번에 한 Phase만 명령. Phase 끝에 검증 → 다음 Phase. 한꺼번에 여러 Phase 시키면 디버깅 어려움.

---

## 부록 C: 자주 빠뜨리는 항목 체크

새 프로젝트 Claude가 자주 빠뜨릴 수 있는 점:

- [ ] **GraphicRaycaster** Stage 루트에 부착 (Canvas만으로는 클릭 안 됨)
- [ ] sprite/cube/text 등 모든 동적 생성물의 **`raycastTarget = false`** 명시 (안 그러면 드래그 차단)
- [ ] HomeButton의 Image는 **`raycastTarget = true`** (버튼은 받아야 함)
- [ ] Edit Mode 생성물은 **`hideFlags = HideAndDontSave`**
- [ ] Stage 루트 RT의 sizeDelta **(1080, 1920) 고정** + CanvasScaler **match=0** (가로 우선)
- [ ] Empty 모듈은 **StageCommon 라벨**, 나머지는 **Stage_NNN 라벨**
- [ ] 우측 외벽 미러는 **음수 scale 금지** — RenderTexture+Blit 방식 사용
- [ ] 가이드 라인 — 핑크는 cubeContainer 자식 / 연두는 그 **부모** 자식 (월드 고정)
- [ ] HomeButton 동작은 `_homeY - _originY` 같은 산식 **금지** — `_homeY` 그 자체로 시프트
- [ ] BuildingView.OnValidate는 RenderBuilding **호출 안 함** (Empty random 보존)
- [ ] 라벨 받침박스는 라벨보다 **먼저 sibling**에 위치 (뒤에 렌더)
- [ ] 받침박스 X 중심은 `GetPreferredValues(text)` 사용 (`ForceMeshUpdate` 금지 — NRE 위험)
- [ ] TowerOrigin은 `[ExecuteAlways]` + OnValidate(delayCall) 패턴

---

본 문서 + GameSpec + TechnicalReference 세 개로 새 프로젝트의 빌딩 시스템을 처음부터 동일하게 재구축할 수 있도록 설계.
