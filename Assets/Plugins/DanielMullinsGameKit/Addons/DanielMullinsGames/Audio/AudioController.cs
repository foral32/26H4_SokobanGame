using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pixelplacement;
using UnityEngine.Pool; // 添加此引用

public class AudioController : MonoBehaviour
{
    #region Properties & Fields
    public AudioSource BaseLoopSource { get { return loopSources[0]; } }
    public static AudioController Instance { get; private set; }
    public bool Fading { get; set; }

    // 常量定义
    public const float GBC_INTERIOR_BGM_LOWEREDVOLUME = 0.35f;
    public const float GBC_INTERIOR_BGM_FULLVOLUME = 0.55f;
    private const float DEFAULT_SPATIAL_BLEND = 0.75f;
    private const int MAX_POOL_SIZE = 50;

    // 序列化字段
    [SerializeField]
    private List<AudioSource> loopSources = default;

    // 音频资源集合
    private List<AudioClip> sfx = new List<AudioClip>();
    private List<AudioClip> loops = new List<AudioClip>();

    // 活动音频源管理
    private List<AudioSource> ActiveSFXSources
    {
        get
        {
            activeSFX.RemoveAll(x => x == null || ReferenceEquals(x, null) || !x.isPlaying);
            return activeSFX;
        }
    }
    private List<AudioSource> activeSFX = new List<AudioSource>();

    // 音频播放控制相关
    private Dictionary<string, float> limitedFrequencySounds = new Dictionary<string, float>();
    private Dictionary<string, int> lastPlayedSounds = new Dictionary<string, int>();
    private readonly int[] DEFAULT_LOOPSOURCE_INDICES = new int[] { 0 };

    // 混音器相关
    private List<AudioMixer> loadedMixers = new List<AudioMixer>();
    private AudioMixerGroup currentSFXMixer = default;

    // 对象池
    private ObjectPool<AudioSource> audioSourcePool;

    // 添加一个字段来跟踪音量渐变协程
    private List<Coroutine> fadeCoroutines = new List<Coroutine>();

    // 音频缓存系统
    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private const int MAX_CACHE_SIZE = 100; // 根据项目需求调整缓存大小
    private const float DEFAULT_SOUND_COOLDOWN = 0.02f; // 默认冷却时间0.02秒
    #endregion

    #region Unity生命周期
    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.parent = null;
        DontDestroyOnLoad(gameObject);

        foreach (AudioClip o in Resources.LoadAll<AudioClip>("Audio/SFX"))
        {
            sfx.Add(o);
        }
        foreach (AudioClip o in Resources.LoadAll<AudioClip>("Audio/Loops"))
        {
            loops.Add(o);
        }

        // 初始化对象池
        audioSourcePool = new ObjectPool<AudioSource>(
            createFunc: CreatePooledAudioSource,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: OnDestroyPoolObject,
            defaultCapacity: 10,
            maxSize: MAX_POOL_SIZE
        );
    }

    private void OnDestroy()
    {
        audioSourcePool?.Clear();
        ClearAudioCache();
    }
    #endregion

    #region 对象池管理
    private AudioSource CreatePooledAudioSource()
    {
        GameObject obj = new GameObject("PooledAudio");
        obj.transform.parent = transform;
        return obj.AddComponent<AudioSource>();
    }

    private void OnTakeFromPool(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    private void OnReturnToPool(AudioSource source)
    {
        source.clip = null;
        source.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(AudioSource source)
    {
        Destroy(source.gameObject);
    }
    #endregion

    #region 音效播放接口
    /// <summary>
    /// 播放2D音效
    /// </summary>
    /// <param name="soundId"></param> 音效ID
    /// <param name="volume"></param> 音量设置
    /// <param name="skipToTime"></param> 播放开始时间
    /// <param name="pitch"></param> 音调设置
    /// <param name="randomization"></param> 如果需要播放随机变体音效，可以传入此参数
    /// <param name="distortion"></param> 失真效果设置
    /// <param name="looping"></param> 是否循环播放
    /// <param name="cooldown"></param> 冷却时间，防止音效重复播放过快
    /// <returns></returns>
    public AudioSource PlaySound2D(string soundId, float volume = 0.4f, float skipToTime = 0f,
        AudioParams.Pitch pitch = null,
        AudioParams.Randomization randomization = null, AudioParams.Distortion distortion = null,
        bool looping = false, float cooldown = float.NaN)
    {
        var source = PlaySound3D(soundId, Vector3.zero, volume, skipToTime, pitch, randomization, distortion, looping, cooldown);

        if (source != null)
        {
            source.spatialBlend = 0f;
            //DontDestroyOnLoad(source.gameObject);
        }

        return source;
    }

    public AudioSource PlaySound3D(string soundId, Vector3 position, float volume = 1f, float skipToTime = 0f,
        AudioParams.Pitch pitch = null,
        AudioParams.Randomization randomization = null, AudioParams.Distortion distortion = null,
        bool looping = false, float cooldown = float.NaN)
    {

        // 使用默认冷却时间（如果没有指定）
        if (float.IsNaN(cooldown))
        {
            cooldown = DEFAULT_SOUND_COOLDOWN;
        }
        if (soundId.ToLower() == "bullet_hit")

        {
            // 这里可以添加特定的处理逻辑
            Debug.Log("Bullet hit sound played.");
        }

        if (RepetitionIsTooFrequent(soundId, cooldown))
        {
            return null;
        }
        string randomVariationId = soundId;
        if (randomization != null)
        {
            randomVariationId = GetRandomVariationOfSound(soundId, randomization.noRepeating);
        }

        var source = CreateAudioSourceForSound(randomVariationId, position, looping);
        if (source != null)
        {
            source.volume = volume;
            source.time = source.clip.length * skipToTime;

            if (pitch != null)
            {
                source.pitch = pitch.pitch;
            }

            if (distortion != null)
            {
                if (distortion.muffled)
                {
                    MuffleSource(source);
                }
            }
        }

        activeSFX.Add(source);
        return source;
    }
    #endregion

    #region 循环音乐控制
    public void SetLoopAndPlay(string loopName, int sourceIndex = 0, bool looping = true, bool cancelFades = true)
    {
        if (cancelFades)
        {
            CancelFades();
        }

        // 获取之前的音频片段用于比较
        AudioClip previousClip = loopSources[sourceIndex].clip;

        TrySetLoop(loopName, sourceIndex);

        // 只有当音频片段改变时才重启
        if (previousClip != loopSources[sourceIndex].clip)
        {
            loopSources[sourceIndex].Stop();
            loopSources[sourceIndex].Play();
        }

        loopSources[sourceIndex].loop = looping;
    }

    public void FadeInLoop(float fadeDuration, float toVolume, params int[] sourceIndices)
    {
        CancelFades();

        if (sourceIndices == null || sourceIndices.Length == 0)
        {
            sourceIndices = DEFAULT_LOOPSOURCE_INDICES;
        }

        for (int i = 0; i < sourceIndices.Length; i++)
        {
            StartFadeCoroutine(DoFadeToVolume(fadeDuration, toVolume, sourceIndices[i]));
        }
    }

    public void FadeOutLoop(float fadeDuration, params int[] sourceIndices)
    {
        CancelFades();

        if (sourceIndices == null || sourceIndices.Length == 0)
        {
            sourceIndices = DEFAULT_LOOPSOURCE_INDICES;
        }

        for (int i = 0; i < sourceIndices.Length; i++)
        {
            StartCoroutine(DoFadeToVolume(fadeDuration, 0f, sourceIndices[i]));
        }
    }

    public void CrossFadeLoop(string loopName, float duration, float volume = 1f, float newLoopStartTime = 0f)
    {
        if ((loopSources[0].clip == null || loopSources[0].clip.name != loopName) && loops.Exists(x => x.name == loopName))
        {
            CancelFades();
            StartFadeCoroutine(CrossFade(loopName, volume, duration, newLoopStartTime));
        }
    }
    #endregion

    #region 音频效果处理
    private void MuffleSource(AudioSource source, float cutoff = 300f)
    {
        var filter = source.gameObject.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = cutoff;
    }

    private void UnMuffleSource(AudioSource source)
    {
        var lowPassFilter = source.GetComponent<AudioLowPassFilter>();
        if (lowPassFilter != null)
        {
            Destroy(lowPassFilter);
        }
    }

    public void SetAllSoundsPaused(bool paused)
    {
        ActiveSFXSources.ForEach(x =>
        {
            if (paused)
            {
                x.Pause();
            }
            else
            {
                x.UnPause();
            }
        });
    }
    #endregion

    #region 工具方法
    private float GetVolumeFromOptions(int volume, int maxVolume)
    {
        float normalizedValue = volume / (float)maxVolume;
        float adjustedValue = Mathf.Pow(normalizedValue, 0.2f);
        return (1f - adjustedValue) * -80f;
    }

    private bool RepetitionIsTooFrequent(string soundId, float coolDown)
    {
        float time = Time.unscaledTime;
        string soundKey = soundId;

        if (limitedFrequencySounds.ContainsKey(soundKey))
        {
            if (time - coolDown > limitedFrequencySounds[soundKey])
            {
                limitedFrequencySounds[soundKey] = time;
                return false;
            }
        }
        else
        {
            limitedFrequencySounds.Add(soundKey, time);
            return false;
        }

        return true;
    }

    private string GetRandomVariationOfSound(string soundPrefix, bool noRepeating)
    {
        string soundId = "";

        if (!string.IsNullOrEmpty(soundPrefix))
        {
            List<AudioClip> variations = sfx.FindAll(x => x != null && x.name.ToLowerInvariant().StartsWith(soundPrefix.ToLowerInvariant()));
            if (variations.Count > 0)
            {
                int index = Random.Range(0, variations.Count);
                if (noRepeating)
                {
                    if (!lastPlayedSounds.ContainsKey(soundPrefix))
                    {
                        lastPlayedSounds.Add(soundPrefix, index);
                    }
                    else
                    {
                        int breakOutCounter = 0;
                        const int BREAK_OUT_THRESHOLD = 100;
                        while (lastPlayedSounds[soundPrefix] == index && breakOutCounter < BREAK_OUT_THRESHOLD)
                        {
                            index = Random.Range(0, variations.Count) + 1;
                            breakOutCounter++;
                        }

                        if (breakOutCounter >= BREAK_OUT_THRESHOLD - 1)
                        {
                            Debug.Log("Broke out of infinite loop! AudioController.PlayRandomSound.");
                        }

                        lastPlayedSounds[soundPrefix] = index;
                    }
                }

                soundId = variations[index].name;
            }
            else
            {
                soundId = soundPrefix;
            }
        }
        return soundId;
    }

    public AudioClip GetLoopClip(string loopId)
    {
        // 先检查缓存
        string key = loopId.ToLowerInvariant();
        if (clipCache.TryGetValue(key, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        // 缓存中没有，从loops列表查找
        AudioClip clip = loops.Find(x => x.name.ToLowerInvariant() == key);

        // 如果找到了，加入缓存
        if (clip != null && clipCache.Count < MAX_CACHE_SIZE)
        {
            clipCache[key] = clip;
        }

        return clip;
    }

    public AudioClip GetAudioClip(string soundId)
    {
        // 先检查缓存
        string key = soundId.ToLowerInvariant();
        if (clipCache.TryGetValue(key, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        // 缓存中没有，从sfx列表查找
        AudioClip clip = sfx.Find(x => x.name.ToLowerInvariant() == key);

        // 如果找到了，加入缓存
        if (clip != null && clipCache.Count < MAX_CACHE_SIZE)
        {
            clipCache[key] = clip;
        }

        return clip;
    }

    // 清理缓存的方法
    public void ClearAudioCache()
    {
        clipCache.Clear();
        Resources.UnloadUnusedAssets();
    }
    #endregion

    #region 私有辅助方法
    private AudioSource CreateAudioSourceForSound(string soundId, Vector3 position, bool looping)
    {
        if (!string.IsNullOrEmpty(soundId))
        {
            AudioClip sound = GetAudioClip(soundId);

            if (sound != null)
            {
                return InstantiateAudioObject(sound, position, looping);
            }
            else
            {
#if UNITY_EDITOR
                // 在编辑器模式下，如果找不到音效文件，输出警告信息
                Debug.Log($"[AudioController] Audio clip not found: {soundId}. Make sure the file exists in Resources/Audio/SFX folder.");
#endif  
            }
        }

        return null;
    }

    private AudioSource InstantiateAudioObject(AudioClip clip, Vector3 pos, bool looping)
    {
        AudioSource aSource = audioSourcePool.Get();
        aSource.transform.position = pos;
        aSource.clip = clip;
        aSource.outputAudioMixerGroup = currentSFXMixer;
        aSource.spatialBlend = DEFAULT_SPATIAL_BLEND;

        aSource.Play();
        if (looping)
        {
            aSource.loop = true;
        }
        else
        {
            StartCoroutine(ReturnToPoolAfterPlay(aSource));
        }
        return aSource;
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource source)
    {
        if (source == null || source.clip == null) yield break;
        yield return new WaitForSeconds(source.clip.length * 3f);
        if (source != null)
        {
            activeSFX.Remove(source);
            audioSourcePool.Release(source);
        }
    }

    private void CancelFades()
    {
        // 只停止淡入淡出相关的协程
        foreach (var coroutine in fadeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        fadeCoroutines.Clear();

        foreach (AudioSource loopSource in loopSources)
        {
            Tween.Cancel(loopSource.GetInstanceID());
        }
        Fading = false;
    }

    private void TrySetLoop(string loopName, int sourceIndex = 0)
    {
        AudioClip loop = GetLoop(loopName);

        if (loop != null)
        {
            loopSources[sourceIndex].clip = loop;
            loopSources[sourceIndex].pitch = 1f;
        }
    }

    private AudioClip GetLoop(string loopName)
    {
        return loops.Find(x => x.name == loopName);
    }

    private IEnumerator DoFadeToVolume(float duration, float volume, int sourceIndex = 0)
    {
        Fading = true;

        Tween.Volume(loopSources[sourceIndex], volume, duration, 0f, Tween.EaseInOut);
        yield return new WaitForSeconds(duration);

        Fading = false;
    }

    private IEnumerator CrossFade(string newLoop, float volume, float duration, float newLoopStartTimeNormalized, int sourceIndex = 0)
    {
        Fading = true;

        // 保存当前音源信息
        int nextSourceIndex = (sourceIndex + 1) % loopSources.Count;
        float startVolume = loopSources[sourceIndex].volume;

        // 设置新的音乐到第二个音源
        TrySetLoop(newLoop, nextSourceIndex);
        loopSources[nextSourceIndex].volume = 0f;
        loopSources[nextSourceIndex].time = newLoopStartTimeNormalized * loopSources[nextSourceIndex].clip.length;
        loopSources[nextSourceIndex].Play();

        // 同步进行淡出和淡入
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 使用平滑的渐变曲线
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 同时更新两个音源的音量
            loopSources[sourceIndex].volume = Mathf.Lerp(startVolume, 0f, smoothT);
            loopSources[nextSourceIndex].volume = Mathf.Lerp(0f, volume, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终状态正确
        loopSources[sourceIndex].Stop();
        loopSources[sourceIndex].volume = 0f;
        loopSources[nextSourceIndex].volume = volume;

        Fading = false;
    }

    // 修改启动淡入淡出协程的方法
    private void StartFadeCoroutine(IEnumerator fadeRoutine)
    {
        var coroutine = StartCoroutine(fadeRoutine);
        fadeCoroutines.Add(coroutine);
    }
    #endregion
}