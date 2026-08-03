using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KS.TopTower
{
    /// <summary>
    /// 오프라인 복귀 보상 팝업. 씬 배치 불필요 — Show()가 오버레이 캔버스/UI를 코드로 생성.
    /// 기본 오프라인 골드는 이미 지급된 상태로 '안내'만 하며, [2배(광고)]는 Phase 3에서 연결(현재 비활성).
    /// </summary>
    public class OfflineRewardPopup : MonoBehaviour
    {
        public static void Show(long gold, long seconds)
        {
            if (gold <= 0) return;
            var go = new GameObject("OfflineRewardPopup");
            var p = go.AddComponent<OfflineRewardPopup>();
            p.Build(gold, seconds);
        }

        private void Build(long gold, long seconds)
        {
            // 오버레이 캔버스
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // 어둡게 배경 (클릭 차단)
            var dim = CreateChild("Dim", transform);
            StretchFull(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.6f);

            // 패널
            var panel = CreateChild("Panel", transform);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(760, 520);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.16f, 0.18f, 0.26f, 1f);

            // 제목
            var title = CreateText("Title", panel, "오프라인 수익", 46, TextAlignmentOptions.Center);
            title.anchorMin = new Vector2(0, 1); title.anchorMax = new Vector2(1, 1); title.pivot = new Vector2(0.5f, 1);
            title.anchoredPosition = new Vector2(0, -40); title.sizeDelta = new Vector2(-40, 70);

            // 본문
            string body = "자리를 비운 사이\n" + FormatDuration(seconds) + " 동안\n" +
                          "<size=64><color=#FFD34D>" + gold.ToString("N0") + "</color></size> 골드를 벌었어요!";
            var msg = CreateText("Body", panel, body, 34, TextAlignmentOptions.Center);
            msg.anchorMin = new Vector2(0, 0.35f); msg.anchorMax = new Vector2(1, 0.85f);
            msg.offsetMin = new Vector2(30, 0); msg.offsetMax = new Vector2(-30, 0);

            // [받기]
            var receive = CreateButton("Receive", panel, "받기", new Color(0.23f, 0.55f, 0.35f));
            receive.anchorMin = new Vector2(0.5f, 0); receive.anchorMax = new Vector2(0.5f, 0);
            receive.pivot = new Vector2(0.5f, 0);
            receive.anchoredPosition = new Vector2(-190, 40); receive.sizeDelta = new Vector2(340, 96);
            receive.GetComponent<Button>().onClick.AddListener(Close);

            // [2배 받기 (광고)] — Phase 3 연결 예정, 지금은 비활성
            var dbl = CreateButton("Double", panel, "2배 받기 (광고)", new Color(0.30f, 0.32f, 0.42f));
            dbl.anchorMin = new Vector2(0.5f, 0); dbl.anchorMax = new Vector2(0.5f, 0);
            dbl.pivot = new Vector2(0.5f, 0);
            dbl.anchoredPosition = new Vector2(190, 40); dbl.sizeDelta = new Vector2(340, 96);
            dbl.GetComponent<Button>().interactable = false;   // Phase 3에서 광고 연동
        }

        private void Close()
        {
            Destroy(gameObject);
        }

        // ── 시간 포맷 ──
        private static string FormatDuration(long seconds)
        {
            if (seconds < 60) return seconds + "초";
            long m = seconds / 60, h = m / 60; m %= 60;
            if (h > 0) return h + "시간 " + m + "분";
            return m + "분";
        }

        // ── UI 생성 헬퍼 ──
        private static RectTransform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var rt = CreateChild(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text; tmp.fontSize = size; tmp.color = Color.white;
            tmp.alignment = align; tmp.richText = true; tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            return rt;
        }

        private static RectTransform CreateButton(string name, Transform parent, string label, Color color)
        {
            var rt = CreateChild(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var txt = CreateText("Label", rt, label, 30, TextAlignmentOptions.Center);
            StretchFull(txt);
            return rt;
        }
    }
}
