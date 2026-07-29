using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 빌딩 출력 기준점(Origin Y) + 빌딩 홈 위치(Home Y) 슬라이더 + 시각 가이드 토글.
    /// IngameScene의 TopTower 오브젝트에 부착.
    /// [ExecuteAlways]로 Scene view에서도 즉시 반영.
    /// </summary>
    [ExecuteAlways]
    public class TowerOrigin : MonoBehaviour
    {
        [Header("고정점 Y축")]
        [Tooltip("빌딩 출력 기준점 — 1F cube bottom 라인의 절대 y 좌표. Background_Main bottom도 이 라인에 정렬.")]
        [Range(-960f, 960f)]
        [SerializeField] private float _originY = -400f;

        [Tooltip("빌딩 홈 Y축 — HomeButton 클릭 시 빌딩이 이동할 위치.")]
        [Range(-960f, 960f)]
        [SerializeField] private float _homeY = 0f;

        [Tooltip("시각 가이드 라인 표시 (핑크=Origin, 연두=Home). 게임 출시 시 끄기.")]
        [SerializeField] private bool _showGuideLines = true;

        [Header("줌 한계")]
        [Tooltip("최소 축소 배율. 1.0 미만이면 축소 가능.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _zoomMin = 0.5f;
        [Tooltip("최대 확대 배율. 1.0 초과이면 확대 가능.")]
        [Range(1f, 5f)]
        [SerializeField] private float _zoomMax = 2f;

        public float OriginY => _originY;
        public float HomeY => _homeY;
        public bool ShowOriginGuideLine => _showGuideLines;
        public bool ShowHomeGuideLine => _showGuideLines;
        public float ZoomMin => _zoomMin;
        public float ZoomMax => _zoomMax;

        private void OnEnable()
        {
            ApplyToBuildingView();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                ApplyToBuildingView();
            };
        }
#endif

        /// <summary>
        /// Scene에서 BuildingView 찾아 origin/home y + 가이드 토글 모두 전달.
        /// BackgroundView도 origin 정렬 적용.
        /// </summary>
        private void ApplyToBuildingView()
        {
            var bv = FindObjectOfType<BuildingView>();
            if (bv == null) return;

            bv.SetShowOriginGuideLine(_showGuideLines);
            bv.SetShowHomeGuideLine(_showGuideLines);
            bv.SetHomeY(_homeY);
            bv.SetOriginY(_originY);
            bv.SetZoomLimits(_zoomMin, _zoomMax);

            var bg = FindObjectOfType<BackgroundView>();
            if (bg != null)
            {
                float mainHalfHeight = bg.GetMainImageHalfHeight();
                bg.SetOriginY(_originY + mainHalfHeight);
            }
        }
    }
}
