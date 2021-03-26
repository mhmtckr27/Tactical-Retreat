using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CameraClearFlags clearFlags;

    private Camera cam;
	private void Awake()
	{
        cam = GetComponent<Camera>();
    }
	private void Start()
	{
        cam.clearFlags = clearFlags;
	}

    public float cameraDragSpeed = 50;
    bool shouldMove = false;
    float speed;
    private void Update()
    {
        if (Input.GetMouseButton(0) || Input.touchCount == 1)
        {
            shouldMove = true;
        }
		else
		{
            shouldMove = false;
		}
    }

	private void LateUpdate()
	{
		if (shouldMove)
		{
#if UNITY_EDITOR
			speed = 50 * Time.deltaTime;
            cam.transform.position -= new Vector3(Input.GetAxis("Mouse Y") * speed, 0, -Input.GetAxis("Mouse X") * speed);
#elif UNITY_ANDROID
			speed = cameraDragSpeed * Time.deltaTime;
            cam.transform.position -= new Vector3(Input.touches[0].deltaPosition.y * speed, 0, -Input.touches[0].deltaPosition.x * speed);
#endif
        }
    }
}
