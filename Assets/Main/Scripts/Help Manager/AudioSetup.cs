using UnityEngine;

[CreateAssetMenu(
    fileName = "AudioSetup",
    menuName = "Scriptable Objects/Audio Setup"
)]
public class AudioSetup : ScriptableObject
{ 
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Range(0f, 1f)]
    public float environmentVolume = 1f;

    [Range(0f, 1f)]
    public float specialVolume = 1f;
  
}