using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// Stage_001.prefab 등 스테이지 프리팹의 root에 붙어서 StageData를 읽고
    /// UI Image 기반으로 빌딩 Cube들을 동적 생성/배치한다.
    /// 1단계 — 단순 색상 표시 (sprite 매핑 없음).
    /// [ExecuteAlways]: Prefab Edit Mode/에디터에서도 미리보기 생성. 생성된 Cube는 HideAndDontSave로 prefab 저장에서 제외.
    /// </summary>
    [ExecuteAlways]
    public class BuildingView : MonoBehaviour
    {
        [Header("데이터")]
        [Tooltip("이 빌딩에 사용할 스테이지 데이터 (StageData ScriptableObject).")]
        [SerializeField] private StageData _stageData;

        [Header("참조")]
        [Tooltip("큐브/모듈/외벽 sprite들이 자식으로 생성될 컨테이너 (보통 StageViewport 안의 BuildingContainer).")]
        [SerializeField] private RectTransform _cubeContainer;

        // 배경 뷰. 같은 프리팹에서 자동 탐색되므로 인스펙터에 노출하지 않음(런타임 캐시).
        private BackgroundView _backgroundView;

        // ── 빌딩 고정 크기 상수 (인스펙터 조절 불가) ──
        // _gridWidth: 외벽 포함 한 층 폭 9칸 = [배경1][외벽1][실내5][외벽1][배경1].
        //   (13열 확장은 양옆 배경 여백일 뿐, 빌딩 폭 자체는 9 고정)
        // _cubeAspectRatio: 큐브(롱행) 세로 비율 1:1.3.
        // _ceilingHeightRatio: 구분줄(숏행) 세로 = 큐브 세로의 0.34배 (못박음).
        private const int _gridWidth = 9;
        private const float _cubeAspectRatio = 1.3f;
        private const float _ceilingHeightRatio = 0.34f;

        [Header("층 라벨")]
        [Tooltip("TMP SDF 폰트 asset. null이면 TMP 기본(LiberationSans SDF) 사용.")]
        [SerializeField] private TMP_FontAsset _labelFont;
        [SerializeField] private Color _labelColor = Color.white;
        [Tooltip("글자 크기 (TMP fontSize 단위).")]
        [SerializeField] private float _labelFontSize = 32f;
        [SerializeField] private bool _labelUseStroke = false;
        [SerializeField] private Color _labelStrokeColor = Color.black;
        [Tooltip("외곽선 굵기 (0=없음, 0.2=두꺼움). 외부 기준 생성 — 글자 본체 침범 X. Padding 9 폰트 기준 안전 영역.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _labelStrokeThickness = 0.1f;

        [Header("층 라벨 - 받침박스")]
        [SerializeField] private bool _labelBackdrop = false;
        [Tooltip("받침박스 색 (알파 포함 — 투명도 조절).")]
        [SerializeField] private Color _labelBackdropColor = new Color(0f, 0f, 0f, 0.5f);
        [Tooltip("받침박스 크기 (큐브 단위). X=가로 칸수, Y=구분줄 높이 배수. (2,1)=라벨 크기 동일.")]
        [SerializeField] private Vector2 _labelBackdropSize = new Vector2(2f, 1f);
        [Tooltip("받침박스 위치 오프셋 (픽셀). 라벨 좌상단 기준.")]
        [SerializeField] private Vector2 _labelBackdropOffset = Vector2.zero;

        // Addressables 라벨 — Sync 도구가 폴더명을 라벨로 자동 부여.
        private const string StageCommonLabel = "StageCommon";              // 모든 stage 공통 (Empty 등)
        private const string StageSpecificLabelPattern = "Stage_{0:D3}";    // stage 전용 (Wall 등)

        // sprite name 명명 규칙
        //  - 공통: {Group}_{Type}_{ID}              예: Facility_Empty_001
        //  - 전용: Stage_{NNN}_{Type}_{ID}          예: Stage_001_Wall_001
        // Empty는 그룹 접두어(Structural→Facility 등)가 바뀌어도 잡히도록 _{Type}_ 키워드 매칭.
        private const string EmptyNameKeyword = "_Empty_";

        // Indoor ceiling sprite 없을 때 fallback 색 (진파랑)
        private static readonly Color CeilingFallbackColor = new Color(0.1f, 0.15f, 0.4f);

        // Empty 모듈 ID 분류 (기획)
        //   001~020: 기본 1×4 (엘베 Left/Right용)
        //   021~040: 특수 1×4 (용도 미정)
        //   100~   : 1×2 (엘베 Center용, 추후)
        private const int EmptyBasicIdMin = 1;
        private const int EmptyBasicIdMax = 20;

        private const int MaxRetries = 10;
        private int _retryCount;

        // 렌더 세대 카운터. 시작 시 RenderBuilding이 여러 번 호출될 때, 이전 렌더의 비동기 sprite/label
        // continuation이 ClearCubes 이후에 생성물을 남겨 "두 벌 겹침"이 생기는 것을 방지.
        // RenderBuilding마다 증가시키고, 비동기 로더/라벨은 이 값이 유지될 때만 생성한다.
        private int _renderGeneration;

        // 시각 가이드 라인 표시 여부. TowerOrigin이 SetShowOriginGuideLine로 설정.
        private bool _showOriginGuideLine = false;
        private GameObject _originGuideLineGO; // 가이드 라인 GameObject 캐시 (토글 시 재생성 안 하기 위함)

        // 빌딩 홈 Y축 (HomeButton 클릭 시 이동할 위치) + 연두 가이드 라인
        private float _homeY = 0f;
        private bool _showHomeGuideLine = false;
        private GameObject _homeGuideLineGO;

        private const float HomeMoveDuration = 0.3f; // 애니메이션 시간 (초)

        // 1F cube bottom의 cubeContainer 좌표 y 절대값. TopTowerIngame이 SetOriginY로 설정.
        // 기본값은 0 — prefab edit mode에서는 이 값. Play에서 TopTowerIngame이 덮어씀.
        private float _originY = 0f;

        // 줌 상태 (확대/축소). TowerOrigin에서 SetZoomLimits로 한계 주입.
        private float _zoomMin = 0.5f;
        private float _zoomMax = 2f;
        private float _currentZoom = 1f;

        // RenderBuilding 시점에 계산되는 cube 배치 y offset. 모든 sprite 메서드가 참조.
        // 1F cube bottom이 cubeContainer center 좌표상 _originY가 되도록 자동 보정.
        private float _currentYOffset;

        // 층별 큐브 중심 y (container-center 좌표). RenderBuilding에서 채움. ScrollToFloor(카메라 이동)에 사용.
        private readonly Dictionary<int, float> _floorCenterY = new Dictionary<int, float>();

        private void Awake()
        {
            EnsureCanvasSetup();
            EnsureScales();
            // 홈 버튼은 UI라서 InGameScene의 TowerUI가 담당. 여기선 public MoveToHomeY만 노출.
            PullValuesFromTowerOrigin();
        }

        /// <summary>
        /// Scene에 TowerOrigin 컴포넌트가 있으면 그 값을 BuildingView + BackgroundView에 즉시 적용.
        /// Edit Mode에서 Stage_xxx 프리팹을 드래그로 불러왔을 때 Main image와 1F 바닥이 처음부터 정렬되도록.
        /// </summary>
        private void PullValuesFromTowerOrigin()
        {
            var to = FindObjectOfType<TowerOrigin>();
            if (to == null) return;

            SetOriginY(to.OriginY);
            SetHomeY(to.HomeY);
            SetShowOriginGuideLine(to.ShowOriginGuideLine);
            SetShowHomeGuideLine(to.ShowHomeGuideLine);
            SetZoomLimits(to.ZoomMin, to.ZoomMax);

            if (_backgroundView == null) _backgroundView = GetComponentInParent<BackgroundView>();
            if (_backgroundView == null) _backgroundView = transform.root.GetComponentInChildren<BackgroundView>();
            if (_backgroundView != null)
                _backgroundView.SetOriginY(to.OriginY + _backgroundView.GetMainImageHalfHeight());
        }

        private void OnDestroy()
        {
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector "층 라벨" 필드 변경 시, 기존 라벨 GameObject들을 in-place 갱신.
        /// RenderBuilding 호출하지 않음 — 빈 모듈 sprite 재랜덤 방지.
        /// </summary>
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

            // 받침박스 크기/위치 재계산을 위한 셀 치수
            float containerWidth = _cubeContainer.rect.width;
            float cubeWidth = _gridWidth > 0 ? containerWidth / _gridWidth : 0f;
            float cubeHeight = cubeWidth * _cubeAspectRatio;
            float ceilingHeight = cubeHeight * _ceilingHeightRatio;

            // 라벨 갱신 (Legacy Text 마이그레이션 + TMP 속성 적용)
            for (int i = 0; i < _cubeContainer.childCount; i++)
            {
                var child = _cubeContainer.GetChild(i);
                if (!child.name.StartsWith("FloorLabel_")) continue;

                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                {
                    var legacy = child.GetComponent<Text>();
                    string oldLabelText = legacy != null ? legacy.text : child.name;
                    if (legacy != null) DestroyImmediate(legacy);
                    var legacyOutline = child.GetComponent<UnityEngine.UI.Outline>();
                    if (legacyOutline != null) DestroyImmediate(legacyOutline);
                    tmp = child.gameObject.AddComponent<TextMeshProUGUI>();
                    tmp.raycastTarget = false;
                    ApplyLabelProperties(tmp, oldLabelText);
                }
                else
                {
                    ApplyLabelProperties(tmp, tmp.text);
                }

                // 받침박스 동기화
                SyncBackdropForLabel(child, cubeWidth, ceilingHeight);
            }
        }

        /// <summary>
        /// 라벨 child에 대응하는 받침박스 GameObject를 _labelBackdrop 상태에 맞춰 생성/삭제/갱신.
        /// </summary>
        private void SyncBackdropForLabel(Transform labelChild, float cubeWidth, float ceilingHeight)
        {
            string suffix = labelChild.name.Substring("FloorLabel_".Length); // 예: "F1", "F-2"
            string backdropName = "FloorLabelBackdrop_" + suffix;
            Transform existing = _cubeContainer.Find(backdropName);
            var labelRt = labelChild.GetComponent<RectTransform>();
            if (labelRt == null) return;
            float labelX = labelRt.anchoredPosition.x;
            float ceilingTopY = labelRt.anchoredPosition.y;
            var labelText = labelChild.GetComponent<TextMeshProUGUI>();

            if (_labelBackdrop)
            {
                if (existing == null)
                {
                    int floorIndex = 0;
                    string num = suffix.StartsWith("F") ? suffix.Substring(1) : suffix;
                    int.TryParse(num, out floorIndex);
                    CreateFloorLabelBackdrop(floorIndex, labelX, ceilingTopY, cubeWidth, ceilingHeight, labelText);
                    existing = _cubeContainer.Find(backdropName);
                    if (existing != null)
                        existing.SetSiblingIndex(labelChild.GetSiblingIndex());
                }
                if (existing != null)
                {
                    var img = existing.GetComponent<Image>();
                    if (img != null) img.color = _labelBackdropColor;
                    var bdRt = existing.GetComponent<RectTransform>();
                    if (bdRt != null) ApplyBackdropTransform(bdRt, labelText, labelX, ceilingTopY, cubeWidth, ceilingHeight);
                }
            }
            else if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
        }
#endif

        /// <summary>
        /// Stage_001.prefab의 일부 RectTransform LocalScale이 (0,0,0)으로 잘못 저장된 경우 보정.
        /// scale 0이면 자식 GameObject도 모두 안 보임.
        /// </summary>
        private void EnsureScales()
        {
            // 자신(root)부터 부모 체인 전체 scale 보정
            FixScaleIfZero(transform);
            if (_cubeContainer != null)
            {
                Transform t = _cubeContainer;
                while (t != null)
                {
                    FixScaleIfZero(t);
                    t = t.parent;
                }
            }
        }

        private void FixScaleIfZero(Transform t)
        {
            var s = t.localScale;
            if (s.x == 0f || s.y == 0f || s.z == 0f)
            {
                Debug.LogWarning($"[BuildingView] {t.name}의 localScale이 0. (1,1,1)로 강제 보정.");
                t.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Stage_001.prefab의 Canvas가 잘못 저장된 경우(World Space + ConstantPixelSize) 자동 보정.
        /// prefab을 직접 수정하면 이 코드는 no-op.
        /// </summary>
        private void EnsureCanvasSetup()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isRootCanvas) return;

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0f; // 가로 우선 — 모든 디바이스에서 빌딩 가로 위치 고정
            }
        }

        private void Start()
        {
            RenderBuilding();
        }

        public void SetStageData(StageData data)
        {
            _stageData = data;
            RenderBuilding();
        }

        /// <summary>
        /// 1F cube bottom의 cubeContainer 좌표 y 절대값 설정. 빌딩 전체가 이 라인 기준 위로 쌓임.
        /// TopTowerIngame이 stage 로드 후 호출 — 모든 stage 공통 y.
        /// </summary>
        public void SetOriginY(float y)
        {
            if (Mathf.Approximately(_originY, y)) return; // 변경 없으면 RenderBuilding 호출 X
            _originY = y;
            RenderBuilding();
        }

        /// <summary>
        /// 핑크 가로 가이드 라인 표시 여부. TowerOrigin의 토글 값을 전달.
        /// 빌딩 재생성 없이 라인 GameObject만 처리 — 빈 모듈 sprite 안 바뀜.
        /// </summary>
        public void SetShowOriginGuideLine(bool show)
        {
            _showOriginGuideLine = show;
            UpdateOriginGuideLine();
        }

        /// <summary>
        /// 빌딩 홈 Y축 설정. HomeButton 클릭 시 빌딩이 이동할 위치.
        /// </summary>
        public void SetHomeY(float y)
        {
            _homeY = y;
            UpdateHomeGuideLine();
        }

        /// <summary>
        /// 연두 가로 가이드 라인 (homeY) 표시 여부.
        /// </summary>
        public void SetShowHomeGuideLine(bool show)
        {
            _showHomeGuideLine = show;
            UpdateHomeGuideLine();
        }

        /// <summary>
        /// HomeButton OnClick에서 호출. cubeContainer 시프트를 _homeY로 설정 + 애니메이션.
        /// _homeY 자체가 빌딩의 시프트 목표값 (OriginY는 1F 렌더링 전용, HomeButton과 무관).
        /// 빌딩 layout 재생성 X (sprite 안 바뀜).
        /// </summary>
        public void MoveToHomeY()
        {
            if (!Application.isPlaying)
            {
                SetShiftY(_homeY);
                return;
            }
            AnimateShiftAsync(_homeY, HomeMoveDuration).Forget();
        }

        /// <summary>
        /// 빌딩 추가 시프트(현재 layout 기준 위/아래 offset). 드래그/휠에서 호출.
        /// cubeContainer.anchoredPosition.y만 변경 → sprite 재생성 없음.
        /// </summary>
        public float ShiftY => _cubeContainer != null ? _cubeContainer.anchoredPosition.y : 0f;

        public void SetShiftY(float shift)
        {
            if (_cubeContainer == null) return;
            var pos = _cubeContainer.anchoredPosition;
            pos.y = shift;
            _cubeContainer.anchoredPosition = pos;

            // 배경도 같이 시프트 (빌딩과 한 몸)
            if (_backgroundView == null) _backgroundView = GetComponentInParent<BackgroundView>();
            if (_backgroundView == null) _backgroundView = transform.root.GetComponentInChildren<BackgroundView>();
            if (_backgroundView != null) _backgroundView.SetShiftY(shift);
        }

        /// <summary>
        /// 빌딩 가로 시프트(복귀형 드래그 전용). cubeContainer.anchoredPosition.x만 변경.
        /// 손 떼면 컨트롤러가 홈 x로 되돌린다. y는 건드리지 않음.
        /// </summary>
        public float ShiftX => _cubeContainer != null ? _cubeContainer.anchoredPosition.x : 0f;

        public void SetShiftX(float shift)
        {
            if (_cubeContainer == null) return;
            var pos = _cubeContainer.anchoredPosition;
            pos.x = shift;
            _cubeContainer.anchoredPosition = pos;

            // 배경도 같이 시프트 (빌딩과 한 몸)
            if (_backgroundView == null) _backgroundView = GetComponentInParent<BackgroundView>();
            if (_backgroundView == null) _backgroundView = transform.root.GetComponentInChildren<BackgroundView>();
            if (_backgroundView != null) _backgroundView.SetShiftX(shift);
        }

        /// <summary>해당 층 큐브가 화면(뷰포트) 중앙에 오도록 스크롤 (카메라 이동). 건설 시작 시 호출.</summary>
        public void ScrollToFloor(int floorIndex)
        {
            if (_cubeContainer == null) return;
            if (!_floorCenterY.TryGetValue(floorIndex, out float centerY)) return;
            SetShiftY(-centerY * _currentZoom);
        }

        /// <summary>
        /// 빌딩 layout(1F bottom)의 cubeContainer 좌표 + 현재 시프트 적용한 실제 y.
        /// 드래그 한계 계산 등에 사용.
        /// </summary>
        public float CurrentBuildingOriginY => _originY + ShiftY;

        /// <summary>
        /// cubeContainer 시프트를 startShift → endShift로 duration에 걸쳐 부드럽게 이동.
        /// </summary>
        private async UniTask AnimateShiftAsync(float endShift, float duration)
        {
            float startShift = ShiftY;
            if (Mathf.Approximately(startShift, endShift)) return;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
                SetShiftY(Mathf.Lerp(startShift, endShift, eased));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            SetShiftY(endShift);
        }

        /// <summary>
        /// 빌딩 layout 전체 높이 (드래그 한계 계산용). N × stackHeight.
        /// </summary>
        public float GetTotalBuildingHeight()
        {
            if (_stageData?.Floors == null || _stageData.Floors.Count == 0) return 0f;
            if (_cubeContainer == null) return 0f;
            float cubeWidth = _cubeContainer.rect.width / _gridWidth;
            float cubeHeight = cubeWidth * _cubeAspectRatio;
            float ceilingHeight = cubeHeight * _ceilingHeightRatio;
            float stackHeight = cubeHeight + ceilingHeight;
            return _stageData.Floors.Count * stackHeight;
        }

        public float OriginY => _originY;
        public float HomeY => _homeY;
        public float CurrentZoom => _currentZoom;
        public float ZoomMin => _zoomMin;
        public float ZoomMax => _zoomMax;

        /// <summary>
        /// 줌 한계 설정. TowerOrigin이 OnEnable/OnValidate에서 호출.
        /// 현재 줌이 새 범위를 벗어나면 즉시 clamp.
        /// </summary>
        public void SetZoomLimits(float min, float max)
        {
            _zoomMin = Mathf.Max(0.01f, min);
            _zoomMax = Mathf.Max(_zoomMin, max);
            float clamped = Mathf.Clamp(_currentZoom, _zoomMin, _zoomMax);
            if (!Mathf.Approximately(clamped, _currentZoom))
                SetZoom(clamped);
        }

        /// <summary>
        /// 빌딩(cubeContainer) + 배경(BackgroundGroup) 한 몸 localScale 설정.
        /// 한계값으로 clamp. 화면 중앙 기준(각 RT의 pivot이 (0.5, 0.5)) 확대/축소.
        /// </summary>
        public void SetZoom(float zoom)
        {
            zoom = Mathf.Clamp(zoom, _zoomMin, _zoomMax);
            _currentZoom = zoom;
            if (_cubeContainer != null)
                _cubeContainer.localScale = new Vector3(zoom, zoom, 1f);
            if (_backgroundView == null) _backgroundView = GetComponentInParent<BackgroundView>();
            if (_backgroundView == null) _backgroundView = transform.root.GetComponentInChildren<BackgroundView>();
            if (_backgroundView != null) _backgroundView.SetZoom(zoom);
        }

        public void RenderBuilding()
        {
            if (_stageData == null)
            {
                Debug.LogWarning("[BuildingView] StageData가 없습니다.");
                return;
            }
            if (_cubeContainer == null)
            {
                Debug.LogWarning("[BuildingView] CubeContainer가 설정 안 됨.");
                return;
            }
            if (_stageData.Floors == null || _stageData.Floors.Count == 0)
            {
                Debug.LogWarning("[BuildingView] StageData에 Floor가 없습니다.");
                return;
            }

            // UI Layout 강제 갱신
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cubeContainer);

            // 컨테이너 크기 → Cube 크기 계산
            float containerWidth = _cubeContainer.rect.width;
            float cubeWidth = containerWidth / _gridWidth;
            float cubeHeight = cubeWidth * _cubeAspectRatio;
            float ceilingHeight = cubeHeight * _ceilingHeightRatio;
            float stackHeight = cubeHeight + ceilingHeight; // 한 층 점유 = cube + 그 아래 구분줄

            if (cubeWidth <= 0)
            {
                if (_retryCount < MaxRetries && gameObject.activeInHierarchy)
                {
                    _retryCount++;
                    StartCoroutine(RetryNextFrame());
                }
                else
                {
                    Debug.LogError("[BuildingView] Container 크기가 계속 0. BuildingContainer/StageViewport/Canvas의 RectTransform 설정 확인 필요. (Anchor stretch + Offset 0 0 0 0)");
                }
                return;
            }
            _retryCount = 0;

            // 이 렌더의 세대 번호 확정. 이후 비동기 로더/라벨은 gen == _renderGeneration일 때만 생성.
            _renderGeneration++;
            int gen = _renderGeneration;

            ClearCubes();

            // Floor 위에서 아래 순서 (큰 FloorIndex 먼저)
            var sortedFloors = _stageData.Floors.OrderByDescending(f => f.FloorIndex).ToList();

            // 1F cube bottom이 cubeContainer center 좌표상 _originY에 오도록 yOffset 계산.
            int floor1Idx = sortedFloors.FindIndex(f => f.FloorIndex == 1);
            if (floor1Idx < 0)
            {
                var fallback = sortedFloors.Where(f => f.FloorIndex > 0).OrderBy(f => f.FloorIndex).FirstOrDefault();
                floor1Idx = (fallback != null) ? sortedFloors.IndexOf(fallback) : 0;
            }
            float halfHeight = _cubeContainer.rect.height * 0.5f;
            // cube의 anchor (0,1) 기준 anchored.y는 cubeContainer 좌상단에서 아래로 음수.
            // 1F cube bottom anchored = -(idx_1F + 1) * stackHeight + yOffset
            // 1F cube bottom in container-center coords = halfHeight + (그 anchored.y) = _originY
            // → yOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight
            _currentYOffset = _originY - halfHeight + (floor1Idx + 1) * stackHeight;

            _floorCenterY.Clear();
            for (int floorIdx = 0; floorIdx < sortedFloors.Count; floorIdx++)
            {
                var floor = sortedFloors[floorIdx];
                if (floor.Cubes == null || floor.Cubes.Length != _gridWidth)
                {
                    Debug.LogWarning($"[BuildingView] Floor {floor.FloorIndex}의 Cubes 배열이 비정상 (null 또는 길이 != {_gridWidth})");
                    continue;
                }

                for (int col = 0; col < _gridWidth; col++)
                {
                    CreateCubeImage(floor.Cubes[col], col, floorIdx, cubeWidth, cubeHeight, ceilingHeight, stackHeight);
                }

                // 이 층 큐브 중심의 container-center 좌표 y (카메라 스크롤용). anchor(0,1) 기준.
                float cubeTopCenter = halfHeight + (-floorIdx * stackHeight - ceilingHeight + _currentYOffset);
                _floorCenterY[floor.FloorIndex] = cubeTopCenter - cubeHeight * 0.5f;
            }

            // 빌딩 높이에 따라 BackgroundView의 sky/under 영역 자동 조정
            float totalBuildingHeight = sortedFloors.Count * stackHeight;
            if (_backgroundView == null) _backgroundView = GetComponentInParent<BackgroundView>();
            if (_backgroundView == null) _backgroundView = transform.root.GetComponentInChildren<BackgroundView>();

            if (_backgroundView != null)
            {
                _backgroundView.AdjustHeights(totalBuildingHeight);
            }

            // 빌딩 위치/스크롤은 사용자가 prefab에서 직접 정렬. 코드는 끼어들지 않음.

            // sprite 비동기 로드 + 배치 — 에디터 prefab edit mode에서도 미리보기 작동.
            LoadSpritesThenLabelsAsync(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight, gen).Forget();

            // 가이드 라인 표시 (디버그용)
            UpdateOriginGuideLine();
            UpdateHomeGuideLine();
        }

        /// <summary>
        /// 1F bottom 라인(originY) 위치를 핑크색 가로 직선으로 시각화.
        /// Edit Mode 전용 (Scene View / Prefab View). Play 모드에선 항상 숨김.
        /// 캐시된 GameObject 재사용 (토글 시 재생성 X → 빌딩 sprite 영향 없음).
        /// </summary>
        private void UpdateOriginGuideLine()
        {
            if (_cubeContainer == null) return;

            // 캐시 dangling check (ClearCubes로 destroy됐을 수 있음)
            if (_originGuideLineGO == null || _originGuideLineGO.Equals(null))
                _originGuideLineGO = null;

            // Play 모드에선 가이드 라인 절대 표시 X (디자이너용 시각 보조선)
            if (Application.isPlaying || !_showOriginGuideLine)
            {
                if (_originGuideLineGO != null) _originGuideLineGO.SetActive(false);
                return;
            }

            if (_originGuideLineGO == null)
            {
                _originGuideLineGO = new GameObject("__OriginGuideLine", typeof(RectTransform), typeof(Image));
                if (!Application.isPlaying)
                    _originGuideLineGO.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                _originGuideLineGO.transform.SetParent(_cubeContainer, false);

                var rtNew = _originGuideLineGO.GetComponent<RectTransform>();
                rtNew.anchorMin = new Vector2(0, 0.5f);
                rtNew.anchorMax = new Vector2(1, 0.5f);
                rtNew.pivot = new Vector2(0.5f, 0.5f);
                rtNew.sizeDelta = new Vector2(0, 8f);

                var imgNew = _originGuideLineGO.GetComponent<Image>();
                imgNew.color = new Color(1f, 0.4f, 0.8f, 0.9f);
                imgNew.raycastTarget = false;
            }

            _originGuideLineGO.SetActive(true);
            var rt = _originGuideLineGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, _originY);
            _originGuideLineGO.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 연두 가로 가이드 라인 (homeY 위치) 갱신.
        /// Edit Mode 전용 (Scene View / Prefab View). Play 모드에선 항상 숨김.
        /// cubeContainer가 아닌 그 부모 RT의 자식으로 생성 → 빌딩 시프트와 무관하게 월드 고정.
        /// </summary>
        private void UpdateHomeGuideLine()
        {
            if (_cubeContainer == null) return;
            var staticParent = _cubeContainer.parent as RectTransform;
            if (staticParent == null) return;

            if (_homeGuideLineGO == null || _homeGuideLineGO.Equals(null))
                _homeGuideLineGO = null;

            // Play 모드에선 가이드 라인 절대 표시 X (디자이너용 시각 보조선)
            if (Application.isPlaying || !_showHomeGuideLine)
            {
                if (_homeGuideLineGO != null) _homeGuideLineGO.SetActive(false);
                return;
            }

            if (_homeGuideLineGO == null)
            {
                _homeGuideLineGO = new GameObject("__HomeGuideLine", typeof(RectTransform), typeof(Image));
                if (!Application.isPlaying)
                    _homeGuideLineGO.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                _homeGuideLineGO.transform.SetParent(staticParent, false);

                var rtNew = _homeGuideLineGO.GetComponent<RectTransform>();
                rtNew.anchorMin = new Vector2(0, 0.5f);
                rtNew.anchorMax = new Vector2(1, 0.5f);
                rtNew.pivot = new Vector2(0.5f, 0.5f);
                rtNew.sizeDelta = new Vector2(0, 8f);

                var imgNew = _homeGuideLineGO.GetComponent<Image>();
                imgNew.color = new Color(0.6f, 1f, 0.4f, 0.9f); // 연두
                imgNew.raycastTarget = false;
            }

            _homeGuideLineGO.SetActive(true);
            var rt = _homeGuideLineGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, _homeY);
            _homeGuideLineGO.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 모든 stage sprite 비동기 로드 후 → 층 라벨 마지막에 추가.
        /// 라벨이 항상 모듈/외벽/구분줄 sprite 위에 렌더링되도록 sibling order 보장.
        /// </summary>
        private async UniTaskVoid LoadSpritesThenLabelsAsync(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight, int gen)
        {
            await UniTask.WhenAll(
                LoadAndPlaceEmptyModulesAsync(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight, gen),
                LoadAndPlaceWallsAsync(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight, gen),
                LoadAndPlaceElevatorAsync(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight, gen),
                LoadAndPlaceCeilingsAsync(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight, gen),
                LoadAndPlaceRootAsync(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight, gen)
            );
            if (gen != _renderGeneration) return; // 오래된 렌더면 라벨 생성 안 함
            PlaceFloorLabels(sortedFloors, cubeWidth, cubeHeight, ceilingHeight, stackHeight);
        }

        /// <summary>
        /// 뿌리(Root) sprite를 Bottom row 아래에 단일 이미지로 stretch 배치.
        /// 조건: 최하층이 지하층(FloorIndex&lt;0)일 때만 — 즉 Bottom row가 존재할 때만.
        /// 가로: 7칸 (col 1 좌측 외벽 ~ col 7 우측 외벽).
        /// 세로: 2 × stackHeight (큐브+반큐브+큐브+반큐브).
        /// </summary>
        private async UniTask LoadAndPlaceRootAsync(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight, int gen)
        {
            if (_stageData == null) return;

            var deepest = sortedFloors.LastOrDefault();
            if (deepest == null) return; // 층이 아예 없을 때만 Root 없음 (지하 유무 무관 — 항상 최하단에 뿌리)

            string label = string.Format(StageSpecificLabelPattern, _stageData.StageID);
            var sprites = await LoadSpritesByLabelAsync(label);
            if (gen != _renderGeneration) return; // 오래된 렌더면 생성 안 함
            if (sprites == null) return;

            var rootSprite = sprites.FirstOrDefault(s => s.name.Contains("_Root_"));
            if (rootSprite == null) return;

            int deepestIdx = sortedFloors.Count - 1;
            // Bottom row top = deepest 큐브 바닥 = -(deepestIdx+1)*stackHeight + yOffset
            // Bottom row bottom = -(deepestIdx+1)*stackHeight - ceilingHeight + yOffset
            // Root top = Bottom row bottom
            float rootTopY = -(deepestIdx + 1) * stackHeight - ceilingHeight + _currentYOffset;
            float rootHeight = 2f * stackHeight;
            float rootX = 1f * cubeWidth;       // col 1 (좌측 외벽)
            float rootWidth = 7f * cubeWidth;   // col 1~7

            CreateSpriteImage("Root", rootSprite, rootX, rootTopY, rootWidth, rootHeight);
        }

        /// <summary>
        /// 빈 모듈(EmptyModule) sprite를 StageCommon 라벨로 일괄 로드.
        /// 엘베 위치(Left/Right)에 따른 1×4 Indoor 영역에 층마다 랜덤 sprite를 배치.
        /// 기본 ID(001~020)만 사용 — 특수(021~040)/1×2(100~)는 추후.
        /// 라벨/sprite 미등록 시 silent skip.
        /// </summary>
        private async UniTask LoadAndPlaceEmptyModulesAsync(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight, int gen)
        {
            if (_stageData == null) return;
            if (_stageData.ElevatorPosition == ElevatorPosition.Center) return; // 1×2 모듈 추후 구현

            var sprites = await LoadSpritesByLabelAsync(StageCommonLabel);
            if (gen != _renderGeneration) return; // 오래된 렌더면 생성 안 함

            // {Group}_Empty_NNN name + ID 001~020 필터 (그룹 접두어 무관). 없으면 빈방만 못 그림.
            var basicEmpties = new List<Sprite>();
            if (sprites != null)
                basicEmpties = sprites
                    .Where(s => s.name.Contains(EmptyNameKeyword))
                    .Where(s => TryParseTrailingId(s.name, out int id) && id >= EmptyBasicIdMin && id <= EmptyBasicIdMax)
                    .ToList();

            // 모듈 영역 (엘베 위치 기반)
            // Left(C, col 2)  → 모듈 D-E-F-G = col 3~6
            // Right(G, col 6) → 모듈 C-D-E-F = col 2~5
            int moduleStartCol, moduleEndCol;
            switch (_stageData.ElevatorPosition)
            {
                case ElevatorPosition.Left:  moduleStartCol = 3; moduleEndCol = 6; break;
                case ElevatorPosition.Right: moduleStartCol = 2; moduleEndCol = 5; break;
                default: return;
            }

            float moduleWidthPx = (moduleEndCol - moduleStartCol + 1) * cubeWidth;
            _cachedEmpties = basicEmpties;
            _moduleGeo.Clear();

            for (int floorIdx = 0; floorIdx < sortedFloors.Count; floorIdx++)
            {
                var floor = sortedFloors[floorIdx];
                if (floor.Cubes == null || floor.Cubes.Length != _gridWidth) continue;

                // 모듈 영역이 전부 Indoor일 때만 배치
                bool allIndoor = true;
                for (int c = moduleStartCol; c <= moduleEndCol; c++)
                    if (floor.Cubes[c] != CubeType.Indoor) { allIndoor = false; break; }
                if (!allIndoor) continue;

                int fi = floor.FloorIndex;
                float x = moduleStartCol * cubeWidth;
                float y = -floorIdx * stackHeight - ceilingHeight + _currentYOffset;
                _moduleGeo[fi] = new Vector4(x, y, moduleWidthPx, cubeHeight);
                PlaceFloorModule(fi, x, y, moduleWidthPx, cubeHeight);
            }
        }

        // 모듈 영역 지오메트리 + 빈방 스프라이트 캐시 (RefreshFloorModule 재사용용)
        private readonly Dictionary<int, Vector4> _moduleGeo = new Dictionary<int, Vector4>();
        private List<Sprite> _cachedEmpties = new List<Sprite>();

        /// <summary>한 층 모듈 슬롯을 상태에 맞게 생성 (빈방/공사중/입주완료).</summary>
        private void PlaceFloorModule(int fi, float x, float y, float moduleWidthPx, float cubeHeight)
        {
            var bm = BuildManager.Instance;
            var slot = bm != null ? bm.GetSlot(fi) : null;
            if (slot != null && slot.status == BuildManager.SlotStatus.Constructing)
            {
                CreateConstructionModule(fi, x, y, moduleWidthPx, cubeHeight);
            }
            else if (slot != null && slot.status == BuildManager.SlotStatus.Built
                     && slot.module != null && slot.module.sprite != null)
            {
                var built = CreateSpriteImage("BuiltModule_F" + fi, slot.module.sprite, x, y, moduleWidthPx, cubeHeight);
                TryMakeClickable(built, slot.module.sprite.name, fi);
            }
            else
            {
                if (_cachedEmpties == null || _cachedEmpties.Count == 0) return;
                var sprite = _cachedEmpties[Random.Range(0, _cachedEmpties.Count)];
                var emptyGo = CreateSpriteImage("EmptyModule_F" + fi, sprite, x, y, moduleWidthPx, cubeHeight);
                TryMakeClickable(emptyGo, sprite.name, fi);
            }
        }

        /// <summary>해당 층 모듈만 파괴 후 재생성 (전체 재렌더 없이 — 깜빡임 방지). BuildManager가 호출.</summary>
        public void RefreshFloorModule(int floorIndex)
        {
            if (_cubeContainer == null) return;
            if (!_moduleGeo.TryGetValue(floorIndex, out var g)) return;

            string en = "EmptyModule_F" + floorIndex, bn = "BuiltModule_F" + floorIndex, cn = "Construction_F" + floorIndex;
            for (int i = _cubeContainer.childCount - 1; i >= 0; i--)
            {
                var ch = _cubeContainer.GetChild(i);
                if (ch.name == en || ch.name == bn || ch.name == cn)
                {
                    if (Application.isPlaying) Destroy(ch.gameObject); else DestroyImmediate(ch.gameObject);
                }
            }
            PlaceFloorModule(floorIndex, g.x, g.y, g.z, g.w);
        }

        /// <summary>실내공사모듈: 흰 네모 + 남은시간 카운트다운(자체 갱신).</summary>
        private void CreateConstructionModule(int floorIndex, float x, float y, float width, float height)
        {
            var go = new GameObject($"Construction_F{floorIndex}", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            if (!Application.isPlaying) go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_cubeContainer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(width, height);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = Color.white; img.raycastTarget = false;   // 실내공사모듈 임시 흰 네모

            var txtGo = new GameObject("Countdown", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            if (!Application.isPlaying) txtGo.hideFlags = HideFlags.HideAndDontSave;
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tmp = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.font = _labelFont != null ? _labelFont : TMPro.TMP_Settings.defaultFontAsset;
            tmp.text = ""; tmp.fontSize = 40; tmp.color = Color.black;
            tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.raycastTarget = false;

            var cd = go.AddComponent<ConstructionCountdown>();
            cd.Init(floorIndex, tmp);
        }

        /// <summary>
        /// 외벽 sprite를 Stage_{NNN} 라벨로 로드 → 모든 Outdoor cube에 적용.
        /// 지상층(FloorIndex≥1): _Wall_ (단 1F는 _Gate_ 우선).
        /// 지하층(FloorIndex&lt;0): _Underwall_ (지하외벽).
        /// 우측은 Sprite.Create + UV 반전으로 미러 sprite 생성.
        /// </summary>
        private async UniTask LoadAndPlaceWallsAsync(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight, int gen)
        {
            if (_stageData == null) return;

            string label = string.Format(StageSpecificLabelPattern, _stageData.StageID);
            var sprites = await LoadSpritesByLabelAsync(label);
            if (gen != _renderGeneration) return; // 오래된 렌더면 생성 안 함
            if (sprites == null) return;

            // 지상 외벽
            var wallLeft = sprites.FirstOrDefault(s => s.name.Contains("_Wall_"));
            var wallRight = wallLeft != null ? CreateMirroredSprite(wallLeft) : null;

            // 지하 외벽
            var underwallLeft = sprites.FirstOrDefault(s => s.name.Contains("_Underwall_"));
            var underwallRight = underwallLeft != null ? CreateMirroredSprite(underwallLeft) : null;

            // Gate sprite (1F 외벽 자리 전용). 없으면 1F도 일반 외벽 사용.
            var gateLeftSprite = sprites.FirstOrDefault(s => s.name.Contains("_Gate_"));
            var gateRightSprite = gateLeftSprite != null ? CreateMirroredSprite(gateLeftSprite) : null;

            float centerCol = (_gridWidth - 1) * 0.5f;

            for (int floorIdx = 0; floorIdx < sortedFloors.Count; floorIdx++)
            {
                var floor = sortedFloors[floorIdx];
                if (floor.Cubes == null || floor.Cubes.Length != _gridWidth) continue;

                bool isFirstFloor = floor.FloorIndex == 1;
                bool isBasement = floor.FloorIndex < 0;
                bool useGate = isFirstFloor && gateLeftSprite != null;

                for (int col = 0; col < _gridWidth; col++)
                {
                    if (floor.Cubes[col] != CubeType.Outdoor) continue;
                    bool isRight = col > centerCol;
                    Sprite sprite;
                    string spriteKind;
                    if (useGate)
                    {
                        sprite = isRight ? gateRightSprite : gateLeftSprite;
                        spriteKind = "Gate";
                    }
                    else if (isBasement)
                    {
                        sprite = isRight ? underwallRight : underwallLeft;
                        spriteKind = "Underwall";
                    }
                    else
                    {
                        sprite = isRight ? wallRight : wallLeft;
                        spriteKind = "Wall";
                    }
                    if (sprite == null) continue; // 해당 종류 sprite 미등록 — 그 셀만 skip

                    CreateSpriteImage($"{spriteKind}_F{floor.FloorIndex}_C{col}", sprite,
                        col * cubeWidth, -floorIdx * stackHeight - ceilingHeight + _currentYOffset,
                        cubeWidth, cubeHeight);
                }
            }
        }

        /// <summary>
        /// 층 사이 구분줄 sprite 배치 + 최하 지하층 아래 Bottom row 추가.
        /// Indoor: _Floor_ (지상/지하 공통).
        /// Outdoor: 지상층(FloorIndex≥1) → _Wallfr_, 지하층(FloorIndex&lt;0) → _Underwallfr_.
        /// 최하 Bn 층 아래 Bottom row: Indoor=_Bottom_, Outdoor=_Underwallfr_.
        /// </summary>
        private async UniTask LoadAndPlaceCeilingsAsync(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight, int gen)
        {
            if (_stageData == null) return;

            string label = string.Format(StageSpecificLabelPattern, _stageData.StageID);
            var sprites = await LoadSpritesByLabelAsync(label);
            if (gen != _renderGeneration) return; // 오래된 렌더면 생성 안 함

            // sprites가 null이어도 Indoor 단색 fallback은 진행해야 함
            Sprite floorSprite = null;
            Sprite wallfrLeft = null, wallfrRight = null;
            Sprite underwallfrLeft = null, underwallfrRight = null;
            Sprite bottomfrLeft = null, bottomfrRight = null;
            Sprite bottomSprite = null;
            if (sprites != null)
            {
                floorSprite = sprites.FirstOrDefault(s => s.name.Contains("_Floor_"));
                wallfrLeft = sprites.FirstOrDefault(s => s.name.Contains("_Wallfr_"));
                if (wallfrLeft != null) wallfrRight = CreateMirroredSprite(wallfrLeft);
                underwallfrLeft = sprites.FirstOrDefault(s => s.name.Contains("_Underwallfr_"));
                if (underwallfrLeft != null) underwallfrRight = CreateMirroredSprite(underwallfrLeft);
                // 최하구분층 외벽 전용. 없으면 아래 Bottom row에서 Underwallfr로 폴백.
                bottomfrLeft = sprites.FirstOrDefault(s => s.name.Contains("_Bottomfr_"));
                if (bottomfrLeft != null) bottomfrRight = CreateMirroredSprite(bottomfrLeft);
                bottomSprite = sprites.FirstOrDefault(s => s.name.Contains("_Bottom_"));
            }

            float centerCol = (_gridWidth - 1) * 0.5f;

            // 각 floor의 자기 위 ceiling row.
            for (int floorIdx = 0; floorIdx < sortedFloors.Count; floorIdx++)
            {
                var floor = sortedFloors[floorIdx];
                if (floor.Cubes == null || floor.Cubes.Length != _gridWidth) continue;

                bool isBasement = floor.FloorIndex < 0;
                float ceilingTopY = -floorIdx * stackHeight + _currentYOffset;

                for (int col = 0; col < _gridWidth; col++)
                {
                    CubeType type = floor.Cubes[col];

                    if (type == CubeType.Indoor)
                    {
                        if (floorSprite != null)
                        {
                            CreateSpriteImage($"Floor_F{floor.FloorIndex}_C{col}", floorSprite,
                                col * cubeWidth, ceilingTopY, cubeWidth, ceilingHeight);
                        }
                        else
                        {
                            // 단색 fallback (진파랑) — _Floor_ sprite 미등록 시
                            CreateColorImage($"FloorColor_F{floor.FloorIndex}_C{col}", CeilingFallbackColor,
                                col * cubeWidth, ceilingTopY, cubeWidth, ceilingHeight);
                        }
                    }
                    else if (type == CubeType.Outdoor)
                    {
                        bool isRight = col > centerCol;
                        Sprite outdoorSprite;
                        string kind;
                        if (isBasement)
                        {
                            outdoorSprite = isRight ? underwallfrRight : underwallfrLeft;
                            kind = "Underwallfr";
                        }
                        else
                        {
                            outdoorSprite = isRight ? wallfrRight : wallfrLeft;
                            kind = "Wallfr";
                        }
                        if (outdoorSprite != null)
                        {
                            CreateSpriteImage($"{kind}_F{floor.FloorIndex}_C{col}", outdoorSprite,
                                col * cubeWidth, ceilingTopY, cubeWidth, ceilingHeight);
                        }
                        // 해당 sprite 미등록 시 silent skip
                    }
                    // Background 위는 항상 투명 — 안 그림
                }
            }

            // 최하층 아래 Bottom row (Indoor=_Bottom_, Outdoor=_Bottomfr_ / 없으면 _Underwallfr_ 폴백).
            // 지하/지상 무관하게 빌딩 최하층이면 항상 바닥 마감 줄을 깐다 (그 아래 Root와 짝).
            var deepest = sortedFloors.LastOrDefault();
            if (deepest != null
                && deepest.Cubes != null && deepest.Cubes.Length == _gridWidth)
            {
                int deepestIdx = sortedFloors.Count - 1;
                // Bn 큐브 바닥 = -(deepestIdx+1)*stackHeight + yOffset → 그 자리에서 ceilingHeight만큼 아래로 row 점유
                float bottomTopY = -(deepestIdx + 1) * stackHeight + _currentYOffset;

                for (int col = 0; col < _gridWidth; col++)
                {
                    CubeType type = deepest.Cubes[col];
                    if (type == CubeType.Indoor)
                    {
                        if (bottomSprite != null)
                        {
                            CreateSpriteImage($"Bottom_C{col}", bottomSprite,
                                col * cubeWidth, bottomTopY, cubeWidth, ceilingHeight);
                        }
                    }
                    else if (type == CubeType.Outdoor)
                    {
                        bool isRight = col > centerCol;
                        var leftEdge = bottomfrLeft != null ? bottomfrLeft : underwallfrLeft;
                        var rightEdge = bottomfrRight != null ? bottomfrRight : underwallfrRight;
                        var sprite = isRight ? rightEdge : leftEdge;
                        if (sprite != null)
                        {
                            CreateSpriteImage($"BottomEdge_C{col}", sprite,
                                col * cubeWidth, bottomTopY, cubeWidth, ceilingHeight);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 각 ceiling row에 그 아래 floor의 라벨(1F, 2F, B1, ...)을 TMP 텍스트로 표시.
        /// 폰트/색/크기/외곽선은 Inspector "층 라벨" 헤더 필드로 조정. 위치/스타일은 내부 상수.
        /// 외곽선은 TMP SDF 내장 — UI.Outline 같은 4중 그림자 트릭 없이 깔끔한 윤곽.
        /// </summary>
        private void PlaceFloorLabels(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight)
        {
            const int labelCol = 2;             // Indoor 좌측 시작점
            const string labelPrefix = "▼ ";

            for (int floorIdx = 0; floorIdx < sortedFloors.Count; floorIdx++)
            {
                var floor = sortedFloors[floorIdx];
                string label = labelPrefix + GetFloorLabel(floor.FloorIndex);

                float ceilingTopY = -floorIdx * stackHeight + _currentYOffset;
                float labelX = labelCol * cubeWidth;

                // 라벨 먼저 생성 (텍스트 폭 측정용)
                var go = new GameObject($"FloorLabel_F{floor.FloorIndex}", typeof(RectTransform), typeof(TextMeshProUGUI));
                if (!Application.isPlaying)
                    go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetParent(_cubeContainer, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(labelX, ceilingTopY);
                rt.sizeDelta = new Vector2(cubeWidth * LabelWidthInCubes, ceilingHeight);

                var text = go.GetComponent<TextMeshProUGUI>();
                ApplyLabelProperties(text, label);
                text.raycastTarget = false;

                // 받침박스 (라벨 다음에 만든 뒤 sibling 순서를 라벨 직전으로 이동 → 라벨 뒤에 렌더)
                if (_labelBackdrop)
                {
                    CreateFloorLabelBackdrop(floor.FloorIndex, labelX, ceilingTopY, cubeWidth, ceilingHeight, text);
                    var backdrop = _cubeContainer.Find($"FloorLabelBackdrop_F{floor.FloorIndex}");
                    if (backdrop != null)
                        backdrop.SetSiblingIndex(go.transform.GetSiblingIndex());
                }
            }
        }

        // 층 라벨 rect 폭 (큐브 단위). PlaceFloorLabels와 ApplyBackdropTransform 공용.
        private const float LabelWidthInCubes = 2f;

        /// <summary>
        /// 층 라벨용 받침박스 GameObject 생성. pivot=(0.5,0.5) — 텍스트 중심 기준 대칭 확장.
        /// </summary>
        private void CreateFloorLabelBackdrop(int floorIndex, float labelX, float ceilingTopY, float cubeWidth, float ceilingHeight, TextMeshProUGUI labelText)
        {
            var go = new GameObject($"FloorLabelBackdrop_F{floorIndex}", typeof(RectTransform), typeof(Image));
            if (!Application.isPlaying)
                go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_cubeContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            ApplyBackdropTransform(rt, labelText, labelX, ceilingTopY, cubeWidth, ceilingHeight);

            var img = go.GetComponent<Image>();
            img.color = _labelBackdropColor;
            img.raycastTarget = false;
        }

        /// <summary>
        /// 받침박스 RT를 실제 텍스트의 시각적 중심에 배치 + size를 큐브/구분줄 단위 배수로 적용.
        /// labelText의 preferredWidth를 측정해 텍스트 X 중심을 정확히 산출.
        /// </summary>
        private void ApplyBackdropTransform(RectTransform rt, TextMeshProUGUI labelText, float labelX, float ceilingTopY, float cubeWidth, float ceilingHeight)
        {
            if (rt.pivot.x != 0.5f || rt.pivot.y != 0.5f)
                rt.pivot = new Vector2(0.5f, 0.5f);

            // 텍스트 X 중심 — GetPreferredValues로 폭만 계산 (렌더링 트리거 X, 라벨 생성 직후에도 안전).
            float textCenterX;
            if (labelText != null && labelText.font != null && !string.IsNullOrEmpty(labelText.text))
            {
                Vector2 prefSize = labelText.GetPreferredValues(labelText.text);
                textCenterX = labelX + prefSize.x * 0.5f;
            }
            else
            {
                // fallback (라벨/폰트/텍스트 미준비): 라벨 rect 중심
                textCenterX = labelX + cubeWidth * LabelWidthInCubes * 0.5f;
            }
            // 텍스트 Y 중심 = 라벨 rect 세로 중심 (MidlineLeft alignment이라 라벨 rect 가운데)
            float textCenterY = ceilingTopY - ceilingHeight * 0.5f;

            rt.anchoredPosition = new Vector2(textCenterX + _labelBackdropOffset.x, textCenterY + _labelBackdropOffset.y);
            rt.sizeDelta = new Vector2(cubeWidth * _labelBackdropSize.x, ceilingHeight * _labelBackdropSize.y);
        }

        /// <summary>
        /// TMP 라벨 인스턴스에 현재 Inspector 값을 적용. 생성/갱신 양쪽에서 공용.
        /// 외곽선은 material 인스턴스 프로퍼티(_OutlineColor/_OutlineWidth) 직접 변경 +
        /// UpdateMeshPadding으로 mesh 경계 확장 — 외곽선이 클리핑되어 뭉개지는 현상 방지.
        /// </summary>
        private void ApplyLabelProperties(TextMeshProUGUI text, string label)
        {
            if (text == null) return;

            // 0) 회사 커스텀 TMP의 TextAnimator 필드 (defaultAppearancesTags 등) 초기화.
            //    런타임 생성 시 null이라 Canvas Rebuild의 GenerateTextMesh에서 NRE 발생.
            EnsureTextAnimatorFieldsInitialized(text);

            // 1) 폰트 결정 — null이면 Canvas Rebuild 시점에 GenerateTextMesh가 NRE.
            //    명시 폰트 → TMP 기본 → 그래도 없으면 컴포넌트 비활성화로 렌더 시도 차단.
            var font = _labelFont != null ? _labelFont : TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogWarning("[BuildingView] TMP_FontAsset 없음 — Window > TextMeshPro > Import TMP Essential Resources 필요. 라벨 표시 스킵.");
                text.enabled = false;
                return;
            }
            text.enabled = true;

            // 2) 기본 속성 — font를 가장 먼저 설정 (material 인스턴스 생성의 전제).
            text.font = font;
            text.text = label;
            text.color = _labelColor;
            text.fontSize = _labelFontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = false;

            // 3) 외곽선 — fontMaterial 인스턴스 안전 처리. null이면 다음 OnValidate에서 갱신될 것이라 silent skip.
            var mat = text.fontMaterial;
            if (mat != null)
            {
                float width = _labelUseStroke ? _labelStrokeThickness : 0f;
                mat.SetColor(ShaderUtilities.ID_OutlineColor, _labelStrokeColor);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, width);
                text.UpdateMeshPadding();
            }
            text.SetAllDirty();
        }

        // 회사 커스텀 TMP의 TextAnimator 통합 필드 — 런타임 생성 시 null이면 GenerateTextMesh에서 NRE.
        // 리플렉션으로 빈 배열 주입. 필드 정보는 static 캐시.
        private static System.Reflection.FieldInfo s_tmpDefaultAppearancesTagsField;
        private static System.Reflection.FieldInfo s_tmpDefaultBehaviorsTagsField;
        private static System.Reflection.FieldInfo s_tmpDefaultStyleTagsField;
        private static bool s_tmpFieldsLookedUp;

        private static void EnsureTextAnimatorFieldsInitialized(TextMeshProUGUI text)
        {
            if (text == null) return;
            if (!s_tmpFieldsLookedUp)
            {
                var type = typeof(TextMeshProUGUI);
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                s_tmpDefaultAppearancesTagsField = type.GetField("defaultAppearancesTags", flags);
                s_tmpDefaultBehaviorsTagsField = type.GetField("defaultBehaviorsTags", flags);
                s_tmpDefaultStyleTagsField = type.GetField("defaultStyleTags", flags);
                s_tmpFieldsLookedUp = true;
            }
            AssignEmptyIfNull(s_tmpDefaultAppearancesTagsField, text);
            AssignEmptyIfNull(s_tmpDefaultBehaviorsTagsField, text);
            AssignEmptyIfNull(s_tmpDefaultStyleTagsField, text);
        }

        private static void AssignEmptyIfNull(System.Reflection.FieldInfo field, TextMeshProUGUI text)
        {
            if (field == null) return;
            if (field.GetValue(text) == null)
                field.SetValue(text, System.Array.Empty<string>());
        }

        /// <summary>
        /// FloorIndex → 표시 라벨. 양수 N → "{N}F", 음수 -N → "B{N}".
        /// </summary>
        private static string GetFloorLabel(int floorIndex)
        {
            if (floorIndex > 0) return $"{floorIndex}F";
            return $"B{-floorIndex}";
        }

        /// <summary>
        /// sprite 없이 단색만 채우는 Image GameObject 생성기 (ceiling fallback 등).
        /// 에디터 모드에선 HideAndDontSave 플래그로 prefab 저장에서 제외.
        /// </summary>
        private void CreateColorImage(string goName, Color color, float anchoredX, float anchoredY, float width, float height)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            if (!Application.isPlaying)
                go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_cubeContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(anchoredX, anchoredY);
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        /// <summary>
        /// 엘리베이터 sprite를 Stage_{NNN} 라벨에서 찾아 StageData.ElevatorPosition col에 모든 floor 배치.
        /// 1×1 cube 크기. sprite name에 "_Elevator_" 포함.
        /// </summary>
        private async UniTask LoadAndPlaceElevatorAsync(List<FloorData> sortedFloors, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight, int gen)
        {
            if (_stageData == null) return;

            string label = string.Format(StageSpecificLabelPattern, _stageData.StageID);
            var sprites = await LoadSpritesByLabelAsync(label);
            if (gen != _renderGeneration) return; // 오래된 렌더면 생성 안 함
            if (sprites == null) return;

            var elevatorSprite = sprites.FirstOrDefault(s => s.name.Contains("_Elevator_"));
            if (elevatorSprite == null) return;

            int elevatorCol = GetElevatorColumn();

            for (int floorIdx = 0; floorIdx < sortedFloors.Count; floorIdx++)
            {
                var floor = sortedFloors[floorIdx];
                if (floor.Cubes == null || floor.Cubes.Length != _gridWidth) continue;
                if (floor.Cubes[elevatorCol] != CubeType.Indoor) continue;

                var elevGo = CreateSpriteImage($"Elevator_F{floor.FloorIndex}", elevatorSprite,
                    elevatorCol * cubeWidth, -floorIdx * stackHeight - ceilingHeight + _currentYOffset,
                    cubeWidth, cubeHeight);
                TryMakeClickable(elevGo, elevatorSprite.name, floor.FloorIndex);
            }
        }

        /// <summary>
        /// StageData.ElevatorPosition → Indoor 5칸(C/D/E/F/G) 중 엘베 col 반환.
        /// C=col 2, E=col 4, G=col 6.
        /// </summary>
        private int GetElevatorColumn()
        {
            // Indoor 영역에서 동적 계산 (고정 알파벳 X). StageData가 담당.
            return _stageData.GetElevatorColumn();
        }

        /// <summary>
        /// 좌우 반전된 미러 Sprite를 런타임 생성. RenderTexture + Graphics.Blit으로 픽셀 반전 →
        /// 새 Texture2D + Sprite. Sprite.Create의 음수 width 방식보다 안정적 (Prefab Edit Mode에서도 작동).
        /// 텍스처의 Read/Write enabled 불필요.
        /// </summary>
        private Sprite CreateMirroredSprite(Sprite source)
        {
            var srcTex = source.texture;
            var srcRect = source.rect;
            int w = (int)srcRect.width;
            int h = (int)srcRect.height;

            // RenderTexture에 좌우 반전 blit (scale=(-1,1), offset=(1,0) → UV X 반전)
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);

            // source sprite의 rect 영역만 사용하는 sub UV 계산
            float u0 = srcRect.x / srcTex.width;
            float v0 = srcRect.y / srcTex.height;
            float uW = srcRect.width / srcTex.width;
            float vH = srcRect.height / srcTex.height;
            // Blit의 scale/offset은 (dst UV) → (src UV) 매핑. X만 반전:
            Graphics.Blit(srcTex, rt, new Vector2(-uW, vH), new Vector2(u0 + uW, v0));

            // RenderTexture → Texture2D 추출
            var newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            newTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            newTex.Apply();
            newTex.hideFlags = HideFlags.HideAndDontSave;
            newTex.name = source.name + "_MirroredTex";

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            var pivotNormalized = new Vector2(source.pivot.x / srcRect.width, source.pivot.y / srcRect.height);
            var mirrored = Sprite.Create(newTex, new Rect(0, 0, w, h), pivotNormalized, source.pixelsPerUnit);
            mirrored.hideFlags = HideFlags.HideAndDontSave;
            mirrored.name = source.name + "_Mirrored";
            return mirrored;
        }

        /// <summary>
        /// 공통 sprite Image GameObject 생성기. cubeContainer 자식으로 anchor=top-left, pivot(0,1) 정렬.
        /// 에디터 모드에선 HideAndDontSave 플래그로 prefab 저장에서 제외.
        /// </summary>
        private GameObject CreateSpriteImage(string goName, Sprite sprite, float anchoredX, float anchoredY, float width, float height)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            if (!Application.isPlaying)
                go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_cubeContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(anchoredX, anchoredY);
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
            return go;
        }

        /// <summary>
        /// 런타임 전용: 해당 모듈이 clickable(모듈 설정 툴 토글)이면 raycast 켜고 ModuleClickable 부착.
        /// 클릭 시 방 정보 팝업. 드래그는 IDragHandler 없어서 상위 StageViewport로 전달됨.
        /// </summary>
        private void TryMakeClickable(GameObject go, string spriteName, int floorIndex)
        {
            if (!Application.isPlaying || go == null) return;
            var md = ModuleDatabase.GetBySpriteName(spriteName);
            if (md == null || !md.clickable) return;
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            var clk = go.AddComponent<ModuleClickable>();
            clk.spriteName = spriteName;
            clk.floorIndex = floorIndex;
        }

        /// <summary>
        /// 라벨로 sprite 일괄 로드. 라벨 미등록/0개면 null 반환 (silent skip).
        /// Play Mode: Addressables 사용. Edit Mode(Prefab Edit Mode 포함): AssetDatabase 동기 로드 — Addressables가 Edit Mode에서 안정적이지 않음.
        /// </summary>
        private async UniTask<IList<Sprite>> LoadSpritesByLabelAsync(string label)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return LoadSpritesByLabelEditor(label);
            }
#endif
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(Sprite));
            await locationsHandle.Task;
            bool exists = locationsHandle.Status == AsyncOperationStatus.Succeeded
                          && locationsHandle.Result != null
                          && locationsHandle.Result.Count > 0;
            Addressables.Release(locationsHandle);
            if (!exists) return null;

            var handle = Addressables.LoadAssetsAsync<Sprite>(label, null);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.Result.Count == 0)
                return null;
            return handle.Result;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Edit Mode 전용: Addressables Settings에서 라벨 매칭된 entry들을 AssetDatabase로 동기 로드.
        /// Prefab Edit Mode 미리보기에서 sprite 표시되도록.
        /// </summary>
        private IList<Sprite> LoadSpritesByLabelEditor(string label)
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return null;

            var result = new List<Sprite>();
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;
                    if (!entry.labels.Contains(label)) continue;
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(path)) continue;
                    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) result.Add(sprite);
                }
            }
            return result.Count > 0 ? result : null;
        }
#endif

        /// <summary>
        /// sprite name 끝의 숫자 ID 추출 (예: "Structural_Empty_001" → 1).
        /// </summary>
        private static bool TryParseTrailingId(string name, out int id)
        {
            id = 0;
            int underscoreIdx = name.LastIndexOf('_');
            if (underscoreIdx < 0 || underscoreIdx == name.Length - 1) return false;
            return int.TryParse(name.Substring(underscoreIdx + 1), out id);
        }

        private IEnumerator RetryNextFrame()
        {
            yield return null;
            RenderBuilding();
        }

        private void ClearCubes()
        {
            for (int i = _cubeContainer.childCount - 1; i >= 0; i--)
            {
                var child = _cubeContainer.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        private void CreateCubeImage(CubeType type, int col, int floorIdx, float cubeWidth, float cubeHeight, float ceilingHeight, float stackHeight)
        {
            var go = new GameObject($"Cube_{col}_{floorIdx}", typeof(RectTransform), typeof(Image));
            // 에디터 모드(Prefab Edit Mode 등): 미리보기만 — prefab에 저장 X, Hierarchy에 안 보임
            if (!Application.isPlaying)
                go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_cubeContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);     // 좌상단 기준
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            // cube top y = ceiling 한 row 만큼 내려옴 (cube 위에 ceiling row가 자리 잡음)
            rt.anchoredPosition = new Vector2(col * cubeWidth, -floorIdx * stackHeight - ceilingHeight + _currentYOffset);
            rt.sizeDelta = new Vector2(cubeWidth, cubeHeight);

            var img = go.GetComponent<Image>();
            img.color = GetCubeColor(type);
            img.raycastTarget = false;
        }

        private Color GetCubeColor(CubeType type)
        {
            // 모든 cube placeholder는 투명. sprite(빈 모듈/외벽 등)가 cube 위에 별도 GameObject로 덮여 시각 표현.
            // sprite 미적용 영역은 그대로 빈 영역으로 보여 BackgroundGroup이 드러남.
            return Color.clear;
        }
    }
}
