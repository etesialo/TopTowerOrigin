using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 기기 세이프에어리어(노치/홈바 등)에 맞춰 RectTransform 앵커를 자동 조정.
    /// InGameScene의 UICanvas 아래 SafeArea 오브젝트에 부착. UI/HUD는 이 안에 배치.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafe;
        private int _lastW, _lastH;

        private void Awake() { _rt = GetComponent<RectTransform>(); Apply(); }
        private void OnEnable() { Apply(); }

        private void Update()
        {
            if (Screen.safeArea != _lastSafe || Screen.width != _lastW || Screen.height != _lastH)
                Apply();
        }

        private void Apply()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var sa = Screen.safeArea;
            _lastSafe = sa; _lastW = Screen.width; _lastH = Screen.height;

            Vector2 min = sa.position;
            Vector2 max = sa.position + sa.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
