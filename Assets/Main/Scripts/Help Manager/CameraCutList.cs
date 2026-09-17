using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CameraCutList : MonoBehaviour
{
    public List<MiniGameCameraCutscene> miniGameCameraList;
    public List<Camera> cameraList;

    public void SetActiveCameraMiniGame(int index, bool i)
    {
        cameraList[index].gameObject.SetActive(i);
}

}

