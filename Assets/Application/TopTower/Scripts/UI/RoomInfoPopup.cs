using TMPro;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 방 정보 팝업. InGameScene UICanvas에 배치(기본 숨김).
    /// 실내 모듈 클릭 시 ModuleClickable이 ShowFor 호출 → 방 이름/설명/일일 임대료 표시.
    /// 도형 placeholder — 배경/아이콘은 추후 이미지로 교체.
    /// </summary>
    public class RoomInfoPopup : MonoBehaviour
    {
        private static RoomInfoPopup _inst;

        [Tooltip("표시/숨김할 팝업 루트 (닫기 시 비활성).")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _rentText;

        private void Awake()
        {
            _inst = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy() { if (_inst == this) _inst = null; }

        public static void ShowFor(ModuleData data, string fallbackName)
        {
            if (_inst != null) _inst.Show(data, fallbackName);
        }

        private void Show(ModuleData data, string fallbackName)
        {
            if (_panel != null) _panel.SetActive(true);

            string title = (data != null && !string.IsNullOrEmpty(data.roomName)) ? data.roomName : fallbackName;
            if (_titleText != null) _titleText.text = title;
            if (_descText != null) _descText.text = data != null ? data.description : "";
            long rent = data != null ? data.dailyRent : 0;
            if (_rentText != null) _rentText.text = "일일 임대료: " + rent.ToString();
        }

        /// <summary>닫기 버튼 OnClick에 연결.</summary>
        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
