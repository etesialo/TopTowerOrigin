using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

namespace KS.TopTower
{
    /// <summary>
    /// 빌딩 입력 컨트롤러:
    ///   - 세로 드래그(1손가락/마우스) → 스크롤. 한계 시 고무줄 효과.
    ///   - 가로 드래그 → 드래그 동안만 옆으로 밈(복귀형). 손 떼면 정해진 홈 x로 되돌아감.
    ///   - 마우스 휠 → 줌 인/아웃 (화면 중앙 고정).
    ///   - 두 손가락 핀치 → 줌 인/아웃 (모바일).
    /// StageViewport (Stage_001.prefab)에 자동 부착 — raycast 받기 위해 같은 GameObject에 투명 Image 필요.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BuildingDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("참조 (비워두면 자동 탐색)")]
        [SerializeField] private BuildingView _buildingView;

        [Header("줌 감도")]
        [Tooltip("마우스 휠 1 노치당 줌 배율 변화량 (0.1 = 10% 변화).")]
        [SerializeField] private float _wheelZoomStep = 0.1f;
        [Tooltip("핀치 줌 감도 — 두 손가락 거리 변화에 곱해지는 계수.")]
        [SerializeField] private float _pinchZoomSensitivity = 1f;

        [Header("고무줄 효과")]
        [Tooltip("한계 초과 시 드래그 효과 감쇠율 (0~1, 작을수록 한계에서 강하게 저항)")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _rubberBandResistance = 0.3f;
        [Tooltip("한계 초과 후 손 떼면 자동 복귀 시간 (초)")]
        [SerializeField] private float _reboundDuration = 0.25f;

        [Header("가로 드래그 (복귀형)")]
        [Tooltip("가로로 밀 수 있는 최대 거리(px). 이 범위를 넘으면 고무줄 저항. 손 떼면 홈 x로 복귀.")]
        [SerializeField] private float _maxHorizontalPeek = 400f;

        private bool _isDragging;
        private RectTransform _viewportRt;
        private float _previousPinchDistance;

        // 가로 복귀 목표(정해진 x). 최초 드래그 시 현재 x를 캡처.
        private float _homeX;
        private bool _homeXCaptured;

        private void Awake()
        {
            _viewportRt = transform as RectTransform;
            if (_buildingView == null)
                _buildingView = Object.FindObjectOfType<BuildingView>();
        }

        private void Update()
        {
            if (_buildingView == null) return;

            // 핀치 줌 (모바일) — 두 손가락 활성 시 드래그보다 우선
            if (Input.touchCount == 2)
            {
                HandlePinchZoom();
                return;
            }
            _previousPinchDistance = 0f;

            // 마우스 휠 줌 (PC) — 드래그 중엔 무시
            if (_isDragging) return;
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(wheel, 0f)) return;
            // wheel > 0 → 확대, < 0 → 축소
            float factor = 1f + wheel * _wheelZoomStep;
            ApplyZoomMultiplier(factor);
        }

        /// <summary>
        /// 두 손가락 거리 변화량 → 줌 배율로 환산해 적용.
        /// </summary>
        private void HandlePinchZoom()
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float currentDistance = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began || _previousPinchDistance <= 0f)
            {
                _previousPinchDistance = currentDistance;
                return;
            }

            if (currentDistance <= 0f) return;
            float ratio = currentDistance / _previousPinchDistance;
            // sensitivity 적용 — 1.0이면 거리 비율 그대로, >1이면 더 민감
            float factor = 1f + (ratio - 1f) * _pinchZoomSensitivity;
            ApplyZoomMultiplier(factor);
            _previousPinchDistance = currentDistance;
        }

        private void ApplyZoomMultiplier(float factor)
        {
            float newZoom = _buildingView.CurrentZoom * factor;
            _buildingView.SetZoom(newZoom);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            if (!_homeXCaptured && _buildingView != null)
            {
                _homeX = _buildingView.ShiftX;   // 정해진 x = 시작 시점의 x
                _homeXCaptured = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_buildingView == null) return;
            if (Input.touchCount >= 2) return; // 핀치 중엔 드래그 무시

            // 세로: 한계 + 고무줄 (유지)
            float deltaY = eventData.delta.y;
            float currentShift = _buildingView.ShiftY;
            (float minShift, float maxShift) = CalculateLimits();

            float newShift = currentShift + deltaY;
            if (newShift > maxShift)
            {
                float over = newShift - maxShift;
                newShift = maxShift + over * _rubberBandResistance;
            }
            else if (newShift < minShift)
            {
                float over = minShift - newShift;
                newShift = minShift - over * _rubberBandResistance;
            }
            _buildingView.SetShiftY(newShift);

            // 가로: 홈 x 기준 ±_maxHorizontalPeek 범위 내에서만 따라오고, 넘으면 고무줄 (손 떼면 복귀)
            float deltaX = eventData.delta.x;
            float newX = _buildingView.ShiftX + deltaX;
            float maxX = _homeX + _maxHorizontalPeek;
            float minX = _homeX - _maxHorizontalPeek;
            if (newX > maxX)
            {
                float over = newX - maxX;
                newX = maxX + over * _rubberBandResistance;
            }
            else if (newX < minX)
            {
                float over = minX - newX;
                newX = minX - over * _rubberBandResistance;
            }
            _buildingView.SetShiftX(newX);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            ReboundAsync().Forget();
        }

        /// <summary>
        /// 손 뗀 뒤 부드럽게 복귀:
        ///  - 세로: 한계 초과분만 한계 안으로.
        ///  - 가로: 항상 홈 x로 (드래그 동안만 옆으로 밀리는 복귀형).
        /// </summary>
        private async UniTask ReboundAsync()
        {
            if (_buildingView == null) return;
            (float minShift, float maxShift) = CalculateLimits();

            float startY = _buildingView.ShiftY;
            float targetY = Mathf.Clamp(startY, minShift, maxShift);
            float startX = _buildingView.ShiftX;
            float targetX = _homeX;

            if (Mathf.Approximately(startY, targetY) && Mathf.Approximately(startX, targetX)) return;

            float elapsed = 0f;
            while (elapsed < _reboundDuration)
            {
                if (_isDragging) return; // 다시 드래그 시작하면 취소
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _reboundDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _buildingView.SetShiftY(Mathf.Lerp(startY, targetY, eased));
                _buildingView.SetShiftX(Mathf.Lerp(startX, targetX, eased));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _buildingView.SetShiftY(targetY);
            _buildingView.SetShiftX(targetX);
        }

        /// <summary>
        /// shift 한계 계산:
        ///  - 위로 한계 (maxShift): 빌딩 최상층(옥상) cube top이 viewport top 아래로 안 내려감 (= 옥상이 화면 위쪽에 보임)
        ///  - 아래로 한계 (minShift): 빌딩 최하층 cube bottom이 viewport bottom 위로 안 올라감
        /// </summary>
        private (float min, float max) CalculateLimits()
        {
            if (_buildingView == null || _viewportRt == null) return (0f, 0f);

            float zoom = _buildingView.CurrentZoom;
            // 줌 적용: cubeContainer.localScale이 zoom이므로 자식들의 visual 좌표가 zoom배 확장됨.
            float originY = _buildingView.OriginY * zoom;
            float totalHeight = _buildingView.GetTotalBuildingHeight() * zoom;
            float halfViewport = _viewportRt.rect.height * 0.5f;

            float maxShift = halfViewport - originY;                 // 빌딩 1F bottom이 viewport top까지
            float minShift = halfViewport - originY - totalHeight;   // 빌딩 top이 viewport bottom까지

            if (maxShift < minShift)
            {
                float mid = (maxShift + minShift) * 0.5f;
                return (mid, mid);
            }
            return (minShift, maxShift);
        }
    }
}
