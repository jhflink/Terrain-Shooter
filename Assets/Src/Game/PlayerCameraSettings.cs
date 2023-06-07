using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container for camera settings
/// </summary>
public class PlayerCameraSettings : MonoBehaviour
{
    // id of camera settings
    public string id = "";

    // offset from player
    public float CameraOffset = 10.0f;

    // speed to smooth follow player
    public float CameraFollowSpeed = 0.1f;

    // height above ground
    public float CameraHeight = 5.0f;
}
