# Top Tower — 기술 레퍼런스 / 코드 동작 가이드

> **본 문서 범위**: Top Tower의 **빌딩 시각 시스템** 전용. 메커닉/UI/광고 등은 별도.

**문서 목적**: 현재 코드가 **어떤 순서로 어떤 일을 하는지** 한 곳에서 파악. 기획서(`TopTower_GameSpec.md`)는 "무엇을 만드는가"의 사양, 본 문서는 "어떻게 작동하는가"의 동작 명세.

vanilla 이식 시 본 문서의 흐름을 그대로 옮기면 같은 동작이 나오도록 작성.

---

## 1. 파일 카탈로그

```
Assets/Application/TopTower/
├── Editor/
│   ├── StageBuilderTool.cs              ← Stage 자동 생성 도구 (메뉴)
│   └── StageSpritesSyncTool.cs          ← Addressables 자동 등록 도구 (메뉴)
└── Scripts/
    ├── Building/
    │   ├── BuildingView.cs              ← 핵심. 큐브 그리드 렌더 + 시프트 + 줌
    │   ├── BackgroundView.cs            ← 배경 sky/main/under 관리
    │   └── BuildingDragController.cs    ← 입력 (드래그/휠/핀치)
    ├── Data/
    │   └── StageData.cs                 ← ScriptableObject + enums
    └── Ingame/
        ├── TopTowerIngame.cs            ← Addressables로 stage prefab 로드 + 초기 설정 (회사 IngameBase 상속)
        └── TowerOrigin.cs               ← IngameScene의 단일 통제점 (slider/toggle)
```

추가 영향 파일:
- `Assets/Application/Bundles/UI/Prefab/Stage/Stage_001.prefab` — 빌딩의 시각적 컨테이너
- `Assets/Application/Bundles/Scenes/IngameScene.unity` — TopTower GameObject + TowerOrigin
- `Assets/Application/TopTower/LevelData/Stage_001.asset` — StageData ScriptableObject

---

## 2. 파일별 책임 (한 줄 요약)

| 파일 | 책임 | 외부 호출자 |
|---|---|---|
| `BuildingView` | 빌딩 큐브/모듈/외벽/구분줄/라벨/받침박스/뿌리/가이드 라인 전부 렌더링. ShiftY와 Zoom 적용. | TopTowerIngame, TowerOrigin, HomeButton |
| `BackgroundView` | 배경 3종(sky/main/under) sprite 로드 + 위치 정렬 + 시프트/줌 동기화. | BuildingView (RenderBuilding 끝에서) |
| `BuildingDragController` | EventSystem 통한 드래그(IDrag*) + 휠/핀치(Update). BuildingView의 ShiftY/Zoom 메서드 호출. | (Unity input system이 자동 호출) |
| `TowerOrigin` | IngameScene에 살면서 OriginY/HomeY/ShowGuide/Zoom 한계 값을 보유. 변경 시 BuildingView에 push. | (Inspector 슬라이더 — OnEnable/OnValidate가 자동 호출) |
| `TopTowerIngame` | Play 시 stage prefab 로드, TowerOrigin 값을 BuildingView/BackgroundView에 1회 적용. | IngameBase 시스템 |
| `StageData` | Floor 배열 + ElevatorPosition. CubeType/Zone/ElevatorPosition enums. | BuildingView가 읽음 |

---

## 3. Stage 생성에서 첫 프레임까지의 호출 흐름

Play 모드를 가정. (Edit 모드는 §10에 별도 정리.)

```
[Unity Scene Load: IngameScene]
        │
        ▼
1. TowerOrigin.Awake/OnEnable                 ← [ExecuteAlways]
        │  → ApplyToBuildingView() 시도
        │  → 그러나 아직 Stage_001이 씬에 없음 (FindObjectOfType<BuildingView>() == null)
        │  → 그냥 return
        │
2. TopTowerIngame.InitializeCore (회사 SDK가 호출)
        │  await _uiProvider.ActivateManagedUI<UIIngamePanel>()
        │  await LoadStagePrefab("Stage_001.prefab")
        │        │
        │        ▼
3.      Addressables.LoadAssetAsync<GameObject>(addr)
        │  → handle.Result로 Stage_001 prefab 받음
        │  → Instantiate(handle.Result)      ← 빈 부모로 instantiate (Canvas 자체 가짐)
        │
        ▼
4. Stage_001 인스턴스의 컴포넌트들 Awake/Start
        │
        ├── BuildingView.Awake
        │       EnsureCanvasSetup()                ← Canvas 모드/Scaler 보정
        │       EnsureScales()                     ← localScale 0 보정
        │       _homeButton.onClick.AddListener(MoveToHomeY)
        │       PullValuesFromTowerOrigin()        ← TowerOrigin 찾아 OriginY/HomeY/Zoom한계 적용
        │
        ├── BuildingView.Start
        │       RenderBuilding()                   ← (§5 상세)
        │
        ├── BackgroundView.Awake
        │       AutoFindReferences()
        │
        ├── BackgroundView.Start (async)
        │       await LoadBackgroundsAsync()       ← (§6 상세)
        │
        └── BuildingDragController.Awake
                _viewportRt = transform as RectTransform
                _buildingView = FindObjectOfType<BuildingView>()
        │
        ▼
5. TopTowerIngame.ApplyOriginY(stageInstance)      ← prefab 로드 직후 명시 push
        │  TowerOrigin 찾기
        │  buildingView.SetShowOriginGuideLine(showOrigin)
        │  buildingView.SetShowHomeGuideLine(showHome)
        │  buildingView.SetHomeY(homeY)
        │  buildingView.SetOriginY(originY)         ← 변경 시 RenderBuilding 재호출 (Mathf.Approximately 체크)
        │  backgroundView.SetOriginY(originY + mainHalfHeight)
        │
        ▼
6. (다음 프레임) Canvas Update
        BuildingDragController.Update              ← 매 프레임 — 휠/터치 폴링
        TMP labels render via Canvas Rebuild
        BackgroundView.Start의 await 끝 → sprite 적용
```

핵심 포인트:
- **두 군데서 TowerOrigin 값을 BuildingView에 푸시**: BuildingView.Awake의 `PullValuesFromTowerOrigin` + TopTowerIngame.ApplyOriginY. 어느 쪽이든 동작하게 이중화.
- **SetOriginY는 값이 같으면 RenderBuilding 안 함** (Mathf.Approximately) → 무용 재생성 방지

---

## 4. 핵심 클래스 — BuildingView 상세

### 4.1 필드 분류

```
[데이터]
  _stageData: StageData (Inspector 드래그)

[참조 — 자동 탐색 가능]
  _cubeContainer: RectTransform     ← 빌딩 sprite들의 부모
  _backgroundView: BackgroundView
  _homeButton: Button

[빌딩 크기]
  _gridWidth = 9
  _cubeAspectRatio = 1.3
  _ceilingHeightRatio = 1/3

[층 라벨 / 받침박스]
  _labelFont, _labelColor, _labelFontSize, _labelUseStroke, _labelStrokeColor, _labelStrokeThickness
  _labelBackdrop, _labelBackdropColor, _labelBackdropSize, _labelBackdropOffset

[런타임 상태 — Inspector에 안 보임]
  _originY = 0          ← TowerOrigin이 SetOriginY로 설정
  _homeY = 0            ← TowerOrigin
  _showOriginGuideLine, _showHomeGuideLine
  _originGuideLineGO, _homeGuideLineGO  ← 캐시된 가이드 라인 GameObject
  _currentZoom = 1f, _zoomMin = 0.5f, _zoomMax = 2f

[렌더 시 계산]
  _currentYOffset       ← RenderBuilding 시점에 결정. 모든 cube 배치가 이 값 참조.
  _retryCount           ← cubeContainer 폭 0일 때 다음 프레임 재시도용
```

### 4.2 RenderBuilding 진입점

호출 시점:
1. `Start()` — 첫 렌더
2. `SetStageData(data)` — stage 교체 시
3. `SetOriginY(y)` — 값 변경 감지 시

내부 흐름은 §5.

### 4.3 ShiftY / Zoom

```
ShiftY 변경:
  SetShiftY(shift)
    cubeContainer.anchoredPosition.y = shift
    _backgroundView.SetShiftY(shift)      ← 같이 이동 = 한 몸

Zoom 변경:
  SetZoom(zoom)
    Clamp(zoom, _zoomMin, _zoomMax)
    cubeContainer.localScale = (zoom, zoom, 1)
    _backgroundView.SetZoom(zoom)         ← 같이 줌 = 한 몸
```

### 4.4 MoveToHomeY (HomeButton)

```
public void MoveToHomeY()
{
    if (!Application.isPlaying) { SetShiftY(_homeY); return; }
    AnimateShiftAsync(_homeY, 0.3f).Forget();
}

private async UniTask AnimateShiftAsync(float endShift, float duration)
{
    float start = ShiftY;
    if (Mathf.Approximately(start, endShift)) return;
    float elapsed = 0f;
    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1 - Pow(1 - t, 3);       // ease-out cubic
        SetShiftY(Mathf.Lerp(start, endShift, eased));
        await UniTask.Yield(PlayerLoopTiming.Update);
    }
    SetShiftY(endShift);
}
```

**OriginY는 끼지 않음** — Home Y가 그 자체로 cubeContainer.anchoredPosition.y의 목표값.

---

## 5. RenderBuilding 상세 — 한 줄 한 줄

### 5.1 진입

```
1. _stageData / _cubeContainer / Floors null 체크 → 경고 후 return
2. Canvas.ForceUpdateCanvases()
   LayoutRebuilder.ForceRebuildLayoutImmediate(_cubeContainer)
3. cubeWidth = _cubeContainer.rect.width / _gridWidth
   cubeHeight = cubeWidth * _cubeAspectRatio
   ceilingHeight = cubeHeight * _ceilingHeightRatio
   stackHeight = cubeHeight + ceilingHeight
4. if (cubeWidth <= 0):
      retry < 10이면 다음 프레임에 재시도, 아니면 에러 로그 후 return
5. ClearCubes() → cubeContainer 모든 자식 destroy
6. sortedFloors = _stageData.Floors.OrderByDescending(f => f.FloorIndex)
```

### 5.2 yOffset 계산 (핵심)

빌딩 1F cube **bottom**이 cubeContainer 좌표상 절대 Y = `_originY` 위치에 오도록.

```
floor1Idx = sortedFloors.FindIndex(f => f.FloorIndex == 1)
           (없으면 가장 작은 양수의 index, 그것도 없으면 0)
halfHeight = _cubeContainer.rect.height * 0.5
_currentYOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight
```

**유도**: cube들은 anchor (0, 1)로 top-left 기준 위치됨. 1F cube의 top-left 기준 anchoredPosition.y:
```
cubeTopY = -floor1Idx * stackHeight - ceilingHeight + _currentYOffset
cubeBottomY = cubeTopY - cubeHeight = -(floor1Idx + 1) * stackHeight + _currentYOffset
```

cubeContainer 중심 좌표로 환산 (pivot 중심):
```
cube bottom in center coords = halfHeight + cubeBottomY
                             = halfHeight - (floor1Idx + 1) * stackHeight + _currentYOffset
```

이것이 `_originY`와 같아야 하므로:
```
_originY = halfHeight - (floor1Idx + 1) * stackHeight + _currentYOffset
_currentYOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight  ✓
```

### 5.3 큐브 placeholder 배치

```
for each (floor, floorIdx) in sortedFloors:
    for col in 0..8:
        type = floor.Cubes[col]
        CreateCubeImage(type, col, floorIdx, ...)
```

`CreateCubeImage` — anchor (0, 1) pivot (0, 1) 좌상단 기준, `raycastTarget=false`:
```
go.anchoredPosition = (col * cubeWidth, -floorIdx * stackHeight - ceilingHeight + _currentYOffset)
go.sizeDelta = (cubeWidth, cubeHeight)
go.color = Color.clear      ← 색 안 칠함. 실제 모양은 sprite 레이어가 덮음.
```

모든 cube는 투명 placeholder. 실제 sprite는 그 위/같은 자리에 별도 GameObject로.

### 5.4 BackgroundView 높이 조정

```
totalBuildingHeight = sortedFloors.Count * stackHeight
_backgroundView.AdjustHeights(totalBuildingHeight)
```

### 5.5 비동기 sprite 로드

```
LoadSpritesThenLabelsAsync(sortedFloors, cubeWidth, ...).Forget()
```

→ §6에서 상세.

> **동기/비동기 경계**: `RenderBuilding()` 자체는 동기 — 호출 직후 cube placeholder는 즉시 존재. sprite/라벨/받침박스는 `Forget()`된 fire-and-forget으로 몇 프레임 뒤 채워짐. **Play 첫 몇 ms엔 sprite 없는 placeholder만 보이는 게 정상** (디버깅 시 혼동 주의).

### 5.6 가이드 라인

```
UpdateOriginGuideLine()
UpdateHomeGuideLine()
```

Play 모드에선 항상 비활성 (메서드 내부에서 `Application.isPlaying`이면 early return + 라인 hide).

---

## 6. Sprite 로드 파이프라인 — LoadSpritesThenLabelsAsync

5개 작업이 병렬로 await, 끝나면 라벨 배치.

```
await UniTask.WhenAll(
    LoadAndPlaceEmptyModulesAsync,    ← Indoor 1×4 모듈 영역
    LoadAndPlaceWallsAsync,           ← Outdoor cube (_Wall_, _Gate_, _Underwall_)
    LoadAndPlaceElevatorAsync,        ← Elevator 1칸
    LoadAndPlaceCeilingsAsync,        ← Ceiling row 전체 + Bottom row
    LoadAndPlaceRootAsync             ← Bottom row 아래 단일 stretch
)
PlaceFloorLabels(sortedFloors, ...)   ← TMP 텍스트 + 받침박스
```

각 메서드 모두 `LoadSpritesByLabelAsync(label)` 헬퍼 사용:
- Play: Addressables.LoadAssetsAsync
- Edit: AssetDatabase 동기 로드 (Prefab Edit Mode 미리보기용)

### 6.1 LoadAndPlaceEmptyModulesAsync

라벨 `StageCommon` 로드 → `_Empty_` prefix + ID 1~20 필터.

```
ElevatorPosition별 모듈 영역:
  Left  → moduleStartCol = 3, moduleEndCol = 6  (D-E-F-G)
  Right → moduleStartCol = 2, moduleEndCol = 5  (C-D-E-F)
  Center → return (1×2 모듈 별도 카탈로그 — 미구현)

각 floor 순회:
  if 모듈 영역 [moduleStartCol..moduleEndCol]이 전부 Indoor:
    sprite = Random.Range(0, basicEmpties.Count)에서 하나
    CreateSpriteImage("EmptyModule_F{idx}", sprite,
                      moduleStartCol * cubeWidth,
                      -floorIdx * stackHeight - ceilingHeight + _currentYOffset,
                      moduleWidth, cubeHeight)
```

⚠ **Random.Range 호출 — 같은 stage라도 RenderBuilding 호출마다 다른 sprite 선택**. 그래서 토글 같은 미세 옵션 변경 시 RenderBuilding을 다시 부르지 않도록 주의 (cf. §8 OnValidate 패턴).

> **비결정성 메모**: 현재 Empty 모듈은 매 RenderBuilding마다 새로 random — 같은 stage를 두 번 로드해도 다른 모습. 추후 "stage 클리어 시 동일 모습 재현" 같은 요구가 생기면 시드 기반 결정성 random으로 전환 필요 (`Random.InitState(stageID)` 등).

### 6.2 LoadAndPlaceWallsAsync

라벨 `Stage_{NNN}` 로드 → 좌측 sprite 4종 매칭 + 우측은 미러 생성.

```
wallLeft     = sprites.First(s => s.name.Contains("_Wall_"))
underwallLeft = sprites.First(s => s.name.Contains("_Underwall_"))
gateLeft     = sprites.First(s => s.name.Contains("_Gate_"))
미러: CreateMirroredSprite(leftSprite) — §7

각 (floor, col) 순회 (Outdoor cube만):
  isFirstFloor = floor.FloorIndex == 1
  isBasement = floor.FloorIndex < 0
  useGate = isFirstFloor && gateLeft != null
  isRight = col > centerCol (=(gridWidth-1)/2 = 4)

  결정:
    useGate → Gate (좌/우)
    isBasement → Underwall (좌/우)
    else → Wall (좌/우)

  sprite null이면 skip.

  CreateSpriteImage("{Kind}_F{n}_C{col}", sprite,
                    col * cubeWidth,
                    -floorIdx * stackHeight - ceilingHeight + _currentYOffset,
                    cubeWidth, cubeHeight)
```

### 6.3 LoadAndPlaceElevatorAsync

라벨 `Stage_{NNN}`에서 `_Elevator_` 매칭. ElevatorPosition.col에 모든 floor 배치 (해당 셀이 Indoor일 때).

```
elevatorCol = Left:2 / Center:4 / Right:6
```

### 6.4 LoadAndPlaceCeilingsAsync

각 floor의 ceiling row 배치 + 최하 지하층 아래 Bottom row.

```
sprites = LoadSpritesByLabelAsync("Stage_{NNN}")
floorSprite       = "_Floor_"
wallfrLeft        = "_Wallfr_"  (우측 미러)
underwallfrLeft   = "_Underwallfr_"  (우측 미러)
bottomSprite      = "_Bottom_"

for each (floor, floorIdx):
    isBasement = floor.FloorIndex < 0
    ceilingTopY = -floorIdx * stackHeight + _currentYOffset

    for col in 0..8:
        type = floor.Cubes[col]
        if Indoor:
            floorSprite ? CreateSpriteImage(... cubeWidth, ceilingHeight)
                         : CreateColorImage(진파랑, ...)   ← fallback
        elif Outdoor:
            isRight = col > centerCol
            sprite = isBasement
                     ? (isRight ? underwallfrRight : underwallfrLeft)
                     : (isRight ? wallfrRight     : wallfrLeft)
            sprite ? CreateSpriteImage(...) : skip
        # Background는 안 그림

# Bottom row (최하 지하층 아래만)
deepest = sortedFloors.Last
if deepest.FloorIndex < 0:
    deepestIdx = sortedFloors.Count - 1
    bottomTopY = -(deepestIdx + 1) * stackHeight + _currentYOffset
    for col in 0..8:
        type = deepest.Cubes[col]
        Indoor + bottomSprite → CreateSpriteImage(...)
        Outdoor + underwallfrLeft/Right → CreateSpriteImage(...)
```

### 6.5 LoadAndPlaceRootAsync

```
deepest = sortedFloors.Last
if deepest == null || deepest.FloorIndex >= 0: return   # 지하층 없으면 skip

rootSprite = sprites.First(s => s.name.Contains("_Root_"))

deepestIdx = sortedFloors.Count - 1
rootTopY = -(deepestIdx + 1) * stackHeight - ceilingHeight + _currentYOffset
rootHeight = 2 * stackHeight
rootX = 1 * cubeWidth
rootWidth = 7 * cubeWidth

CreateSpriteImage("Root", rootSprite, rootX, rootTopY, rootWidth, rootHeight)
```

---

## 7. CreateMirroredSprite — 우측 외벽 미러 생성

음수 scale은 prefab edit mode에서 불안정 → **RenderTexture + Graphics.Blit** 방식으로 픽셀 반전한 새 Sprite 생성.

```
1. srcRect = source.rect (sprite atlas 내 영역)
2. RenderTexture rt = GetTemporary(w, h, 0, ARGB32)
3. RenderTexture.active = rt; GL.Clear(...)
4. UV X 반전을 위한 scale/offset 계산:
     u0 = srcRect.x / srcTex.width
     v0 = srcRect.y / srcTex.height
     uW = srcRect.width / srcTex.width
     vH = srcRect.height / srcTex.height
5. Graphics.Blit(srcTex, rt, new Vector2(-uW, vH), new Vector2(u0 + uW, v0))
                                                     ↑ 음수 X scale + offset 보정
6. newTex = new Texture2D(w, h, RGBA32, false)
   newTex.ReadPixels(rect, 0, 0); newTex.Apply()
   newTex.hideFlags = HideAndDontSave
7. RenderTexture.active = prev; ReleaseTemporary(rt)
8. pivotNormalized = (source.pivot.x / w, source.pivot.y / h)
   mirrored = Sprite.Create(newTex, rect, pivotNormalized, source.pixelsPerUnit)
   mirrored.hideFlags = HideAndDontSave
9. return mirrored
```

- 텍스처 Read/Write enabled 불필요 (RenderTexture 경유)
- 결과 sprite는 메모리 leak 방지 위해 HideAndDontSave
- prefab edit mode에서도 작동 (음수 scale 방식의 한계 회피)

---

## 8. Inspector OnValidate 패턴 — 빌딩 재생성 없이 미세 갱신

문제: 라벨 색/크기 같은 사소한 옵션 만질 때마다 RenderBuilding 부르면 Random Empty 모듈이 매번 재선택되어 시각이 흔들림.

해결: `BuildingView.OnValidate` (Editor 전용)가 RenderBuilding 안 부르고 **기존 라벨 GameObject만 in-place 갱신**.

```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this == null) return;
        UpdateAllFloorLabels();
    };
}

private void UpdateAllFloorLabels()
{
    if (_cubeContainer == null) return;
    cubeWidth/ceilingHeight 재계산 (받침박스 크기/위치 재산출용)

    for child in _cubeContainer:
        if child.name.StartsWith("FloorLabel_"):
            tmp = child.GetComponent<TextMeshProUGUI>()
            if tmp == null:
                # Legacy Text → TMP 마이그레이션 (옛 에디터 세션 잔재)
                ...
            else:
                ApplyLabelProperties(tmp, tmp.text)
            SyncBackdropForLabel(child, cubeWidth, ceilingHeight)
}
```

`SyncBackdropForLabel`은 `FloorLabelBackdrop_F{N}` GameObject를 찾아:
- `_labelBackdrop == true`: 없으면 생성, 있으면 색/위치/크기 갱신
- `_labelBackdrop == false`: 있으면 destroy

핵심: **빌딩 sprite는 건드리지 않음** → Empty 모듈 sprite random 결과 보존.

---

## 9. 가이드 라인 갱신 패턴

각 가이드 라인 메서드(`UpdateOriginGuideLine`, `UpdateHomeGuideLine`)는:
1. `_cubeContainer == null` → return
2. Play 모드 OR `_show*GuideLine == false` → 라인 있으면 hide, return
3. 캐시 GameObject 없으면 생성 (HideAndDontSave 플래그)
4. anchoredPosition 갱신, SetAsLastSibling

핑크 라인:
- 부모: `_cubeContainer`
- anchoredPosition.y = `_originY`
- → 빌딩과 같이 이동 (1F 바닥을 따라감)

연두 라인:
- 부모: `_cubeContainer.parent` (RT) — **빌딩과 분리된 정적 부모**
- anchoredPosition.y = `_homeY`
- → 빌딩이 시프트되어도 라인은 월드 고정 (HomeY 진짜 도착점 역할)

토글 on/off 시 라인 GameObject는 **재생성 안 함, 캐시 재활성/비활성만** → 빌딩 sprite random 결과 보호.

---

## 10. Edit 모드 동작

`BuildingView` 클래스에 `[ExecuteAlways]` 부착.

```
[Prefab을 IngameScene Hierarchy에 드래그]
        │
        ▼
1. Stage_001 컴포넌트들 Awake (Edit 모드도 호출됨)
        │
        ├── BuildingView.Awake
        │       EnsureCanvasSetup, EnsureScales, button 리스너
        │       PullValuesFromTowerOrigin()   ← TowerOrigin 찾아 값 즉시 적용
        │
        └── BackgroundView.Awake
                AutoFindReferences()

2. BuildingView.Start → RenderBuilding
   (BackgroundView.Start의 async는 Edit Mode에선 Editor 코루틴 처리)

3. 사용자가 TowerOrigin Inspector 만짐
   → TowerOrigin.OnValidate (delayCall)
   → ApplyToBuildingView
   → buildingView.SetOriginY(...)  ← 변경 시 RenderBuilding 재호출
```

Edit 모드 sprite 로드:
- Addressables는 Edit Mode에서 동기 로드가 불안정 → `LoadSpritesByLabelEditor` 헬퍼가 `AddressableAssetSettings`에서 entry 직접 조회 + `AssetDatabase.LoadAssetAtPath` 동기 로드.

HideAndDontSave:
- Edit 모드에서 생성한 모든 sprite/cube GameObject는 `hideFlags = HideAndDontSave` → prefab 저장에 포함되지 않고 Hierarchy에 안 보임.
- Play 시작 시 ClearCubes에서 destroy되고 새로 만들어짐.

---

## 11. BuildingDragController 입력 흐름

### 11.1 Update (매 프레임)

```
if (_buildingView == null) return

# 1. 핀치 우선 (모바일)
if (Input.touchCount == 2):
    HandlePinchZoom()
    return
else:
    _previousPinchDistance = 0

# 2. 휠 (PC) — 드래그 중엔 무시
if (_isDragging) return
wheel = Input.mouseScrollDelta.y
if (≈0) return
factor = 1 + wheel * _wheelZoomStep      # ±0.1 per notch
ApplyZoomMultiplier(factor)              # _buildingView.SetZoom(currentZoom * factor)
```

### 11.2 IBeginDrag / IDrag / IEndDrag

```
OnBeginDrag → _isDragging = true

OnDrag(eventData):
    if (Input.touchCount >= 2) return   # 핀치 중 드래그 무시

    deltaY = eventData.delta.y
    (minShift, maxShift) = CalculateLimits()
    newShift = currentShift + deltaY

    # 한계 초과 시 고무줄
    if (newShift > maxShift):
        newShift = maxShift + (over) * 0.3
    elif (newShift < minShift):
        newShift = minShift - (over) * 0.3

    _buildingView.SetShiftY(newShift)

OnEndDrag:
    _isDragging = false
    ReboundIfOutOfLimitsAsync().Forget()
```

### 11.3 ReboundIfOutOfLimitsAsync

```
(minShift, maxShift) = CalculateLimits()
target = Clamp(currentShift, min, max)
if (≈current) return

start = currentShift
elapsed = 0
while (elapsed < 0.25):
    if (_isDragging) return       # 다시 드래그하면 취소
    elapsed += deltaTime
    t = Clamp01(elapsed / 0.25)
    eased = 1 - Pow(1 - t, 3)
    SetShiftY(Lerp(start, target, eased))
    await Yield(Update)
SetShiftY(target)
```

### 11.4 CalculateLimits (zoom 반영)

```
zoom = _buildingView.CurrentZoom
scaledOriginY    = _buildingView.OriginY * zoom
scaledTotalHeight = _buildingView.GetTotalBuildingHeight() * zoom
halfViewport     = _viewportRt.rect.height * 0.5

maxShift = halfViewport - scaledOriginY                     # 1F bottom이 viewport top까지
minShift = halfViewport - scaledOriginY - scaledTotalHeight # 빌딩 top이 viewport bottom까지

if maxShift < minShift:
    mid = (maxShift + minShift) / 2
    return (mid, mid)               # 빌딩이 viewport보다 작음 → 고정
return (minShift, maxShift)
```

### 11.5 HandlePinchZoom

```
t0, t1 = Input.GetTouch(0/1)
current = Distance(t0.position, t1.position)

if (t0 또는 t1.phase == Began || _previousPinchDistance <= 0):
    _previousPinchDistance = current
    return

ratio = current / _previousPinchDistance
factor = 1 + (ratio - 1) * _pinchZoomSensitivity      # 기본 1.0이면 비율 그대로
ApplyZoomMultiplier(factor)
_previousPinchDistance = current
```

---

## 12. BackgroundView 상세

### 12.1 자식 3종

| Image | RT 설정 | 의미 |
|---|---|---|
| Background_Sky | pivot (0.5, 0) | bottom 기준. 위로 자람. |
| Background_Main | pivot (0.5, 0.5) | center. 1F 바닥에 정렬. |
| Background_Underground | pivot (0.5, 1) | top 기준. 아래로 자람. |

### 12.2 SetOriginY

```
public void SetOriginY(float mainAnchoredY)
{
    mainRt.anchoredPosition.y = mainAnchoredY
    SnapSkyAndUnderToMain()
}
```

호출자: `TopTowerIngame.ApplyOriginY` 또는 `BuildingView.PullValuesFromTowerOrigin`이 `originY + GetMainImageHalfHeight()` 전달.

이 값을 받으면 Main center가 그 Y에 위치 → Main pivot center라 bottom edge가 `mainAnchoredY - halfHeight = originY`에 위치 = 빌딩 1F 바닥과 일치.

### 12.3 SnapSkyAndUnderToMain

```
mainTop = mainRt.anchoredPosition.y + mainRt.sizeDelta.y / 2
mainBottom = mainRt.anchoredPosition.y - mainRt.sizeDelta.y / 2

skyRt.anchoredPosition.y = mainTop      # sky pivot bottom → 이 y가 sky 바닥
underRt.anchoredPosition.y = mainBottom # under pivot top → 이 y가 under 윗면
```

### 12.4 AdjustHeights

```
public void AdjustHeights(float buildingHeight)
{
    marginedHeight = max(buildingHeight * _heightMargin, _minHeight)
    skyRt.sizeDelta.y = marginedHeight
    underRt.sizeDelta.y = marginedHeight
    SnapSkyAndUnderToMain()
}
```

호출자: BuildingView.RenderBuilding 끝에서.

### 12.5 SetShiftY / SetZoom

```
SetShiftY(shift):
    bgGroup = mainImage.parent (RT)
    bgGroup.anchoredPosition.y = shift

SetZoom(zoom):
    bgGroup.localScale = (zoom, zoom, 1)
```

BuildingView가 ShiftY/Zoom 메서드 안에서 BackgroundView의 동일 메서드 호출 → 한 몸 이동/줌.

### 12.6 LoadBackgroundsAsync

```
1. _stageData 없으면 return
2. stageID = _stageData.StageID
3. Addressables.LoadResourceLocationsAsync("Background", typeof(Sprite))
   결과 없으면 silent skip (라벨 미등록 케이스)
4. Addressables.LoadAssetsAsync<Sprite>("Background", null)
5. name prefix 매칭으로 main/sky/under sprite 할당:
   "{NNN:D3}_Background_Main_"
   "{NNN:D3}_Background_Sky_"
   "{NNN:D3}_Background_Under_"
```

---

## 13. TowerOrigin 단일 통제점 패턴

```
[ExecuteAlways]
public class TowerOrigin : MonoBehaviour
{
    [SerializeField] float _originY = -400
    [SerializeField] float _homeY = 0
    [SerializeField] bool _showGuideLines = true
    [SerializeField] float _zoomMin = 0.5
    [SerializeField] float _zoomMax = 2

    void OnEnable() => ApplyToBuildingView()

#if UNITY_EDITOR
    void OnValidate()
        EditorApplication.delayCall += () => {
            if (this != null) ApplyToBuildingView()
        }
#endif

    void ApplyToBuildingView()
        bv = FindObjectOfType<BuildingView>()
        if bv == null: return
        bv.SetShowOriginGuideLine(_showGuideLines)
        bv.SetShowHomeGuideLine(_showGuideLines)
        bv.SetHomeY(_homeY)
        bv.SetOriginY(_originY)                    ← 변경 감지로 RenderBuilding 트리거 가능
        bv.SetZoomLimits(_zoomMin, _zoomMax)

        bg = FindObjectOfType<BackgroundView>()
        if bg != null:
            bg.SetOriginY(_originY + bg.GetMainImageHalfHeight())
}
```

핵심:
- **단일 진실의 원천(single source of truth)**: 모든 시각 정렬 값을 한 컴포넌트에 모아 두고 자동 push
- BuildingView 측에는 같은 값들의 사본이 있지만 노출 안 함 (private). TowerOrigin이 일방향 push.
- `[ExecuteAlways]` + `OnValidate(delayCall)` 패턴으로 Edit/Play 모두 즉시 반영

---

## 14. TopTowerIngame 역할 (vanilla 골격)

**역할**: Play 시작 시 Stage prefab을 Addressables로 로드 + 인스턴스화 + TowerOrigin 값을 빌딩/배경에 한 번 push.

```csharp
public class TopTowerIngame : MonoBehaviour
{
    [SerializeField] string _stageAddress = "Assets/.../Stage_001.prefab";

    async UniTaskVoid Start()
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(_stageAddress);
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded) return;
        var stage = Instantiate(handle.Result);
        ApplyOriginY(stage);
    }

    void ApplyOriginY(GameObject stage)
    {
        var to = FindObjectOfType<TowerOrigin>();
        if (to == null) return;
        var bv = stage.GetComponentInChildren<BuildingView>(true);
        if (bv != null) {
            bv.SetShowOriginGuideLine(to.ShowOriginGuideLine);
            bv.SetShowHomeGuideLine(to.ShowHomeGuideLine);
            bv.SetHomeY(to.HomeY);
            bv.SetOriginY(to.OriginY);
            bv.SetZoomLimits(to.ZoomMin, to.ZoomMax);
        }
        var bg = stage.GetComponentInChildren<BackgroundView>(true);
        if (bg != null) bg.SetOriginY(to.OriginY + bg.GetMainImageHalfHeight());
    }
}
```

BuildingView.Awake의 `PullValuesFromTowerOrigin`이 같은 일을 하므로 사실상 보험. 둘 중 하나만 있어도 정상 동작 (둘 다 있으면 같은 값 두 번 적용 — 무해).

> **현 프로젝트의 회사 fork 버전**(IngameBase 상속, `_uiProvider`/`DLogger` Inject)은 본 vanilla 골격으로 **완전 교체**. 회사 의존 코드 단 한 줄도 vanilla에 옮기지 말 것.

---

## 15. 도구 — StageSpritesSyncTool (Editor)

메뉴: `Tools > Top Tower > Sync Addressables`.

```
Sync():
    settings = AddressableAssetSettingsDefaultObject.Settings
    SyncFolderRecursive("Assets/Application/TopTower", ...)
    settings.SetDirty(EntryMoved, null, true)
    AssetDatabase.SaveAssets()
    Debug.Log("새 등록 N | 기존 M | 라벨 L | 스킵 S")

SyncFolderRecursive(folderPath, ...):
    if folderName == "Editor": skip (재귀 제외)
    label = folderName
    for each asset in folderPath (직속만):
        if not IsAddressableType(path): skip
        entry = settings.FindAssetEntry(guid)
        if null: entry = settings.CreateOrMoveEntry(guid, defaultGroup, ...)
        entry.address = path                         # 풀 경로로 통일
        entry.labels의 기존 라벨 모두 제거
        entry.SetLabel(label, true, ...)             # 폴더명 1개로 통일
    for each subfolder: 재귀

IsAddressableType(path):
    .cs, .asmdef, .asmref, .uxml, .uss, .meta → false
    그 외 → true (sprite, prefab, asset, audio 등)
```

장점:
- 폴더 이동/이름 변경 시 라벨 자동 정리
- 새 sprite 추가 시 메뉴 한 번 → 등록 + 라벨 완료

---

## 16. 호출 시퀀스 다이어그램 — 사용자가 슬라이더를 만질 때

Edit 모드 가정.

```
[User drags TowerOrigin's "Origin Y" slider]
        │
        ▼
1. Unity: TowerOrigin.OnValidate fires
        │
2.      EditorApplication.delayCall += () => ApplyToBuildingView()
        │
        ▼  (다음 editor 업데이트 사이클)
3. ApplyToBuildingView()
        │  bv.SetShowOriginGuideLine(show)   ← UpdateOriginGuideLine 호출 (캐시 라인 표시/숨김)
        │  bv.SetShowHomeGuideLine(show)
        │  bv.SetHomeY(homeY)                ← UpdateHomeGuideLine (라인 위치만 갱신)
        │  bv.SetOriginY(originY)            ← Mathf.Approximately로 변경 감지
        │      └─ 값 변경됨 → RenderBuilding()
        │          ├─ ClearCubes
        │          ├─ Cube placeholder 재배치
        │          ├─ Sprite load 재시작 (LoadSpritesThenLabels)
        │          │   └─ Empty 모듈 Random 다시 (눈에 띌 수 있음)
        │          ├─ BackgroundView.AdjustHeights
        │          ├─ UpdateOriginGuideLine + UpdateHomeGuideLine
        │  bv.SetZoomLimits(min, max)        ← 현재 zoom이 범위 밖이면 clamp + SetZoom
        │  bg.SetOriginY(originY + halfH)     ← Main image 재정렬
```

→ Inspector 슬라이더 한 번 만지면 빌딩 전체가 재정렬 + 재렌더링.

대안: 받침박스 색만 만지면? → `BuildingView.OnValidate` (별도)가 `UpdateAllFloorLabels`만 호출 → 라벨만 in-place 갱신, 빌딩 sprite 안 건드림.

---

## 17. 자주 발생한 함정 (학습 사항)

| 함정 | 증상 | 해결 |
|---|---|---|
| `Canvas`에 `GraphicRaycaster` 없음 | 클릭/드래그 안 됨, 휠은 됨 | Stage 루트에 GraphicRaycaster 추가 |
| `Image.raycastTarget = true` (기본) | 빌딩 sprite가 클릭 이벤트 가로채 SafeArea 드래그 차단 | 모든 sprite/cube 생성 시 `raycastTarget = false` 명시 |
| Stage_001 prefab을 IngameScene에 드래그한 상태로 작업 | TowerOrigin이 이미 OnEnable했어도 Stage_001은 나중에 들어왔으므로 값 push 안 됨 | BuildingView.Awake에서 `PullValuesFromTowerOrigin` 호출 (보험) |
| TMP `ForceMeshUpdate()`를 라벨 생성 직후 호출 | NRE in GenerateTextMesh | `GetPreferredValues(string)`만 사용 |
| 회사 fork TMP의 SerializeField 배열이 런타임 생성 시 null | NRE in GenerateTextMesh (line 4422) | **vanilla TMP에선 발생 X — 이 함정 무시.** 현 코드의 리플렉션 hack(`EnsureTextAnimatorFieldsInitialized`)은 vanilla 옮길 때 §18대로 삭제. |
| Empty 모듈 random이 매번 RenderBuilding마다 재선택 | 토글 만질 때마다 시각 흔들림 | OnValidate → `UpdateAllFloorLabels` (in-place 갱신), `SetOriginY` Approximately 변경 감지 |
| 가이드 라인이 cubeContainer 자식이라 빌딩과 같이 이동 | Home Y 라인이 고정점 역할 못 함 | 연두 라인만 부모를 cubeContainer.parent로 옮김 (월드 고정) |
| Canvas 크기가 SafeArea conform으로 비정상 stretch | UI 비례 깨짐 | Stage_001 root sizeDelta (1080, 1920) 고정 + CanvasScaler match 0 (가로 우선) |
| EventSystem 중복 (IngameScene + RootCanvas 양쪽) | "2 event systems" 경고 무한 | IngameScene의 EventSystem 삭제 |

---

## 18. vanilla 이식 시 단순화 가능 지점

| 항목 | 현 코드 | vanilla |
|---|---|---|
| TMP NRE 우회 | `EnsureTextAnimatorFieldsInitialized` + 리플렉션 + static FieldInfo 캐시 | **전부 삭제** |
| `TopTowerIngame` | IngameBase 상속, _uiProvider 등 Inject | 단순 MonoBehaviour로 Start에서 Addressables 로드만 |
| `UIIngamePanelMediator` | UIService/AdManager/SceneFlowManager 등 7개 Inject | 새 기획에 맞춰 처음부터 |
| `__RootCanvas__` | 회사 prefab, EventSystem 포함 | vanilla EventSystem GameObject 직접 |
| 폰트 fallback 가드 (`text.enabled = false`) | 회사 fork 우회용 안전책 | vanilla TMP라 큰 의미 없음, 유지해도 무해 |

전체 작업량: **BuildingView.cs에서 약 30~40줄 제거** + 회사 의존 파일 2~3개 재작성. 코어 큐브/렌더 로직은 무수정 이식.

---

## 부록: 빌딩 한 층 좌표 시각화

```
                                                    cubeContainer 중심 좌표
                                                          (y=0)
                                                            │
        ┌────────────────────────────────────────────────────────┐
        │                                                        │ ← +halfHeight
        │                                                        │
        │  [floorIdx=0 ceiling]   ceilingTopY = currentYOffset   │
        │  [floorIdx=0 cube  ]    cubeTopY = -ceilingHeight + Y₀ │
        │                                                        │
        │  [floorIdx=1 ceiling]   ceilingTopY = -1*stack + Y₀    │
        │  [floorIdx=1 cube  ]                                   │
        │                                                        │
        │  ...                                                   │
        │                                                        │
        │  [floorIdx=N-1 ceiling]                                │
        │  [floorIdx=N-1 cube  ]  cubeBottomY = -N*stack + Y₀   │
        │                                                        │
        │  [Bottom row]      (지하 있을 때)                       │
        │  [Root  (4 rows)]  (지하 있을 때)                       │
        │                                                        │
        └────────────────────────────────────────────────────────┘ ← -halfHeight

Y₀ = _currentYOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight
```

**1F cube bottom anchored y = -(floor1Idx + 1) * stackHeight + Y₀ = _originY - halfHeight**
**1F cube bottom in cubeContainer center coord = halfHeight + (-(floor1Idx+1)*stack + Y₀) = _originY**  ✓

---

이 문서가 코드 동작의 완전 사본. 새 프로젝트에서 BuildingView를 처음부터 재작성할 때 본 문서의 §5~§7만 따라 짜면 동일 결과 보장.
