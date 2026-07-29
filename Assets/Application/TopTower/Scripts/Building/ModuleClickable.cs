using UnityEngine;
using UnityEngine.EventSystems;

namespace KS.TopTower
{
    /// <summary>
    /// 클릭 가능한 실내 모듈 스프라이트에 부착(BuildingView가 clickable 모듈에만 부착).
    /// <b>1.5초 이상 꾹 누르면(long-press)</b> 방 정보 팝업. 짧은 클릭/드래그는 무시(오터치 방지).
    /// IDragHandler를 구현하지 않으므로 드래그는 상위(StageViewport)로 전달됨.
    /// </summary>
    public class ModuleClickable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public string spriteName;   // 예: "Facility_Empty_002" → ModuleDatabase 조회 키

        [Tooltip("팝업이 뜨기까지 눌러야 하는 시간(초).")]
        [SerializeField] private float _holdSeconds = 1.5f;

        // 이 거리 이상 움직이면 드래그로 간주 → long-press 취소
        private const float MoveCancelThreshold = 15f;

        private bool _pressing;
        private bool _fired;
        private float _pressStartTime;
        private Vector2 _pressScreenPos;

        public void OnPointerDown(PointerEventData e)
        {
            _pressing = true;
            _fired = false;
            _pressStartTime = Time.unscaledTime;
            _pressScreenPos = e.position;
        }

        public void OnPointerUp(PointerEventData e)
        {
            _pressing = false;   // 1.5초 전에 떼면 취소
        }

        private void Update()
        {
            if (!_pressing || _fired) return;

            // 누른 채 이동하면 드래그로 판단 → 취소 (빌딩 스크롤 중 오작동 방지)
            Vector2 cur = Input.mousePosition;
            if (Vector2.Distance(cur, _pressScreenPos) > MoveCancelThreshold)
            {
                _pressing = false;
                return;
            }

            if (Time.unscaledTime - _pressStartTime >= _holdSeconds)
            {
                _fired = true;
                _pressing = false;
                var data = ModuleDatabase.GetBySpriteName(spriteName);
                RoomInfoPopup.ShowFor(data, spriteName);
            }
        }
    }
}
