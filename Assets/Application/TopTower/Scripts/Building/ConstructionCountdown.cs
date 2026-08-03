using TMPro;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>
    /// 실내공사모듈 위에 남은 건설 시간을 실시간 표시. BuildManager.RemainingSeconds(floor)를 매 프레임 읽어 갱신.
    /// </summary>
    public class ConstructionCountdown : MonoBehaviour
    {
        public int floorIndex;
        public int cellIndex;
        [SerializeField] private TMP_Text _text;

        public void Init(int floor, int cell, TMP_Text text)
        {
            floorIndex = floor;
            cellIndex = cell;
            _text = text;
        }

        private void Update()
        {
            if (_text == null) return;
            var bm = BuildManager.Instance;
            float remain = bm != null ? bm.RemainingSeconds(floorIndex, cellIndex) : 0f;
            _text.text = Mathf.CeilToInt(remain) + "s";
        }
    }
}
