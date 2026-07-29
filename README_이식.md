# Top Tower 코어 이식 번들 (RootBox 탈피)

이 폴더는 기존 회사 프로젝트(`newbon_group3`)에서 **Top Tower 빌딩 코어**만 뽑아
새 vanilla Unity 프로젝트에 그대로 드롭인할 수 있게 정리한 번들이다.

- namespace: `NGFE.TopTower` → **`KS.TopTower`** (Editor 도구는 `KS.TopTower.EditorTools`)로 일괄 치환됨
- `TopTowerIngame.cs`: 회사 `IngameBase`/`UIProvider`/`DLogger` 의존 제거 → 단순 `MonoBehaviour`로 재작성
  (동작은 동일: Addressables로 Stage 프리팹 로드 → Instantiate → TowerOrigin 값 적용)
- 실행 구조: **빈 베이스 씬 → 스테이지 프리팹 동적 호출(Addressables)** — 기존과 동일 방식 유지
- 라벨 폰트(`07LightNovelPOP SDF`)는 제외 → 프리팹의 `_labelFont`는 null(TMP 기본 폰트 fallback)

번들 안의 `.cs`·에셋·프리팹은 모두 원본 **`.meta`(GUID)** 를 그대로 가져왔으므로,
프리팹의 스크립트/스프라이트 참조가 새 프로젝트에서도 끊기지 않는다.

---

## 포함물

```
Assets/Application/TopTower/          코어 (스크립트 9 + 이미지 + LevelData)
  Scripts/Building/ (BuildingView, BackgroundView, BuildingDragController, BackgroundScrollSync)
  Scripts/Data/     (StageData)
  Scripts/Ingame/   (TowerOrigin, TopTowerIngame ← vanilla)
  Editor/           (StageBuilderTool, StageSpritesSyncTool)
  Image/Background, Image/Module/{Stage_001, StageCommon}
  Image/UI/         (IMG_HomeIcon_Bottom, IMG_GradientTop/Bottom ← 프리팹이 쓰는 UI 스프라이트)
  LevelData/Stage_001.asset
Assets/Application/Bundles/UI/Prefab/Stage/Stage_001.prefab   빌딩 시각 컨테이너
Docs/                                  기획/사양/기술 문서 8종 + ClaudeCode 환경
CLAUDE.md                              새 프로젝트용 vanilla 규칙
README_이식.md                         (본 문서)
```

씬(`IngameScene`)은 의도적으로 포함하지 않음 — 원본 씬은 RootBox 범벅이라
새 프로젝트에서 최소 구성으로 새로 만든다 (아래 4단계).

---

## 이식 절차

### 1. 새 프로젝트 생성 + 패키지 (Docs/TopTower_BuildPlan.md §0 참조)

- Unity Hub → **2D (URP)** 템플릿으로 새 프로젝트 (버전은 원본과 동일 권장)
- Package Manager 설치:
  - `com.unity.addressables`
  - `com.unity.textmeshpro` (TMP)
  - UniTask (Git URL): `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- **Window > TextMeshPro > Import TMP Essential Resources**
- **Window > Asset Management > Addressables > Groups** → Create Addressables Settings
- (핀치 줌은 구 Input 사용) Project Settings > Player > Active Input Handling = **Both** 또는 **Input Manager (Old)**

### 2. 드롭인

이 번들의 **`Assets/`, `Docs/`, `CLAUDE.md`** 를 새 프로젝트 루트에 통째로 복사.
`.meta`까지 같이 복사할 것 (이미 포함되어 있음). Unity가 임포트하면 컴파일 에러 0개여야 정상.

### 3. Addressables 등록

- 메뉴 **Tools > Top Tower > Sync Addressables** 실행
  → `Assets/Application/TopTower/` 하위 스프라이트가 폴더명 라벨(`Stage_001`, `StageCommon`, `Background`)로 자동 등록
- **Stage_001.prefab 수동 등록**: 프리팹 선택 → Inspector에서 Addressable 체크 →
  주소를 정확히 `Assets/Application/Bundles/UI/Prefab/Stage/Stage_001.prefab` 로 설정
  (Sync 도구는 `TopTower/` 폴더만 스캔하므로 이 프리팹은 수동. `TopTowerIngame._stageAddress` 기본값과 일치해야 함)

### 4. 씬 구성 (베이스 씬 + 프리팹 호출 구조)

새 씬 `IngameScene` 생성 후:
- **GameObject > UI > Event System** 1개 (클릭/드래그 필수. 정확히 1개만)
- 빈 GameObject `TopTower` 생성 → 컴포넌트 2개 부착:
  - `TowerOrigin` (Origin Y/Home Y/가이드/줌 한계 통제)
  - `TopTowerIngame` (Play 시 Stage 프리팹 로드 진입점)

Play → `TopTowerIngame`이 Stage_001.prefab을 Addressables로 로드·인스턴스화하고
`TowerOrigin` 값을 빌딩/배경에 적용한다.

### 5. 폰트 (선택)

층 라벨은 현재 TMP 기본 폰트(LiberationSans)로 표시 → `▼`·한글이 깨질 수 있음.
한글 폰트가 필요하면 TMP Font Asset을 만들어 Stage_001 프리팹의 BuildingView `_labelFont`에 지정.

---

## 검증 체크리스트

- [ ] 임포트 후 콘솔 컴파일 에러 0개
- [ ] Edit 모드에서 Stage_001.prefab을 씬에 드래그 → 빌딩 큐브/외벽/엘베/모듈 미리보기 표시
- [ ] Play → 자동으로 빌딩 + 배경 표시, 1F 바닥이 Origin Y 라인에 정렬
- [ ] 드래그(세로 스크롤) + 마우스 휠/핀치 줌 동작
- [ ] HomeButton 클릭 시 Home Y로 부드럽게 이동
- [ ] 층 라벨 표시 (폰트는 5번 참조)

상세 동작/함정: `Docs/TopTower_TechnicalReference.md` §17, `Docs/TopTower_BuildPlan.md` 부록 C.
