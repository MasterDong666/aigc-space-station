using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 剧情视频定义。
/// 真实视频放入 Assets/StreamingAssets/Video/{fileName} 后自动切换为真播放；
/// 文件缺失时使用占位卡（fallbackTitle/fallbackBody）自动推进，保证空壳流程可跑通。
/// </summary>
[Serializable]
public struct VideoClipDef
{
    public string clipId;
    public string fileName;
    public string displayName;
    public string fallbackTitle;
    public string fallbackBody;
    public float durationSeconds;
}

/// <summary>剧情视频清单（集中配置，单一数据源）。</summary>
public static class VideoCatalog
{
    public static VideoClipDef[] All { get; } =
    {
        new VideoClipDef
        {
            clipId = "ending_teaser",
            fileName = "ending_teaser.mp4",
            displayName = "结局引入",
            fallbackTitle = "【占位】结局引入视频",
            fallbackBody = "归墟之上，两星之间的最终抉择即将揭晓。\n（正式视频未接入，占位卡将自动跳过）",
            durationSeconds = 4f,
        },
        new VideoClipDef
        {
            clipId = "ending_prequel",
            fileName = "ending_prequel.mp4",
            displayName = "结局前奏",
            fallbackTitle = "【占位】结局前奏视频",
            fallbackBody = "地球正在醒来，最终协议已解锁。\n（正式视频未接入，占位卡将自动跳过）",
            durationSeconds = 4f,
        },
        new VideoClipDef
        {
            clipId = "ending_terminate",
            fileName = "ending_terminate.mp4",
            displayName = "结局A · 终止重塑计划",
            fallbackTitle = "【占位】结局A · 把未来交给生命",
            fallbackBody = "你关闭了持续三百余年的重塑计划，地球依靠自己的节奏恢复。\n（正式视频未接入，占位卡将自动跳过）",
            durationSeconds = 4f,
        },
        new VideoClipDef
        {
            clipId = "ending_sacrifice",
            fileName = "ending_sacrifice.mp4",
            displayName = "结局B · 牺牲完成最终校准",
            fallbackTitle = "【占位】结局B · 第七十九任的最后航程",
            fallbackBody = "你以自身生物信息完成最后一次不可逆的校准，成为复苏的一部分。\n（正式视频未接入，占位卡将自动跳过）",
            durationSeconds = 4f,
        },
        new VideoClipDef
        {
            clipId = "opening",
            fileName = "opening.mp4",
            displayName = "正序视频",
            fallbackTitle = "【占位】正序视频",
            fallbackBody = "本节点当前由 FrontEnd 叙事卡呈现。\n如需视频化，请调用 VideoManager.Play(\"opening\", …) 替换叙事入口。",
            durationSeconds = 4f,
        },
        new VideoClipDef
        {
            clipId = "act_one",
            fileName = "act_one.mp4",
            displayName = "第一幕",
            fallbackTitle = "【占位】第一幕视频",
            fallbackBody = "第七十九任修复官登舰，前往归墟空间站。\n（正式视频未接入，占位卡将自动跳过）",
            durationSeconds = 4f,
        },
        new VideoClipDef
        {
            clipId = "holiday",
            fileName = "holiday.mp4",
            displayName = "休假视频",
            fallbackTitle = "【占位】休假视频",
            fallbackBody = "本节点当前由 FrontEnd 返航叙事卡呈现。\n如需视频化，请调用 VideoManager.Play(\"holiday\", …) 替换叙事入口。",
            durationSeconds = 4f,
        },
    };

    public static VideoClipDef Find(string clipId)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].clipId == clipId)
            {
                return All[i];
            }
        }

        Debug.LogWarning("[VIDEO] 未知视频 ID：" + clipId);
        return default;
    }
}

/// <summary>
/// 剧情视频管理器（静态）。
/// 占位策略：HasClip == false 时不创建 VideoPlayer（避免 Unity 对不存在路径报错），
/// 由 VideoHostController 显示占位卡并在 durationSeconds 后自动完成。
/// 完成回调保证恰好一次；场景切换（LoadSceneMode.Single）时宿主随场景销毁，不缓存。
/// </summary>
public static class VideoManager
{
    private static VideoHostController host;

    public static bool IsPlaying => host != null;
    public static string CurrentClipId =>
        host != null ? host.ClipId : string.Empty;

    public static event Action<string> PlaybackStarted;
    public static event Action<string> PlaybackFinished;

    public static bool HasClip(string clipId)
    {
        VideoClipDef def = VideoCatalog.Find(clipId);
        if (string.IsNullOrEmpty(def.clipId))
        {
            return false;
        }

        return File.Exists(
            Path.Combine(
                Application.streamingAssetsPath,
                "Video",
                def.fileName
            )
        );
    }

    /// <summary>
    /// 播放剧情视频（无文件时自动降级为占位卡）。
    /// 若已有视频在播放，先跳过当前再播放新的。
    /// </summary>
    public static bool Play(string clipId, Action onFinished)
    {
        VideoClipDef def = VideoCatalog.Find(clipId);
        if (string.IsNullOrEmpty(def.clipId))
        {
            onFinished?.Invoke();
            return false;
        }

        if (host != null)
        {
            host.Skip();
        }

        GameObject hostGo = new GameObject("FlowVideoHost");
        host = hostGo.AddComponent<VideoHostController>();
        host.Play(def, () =>
        {
            host = null;
            PlaybackFinished?.Invoke(clipId);
            onFinished?.Invoke();
        });

        PlaybackStarted?.Invoke(clipId);
        return true;
    }

    /// <summary>跳过当前播放（等效点击 Skip 按钮）。</summary>
    public static void Skip()
    {
        if (host != null)
        {
            host.Skip();
        }
    }

    /// <summary>
    /// 预留：把视频渲染进既有 RawImage 媒体位（后续美术到位时使用）。
    /// 本阶段占位实现：直接走标准 Play 流程。
    /// </summary>
    public static bool TryPlayInMediaSlot(
        string clipId,
        RawImage mediaSlot,
        Action onFinished
    )
    {
        return Play(clipId, onFinished);
    }
}
