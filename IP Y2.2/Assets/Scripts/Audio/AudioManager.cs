/// <summary>
/// File: AudioManager.cs
/// Author: Jayden Wong
/// Description: Manages background music crossfading between scenes and rain ambience with persistent volume control.
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
  public static AudioManager Instance { get; private set; }

  [Header("BGM Clips")]
  [SerializeField] private AudioClip menuBGM;
  [SerializeField] private AudioClip gameBGM;

  [Header("Ambience")]
  [SerializeField] private AudioClip rainClip;

  [Header("Volume Tuning")]
  [SerializeField][Range(0f, 1f)] private float bgmMaxVolume = 0.4f;
  [SerializeField][Range(0f, 1f)] private float rainMaxVolume = 1f;

  [Header("Crossfade")]
  [SerializeField] private float crossfadeDuration = 0.3f;

  [Header("SFX Settings")]
  [SerializeField] private float sfxCooldown = 1f;

  private AudioSource bgmSource;
  private AudioSource rainSource;
  private AudioSource sfxSource;
  private float currentVolume = 1f;
  private Coroutine crossfadeRoutine;

  private const string VolumeKey = "MusicVolume";

  private Dictionary<string, float> lastPlayedTimes = new Dictionary<string, float>();

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(this);
      return;
    }

    Instance = this;

    bgmSource = gameObject.AddComponent<AudioSource>();
    bgmSource.loop = true;
    bgmSource.playOnAwake = false;

    rainSource = gameObject.AddComponent<AudioSource>();
    rainSource.loop = true;
    rainSource.playOnAwake = false;

    sfxSource = gameObject.AddComponent<AudioSource>();
    sfxSource.playOnAwake = false;
  }

  void OnEnable()
  {
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  void OnDisable()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  void OnDestroy()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  void Start()
  {
    currentVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    ApplyVolume();

    if (rainClip != null)
    {
      rainSource.clip = rainClip;
      rainSource.Play();
    }

    if (menuBGM != null)
    {
      bgmSource.clip = menuBGM;
      bgmSource.Play();
    }
  }

  void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    AudioClip targetClip = scene.name == "GameScene" ? gameBGM : menuBGM;

    if (targetClip == null || bgmSource.clip == targetClip)
      return;

    if (crossfadeRoutine != null)
      StopCoroutine(crossfadeRoutine);

    crossfadeRoutine = StartCoroutine(CrossfadeBGM(targetClip));
  }

  /// <summary>
  /// Crossfades from current BGM to new clip over configured duration
  /// </summary>
  IEnumerator CrossfadeBGM(AudioClip newClip)
  {
    float startVol = bgmSource.volume;

    // Use unscaledDeltaTime so crossfade works even when Time.timeScale = 0
    for (float t = 0f; t < crossfadeDuration; t += Time.unscaledDeltaTime)
    {
      bgmSource.volume = Mathf.Lerp(startVol, 0f, t / crossfadeDuration);
      yield return null;
    }

    bgmSource.volume = 0f;
    bgmSource.clip = newClip;
    bgmSource.Play();

    float targetVol = currentVolume * bgmMaxVolume;

    for (float t = 0f; t < crossfadeDuration; t += Time.unscaledDeltaTime)
    {
      bgmSource.volume = Mathf.Lerp(0f, targetVol, t / crossfadeDuration);
      yield return null;
    }

    bgmSource.volume = targetVol;
    crossfadeRoutine = null;
  }

  /// <summary>
  /// Plays SFX with cooldown protection to prevent rapid retriggering
  /// </summary>
  public void PlaySFX(AudioClip clip)
  {
    if (clip == null) return;

    string clipName = clip.name;
    float currentTime = Time.time;

    // Check if this clip was played recently
    if (lastPlayedTimes.ContainsKey(clipName))
    {
      if (currentTime - lastPlayedTimes[clipName] < sfxCooldown)
      {
        return; // Too soon, ignore this request
      }
    }

    // Play the sound and update last played time
    sfxSource.PlayOneShot(clip, currentVolume);
    lastPlayedTimes[clipName] = currentTime;
  }

  /// <summary>
  /// Sets master volume and persists to PlayerPrefs
  /// </summary>
  public void SetVolume(float value)
  {
    currentVolume = value;
    ApplyVolume();
    PlayerPrefs.SetFloat(VolumeKey, value);
  }

  void ApplyVolume()
  {
    bgmSource.volume = currentVolume * bgmMaxVolume;
    rainSource.volume = currentVolume * rainMaxVolume;
  }

  /// <summary>
  /// Initializes UI slider with current volume and binds value change event
  /// </summary>
  public void InitSlider(Slider slider)
  {
    slider.value = currentVolume;
    slider.onValueChanged.AddListener(SetVolume);
  }
}
