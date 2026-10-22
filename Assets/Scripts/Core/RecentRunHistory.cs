using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceExplorer.Core
{
    [Serializable]
    public sealed class RecentRunRecord
    {
        public int FinalScore;
        public float SurvivalTimeSeconds;
        public int PlayerLevel;
    }

    public static class RecentRunHistory
    {
        public const int MaximumRecords = 20;
        private const string PlayerPrefsKey = "SpaceExplorer.RecentRunHistory.v1";

        [Serializable]
        public sealed class SaveData
        {
            public List<RecentRunRecord> Records = new List<RecentRunRecord>();
        }

        // Đọc lịch sử JSON đã lưu và trả danh sách rỗng nếu chưa có dữ liệu hợp lệ.
        public static List<RecentRunRecord> LoadRecentRuns()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
                return new List<RecentRunRecord>();

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PlayerPrefsKey));
                if (data?.Records == null)
                    return new List<RecentRunRecord>();

                if (data.Records.Count > MaximumRecords)
                    data.Records.RemoveRange(MaximumRecords, data.Records.Count - MaximumRecords);

                data.Records.RemoveAll(record => record == null);

                return data.Records;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Recent run history could not be read and will be treated as empty: {exception.Message}");
                return new List<RecentRunRecord>();
            }
        }

        // Đưa run hoàn tất lên đầu danh sách và cắt lịch sử về tối đa 20 bản ghi.
        public static void RecordCompletedRun(RunResult result)
        {
            List<RecentRunRecord> records = LoadRecentRuns();
            records.Insert(0, new RecentRunRecord
            {
                FinalScore = result.FinalScore,
                SurvivalTimeSeconds = result.SurvivalTimeSeconds,
                PlayerLevel = result.LevelReached
            });

            if (records.Count > MaximumRecords)
                records.RemoveRange(MaximumRecords, records.Count - MaximumRecords);

            try
            {
                PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(new SaveData { Records = records }));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Completed run history could not be saved: {exception.Message}");
            }
        }
    }
}
