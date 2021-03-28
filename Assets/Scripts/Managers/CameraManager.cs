using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private float minCamSize = 2;
    [SerializeField] private float maxCamSize = 10;
    [SerializeField] private float dragSpeed = 50;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private CameraClearFlags clearFlags;

    private bool shouldMove = false;
    private float zoomAmount = 0f;
    private float speed = 50f;

    private float initialCamSize;
    private Camera cam;

	private void Awake()
	{
        cam = GetComponent<Camera>();
        initialCamSize = cam.orthographicSize;
    }

	private void Start()
	{
        cam.clearFlags = clearFlags;
	}

    private void Update()
    {
#if UNITY_EDITOR
        shouldMove = Input.GetMouseButton(0);
        zoomAmount = Input.GetAxis("Mouse ScrollWheel");
#elif UNITY_ANDROID
        shouldMove = (Input.touchCount == 1);
        if(Input.touchCount == 2)
		{
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0LastFramePos = touch0.position - touch0.deltaPosition;
            Vector2 touch1LastFramePos = touch1.position - touch1.deltaPosition;

            float distBetweenTouchesLastFrame = (touch1LastFramePos - touch0LastFramePos).sqrMagnitude;
            float distBetweenTouchesCurrentFrame = (touch1.position - touch0.position).sqrMagnitude;

            zoomAmount = (distBetweenTouchesCurrentFrame - distBetweenTouchesLastFrame) * 0.000002f;
        }
        else
        {
            zoomAmount = 0;
        }

#endif
    }

    private void LateUpdate()
	{
		if (shouldMove)
		{
#if UNITY_EDITOR
			speed = (cam.orthographicSize / initialCamSize) * dragSpeed * Time.deltaTime;
            cam.transform.position -= new Vector3(Input.GetAxis("Mouse Y") * speed, 0, -Input.GetAxis("Mouse X") * speed);
#elif UNITY_ANDROID
			speed = (cam.orthographicSize / initialCamSize) * .25f * Time.deltaTime;
            cam.transform.position -= new Vector3(Input.touches[0].deltaPosition.y * speed, 0, -Input.touches[0].deltaPosition.x * speed);
#endif
        }
        Zoom();
    }

    private void Zoom()
	{
        if (zoomAmount != 0)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - zoomAmount * zoomSpeed, minCamSize, maxCamSize);
        }
    }
}