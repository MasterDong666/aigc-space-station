using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 视频 / 占位卡宿主（由 VideoManager 创建与销毁）。
/// 结构：独立 Canvas（sortingOrder 300）→ 遮罩 → 剧情卡 → 标题/正文/角标 → Skip 按钮。
/// 占位模式：显示 fallback 文案，durationSeconds 后自动完成；
/// 真实模式：VideoPlayer + RenderTexture → RawImage（无需相机），Prepare 超时降级占位。
/// </summary>
public class VideoHostController : MonoBehaviour
{
    private VideoClipDef def;
    private System.Action onFinished;
    private bool finished;
    private Coroutine routine;

    private Text titleText;
    private Text bodyText;
    private Text badgeText;
    private RawImage videoImage;

    public string ClipId => def.clipId;

    public void Play(VideoClipDef clipDef, System.Action callback)
    {
        def = clipDef;
        onFinished = callback;
        BuildUI();

        if (VideoManager.HasClip(def.clipId))
        {
            StartRealPlayback();
        }
        else
        {
            StartPlaceholder();
        }
    }

    /// <summary>立即结束当前播放（完成回调仍恰好触发一次）。</summary>
    public void Skip()
    {
        Finish();
    }

    private void BuildUI()
    {
        Canvas canvas = UIFactory.CreateCanvas("FlowVideoCanvas");
        canvas.sortingOrder = 300;
        canvas.transform.SetParent(transform, false);

        Image veil = UIFactory.CreatePanel(
            "VideoVeil",
            canvas.transform,
            new Color(0.01f, 0.02f, 0.04f, 0.92f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = CinematicUIVisuals.CreateCard(
            "VideoCard",
            canvas.transform,
            CinematicUIVisuals.Midnight,
            new Vector2(980f, 560f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(980f, 560f)
        );

        videoImage = CinematicUIVisuals.AddFramedArt(
            "VideoFrame",
            card.transform,
            null,
            new Vector2(920f, 420f),
            CinematicUIVisuals.DeepInk
        );
        SetAnchored(
            videoImage.transform.parent.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -44f),
            new Vector2(920f, 420f)
        );

        badgeText = UIFactory.CreateText(
            "VideoBadge",
            card.transform,
            def.displayName + "  ·  CINEMATIC",
            20,
            CinematicUIVisuals.Sky,
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            badgeText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -480f),
            new Vector2(600f, 32f)
        );

        titleText = UIFactory.CreateText(
            "VideoTitle",
            videoImage.transform,
            string.Empty,
            40,
            UIPalette.TextMain
        );
        UIFactory.Stretch(titleText.rectTransform);

        bodyText = UIFactory.CreateText(
            "VideoBody",
            videoImage.transform,
            string.Empty,
            26,
            UIPalette.TextDim
        );
        SetAnchored(
            bodyText.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -90f),
            new Vector2(760f, 110f)
        );

        Button skip = UIFactory.CreateButton(
            "VideoSkip",
            card.transform,
            "跳过  ›",
            new Vector2(260f, 64f),
            UIPalette.AccentDim,
            26
        );
        SetAnchored(
            skip.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 36f),
            new Vector2(260f, 64f)
        );
        skip.onClick.AddListener(Skip);
    }

    private void StartPlaceholder()
    {
        badgeText.text = "占位视频 · 无文件时将自动跳过";
        titleText.text = def.fallbackTitle;
        bodyText.text = def.fallbackBody;
        routine = StartCoroutine(PlaceholderRoutine());
    }

    private IEnumerator PlaceholderRoutine()
    {
        yield return new WaitForSeconds(def.durationSeconds);
        Finish();
    }

    private void StartRealPlayback()
    {
        badgeText.text = def.displayName + "  ·  正在播放";
        titleText.text = string.Empty;
        bodyText.text = string.Empty;

        VideoPlayer player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.skipOnDrop = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = new RenderTexture(1280, 720, 24);
        videoImage.texture = player.targetTexture;
        player.url = Path.Combine(
            Application.streamingAssetsPath,
            "Video",
            def.fileName
        );
        player.prepareCompleted += p => p.Play();
        player.loopPointReached += p => Finish();

        player.Prepare();
        routine = StartCoroutine(PrepareTimeoutRoutine());
    }

    private IEnumerator PrepareTimeoutRoutine()
    {
        yield return new WaitForSeconds(6f);

        // 准备超时：降级为占位卡，保证流程不卡死
        badgeText.text = "视频加载失败 · 已降级为占位";
        titleText.text = def.fallbackTitle;
        bodyText.text = def.fallbackBody;
        routine = StartCoroutine(PlaceholderRoutine());
    }

    private void Finish()
    {
        if (finished)
        {
            return;
        }

        finished = true;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Destroy(gameObject);

        System.Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
