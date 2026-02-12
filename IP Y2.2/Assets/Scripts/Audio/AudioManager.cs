using System.Collections;
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

  private AudioSource bgmSource;
  private AudioSource rainSource;
  private float currentVolume = 1f;
  private Coroutine crossfadeRoutine;

  private const string VolumeKey = "MusicVolume";

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

  IEnumerator CrossfadeBGM(AudioClip newClip)
  {
    float startVol = bgmSource.volume;

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

  public void InitSlider(Slider slider)
  {
    slider.value = currentVolume;
    slider.onValueChanged.AddListener(SetVolume);
  }
}
