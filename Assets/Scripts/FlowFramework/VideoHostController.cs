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
    private System.Action onBack;
    private bool finished;
    private Coroutine routine;
    private VideoPlayer player;
    private RenderTexture renderTexture;
    private bool fallbackStarted;

    private Text titleText;
    private Text bodyText;
    private Text badgeText;
    private RawImage videoImage;

    public string ClipId => def.clipId;

    public void Play(
        VideoClipDef clipDef,
        System.Action callback,
        System.Action backCallback = null
    )
    {
        def = clipDef;
        onFinished = callback;
        onBack = backCallback;
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

    /// <summary>终止当前视频并回到流程的上一步，不触发完成回调。</summary>
    public void Back()
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

        System.Action callback = onBack;
        onBack = null;
        onFinished = null;
        callback?.Invoke();
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
            new Vector2(1320f, 820f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1320f, 820f)
        );

        videoImage = CinematicUIVisuals.AddFramedArt(
            "VideoFrame",
            card.transform,
            null,
            new Vector2(1240f, 698f),
            CinematicUIVisuals.DeepInk
        );
        AspectRatioFitter aspect = videoImage.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 16f / 9f;
        SetAnchored(
            videoImage.transform.parent.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -34f),
            new Vector2(1240f, 698f)
        );

        badgeText = UIFactory.CreateText(
            "VideoBadge",
            card.transform,
            def.displayName + "  ·  CINEMATIC",
            22,
            UIPalette.TextMain,
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            badgeText.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(720f, 40f)
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
            new Vector2(220f, 64f),
            UIPalette.AccentDim,
            26
        );
        SetAnchored(
            skip.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-30f, 24f),
            new Vector2(220f, 64f)
        );
        skip.onClick.AddListener(Skip);

        Button back = UIFactory.CreateBackButton(
            card.transform,
            Back,
            "VideoBack"
        );
        SetAnchored(
            back.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(30f, 24f),
            new Vector2(156f, 52f)
        );
        back.gameObject.SetActive(onBack != null);
    }

    private void StartPlaceholder()
    {
        if (fallbackStarted || finished)
        {
            return;
        }

        fallbackStarted = true;
        videoImage.texture = null;
        videoImage.color = CinematicUIVisuals.DeepInk;
        badgeText.text = "占位视频 · 无文件时将自动跳过";
        titleText.text = def.fallbackTitle;
        bodyText.text = def.fallbackBody;
        routine = StartCoroutine(PlaceholderRoutine());
    }

    private IEnumerator PlaceholderRoutine()
    {
        yield return new WaitForSecondsRealtime(def.durationSeconds);
        Finish();
    }

    private void StartRealPlayback()
    {
        badgeText.text = def.displayName + "  ·  正在播放";
        titleText.text = string.Empty;
        bodyText.text = string.Empty;
        // AddFramedArt 在没有纹理时会用深色占位。接入真实视频后必须恢复白色，
        // 否则 RawImage 会将视频与深色相乘，导致画面看起来近乎全黑。
        videoImage.color = Color.white;

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.skipOnDrop = true;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.renderMode = VideoRenderMode.RenderTexture;
        renderTexture = new RenderTexture(1920, 1080, 0);
        renderTexture.Create();
        player.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
        player.url = Path.Combine(
            Application.streamingAssetsPath,
            "Video",
            def.fileName
        );
        player.prepareCompleted += HandlePrepared;
        player.loopPointReached += HandlePlaybackFinished;
        player.errorReceived += HandlePlaybackError;

        player.Prepare();
        routine = StartCoroutine(PrepareTimeoutRoutine());
    }

    private IEnumerator PrepareTimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(10f);

        // 准备超时：降级为占位卡，保证流程不卡死
        StartPlaybackFallback("视频加载超时");
    }

    private void HandlePrepared(VideoPlayer preparedPlayer)
    {
        if (finished || fallbackStarted)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        preparedPlayer.EnableAudioTrack(0, true);
        preparedPlayer.SetDirectAudioMute(0, false);
        preparedPlayer.SetDirectAudioVolume(0, 1f);
        preparedPlayer.Play();
    }

    private void HandlePlaybackFinished(VideoPlayer _)
    {
        Finish();
    }

    private void HandlePlaybackError(VideoPlayer _, string message)
    {
        Debug.LogWarning("[VIDEO] 播放失败 " + def.clipId + "：" + message);
        StartPlaybackFallback("视频播放失败");
    }

    private void StartPlaybackFallback(string reason)
    {
        if (fallbackStarted || finished)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (player != null)
        {
            player.Stop();
        }

        fallbackStarted = true;
        videoImage.texture = null;
        videoImage.color = CinematicUIVisuals.DeepInk;
        badgeText.text = reason + " · 已自动降级";
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
        onBack = null;
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.prepareCompleted -= HandlePrepared;
            player.loopPointReached -= HandlePlaybackFinished;
            player.errorReceived -= HandlePlaybackError;
            player.Stop();
            player.targetTexture = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
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
