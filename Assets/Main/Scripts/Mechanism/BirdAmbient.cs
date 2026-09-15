using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BirdAmbient : MonoBehaviour
{
    [Header("Bird Sound")]
    public AudioClip birdClip;

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
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            if (!gameObject.activeInHierarchy)
                yield break;

            // Pitch ngẫu nhiên
            audioSource.pitch = Random.Range(minPitch, maxPitch);

            // Volume ngẫu nhiên
            float randomVolume = Random.Range(minVolume, maxVolume);

            // Nhân với Master Volume
            if (AudioManager.Instance != null)
            {
                audioSource.volume = randomVolume * AudioManager.Instance.masterVolume;
            }
            else
            {
                audioSource.volume = randomVolume;
            }

            // Phát tiếng chim
            audioSource.PlayOneShot(birdClip);
        }
    }
}