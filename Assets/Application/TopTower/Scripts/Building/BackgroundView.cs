using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// StageData의 StageID에 따라 배경 sprite 3종(main/sky/under)을 Addressables 라벨(`Background`)로 일괄 로드.
    /// sprite name 규칙: Stage_{NNN}_Background_Main_001, Stage_{NNN}_Background_Sky_001, Stage_{NNN}_Background_Under_001.
    /// 해당 stage + 종류 매칭되는 sprite를 BackgroundGroup의 Image들에 할당.
    /// 미등록/누락 시 silent skip.
    /// </summary>
    public class BackgroundView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private StageData _stageData;

        [Header("References (BackgroundGroup의 자식 Image들)")]
        [SerializeField] private Image _mainImage;
        [SerializeField] private Image _skyImage;
        [SerializeField] private Image _undergroundImage;

        private const string BackgroundLabel = "Background";
        // sprite name 매칭용 (stage 번호는 런타임에 채움)
        // 패턴: {NNN}_Background_{Type}_{ID} (예: 001_Background_Main_001)
        private const string MainNamePattern = "{0:D3}_Background_Main_";
        private const string SkyNamePattern = "{0:D3}_Background_Sky_";
        private const string UnderNamePattern = "{0:D3}_Background_Under_";

        [Header("동적 높이 조정")]
        [Tooltip("빌딩 높이의 몇 배만큼 sky/under 영역을 확보할지")]
        [SerializeField] private float _heightMargin = 1.5f;
        [Tooltip("빌딩이 작아도 보장할 최소 sky/under 높이 (픽셀)")]
        [SerializeField] private float _minHeight = 5000f;

        private void Awake()
        {
            AutoFindReferences();
        }

        private async void Start()
        {
            await LoadBackgroundsAsync();
        }

        /// <summary>
        /// 빌딩 높이에 따라 sky/under RectTransform 세로 자동 조정 + Main을 기준으로 sky/under 위치 스냅.
        /// BuildingView가 RenderBuilding 후 호출.
        /// </summary>
        public void AdjustHeights(float buildingHeight)
        {
            float marginedHeight = Mathf.Max(buildingHeight * _heightMargin, _minHeight);

            SetHeight(_skyImage, marginedHeight);
            SetHeight(_undergroundImage, marginedHeight);

            SnapSkyAndUnderToMain();
        }

        /// <summary>
        /// BackgroundGroup RT 전체를 y만큼 시프트 (BuildingView의 SetShiftY와 동기화 — 한 몸 이동).
        /// BackgroundGroup이 _mainImage의 부모 RT라 그것을 직접 이동.
        /// </summary>
        public void SetShiftY(float shift)
        {
            // BackgroundGroup = mainImage의 부모. 그것을 시프트.
            if (_mainImage == null) return;
            var bgGroup = _mainImage.rectTransform.parent as RectTransform;
            if (bgGroup == null) return;
            var pos = bgGroup.anchoredPosition;
            pos.y = shift;
            bgGroup.anchoredPosition = pos;
        }

        /// <summary>
        /// BackgroundGroup RT를 x만큼 시프트 (BuildingView.SetShiftX와 동기화 — 한 몸 이동).
        /// 가로 드래그(복귀형)에서 호출. y는 건드리지 않음.
        /// </summary>
        public void SetShiftX(float shift)
        {
            if (_mainImage == null) return;
            var bgGroup = _mainImage.rectTransform.parent as RectTransform;
            if (bgGroup == null) return;
            var pos = bgGroup.anchoredPosition;
            pos.x = shift;
            bgGroup.anchoredPosition = pos;
        }

        /// <summary>
        /// BackgroundGroup 전체 localScale 설정 (빌딩 줌과 동기화 — 한 몸 확대/축소).
        /// </summary>
        public void SetZoom(float zoom)
        {
            if (_mainImage == null) return;
            var bgGroup = _mainImage.rectTransform.parent as RectTransform;
            if (bgGroup == null) return;
            bgGroup.localScale = new Vector3(zoom, zoom, 1f);
        }

        /// <summary>
        /// Main image의 sizeDelta.y * 0.5 반환. TopTowerIngame이 Main bottom y 계산에 사용.
        /// </summary>
        public float GetMainImageHalfHeight()
        {
            if (_mainImage == null) return 0f;
            return _mainImage.rectTransform.sizeDelta.y * 0.5f;
        }

        /// <summary>
        /// Main의 anchoredPosition.y를 절대 y로 설정. Sky/Underground는 자동으로 Main 위/아래에 스냅.
        /// TopTowerIngame이 stage 로드 후 호출.
        /// </summary>
        public void SetOriginY(float mainAnchoredY)
        {
            if (_mainImage != null)
            {
                var mainRt = _mainImage.rectTransform;
                var pos = mainRt.anchoredPosition;
                pos.y = mainAnchoredY;
                mainRt.anchoredPosition = pos;
            }
            SnapSkyAndUnderToMain();
        }

        /// <summary>
        /// Sky bottom = Main top, Underground top = Main bottom 자동 정렬.
        /// pivot 가정: Sky (0.5, 0) bottom, Main (0.5, 0.5) center, Under (0.5, 1) top.
        /// </summary>
        private void SnapSkyAndUnderToMain()
        {
            if (_mainImage == null) return;
            var mainRt = _mainImage.rectTransform;
            float mainTop = mainRt.anchoredPosition.y + mainRt.sizeDelta.y * 0.5f;
            float mainBottom = mainRt.anchoredPosition.y - mainRt.sizeDelta.y * 0.5f;

            if (_skyImage != null)
            {
                var skyRt = _skyImage.rectTransform;
                var pos = skyRt.anchoredPosition;
                pos.y = mainTop; // sky pivot bottom → 이 y가 sky 바닥
                skyRt.anchoredPosition = pos;
            }
            if (_undergroundImage != null)
            {
                var underRt = _undergroundImage.rectTransform;
                var pos = underRt.anchoredPosition;
                pos.y = mainBottom; // under pivot top → 이 y가 under 윗면
                underRt.anchoredPosition = pos;
            }
        }

        private void SetHeight(Image img, float height)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            var size = rt.sizeDelta;
            size.y = height;
            rt.sizeDelta = size;
        }

        public async UniTask LoadBackgroundsAsync()
        {
            if (_stageData == null)
            {
                Debug.LogWarning("[BackgroundView] StageData가 없습니다.");
                return;
            }

            int stageID = _stageData.StageID;

            // 라벨 Background로 모든 배경 sprite 일괄 로드
            var locationsHandle = Addressables.LoadResourceLocationsAsync(BackgroundLabel, typeof(Sprite));
            await locationsHandle.Task;
            bool exists = locationsHandle.Status == AsyncOperationStatus.Succeeded
                          && locationsHandle.Result != null
                          && locationsHandle.Result.Count > 0;
            Addressables.Release(locationsHandle);
            if (!exists) return; // 라벨 미등록 — silent skip

            var handle = Addressables.LoadAssetsAsync<Sprite>(BackgroundLabel, null);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null) return;

            string mainPrefix = string.Format(MainNamePattern, stageID);
            string skyPrefix = string.Format(SkyNamePattern, stageID);
            string underPrefix = string.Format(UnderNamePattern, stageID);

            AssignFirstMatch(handle.Result, mainPrefix, _mainImage);
            AssignFirstMatch(handle.Result, skyPrefix, _skyImage);
            AssignFirstMatch(handle.Result, underPrefix, _undergroundImage);
        }

        private void AssignFirstMatch(System.Collections.Generic.IList<Sprite> sprites, string namePrefix, Image targetImage)
        {
            if (targetImage == null) return;
            var sprite = sprites.FirstOrDefault(s => s != null && s.name.StartsWith(namePrefix));
            if (sprite != null)
                targetImage.sprite = sprite;
        }

        /// <summary>
        /// 인스펙터 참조가 비어있으면 BackgroundGroup의 자식에서 이름으로 자동 탐색.
        /// </summary>
        private void AutoFindReferences()
        {
            Transform bgGroup = null;

            // 자기 자신이 BackgroundGroup이거나, 형제/자식에서 탐색
            if (transform.name == "BackgroundGroup") bgGroup = transform;
            else bgGroup = transform.Find("BackgroundGroup");

            if (bgGroup == null)
            {
                // 부모 체인에서 찾기
                Transform t = transform.parent;
                while (t != null)
                {
                    var found = t.Find("BackgroundGroup");
                    if (found != null) { bgGroup = found; break; }
                    t = t.parent;
                }
            }

            if (bgGroup == null) return;

            if (_mainImage == null)
            {
                var n = bgGroup.Find("Background_Main");
                if (n != null) _mainImage = n.GetComponent<Image>();
            }
            if (_skyImage == null)
            {
                var n = bgGroup.Find("Background_Sky");
                if (n != null) _skyImage = n.GetComponent<Image>();
            }
            if (_undergroundImage == null)
            {
                var n = bgGroup.Find("Background_Underground");
                if (n != null) _undergroundImage = n.GetComponent<Image>();
            }
        }
    }
}
