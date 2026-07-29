using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace KS.TopTower
{
    /// <summary>
    /// 스테이지 로더. InGameScene에 배치.
    /// - 스테이지 프리팹을 직접 드래그하거나(우선), 숫자만 입력해 Addressables로 로드.
    /// - 프리팹을 드래그하면 아래 번호가 자동 동기화된다(Editor OnValidate).
    /// TowerOrigin 등 스테이지별 설정은 스테이지 프리팹 안에 포함되어 있으므로 여기서 건드리지 않는다.
    /// </summary>
    public class TowerStage : MonoBehaviour
    {
        [Tooltip("로드할 스테이지 프리팹. 지정하면 이걸 직접 인스턴스화(번호 무시). 드래그 시 아래 번호 자동 동기.")]
        [SerializeField] private GameObject _stagePrefab;

        [Tooltip("스테이지 번호. 프리팹이 비어 있으면 이 번호로 StagePrefab/Stage_{NNN} 을 Addressables 로드.")]
        [SerializeField] private int _stageNumber = 1;

        private const string AddressPattern = "Assets/Application/TopTower/StagePrefab/Stage_{0:D3}.prefab";

        private AsyncOperationHandle<GameObject> _handle;
        private bool _loadedByAddressables;
        private GameObject _instance;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 프리팹 드래그 시 번호 자동 동기화 (프리팹 이름의 끝 숫자).
            if (_stagePrefab != null)
            {
                int n = ParseStageNumber(_stagePrefab.name);
                if (n > 0) _stageNumber = n;
            }
        }
#endif

        private async UniTaskVoid Start()
        {
            if (_stagePrefab != null)
            {
                _instance = Instantiate(_stagePrefab);   // 직접 참조 로드
                return;
            }

            string address = string.Format(AddressPattern, _stageNumber);
            _handle = Addressables.LoadAssetAsync<GameObject>(address);
            await _handle.Task;
            if (_handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[TowerStage] 스테이지 로드 실패: {address}");
                return;
            }
            _loadedByAddressables = true;
            _instance = Instantiate(_handle.Result);
        }

        private void OnDestroy()
        {
            if (_loadedByAddressables && _handle.IsValid())
                Addressables.Release(_handle);
        }

        /// <summary>"Stage_001", "Stage_001(Clone)" 등 이름 끝의 숫자를 추출. 실패 시 -1.</summary>
        private static int ParseStageNumber(string name)
        {
            int us = name.LastIndexOf('_');
            if (us < 0 || us == name.Length - 1) return -1;
            string tail = name.Substring(us + 1);
            int end = 0;
            while (end < tail.Length && char.IsDigit(tail[end])) end++;
            if (end == 0) return -1;
            return int.TryParse(tail.Substring(0, end), out int n) ? n : -1;
        }
    }
}
