using UnityEditor;
using UnityEngine;

namespace KS.TopTower.EditorTools
{
    /// <summary>개발용 세이브 도구.</summary>
    public static class SaveDevMenu
    {
        [MenuItem("Tools/Top Tower/Reset Save (세이브 초기화)")]
        public static void ResetSave()
        {
            SaveSystem.Delete();
            Debug.Log("[SaveDevMenu] 세이브 삭제 완료. 다음 Play는 신규 시작(시작 보상 지급).");
        }
    }
}
