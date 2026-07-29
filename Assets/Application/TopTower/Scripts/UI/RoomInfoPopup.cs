using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// 방 정보 팝업. InGameScene UICanvas에 배치(기본 숨김).
    /// 실내 모듈 클릭 시 ModuleClickable이 ShowFor 호출 → 방 이름/설명/일일 임대료 표시.
    /// 빈 방(슬롯 Empty)일 때만 '임차인 찾기' 버튼 표시 → TenantFinderPopup 오픈.
    /// </summary>
    public class RoomInfoPopup : MonoBehaviour
    {
        private static RoomInfoPopup _inst;

        [Tooltip("표시/숨김할 팝업 루트 (닫기 시 비활성).")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _rentText;
        [Tooltip("임차인 찾기 버튼 (빈 방일 때만 표시).")]
        [SerializeField] private Button _findTenantButton;

        private int _floorIndex;

        private void Awake()
        {
            _inst = this;
            if (_panel != null) _panel.SetActive(false);
            if (_findTenantButton != null) _findTenantButton.onClick.AddListener(OnFindTenant);
        }

        private void OnDestroy()
        {
            if (_findTenantButton != null) _findTenantButton.onClick.RemoveListener(OnFindTenant);
            if (_inst == this) _inst = null;
        }

        public static void ShowFor(ModuleData data, string fallbackName, int floorIndex)
        {
            if (_inst != null) _inst.Show(data, fallbackName, floorIndex);
        }

        public static void CloseIfOpen()
        {
            if (_inst != null) _inst.Close();
        }

        private void Show(ModuleData data, string fallbackName, int floorIndex)
        {
            _floorIndex = floorIndex;
            if (_panel != null) _panel.SetActive(true);

            string title = (data != null && !string.IsNullOrEmpty(data.roomName)) ? data.roomName : fallbackName;
            if (_titleText != null) _titleText.text = title;
            if (_descText != null) _descText.text = data != null ? data.description : "";
            long rent = data != null ? data.dailyRent : 0;
            if (_rentText != null) _rentText.text = "일일 임대료: " + rent.ToString();

            // 빈 방일 때만 '임차인 찾기' 표시 (입주완료/공사중이면 숨김)
            if (_findTenantButton != null)
            {
                var bm = BuildManager.Instance;
                var slot = bm != null ? bm.GetSlot(floorIndex) : null;
                bool isEmpty = slot == null || slot.status == BuildManager.SlotStatus.Empty;
                _findTenantButton.gameObject.SetActive(isEmpty);
            }
        }

        private void OnFindTenant()
        {
            Close();
            TenantFinderPopup.Open(_floorIndex);
        }

        /// <summary>닫기 버튼 OnClick에 연결.</summary>
        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
