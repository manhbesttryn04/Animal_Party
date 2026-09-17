using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BirdAmbient : MonoBehaviour
{
    [Header("Bird Sounds")]
    public List<AudioClip> birdClipList = new List<AudioClip>();

    [Header("Random List Count")]
    [Tooltip("Số lượng AudioClip được phép random.")]
    public int listCount = 3;

    [Header("Random Delay")]
    public float minDelay = 10f;
    public float maxDelay = 25f;

    [Header("Random Pitch")]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Header("Random Volume")]
    public float minVolume = 0.2f;
    public float maxVolume = 1f;

    private AudioSource audioSource;
    private Coroutine birdCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        birdCoroutine = StartCoroutine(BirdSoundLoop());
    }

    private void OnDisable()
    {
        if (birdCoroutine != null)
        {
            StopCoroutine(birdCoroutine);
            birdCoroutine = null;
        }

        audioSource.Stop();
    }

    private IEnumerator BirdSoundLoop()
    {
        while (true)
        {
            // Chờ ngẫu nhiên
            yield return new WaitForSeconds(
                Random.Range(minDelay, maxDelay)
            );

            if (!gameObject.activeInHierarchy)
                yield break;

            // Kiểm tra List có âm thanh không
            if (birdClipList == null || birdClipList.Count == 0)
                continue;

            // Giới hạn số lượng AudioClip được random
            int randomCount = Mathf.Min(listCount, birdClipList.Count);

            // Random AudioClip
            int randomIndex = Random.Range(0, randomCount);
            AudioClip randomClip = birdClipList[randomIndex];

            if (randomClip == null)
                continue;

            // Random Pitch
            audioSource.pitch = Random.Range(
                minPitch,
                maxPitch
            );

            // Random Volume
            float randomVolume = Random.Range(
                minVolume,
                maxVolume
            );

            // Nhân với Master Volume
            if (AudioManager.Instance != null)
            {
                audioSource.volume =
                    randomVolume * AudioManager.Instance.masterVolume;
            }
            else
            {
                audioSource.volume = randomVolume;
            }

            // Phát âm thanh chim
            audioSource.PlayOneShot(randomClip);
        }
    }
}