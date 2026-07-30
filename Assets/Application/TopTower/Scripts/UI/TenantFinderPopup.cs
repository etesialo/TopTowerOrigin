using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// 임차인 찾기 팝업. 그룹 탭(시설/식당/상업/사무/주거/호텔) + 모듈 목록(공사비/임대료) + Y/N 확인.
    /// 탭 버튼과 목록 행은 런타임 생성. Yes → BuildManager.StartBuild → 모든 팝업 닫힘 + 카메라 이동.
    /// 도형 placeholder — 배경/아이콘은 추후 이미지로 교체.
    /// </summary>
    public class TenantFinderPopup : MonoBehaviour
    {
        private static TenantFinderPopup _inst;

        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _tabBar;       // 탭 버튼 부모
        [SerializeField] private RectTransform _listContent;  // 모듈 목록 부모
        [SerializeField] private GameObject _confirmPanel;
        [SerializeField] private TMP_Text _confirmText;
        [SerializeField] private Button _confirmYes;
        [SerializeField] private Button _confirmNo;

        private static readonly ModuleGroup[] Groups =
            { ModuleGroup.Facility, ModuleGroup.Restaurant, ModuleGroup.Commercial, ModuleGroup.Office, ModuleGroup.Residence, ModuleGroup.Hotel };
        private static readonly string[] GroupLabels =
            { "시설", "식당", "상업", "사무", "주거", "호텔" };

        private int _floorIndex;
        private ModuleData _pending;
        private bool _tabsBuilt;
        private ModuleGroup _lastGroup = ModuleGroup.Restaurant;

        private static TMP_FontAsset Font { get { return TMP_Settings.defaultFontAsset; } }

        private void Awake()
        {
            _inst = this;
            if (_panel != null) _panel.SetActive(false);
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
            if (_confirmYes != null) _confirmYes.onClick.AddListener(OnYes);
            if (_confirmNo != null) _confirmNo.onClick.AddListener(OnNo);
            EnsureTabs();
        }

        private void OnDestroy()
        {
            if (_confirmYes != null) _confirmYes.onClick.RemoveListener(OnYes);
            if (_confirmNo != null) _confirmNo.onClick.RemoveListener(OnNo);
            if (_inst == this) _inst = null;
        }

        public static void Open(int floorIndex)
        {
            if (_inst != null) _inst.OpenInternal(floorIndex);
        }

        private void OpenInternal(int floorIndex)
        {
            _floorIndex = floorIndex;
            if (_panel != null) _panel.SetActive(true);
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
            EnsureTabs();
            ShowGroup(_lastGroup);   // 마지막으로 선택한 탭 유지
        }

        public void Close()   // X 버튼
        {
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
            if (_panel != null) _panel.SetActive(false);
        }

        private void EnsureTabs()
        {
            if (_tabsBuilt || _tabBar == null) return;
            EnsureHorizontalLayout(_tabBar);
            for (int i = 0; i < Groups.Length; i++)
            {
                ModuleGroup g = Groups[i];
                var btn = CreateButton(_tabBar, GroupLabels[i], 0, 60, new Color(0.28f, 0.30f, 0.45f));
                btn.onClick.AddListener(delegate { ShowGroup(g); });
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
            }
            _tabsBuilt = true;
        }

        private void ShowGroup(ModuleGroup g)
        {
            _lastGroup = g;
            if (_listContent == null) return;
            EnsureVerticalLayout(_listContent);
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            var mods = ModuleDatabase.GetByGroup(g);
            if (mods.Count == 0)
            {
                CreateLabelRow(_listContent, "(이 그룹에 등록된 모듈이 없습니다)");
                return;
            }
            foreach (var m in mods)
            {
                string name = string.IsNullOrEmpty(m.roomName) ? m.moduleName : m.roomName;
                string label = name + "    공사 " + m.buildCost + " / 임대 " + m.dailyRent;
                ModuleData captured = m;
                var btn = CreateButton(_listContent, label, 0, 84, new Color(0.20f, 0.22f, 0.34f));
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 84;
                btn.onClick.AddListener(delegate { ShowConfirm(captured); });
            }
        }

        private void ShowConfirm(ModuleData m)
        {
            _pending = m;
            string name = string.IsNullOrEmpty(m.roomName) ? m.moduleName : m.roomName;
            if (_confirmText != null)
                _confirmText.text = name + " 을(를) 짓겠습니까?\n공사비 " + m.buildCost + " / 일일 임대 " + m.dailyRent;
            if (_confirmPanel != null) _confirmPanel.SetActive(true);
        }

        private void OnYes()
        {
            if (_pending == null) return;
            bool ok = BuildManager.Instance != null && BuildManager.Instance.StartBuild(_floorIndex, _pending);
            if (ok)
            {
                _pending = null;
                Close();
                RoomInfoPopup.CloseIfOpen();
            }
            else if (_confirmText != null)
            {
                _confirmText.text = "재화가 부족하거나 지을 수 없습니다.";
            }
        }

        private void OnNo()
        {
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
        }

        // ── UI 생성 헬퍼 ──
        private Button CreateButton(RectTransform parent, string label, float width, float height, Color color)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (width > 0) rt.sizeDelta = new Vector2(width, height);
            else rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            var img = go.GetComponent<Image>();
            img.color = color;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)txtGo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(16, 0); trt.offsetMax = new Vector2(-16, 0);
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.font = Font; tmp.text = label; tmp.fontSize = 26; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.raycastTarget = false;
            return btn;
        }

        private void CreateLabelRow(RectTransform parent, string label)
        {
            var go = new GameObject("EmptyLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(rt.sizeDelta.x, 60);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = Font; tmp.text = label; tmp.fontSize = 24; tmp.color = new Color(0.7f, 0.7f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>(); le.minHeight = 60;
        }

        private static void EnsureHorizontalLayout(RectTransform rt)
        {
            var h = rt.GetComponent<HorizontalLayoutGroup>();
            if (h == null)
            {
                h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 6; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
                h.childControlWidth = true; h.childControlHeight = true;
            }
        }

        private static void EnsureVerticalLayout(RectTransform rt)
        {
            var v = rt.GetComponent<VerticalLayoutGroup>();
            if (v == null)
            {
                v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 6; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                v.childControlWidth = true; v.childControlHeight = true;
                v.padding = new RectOffset(8, 8, 8, 8);
            }
        }
    }
}
