using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>存档数据契约（可序列化 DTO，只存值不存运行时对象引用）。</summary>
[Serializable]
public class GameSaveData
{
    public int version = 2;
    public string saveId;
    public long updatedAtUtcTicks;
    public string playerName;
    public string avatarId;
    public bool hasProfile;
    public int earthProgress;
    public int workday;
    public bool openingCompleted;
    public bool firstReturnStarted;
    public bool firstReturnCompleted;
    public bool biodiversityPuzzleCompleted;
    public bool endingCompleted;
    public bool stationIntroSeen;
    public int endingChoice;
    public int task1ManualStreak;
    public bool task1LogUnlocked;
    public int geneTreeUnlockedLevel;
    public List<GenePlotData> genePlots = new List<GenePlotData>();
    public List<int> completedToday = new List<int>();
    public List<IntIntPair> completionHistory = new List<IntIntPair>();
    public List<int> tutorialsSeen = new List<int>();
    public List<string> uniqueProgressRewards = new List<string>();
}

/// <summary>存档选择界面使用的轻量信息。</summary>
public readonly struct SaveSlotSummary
{
    public readonly string SaveId;
    public readonly string PlayerName;
    public readonly string AvatarId;
    public readonly int EarthProgress;
    public readonly int Workday;
    public readonly bool EndingCompleted;
    public readonly long UpdatedAtUtcTicks;

    public SaveSlotSummary(GameSaveData data)
    {
        SaveId = data.saveId;
        PlayerName = data.playerName;
        AvatarId = data.avatarId;
        int completedTaskRuns = 0;
        if (data.completionHistory != null)
        {
            foreach (IntIntPair pair in data.completionHistory)
            {
                completedTaskRuns += Mathf.Max(0, pair.value);
            }
        }

        int correctedProgress = completedTaskRuns *
            MVPGameSession.DefaultTaskReward;
        if (data.biodiversityPuzzleCompleted)
        {
            correctedProgress += 10;
        }

        if (data.endingCompleted)
        {
            correctedProgress = MVPGameSession.EndingProgress;
        }

        EarthProgress = completedTaskRuns > 0 ||
            data.biodiversityPuzzleCompleted || data.endingCompleted
            ? Mathf.Clamp(
                correctedProgress,
                0,
                MVPGameSession.EndingProgress
            )
            : data.earthProgress;
        Workday = data.workday;
        EndingCompleted = data.endingCompleted;
        UpdatedAtUtcTicks = data.updatedAtUtcTicks;
    }
}

/// <summary>JsonUtility 不支持 Dictionary，用键值对列表代替。</summary>
[Serializable]
public struct IntIntPair
{
    public int key;
    public int value;
}

/// <summary>
/// 任务3播种地块的运行时状态（持久化）。
/// sowedAtUtcTicks / matureAtUtcTicks 用于保存压缩培育演示的起止时间。
/// </summary>
[Serializable]
public class GenePlotData
{
    public string regionId;
    public string sporeId;
    public long sowedAtUtcTicks;
    public long matureAtUtcTicks;
    public int quality;
    public bool mutationTriggered;
    public bool mutationViewed;
}

/// <summary>
/// 存档管理器（静态）。
/// JsonUtility 序列化 GameSaveData 到 persistentDataPath/earth_restoration_save.json；
/// 损坏文件自动备份为 .corrupt 后删除，不阻塞游戏。
/// 注意：transient 路由字段（requestedMiniGame/pendingCompletion/requestedNarrative）不存档。
/// </summary>
public static class SaveManager
{
    private const string FileName = "earth_restoration_save.json";
    private const string SavesFolderName = "EarthRestorationSaves";
    private const string ActiveSlotFileName = "active_slot.txt";
    private static bool? hasSaveCache;
    private static string activeSaveId;

    public static string SavePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    private static string SavesDirectory =>
        Path.Combine(Application.persistentDataPath, SavesFolderName);

    private static string ActiveSlotPath =>
        Path.Combine(SavesDirectory, ActiveSlotFileName);

    public static string ActiveSaveId => activeSaveId ?? string.Empty;

    public static bool HasSave
    {
        get
        {
            if (hasSaveCache.HasValue)
            {
                return hasSaveCache.Value;
            }

            MigrateLegacySaveIfNeeded();
            hasSaveCache = GetSaveSlots().Count > 0;
            return hasSaveCache.Value;
        }
    }

    /// <summary>后续 TrySave 写入一个全新的独立槽位。</summary>
    public static void BeginNewSave()
    {
        activeSaveId = Guid.NewGuid().ToString("N");
    }

    /// <summary>把当前会话状态写入存档文件。返回是否成功。</summary>
    public static bool TrySave()
    {
        try
        {
            GameSaveData data = new GameSaveData();
            MVPGameSession.ExportState(data);

            if (string.IsNullOrEmpty(activeSaveId))
            {
                BeginNewSave();
            }

            data.saveId = activeSaveId;
            data.updatedAtUtcTicks = DateTime.UtcNow.Ticks;

            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(SavesDirectory);
            string path = GetSlotPath(activeSaveId);
            File.WriteAllText(path, json);
            File.WriteAllText(ActiveSlotPath, activeSaveId);
            hasSaveCache = true;
            Debug.Log("[SAVE] 已写入槽位：" + path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SAVE] 写入失败：" + ex.Message);
            return false;
        }
    }

    /// <summary>从存档文件恢复到当前会话。损坏文件备份删除并返回 false。</summary>
    public static bool TryLoadIntoSession()
    {
        MigrateLegacySaveIfNeeded();

        // “重新开始”会预先创建一个尚未落盘的新槽位。此时不要自动回载
        // 其他旧档，否则玩家会误以为刚刚重置的还是旧角色。
        if (!string.IsNullOrEmpty(activeSaveId))
        {
            string activePath = GetSlotPath(activeSaveId);
            return File.Exists(activePath) && TryLoadIntoSession(activeSaveId);
        }

        string requestedId = ReadActiveSlotId();
        if (!string.IsNullOrEmpty(requestedId) && TryLoadIntoSession(requestedId))
        {
            return true;
        }

        List<SaveSlotSummary> slots = GetSaveSlots();
        if (slots.Count == 0)
        {
            hasSaveCache = false;
            return false;
        }

        return TryLoadIntoSession(slots[0].SaveId);
    }

    /// <summary>读取指定槽位，并将其设为之后自动保存的当前槽位。</summary>
    public static bool TryLoadIntoSession(string saveId)
    {
        if (string.IsNullOrWhiteSpace(saveId))
        {
            return false;
        }

        string path = GetSlotPath(saveId.Trim());
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            if (data == null)
            {
                throw new InvalidDataException("存档解析结果为空");
            }

            MVPGameSession.ImportState(data);
            activeSaveId = string.IsNullOrWhiteSpace(data.saveId)
                ? saveId.Trim()
                : data.saveId.Trim();
            Directory.CreateDirectory(SavesDirectory);
            File.WriteAllText(ActiveSlotPath, activeSaveId);
            hasSaveCache = true;
            Debug.Log("[SAVE] 已读取槽位：" + path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SAVE] 读取失败：" + ex.Message);

            try
            {
                string corruptPath = path + ".corrupt";
                if (File.Exists(corruptPath))
                {
                    File.Delete(corruptPath);
                }

                File.Move(path, corruptPath);
                hasSaveCache = null;
            }
            catch
            {
                // 备份失败不阻塞游戏
            }

            return false;
        }
    }

    /// <summary>返回全部有效槽位，按最近保存时间从新到旧排序。</summary>
    public static List<SaveSlotSummary> GetSaveSlots()
    {
        MigrateLegacySaveIfNeeded();
        List<SaveSlotSummary> slots = new List<SaveSlotSummary>();
        if (!Directory.Exists(SavesDirectory))
        {
            return slots;
        }

        string[] files = Directory.GetFiles(
            SavesDirectory,
            "save_*.json",
            SearchOption.TopDirectoryOnly
        );
        foreach (string path in files)
        {
            try
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(
                    File.ReadAllText(path)
                );
                if (data == null || !data.hasProfile)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(data.saveId))
                {
                    data.saveId = Path.GetFileNameWithoutExtension(path)
                        .Replace("save_", string.Empty);
                }

                if (data.updatedAtUtcTicks <= 0)
                {
                    data.updatedAtUtcTicks = File.GetLastWriteTimeUtc(path).Ticks;
                }

                slots.Add(new SaveSlotSummary(data));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SAVE] 忽略损坏槽位：" + ex.Message);
            }
        }

        slots.Sort((left, right) =>
            right.UpdatedAtUtcTicks.CompareTo(left.UpdatedAtUtcTicks)
        );
        return slots;
    }

    public static void DeleteSave()
    {
        try
        {
            if (!string.IsNullOrEmpty(activeSaveId))
            {
                string path = GetSlotPath(activeSaveId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            if (File.Exists(ActiveSlotPath))
            {
                File.Delete(ActiveSlotPath);
            }
        }
        catch
        {
            // 删除失败不阻塞游戏
        }

        activeSaveId = null;
        hasSaveCache = null;
    }

    private static string GetSlotPath(string saveId)
    {
        string safeId = saveId.Replace("/", string.Empty)
            .Replace("\\", string.Empty)
            .Replace("..", string.Empty);
        return Path.Combine(SavesDirectory, "save_" + safeId + ".json");
    }

    private static string ReadActiveSlotId()
    {
        if (!File.Exists(ActiveSlotPath))
        {
            return string.Empty;
        }

        try
        {
            return File.ReadAllText(ActiveSlotPath).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void MigrateLegacySaveIfNeeded()
    {
        if (!File.Exists(SavePath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(SavesDirectory);
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(data.saveId))
            {
                data.saveId = Guid.NewGuid().ToString("N");
            }

            data.version = 2;
            if (data.updatedAtUtcTicks <= 0)
            {
                data.updatedAtUtcTicks = File.GetLastWriteTimeUtc(SavePath).Ticks;
            }

            string slotPath = GetSlotPath(data.saveId);
            if (!File.Exists(slotPath))
            {
                File.WriteAllText(slotPath, JsonUtility.ToJson(data, true));
            }

            File.WriteAllText(ActiveSlotPath, data.saveId);
            string migratedPath = SavePath + ".migrated";
            if (File.Exists(migratedPath))
            {
                File.Delete(migratedPath);
            }
            File.Move(SavePath, migratedPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SAVE] 旧存档迁移失败：" + ex.Message);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        hasSaveCache = null;
        activeSaveId = null;
    }
}
