using TMPro;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 골드 UI 표시. GoldManager 구독 → 숫자 텍스트 갱신.
    /// 아이콘/배경은 추후 이미지로 교체 예정 (지금은 도형+텍스트 placeholder).
    /// </summary>
    public class GoldView : MonoBehaviour
    {
        [Tooltip("골드 숫자 표시 TMP 텍스트.")]
        [SerializeField] private TMP_Text _text;

        private GoldManager _mgr;

        private void Start()
        {
            _mgr = GoldManager.Instance != null ? GoldManager.Instance : Object.FindObjectOfType<GoldManager>();
            if (_mgr != null)
            {
                _mgr.OnGoldChanged += Refresh;
                Refresh(_mgr.Gold);
            }
        }

        private void OnDestroy()
        {
            if (_mgr != null) _mgr.OnGoldChanged -= Refresh;
        }

        private void Refresh(long gold)
        {
            if (_text != null) _text.text = gold.ToString();
        }
    }
}
