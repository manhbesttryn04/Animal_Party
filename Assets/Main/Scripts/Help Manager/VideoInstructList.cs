using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoInstructList : MonoBehaviour
{
    public List<VideoClip> videoInstructList;

    public void ShowInstructMiniGame(VideoPlayer video, int index)
    {
        if(videoInstructList[index] == null) return;
        video.clip = videoInstructList[index];
    }
}
