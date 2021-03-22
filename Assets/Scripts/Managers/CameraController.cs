using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] [Range(2, 10)] private float minCameraSize;
    [SerializeField] [Range(2, 10)] private float maxCameraSize;
    [SerializeField] [Range(0.05f, 1f)]private float zoomModifier = 0.1f;
    [SerializeField] [Range(10, 250)] private float shouldZoomThreshold = 10;

    private float distanceBetweenTouches = float.MaxValue;
    private float newDistanceBetweenTouches;
    private Camera cam;
    bool shouldZoom = false;
    //bool should_move = false;

	private void Awake()
	{
        cam = GetComponent<Camera>();
    }

	void Update()
    {/*
        if (Input.touchCount == 1)
        {
            if (Input.touches[0].phase == TouchPhase.Moved && Input.touches[0].deltaPosition.magnitude > 15)
			{
                should_move = true;
            }
			else
			{
                should_move = false;
			}
        }
  else*/if (Input.touchCount == 2)
		{
            newDistanceBetweenTouches = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
            if (Mathf.Abs(newDistanceBetweenTouches - distanceBetweenTouches) > shouldZoomThreshold)
			{
                shouldZoom = true;
			}
			else
			{
                shouldZoom = false;
			}
        }
		else
		{
            shouldZoom = false;
        }
    }
	private void LateUpdate()
	{
		/*if (should_move)
		{
            Map.Instance.transform.position = cam.ScreenToWorldPoint(new Vector3(Input.touches[0].deltaPosition.x, 0, Input.touches[0].deltaPosition.y) * Time.deltaTime);
		}*/
		if (shouldZoom)
		{
            if (newDistanceBetweenTouches < distanceBetweenTouches)
            {
                cam.orthographicSize += zoomModifier;
                if (cam.orthographicSize > maxCameraSize)
                {
                    cam.orthographicSize = maxCameraSize;
                }
            }
            else
            {
                cam.orthographicSize -= zoomModifier;
                if (cam.orthographicSize < minCameraSize)
                {
                    cam.orthographicSize = minCameraSize;
                }
            }
            distanceBetweenTouches = newDistanceBetweenTouches;
		}
    }
}
