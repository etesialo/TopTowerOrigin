using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace KS.TopTower.EditorTools
{
    /// <summary>
    /// Assets/Application/TopTower/ 폴더 전체 스캔 → 모든 asset을 Addressables에 자동 등록 + 라벨 자동 부여.
    /// 메뉴: Tools > Top Tower > Sync Addressables.
    /// 규칙:
    ///   - Address: 풀 경로 (asset path)로 **재설정**. 옛 단축 Address는 모두 풀 경로로 덮어쓰기 → 일관된 표기.
    ///   - 라벨: asset이 속한 직속 폴더 이름 **하나로 재설정**. 옛 라벨은 모두 제거 → 폴더 이동/이름 변경 시 자동 정리.
    ///   - 사용자가 손으로 부여한 라벨도 같이 제거되니, 손 라벨이 필요하면 이 도구 외부에서 관리.
    /// </summary>
    public static class TopTowerAddressablesSyncTool
    {
        private const string MenuPath = "Tools/Top Tower/Sync Addressables";
        private const string RootFolder = "Assets/Application/TopTower";

        [MenuItem(MenuPath)]
        public static void Sync()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[Addressables Sync] AddressableAssetSettings 없음. Window > Asset Management > Addressables > Groups 열어 초기화 필요.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                Debug.LogWarning($"[Addressables Sync] 폴더 없음: {RootFolder}");
                return;
            }
            var defaultGroup = settings.DefaultGroup;
            if (defaultGroup == null)
            {
                Debug.LogError("[Addressables Sync] Default Group이 없음. Addressables Groups 창에서 기본 그룹 설정 필요.");
                return;
            }

            int added = 0, existing = 0, labeled = 0, skippedType = 0;
            SyncFolderRecursive(RootFolder, settings, defaultGroup, ref added, ref existing, ref labeled, ref skippedType);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Addressables Sync] 완료. 새 등록 {added} | 기존 entry {existing} | 라벨 부여 {labeled} | 스킵(코드 등) {skippedType}");
        }

        private static void SyncFolderRecursive(
            string folderPath,
            AddressableAssetSettings settings,
            AddressableAssetGroup defaultGroup,
            ref int added, ref int existing, ref int labeled, ref int skippedType)
        {
            string folderName = Path.GetFileName(folderPath);

            // Editor 폴더는 통째 skip
            if (folderName.Equals("Editor", System.StringComparison.OrdinalIgnoreCase))
                return;

            // 폴더 직속 asset 처리 — 라벨은 폴더명
            string label = folderName;

            string[] allGuids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            bool anyDirectAsset = false;

            foreach (var guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;

                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                if (parent != folderPath) continue; // 직속만, 하위 폴더 자산은 재귀에서

                if (!IsAddressableType(path))
                {
                    skippedType++;
                    continue;
                }

                anyDirectAsset = true;

                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    // 새 entry — Default Group에 추가
                    entry = settings.CreateOrMoveEntry(guid, defaultGroup, false, false);
                    added++;
                }
                else
                {
                    existing++;
                }

                // Address를 풀 경로(asset path)로 재설정 — 옛 단축은 모두 덮어쓰기
                if (entry.address != path)
                    entry.address = path;

                // 라벨 재설정: 기존 라벨 모두 제거 후 폴더명 라벨 하나만 부여
                var oldLabels = new System.Collections.Generic.List<string>(entry.labels);
                foreach (var oldLabel in oldLabels)
                {
                    if (oldLabel != label)
                        entry.SetLabel(oldLabel, false, false, false);
                }
                if (!entry.labels.Contains(label))
                {
                    entry.SetLabel(label, true, false, false);
                    labeled++;
                }
            }

            // 라벨이 처음 등장하는 거면 settings에 등록
            if (anyDirectAsset && !settings.GetLabels().Contains(label))
                settings.AddLabel(label);

            foreach (var sub in AssetDatabase.GetSubFolders(folderPath))
            {
                SyncFolderRecursive(sub, settings, defaultGroup, ref added, ref existing, ref labeled, ref skippedType);
            }
        }

        /// <summary>
        /// Addressables에 등록할 타입인지 결정.
        /// 코드/메타/어셈블리 정의 등 제외 — 그 외는 모두 등록.
        /// </summary>
        private static bool IsAddressableType(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".cs":
                case ".asmdef":
                case ".asmref":
                case ".uxml":
                case ".uss":
                case ".meta":
                    return false;
            }
            return true;
        }
    }
}
