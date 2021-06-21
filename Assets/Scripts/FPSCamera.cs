using UnityEngine;

public class FPSCamera : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] float moveSpeed = 2.0f;
	[SerializeField] float runSpeed = 4.0f;

	[Header("Rotation")]
	[SerializeField] private float verticalSensitivity = 1.0f;
	[SerializeField] private float horizontalSensitivity = 1.0f;
	[SerializeField] private float minPitchDegrees = -90.0f;
	[SerializeField] private float maxPitchDegrees = 90.0f;

	private float pitch = 0.0f;
	private float yaw = 0.0f;
	private bool wasPrevRotating = false;
	private bool isNowRotating = false;

	private void Start()
	{
		Vector3 eulerAngles = transform.eulerAngles;
		pitch = eulerAngles.x;
		yaw = eulerAngles.y;
	}

	private void Update()
	{
		UpdateMovement();
		UpdateRotation();
	}

	private void UpdateMovement()
	{
		float x = Input.GetAxis(Constants.HorizontalAxis);
		float y = Input.GetAxis(Constants.DepthicalAxis);
		float z = Input.GetAxis(Constants.VerticalAxis);

		Vector3 direction = new Vector3(x, y, z);
		float speed = Input.GetButton(Constants.RunKey) ? runSpeed : moveSpeed;

		transform.Translate(direction * speed * Time.deltaTime);
	}

	private void UpdateRotation()
	{
		wasPrevRotating = isNowRotating;
		isNowRotating = Input.GetButton(Constants.CameraMovementKey);

		if (!wasPrevRotating && isNowRotating)
		{
			Cursor.visible = false;
		}
		else if (wasPrevRotating && !isNowRotating)
		{
			Cursor.visible = true;
		}

		if (!isNowRotating)
		{
			return;
		}

		yaw   += Input.GetAxis(Constants.MouseX) * horizontalSensitivity;
		pitch -= Input.GetAxis(Constants.MouseY) * verticalSensitivity;

		pitch = Mathf.Clamp(pitch, minPitchDegrees, maxPitchDegrees);

		transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
	}
}
