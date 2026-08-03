using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KS.TopTower
{
    /// <summary>저장되는 층 슬롯 1개 (빈방은 저장 안 함).</summary>
    [Serializable]
    public class SlotSave
    {
        public int floorIndex;
        public int cellIndex;              // 한 층 내 셀 위치 (지하 0/1, 지상 0)
        public int group;                  // ModuleGroup (int)
        public string moduleName;
        public int status;                 // BuildManager.SlotStatus (int)
        public float remainingBuildSeconds; // Constructing일 때 남은 공사시간
    }

    /// <summary>세이브 파일 전체 구조 (JsonUtility 호환 — Dictionary 불가, List 사용).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = SaveSystem.CurrentVersion;
        public long gold;
        public long lastSaveUnixUtc;
        public List<SlotSave> slots = new List<SlotSave>();
        // (Phase1+) prestige / upgrades / collection / settings 추가 예정
    }

    /// <summary>세이브 파일 IO. persistentDataPath에 JSON 1개.</summary>
    public static class SaveSystem
    {
        public const int CurrentVersion = 1;

        private static string FilePath => Path.Combine(Application.persistentDataPath, "toptower_save.json");

        public static bool Exists() => File.Exists(FilePath);

        public static void Save(SaveData data)
        {
            try
            {
                data.version = CurrentVersion;
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] 저장 실패: " + e.Message);
            }
        }

        /// <summary>세이브 로드. 없으면 null(신규). 손상 시 백업 후 null.</summary>
        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null) return null;
                if (data.slots == null) data.slots = new List<SlotSave>();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] 로드 실패(손상 가능): " + e.Message);
                try { if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".corrupt", true); } catch { }
                return null;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
