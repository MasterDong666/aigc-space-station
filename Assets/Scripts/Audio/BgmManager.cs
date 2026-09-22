using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 跨场景背景音乐：主界面与空间站/每日任务使用不同循环曲目，
/// 场景切换自动淡入淡出，剧情视频播放时自动暂停并在结束后恢复。
/// </summary>
public sealed class BgmManager : MonoBehaviour
{
    private const string MainMenuResource =
        "Audio/Music/MainMenu_RelaxedCultivation";
    private const string StationResource =
        "Audio/Music/Station_GentleOrbit";
    private const string DailyGameScenePath =
        "Assets/Scenes/2DGame/EarthRestoration2D.unity";

    private const float MainMenuVolume = 0.32f;
    private const float StationVolume = 0.28f;
    private const float SceneFadeSeconds = 1.25f;
    private const float VideoFadeSeconds = 0.42f;

    private static BgmManager instance;

    private AudioSource source;
    private AudioClip mainMenuClip;
    private AudioClip stationClip;
    private Coroutine fadeRoutine;
    private AudioClip desiredClip;
    private float desiredVolume;
    private bool mutedForVideo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new("GlobalBgmManager");
        instance = root.AddComponent<BgmManager>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.ignoreListenerPause = true;

        mainMenuClip = Resources.Load<AudioClip>(MainMenuResource);
        stationClip = Resources.Load<AudioClip>(StationResource);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        VideoManager.PlaybackStarted += HandleVideoStarted;
        VideoManager.PlaybackFinished += HandleVideoFinished;
    }

    private void Start()
    {
        ApplySceneMusic(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        VideoManager.PlaybackStarted -= HandleVideoStarted;
        VideoManager.PlaybackFinished -= HandleVideoFinished;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneMusic(scene);
    }

    private void ApplySceneMusic(Scene scene)
    {
        if (scene.path == SceneTransitionManager.FrontEndScenePath)
        {
            SwitchTo(mainMenuClip, MainMenuVolume);
            return;
        }

        if (
            scene.path == SceneTransitionManager.StationHubScenePath ||
            scene.path == DailyGameScenePath
        )
        {
            SwitchTo(stationClip, StationVolume);
            return;
        }

        SwitchTo(null, 0f);
    }

    private void SwitchTo(AudioClip clip, float volume)
    {
        desiredClip = clip;
        desiredVolume = Mathf.Clamp01(volume);

        if (mutedForVideo)
        {
            return;
        }

        if (source.clip == clip && source.isPlaying)
        {
            StartFade(desiredVolume, SceneFadeSeconds, false, null);
            return;
        }

        StartFade(
            0f,
            source.isPlaying ? SceneFadeSeconds * 0.55f : 0f,
            false,
            () =>
            {
                source.Stop();
                source.clip = clip;
                if (clip == null)
                {
                    return;
                }

                source.Play();
                StartFade(desiredVolume, SceneFadeSeconds, false, null);
            }
        );
    }

    private void HandleVideoStarted(string clipId)
    {
        mutedForVideo = true;
        StartFade(0f, VideoFadeSeconds, true, null);
    }

    private void HandleVideoFinished(string clipId)
    {
        mutedForVideo = false;
        if (desiredClip == null)
        {
            return;
        }

        if (source.clip != desiredClip)
        {
            source.Stop();
            source.clip = desiredClip;
        }

        if (!source.isPlaying)
        {
            source.UnPause();
            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        StartFade(desiredVolume, SceneFadeSeconds, false, null);
    }

    private void StartFade(
        float target,
        float duration,
        bool pauseAfter,
        System.Action completed
    )
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(
            FadeRoutine(target, duration, pauseAfter, completed)
        );
    }

    private IEnumerator FadeRoutine(
        float target,
        float duration,
        bool pauseAfter,
        System.Action completed
    )
    {
        float start = source.volume;
        if (duration <= 0f)
        {
            source.volume = target;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(
                    start,
                    target,
                    Mathf.Clamp01(elapsed / duration)
                );
                yield return null;
            }

            source.volume = target;
        }

        if (pauseAfter && source.isPlaying)
        {
            source.Pause();
        }

        fadeRoutine = null;
        completed?.Invoke();
    }
}
