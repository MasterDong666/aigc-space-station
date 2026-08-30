using System;
using UnityEngine;

/// <summary>统一弹窗请求（可扩展数据结构）。</summary>
[Serializable]
public struct PopupRequest
{
    public string popupId;
    public string eyebrow;
    public string title;
    public string body;
    public string confirmLabel;
    public string accentHex;
    public string textureKey;
}

/// <summary>流程内固定弹窗文案（集中配置）。</summary>
public static class PopupRequests
{
    public static PopupRequest AIIntro(string playerName)
    {
        return new PopupRequest
        {
            popupId = "ai_intro",
            eyebrow = "CHENXI ONLINE",
            title = "你好，" + playerName + " 修复官",
            body =
                "我是你的 AI 智能管家晨曦。\n" +
                "接下来你将体验：星际轨道巡检、生态营养液投放与基因孢子培育" +
                "三项每日修复任务，并逐步积累地球修复进度。\n" +
                "当修复进度达到 50，最终抉择将向你开启。",
            confirmLabel = "开始任务  ›",
            accentHex = "#55C8E8",
        };
    }

    public static PopupRequest GameIntro()
    {
        return new PopupRequest
        {
            popupId = "game_intro",
            eyebrow = "RESTORATION PROTOCOL",
            title = "归墟空间站 · 每日任务",
            body =
                "01 星际轨道巡检：指针进入绿色区间时点击锁定参数。\n" +
                "02 生态营养液投放：选择地块与浓度，拖拽框选投放范围。\n" +
                "03 基因孢子培育：调取样本、选择播种区域并释放无人机群。\n" +
                "完成全部任务后可返回诺亚休假。",
            confirmLabel = "进入空间站  ›",
            accentHex = "#73D7B3",
        };
    }
}

/// <summary>
/// 统一弹窗管理器（静态）。
/// 独立宿主 Canvas（sortingOrder 300），同场景内互斥显示；
/// 若已有弹窗在显示，新的请求最多排队 1 个，关闭后自动弹出。
/// </summary>
public static class PopupManager
{
    private static PopupHostController host;
    private static PopupRequest queuedRequest;
    private static System.Action queuedCallback;
    private static bool hasQueued;

    public static bool IsShowing => host != null;

    public static event Action<string> PopupOpened;
    public static event Action<string> PopupClosed;

    public static void Show(PopupRequest request, System.Action onClosed = null)
    {
        if (host != null)
        {
            queuedRequest = request;
            queuedCallback = onClosed;
            hasQueued = true;
            return;
        }

        GameObject hostGo = new GameObject("FlowPopupHost");
        host = hostGo.AddComponent<PopupHostController>();
        host.Show(request, () =>
        {
            host = null;
            PopupClosed?.Invoke(request.popupId);
            onClosed?.Invoke();

            if (hasQueued)
            {
                hasQueued = false;
                PopupRequest next = queuedRequest;
                System.Action nextCallback = queuedCallback;
                queuedRequest = default;
                queuedCallback = null;
                Show(next, nextCallback);
            }
        });

        PopupOpened?.Invoke(request.popupId);
    }

    /// <summary>关闭当前弹窗（等效点击确认按钮，供验证脚本使用）。</summary>
    public static void Close()
    {
        if (host != null)
        {
            host.Close();
        }
    }
}
