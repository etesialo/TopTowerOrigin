using UnityEngine;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// BackgroundGroup이 StageViewport 밖(Stage_001 root)에 있어서 자동 스크롤이 안 됨.
    /// ScrollRect의 Content(ScrollableContent) Y 위치를 BackgroundGroup에 동기화하여 같이 스크롤되게 함.
    /// </summary>
    public class BackgroundScrollSync : MonoBehaviour
    {
        [Tooltip("동기화할 ScrollRect (보통 StageViewport의 ScrollRect).")]
        [SerializeField] private ScrollRect _scrollRect;

        [Tooltip("같이 스크롤시킬 배경 그룹의 RectTransform.")]
        [SerializeField] private RectTransform _backgroundGroup;

        private void Awake()
        {
            AutoFindReferences();
        }

        private void OnEnable()
        {
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.AddListener(OnScrollChanged);
                SyncOnce();
            }
        }

        private void OnDisable()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        /// <summary>
        /// 인스펙터 참조가 비어 있으면 같은 prefab 내에서 자동 탐색.
        /// </summary>
        private void AutoFindReferences()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponentInChildren<ScrollRect>(true);

            if (_backgroundGroup == null)
            {
                var bg = transform.Find("BackgroundGroup");
                if (bg != null) _backgroundGroup = bg as RectTransform;
            }
        }

        private void OnScrollChanged(Vector2 _)
        {
            SyncOnce();
        }

        private void SyncOnce()
        {
            if (_scrollRect == null || _scrollRect.content == null || _backgroundGroup == null) return;

            var contentY = _scrollRect.content.anchoredPosition.y;
            var bgPos = _backgroundGroup.anchoredPosition;
            bgPos.y = contentY;
            _backgroundGroup.anchoredPosition = bgPos;
        }
    }
}
