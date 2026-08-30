using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>存档数据契约（可序列化 DTO，只存值不存运行时对象引用）。</summary>
[Serializable]
public class GameSaveData
{
    public int version = 1;
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

/// <summary>JsonUtility 不支持 Dictionary，用键值对列表代替。</summary>
[Serializable]
public struct IntIntPair
{
    public int key;
    public int value;
}

/// <summary>
/// 任务3播种地块的运行时状态（持久化）。
/// sowedAtUtcTicks 用于真实 72 小时成熟倒计时，退出游戏再进入仍正确。
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
    private static bool? hasSaveCache;

    public static string SavePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave
    {
        get
        {
            if (hasSaveCache.HasValue)
            {
                return hasSaveCache.Value;
            }

            hasSaveCache = File.Exists(SavePath);
            return hasSaveCache.Value;
        }
    }

    /// <summary>把当前会话状态写入存档文件。返回是否成功。</summary>
    public static bool TrySave()
    {
        try
        {
            GameSaveData data = new GameSaveData();
            MVPGameSession.ExportState(data);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            hasSaveCache = true;
            Debug.Log("[SAVE] 已写入：" + SavePath);
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
        if (!File.Exists(SavePath))
        {
            hasSaveCache = false;
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            if (data == null)
            {
                throw new InvalidDataException("存档解析结果为空");
            }

            MVPGameSession.ImportState(data);
            hasSaveCache = true;
            Debug.Log("[SAVE] 已读取：" + SavePath);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SAVE] 读取失败：" + ex.Message);

            try
            {
                string corruptPath = SavePath + ".corrupt";
                if (File.Exists(corruptPath))
                {
                    File.Delete(corruptPath);
                }

                File.Move(SavePath, corruptPath);
                hasSaveCache = false;
            }
            catch
            {
                // 备份失败不阻塞游戏
            }

            return false;
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
        catch
        {
            // 删除失败不阻塞游戏
        }

        hasSaveCache = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        hasSaveCache = null;
    }
}
