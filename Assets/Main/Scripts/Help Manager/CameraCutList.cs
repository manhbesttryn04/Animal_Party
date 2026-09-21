using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CameraCutList : MonoBehaviour
{
    [Header("CutScene Camera")]
    public List<MiniGameCameraCutscene> miniGameCameraList;
    [Header("Camera MiniGame")]
    public List<Camera> cameraList;

    public void SetActiveCameraMiniGame(int index, bool i)
    {
        cameraList[index].gameObject.SetActive(i);
}

}

