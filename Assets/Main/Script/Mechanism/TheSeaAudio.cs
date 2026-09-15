using UnityEngine;

public class TheSeaAudio : MonoBehaviour
{
    private void Start()
    {
        SetupSeaAudio();
    }

    private void OnEnable()
    {
        SetupSeaAudio();
    }

    private void SetupSeaAudio()
    {
        var audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.PlayEnvironment(audio.theNightClip);
        }
    }
}
