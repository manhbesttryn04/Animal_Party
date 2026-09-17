using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Setup List")]
    public List<AudioSetup> audioSetupList = new List<AudioSetup>();
    public AudioSetup mainGameAudioSetup;

    [Header("Audio Sources")]
    public AudioSource musicSource;   // Nhạc nền
    public AudioSource sfxSource;     // Hiệu ứng
    public AudioSource environmentSource; // Môi trường
    public AudioSource specialSource; // Âm thanh đặc biệt
    public AudioSource UISource;
    public List<AudioSource> audioSources;


    [Header("SFX")]
    [Header("Player SFX")]
    public AudioClip walkPlayerClip;
    public AudioClip diceRollClip;
    public AudioClip playerTeleport;
    public AudioClip playerOneClip;
    public AudioClip playerTwoClip;
    public AudioClip playerOneWinClip;
    public AudioClip playerTwoWinClip;
    public AudioClip bothPlayerDrawClip;

    [Header("Main Scene Environment")]
    public AudioClip theSeaClip;
    [Header("Main Menu")]
    public AudioClip theNightClip;
    public AudioClip theSeaAndShipClip;
    public AudioClip musicMainMenuClip;
    [Header("Choose Scene")]
    public AudioClip musicChooseSceneClip;
    public AudioClip doneChooseClickClip;
    public AudioClip startGameButtonClickClip;
   
    [Header("CutScene 1")]
    public AudioClip musicCutScene1Clip;
    public AudioClip shipVoiceClip;
    public AudioClip shipMoveClip;
    [Header("CutScene 2")]
    public AudioClip musicCutScene2Clip;
    public AudioClip llamaVoiceClip;

    [Header("Debuff SFX")]
    public AudioClip cannonClip;
    public AudioClip fallingBom;
    public AudioClip boomClip;
    public AudioClip bebuffRockMagicClip;
    public AudioClip skipDiceClip;
    public AudioClip petrificatioDebuffVoiceClip;
    public AudioClip cannonDebuffVoiceClip;
    [Header("Buff SFX")]
    public AudioClip buffDeffClip;
    public AudioClip buffMagicClip;
    public AudioClip bonusBuffClip;
    public AudioClip coinClip;
    public AudioClip startLeteClip;
    public AudioClip endLeteClip;
    public AudioClip powerCannonVoiceClip;
    public AudioClip bonusDiceVoiceClip;
    public AudioClip cannonShieldVoiceClip;
    public AudioClip petrificationImmunityVoiceClip;
    [Header("Trap Main Scene")]
    public AudioClip PositionSwapVoiceClip;
    public AudioClip bombVoiceClip;

    [Header("Shop SFX")]
    public AudioClip openShopClip;
    public AudioClip movechooseItemClip;
    public AudioClip buyItemClip;
    public AudioClip openCardRamdomClip;
    public AudioClip noCoinBuyItemClip;
    public AudioClip skipBuyClip;

    [Header("UI SFX")]
    public AudioClip openResultPanel;
    public AudioClip nextRound;
    public AudioClip clickButton;
    public AudioClip audioReduction;
    [Header("Camera SFX")]
    public AudioClip moveCamera;
    [Header("Console")]
    public AudioClip consoleControllerConect;
    public AudioClip consoleControllerDisConect;

    [Header("Winner")]
    public AudioClip winnerClip;
    public AudioClip winnerMiniGameClip1;
    public AudioClip winnerMiniGameClip2;
    public AudioClip fourPowerCoinClip;
    public AudioClip threethirtyIndexClip;

    [Header("Teleport")]
    public AudioClip openTeleportClip;
    public AudioClip closeTeleportClip;


    [Header("MusicGame")]
    public AudioClip musicMainClip;
    public AudioClip musicMiniGame1;
    public AudioClip musicMiniGame2;
    public AudioClip musicMiniGame3;
    public AudioClip musicMiniGame4;
    public AudioClip musicMiniGame5;
    public AudioClip musicMiniGame6;
    public AudioClip musicMiniGame7;
    public AudioClip musicMiniGame8;
    public AudioClip musicMiniGame9;
    public AudioClip musicMiniGame10;
    public AudioClip creditMusicClip;

    [Header("Minigame 1")]
    public AudioClip loadBrickClip;
    public AudioClip sharkAttackClip;
    public AudioClip warningClip;
    [Header("Minigame 2")]
    public AudioClip brickFallClip;
    public AudioClip javaLoopClip;
    [Header("Minigam 3")]
    public AudioClip laserHitClip;
    public AudioClip fireHitClip;
    public AudioClip laserMoveClip;
    public AudioClip openFireClip;
    [Header("Minigame 4")]
    public AudioClip scanPiratesClip;
    public AudioClip laughPiratesClip;
    public AudioClip gunShotPiratesClip;
    public AudioClip seaGullClip;
    public AudioClip[] piratesSingClipList;
    [Header("Minigame 5")]
    public AudioClip snowFallClip;
    public AudioClip buffBigClip;
    public AudioClip iceMagicClip;
    [Header("Minigame 6")]
    public AudioClip bombTickingClip;
    public AudioClip explosionBombClip;
    public AudioClip freezeTrapClip; // Biến âm thanh bẫy băng
    public AudioClip magnetTrapClip; // Biến âm thanh bẫy nam châm


    private Coroutine fadeAllAudioCoroutine;

    // Âm lượng gốc của từng nhóm âm thanh.
    // Master Volume sẽ nhân với các giá trị này, không làm mất tỉ lệ ban đầu.
    private float baseMusicVolume = 1f;
    private float baseSFXVolume = 1f;
    private float baseEnvironmentVolume = 1f;
    private float baseSpecialVolume = 1f;
    private float baseUIVolume = 1f;
    private float[] baseExtraSourceVolumes;

    public float masterVolume = 1f;
    private float musicSettingVolume = 1f;
    private float sfxSettingVolume = 1f;
    private bool settingMusicMode;
    private float settingMusicVolumeMultiplier = 0.15f;


    private void Awake()
    {
        SetupSingleton();
        SetupAudioSources();
        CacheExtraAudioSourceVolumes();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        if (musicSource != null)
            musicSource.loop = true;

        if (specialSource != null)
            specialSource.loop = false;

        if (environmentSource != null)
            environmentSource.loop = true;
    }
    private void Start()
    {


    }


    /// <summary>
    /// Đổi nhạc nền
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (musicSource.clip == clip)
            return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    /// <summary>
    /// Phát nhạc và tăng dần âm lượng từ 0 đến mức đã cài đặt.
    /// </summary>
    public void FadeInMusic(AudioClip clip, float fadeTime)
    {
        if (clip == null || musicSource == null)
            return;

        CancelFadeOutAllAudio();

        float targetVolume =
            baseMusicVolume * musicSettingVolume * masterVolume;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        fadeAllAudioCoroutine = StartCoroutine(
            FadeInMusicRoutine(targetVolume, fadeTime)
        );
    }

    private IEnumerator FadeInMusicRoutine(
        float targetVolume,
        float fadeTime
    )
    {
        if (fadeTime <= 0f)
        {
            musicSource.volume = targetVolume;
            fadeAllAudioCoroutine = null;
            yield break;
        }

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.unscaledDeltaTime;

            float percent = Mathf.Clamp01(time / fadeTime);
            musicSource.volume = Mathf.Lerp(
                0f,
                targetVolume,
                percent
            );

            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeAllAudioCoroutine = null;
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }
    public void PlaySFXNoOneShot(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        sfxSource.Stop();
        sfxSource.clip = clip;
        sfxSource.loop = loop;
        sfxSource.Play();
    }

    public void StopSFXNoOneShot()
    {
        sfxSource.Stop();
        sfxSource.clip = null;
        sfxSource.loop = false;
    }
    public void StopMusic()
    {
        musicSource.Stop();
    }
    public void PlayEnvironment(AudioClip clip)
    {

        if (clip == null) return;

        if (environmentSource.clip == clip)
            return;

        environmentSource.clip = clip;
        environmentSource.Play();
    }
    public void StopEnvironment()
    {
        if (environmentSource == null) return;

        environmentSource.Stop();
        environmentSource.clip = null;
    }
    public void PlaySpecial(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;

        if (specialSource.clip == clip &&
            specialSource.isPlaying &&
            specialSource.loop == loop)
            return;

        specialSource.Stop();

        specialSource.clip = clip;
        specialSource.loop = loop;
        specialSource.Play();
    }
    public void StopSpecial()
    {
        specialSource.Stop();
        specialSource.clip = null;
        specialSource.loop = false;
    }
    public void PlaySpecialOneShot(AudioClip clip)
    {
        if (clip == null) return;

        specialSource.PlayOneShot(clip);
    }
    public void PlayUI(AudioClip clip)
    {
        if (clip == null) return;

        UISource.PlayOneShot(clip);
    }

    public void SetupMusicMiniGame(int indexMiniGame)
    {

        SetupAudioByMiniGame(indexMiniGame);

        if (indexMiniGame == 1)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame1);
        }
        else if (indexMiniGame == 2)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame2);
        }
        else if (indexMiniGame == 3)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame3);
        }
        else if (indexMiniGame == 4)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame4);
        }
        else if (indexMiniGame == 5)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame5);
        }
        else if (indexMiniGame == 6)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame6);
        }
        else if (indexMiniGame == 7)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame7);
        }
        else if (indexMiniGame == 8)
        {
            PlayMusic(AudioManager.Instance.musicMiniGame8);
        }
        else if (indexMiniGame == 9)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.musicMiniGame9);
        }
        else if (indexMiniGame == 10)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.musicMiniGame10);
        }
    }
    public void SetupMainGameAudio()
    {
        // Tránh coroutine fade của cutscene/scene trước tiếp tục chạy sang scene mới.
        CancelFadeOutAllAudio();

        if (mainGameAudioSetup != null)
        {
            // Lưu âm lượng gốc
            baseMusicVolume = mainGameAudioSetup.musicVolume;
            baseSFXVolume = mainGameAudioSetup.sfxVolume;

            // Chỉ Music và SFX chịu ảnh hưởng của Setting
            RefreshSettingVolume();

            baseEnvironmentVolume = mainGameAudioSetup.environmentVolume;
            baseSpecialVolume = mainGameAudioSetup.specialVolume;

            RefreshSettingVolume();
        }
        else
        {

        }
    }

    public void SetupAudioByMiniGame(int indexMiniGame)
    {
        // Tránh coroutine fade của scene trước tiếp tục giảm âm lượng minigame mới.
        CancelFadeOutAllAudio();

        int index = indexMiniGame - 1;

        if (index < 0 || index >= audioSetupList.Count)
        {

            return;
        }

        AudioSetup setup = audioSetupList[index];

        // Lưu âm lượng gốc của Music và SFX
        baseMusicVolume = setup.musicVolume;
        baseSFXVolume = setup.sfxVolume;

        // Áp dụng Slider
        RefreshSettingVolume();

        // Environment và Special chỉ chịu ảnh hưởng của Master Volume.
        baseEnvironmentVolume = setup.environmentVolume;
        baseSpecialVolume = setup.specialVolume;

        RefreshSettingVolume();
    }
    //==================================================
    // MUSIC SETTING
    //==================================================

    public void ApplyMusicSetting(float sliderValue)
    {
        musicSettingVolume = Mathf.Clamp01(sliderValue);
        RefreshAllVolumes();
    }

    //==================================================
    // SFX SETTING
    //==================================================

    public void ApplySFXSetting(float sliderValue)
    {
        sfxSettingVolume = Mathf.Clamp01(sliderValue);
        RefreshAllVolumes();
    }

    //==================================================
    // MASTER VOLUME SETTING
    //==================================================

    public void ApplyMasterSetting(float sliderValue)
    {
        masterVolume = Mathf.Clamp01(sliderValue);
        RefreshAllVolumes();
    }

    private void RefreshAllVolumes()
    {
        if (musicSource != null)
        {
            float settingMultiplier =
                settingMusicMode
                    ? settingMusicVolumeMultiplier
                    : 1f;

            musicSource.volume =
                baseMusicVolume *
                musicSettingVolume *
                masterVolume *
                settingMultiplier;
        }
        if (sfxSource != null)
            sfxSource.volume = baseSFXVolume * sfxSettingVolume * masterVolume;

        if (environmentSource != null)
            environmentSource.volume = baseEnvironmentVolume * masterVolume;

        if (specialSource != null)
            specialSource.volume = baseSpecialVolume * masterVolume;

        if (UISource != null)
            UISource.volume = baseUIVolume * masterVolume;

        if (audioSources == null || baseExtraSourceVolumes == null)
            return;

        int count = Mathf.Min(audioSources.Count, baseExtraSourceVolumes.Length);

        for (int i = 0; i < count; i++)
        {
            AudioSource source = audioSources[i];

            if (source == null || IsMainAudioSource(source))
                continue;

            source.volume = baseExtraSourceVolumes[i] * masterVolume;
        }
    }
    public void SetSettingMusicMode(
    bool enabled,
    float volumeMultiplier = 0.15f)
    {
        settingMusicMode = enabled;
        settingMusicVolumeMultiplier =
            Mathf.Clamp01(volumeMultiplier);

        RefreshAllVolumes();
    }

    private void CacheExtraAudioSourceVolumes()
    {
        baseUIVolume = UISource != null ? UISource.volume : 1f;

        if (audioSources == null)
        {
            baseExtraSourceVolumes = new float[0];
            return;
        }

        baseExtraSourceVolumes = new float[audioSources.Count];

        for (int i = 0; i < audioSources.Count; i++)
        {
            baseExtraSourceVolumes[i] =
                audioSources[i] != null ? audioSources[i].volume : 1f;
        }
    }

    private bool IsMainAudioSource(AudioSource source)
    {
        return source == musicSource ||
               source == sfxSource ||
               source == environmentSource ||
               source == specialSource ||
               source == UISource;
    }

    //==================================================
    // REFRESH SETTING VOLUME
    //==================================================

    public void RefreshSettingVolume()
    {
        float masterSliderValue = 1f;
        float musicSliderValue = 1f;
        float sfxSliderValue = 1f;

        if (SettingManager.Instance != null)
        {
            masterSliderValue =
                SettingManager.Instance.masterValue;

            musicSliderValue =
                SettingManager.Instance.musicValue;

            sfxSliderValue =
                SettingManager.Instance.sfxValue;
        }

        masterVolume = Mathf.Clamp01(masterSliderValue);
        musicSettingVolume = Mathf.Clamp01(musicSliderValue);
        sfxSettingVolume = Mathf.Clamp01(sfxSliderValue);
        RefreshAllVolumes();
    }

    /// <summary>
    /// Giảm toàn bộ AudioSource về 0 theo thời gian.
    /// </summary>
    public void FadeOutAllAudio(float fadeTime)
    {
        CancelFadeOutAllAudio();

        fadeAllAudioCoroutine = StartCoroutine(FadeOutAllAudioRoutine(fadeTime));
    }

    /// <summary>
    /// Dừng riêng coroutine fade âm thanh đang chạy.
    /// Cần gọi khi skip hoặc khi bắt đầu scene mới vì AudioManager là DontDestroyOnLoad.
    /// </summary>
    public void CancelFadeOutAllAudio()
    {
        if (fadeAllAudioCoroutine == null)
            return;

        StopCoroutine(fadeAllAudioCoroutine);
        fadeAllAudioCoroutine = null;
    }

    private IEnumerator FadeOutAllAudioRoutine(float fadeTime)
    {
        float musicStart = musicSource.volume;
        float sfxStart = sfxSource.volume;
        float environmentStart = environmentSource.volume;
        float specialStart = specialSource.volume;

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;

            float t = time / fadeTime;

            musicSource.volume = Mathf.Lerp(musicStart, 0f, t);
            sfxSource.volume = Mathf.Lerp(sfxStart, 0f, t);
            environmentSource.volume = Mathf.Lerp(environmentStart, 0f, t);
            specialSource.volume = Mathf.Lerp(specialStart, 0f, t);

            yield return null;
        }

        musicSource.volume = 0f;
        sfxSource.volume = 0f;
        environmentSource.volume = 0f;
        specialSource.volume = 0f;

        fadeAllAudioCoroutine = null;
    }
    public void StopAllAudio()
    {
        CancelFadeOutAllAudio();

        for (int i = 0; i < audioSources.Count; i++)
        {
            audioSources[i].volume = 0;
        }
        musicSource.volume = 0;
        StopMusic();
        environmentSource.volume = 0;
        StopEnvironment();
        specialSource.volume = 0f;
        StopSpecial();
        sfxSource.volume = 0;
    }
    public void PauseAudio()
    {
        CancelFadeOutAllAudio();

        StopMusic();
        StopEnvironment();
        StopSpecial();
        StopSFXNoOneShot();
    }

    public void PauseGameplayAudio()
    {
        var setting = SettingManager.Instance;
        if(setting != null)
        {
            if (!setting.enableSettingMusic)
            {
                if (musicSource != null) musicSource.Pause();
            }
        }
       
        if (sfxSource != null)
            sfxSource.Pause();

        if (environmentSource != null)
            environmentSource.Pause();

        if (specialSource != null)
            specialSource.Pause();
    }

    public void ResumeGameplayAudio()
    {
        if (musicSource != null) musicSource.UnPause();
        if (sfxSource != null)
            sfxSource.UnPause();

        if (environmentSource != null)
            environmentSource.UnPause();

        if (specialSource != null)
            specialSource.UnPause();
    }

    public void ZeroAllAudio()
    {
        // Skip cutscene gọi hàm này, vì vậy phải dừng fade cũ trước.
        CancelFadeOutAllAudio();

        musicSource.volume = 0;
        sfxSource.volume = 0;
        environmentSource.volume = 0;
        specialSource.volume = 0;
    }
}