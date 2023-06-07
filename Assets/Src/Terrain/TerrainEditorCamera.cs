using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to handle camera logic
/// </summary>
[RequireComponent(typeof(Camera))]
public class TerrainEditorCamera : MonoBehaviour {
	public Camera CameraRaw = null;

	public float acceleration = 50;			// how fast you accelerate
	public float accSprintMultiplier = 4;	// how much faster you go when "sprinting"
	public float lookSensitivity = 1;		// mouse look sensitivity
	public float dampingCoefficient = 5;	// how quickly you break to a halt after you stop your input
		
	Vector3 velocity; // current velocity

	// keys and mouse buttons that are used and if not we will revert from the camera state
	private List<(KeyCode, Vector3)> keys = null;
	private List<int> mouseButtons = new List<int>() { 1 };

    private void Start() {
        // set up movement controls
		keys = new List<(KeyCode, Vector3)>() {
			(KeyCode.W, Vector3.forward),
			(KeyCode.A, Vector3.left),
			(KeyCode.S, Vector3.back),
			(KeyCode.D, Vector3.right),
		};
	}

	/// <summary>
    /// Update the camera input and state
    /// </summary>
    /// <param name="currentState"></param>
    /// <returns>The same or a new state to change to</returns>
    public TerrainEditorMain.EditorInputState InputStateCameraUpdate(TerrainEditorMain.EditorInputState currentState) {
		Cursor.visible = !Input.GetMouseButton(1);

		if(!isAnyCameraInputActive())
			return TerrainEditorMain.EditorInputState.Edit;

		// Position
		velocity += GetAccelerationVector() * Time.deltaTime;

		// Rotation
		if (!Cursor.visible) {
			Vector2 mouseDelta = lookSensitivity * new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
			Quaternion rotation = transform.rotation;
			Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
			Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
			transform.rotation = horiz * rotation * vert;
		}

		return currentState;
	}

	/// <summary>
    /// Update physics of camera even if we're in another state
    /// </summary>
    private void Update() {
		// Physics should run even if we're not in the input state of camera,
		// hence this code being in a general update function
		velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
		transform.position += velocity * Time.deltaTime;
	}

	/// <summary>
    /// Check if we're using any camera input controls
    /// </summary>
    /// <returns></returns>
    public bool isAnyCameraInputActive() {
		// check keys
		foreach (int mouseButton in mouseButtons)
			if (Input.GetMouseButtonDown(mouseButton) || Input.GetMouseButton(mouseButton) || Input.GetMouseButtonUp(mouseButton))
				return true;

		// check mouse buttons
		foreach ((KeyCode keyCode,Vector3 direction) keyMap in keys)
			if (Input.GetKeyDown(keyMap.keyCode) || Input.GetKey(keyMap.keyCode) || Input.GetKeyUp(keyMap.keyCode))
				return true;

		return false;
    }

	/// <summary>
    /// Calculate velocity from input
    /// </summary>
    /// <returns></returns>
	Vector3 GetAccelerationVector() {
		Vector3 moveInput = default;

		// check if we had any input and apply direction vector
		foreach ((KeyCode keyCode, Vector3 direction) keyMap in keys) {
			if(Input.GetKey(keyMap.keyCode))
				moveInput += keyMap.direction;
		}

		// transform vector from local to world space
		Vector3 direction = transform.TransformVector(moveInput.normalized);

		// return direction vector with multiplyer if we hold left shift aka run
		if (Input.GetKey(KeyCode.LeftShift))
			return direction * (acceleration * accSprintMultiplier);

		// return riection vector 
		return direction * acceleration;
	}
}