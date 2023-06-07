using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Logic for player camera behaviour
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    // access player data
    [HideInInspector]
    public PlayerDataContainer DataContainer = null;

    // current camera settings dictating the behaviour
    public PlayerCameraSettings CurrentCameraSetting = null;

    // all camera settings
    private PlayerCameraSettings[] _cameraSettings = null;

    // Update is called once per frame
    void Update()
    {
        // get target position for camera
        Vector3 targetPos = DataContainer.Player.transform.position + (-DataContainer.Player.transform.forward * (CurrentCameraSetting.CameraOffset));

        // smooth follow towards target position and apply
        Vector3 smoothFollow = Vector3.Lerp(transform.position, targetPos, CurrentCameraSetting.CameraFollowSpeed * Time.deltaTime);
        transform.position = new Vector3(smoothFollow.x, DataContainer.Player.transform.position.y + CurrentCameraSetting.CameraHeight, smoothFollow.z);

        // look at camera follow point
        transform.LookAt(DataContainer.CameraFollow.transform);
    }

    /// <summary>
    /// Change camera settings
    /// </summary>
    /// <param name="id"></param>
    public void ChangeCameraSettings(string id)
    {
        if (_cameraSettings == null)
            _cameraSettings = gameObject.GetComponentsInChildren<PlayerCameraSettings>();

        if (CurrentCameraSetting != null && CurrentCameraSetting.id.Equals(id))
            return;

        PlayerCameraSettings newSetting = _cameraSettings.First(x => x.id == id);

        if (newSetting != null)
            CurrentCameraSetting = newSetting;
    }

    /// <summary>
    /// Init camera when enabling gameobject
    /// </summary>
    private void OnEnable()
    {
        InitCamera();
    }

    /// <summary>
    /// Init camera data
    /// </summary>
    private void InitCamera()
    {
        // init follow point
        DataContainer.CameraFollow.transform.position = DataContainer.Player.transform.position;

        // init camera position
        Vector3 newPositions = DataContainer.Player.transform.position + (-DataContainer.Player.transform.forward * CurrentCameraSetting.CameraOffset);
        transform.position = new Vector3(newPositions.x, DataContainer.Player.transform.position.y + CurrentCameraSetting.CameraHeight, newPositions.z);
    }
}
