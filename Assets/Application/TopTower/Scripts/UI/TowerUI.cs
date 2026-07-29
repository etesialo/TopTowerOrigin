using UnityEngine;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// InGameScene의 UI 담당. 홈 버튼 등 UI는 스테이지(프리팹)와 분리되어 여기 소속.
    /// 홈 버튼 클릭 시 현재 로드된 BuildingView를 찾아 MoveToHomeY() 호출 (동작은 기존과 동일).
    /// </summary>
    public class TowerUI : MonoBehaviour
    {
        [Tooltip("홈 버튼 (UICanvas/SafeArea 안의 Button).")]
        [SerializeField] private Button _homeButton;

        private void Awake()
        {
            if (_homeButton != null) _homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnDestroy()
        {
            if (_homeButton != null) _homeButton.onClick.RemoveListener(OnHomeClicked);
        }

        private void OnHomeClicked()
        {
            var bv = Object.FindObjectOfType<BuildingView>();
            if (bv != null) bv.MoveToHomeY();
        }
    }
}
