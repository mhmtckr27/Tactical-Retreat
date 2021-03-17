using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] [Range(2, 10)] private float min_camera_size;
    [SerializeField] [Range(2, 10)] private float max_camera_size;
    [SerializeField] [Range(0.05f, 1f)]private float zoom_modifier = 0.1f;
    [SerializeField] [Range(10, 250)] private float should_zoom_threshold = 10;

    private float distance_between_touches = float.MaxValue;
    private float new_distance_between_touches;
    private Vector2 delta_touch_position;
    private Camera cam;
    bool should_zoom = false;
    bool should_move = false;

	private void Awake()
	{
        cam = GetComponent<Camera>();
    }

	void Update()
    {
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
        else if (Input.touchCount == 2)
		{
            new_distance_between_touches = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
            if (Mathf.Abs(new_distance_between_touches - distance_between_touches) > should_zoom_threshold)
			{
                should_zoom = true;
			}
			else
			{
                should_zoom = false;
			}
        }
		else
		{
            should_zoom = false;
        }
    }
	private void LateUpdate()
	{
		if (should_move)
		{
            Map.Instance.transform.position = cam.ScreenToWorldPoint(new Vector3(Input.touches[0].deltaPosition.x, 0, Input.touches[0].deltaPosition.y) * Time.deltaTime);
		}
		if (should_zoom)
		{
            if (new_distance_between_touches < distance_between_touches)
            {
                cam.orthographicSize += zoom_modifier;
                if (cam.orthographicSize > max_camera_size)
                {
                    cam.orthographicSize = max_camera_size;
                }
            }
            else
            {
                cam.orthographicSize -= zoom_modifier;
                if (cam.orthographicSize < min_camera_size)
                {
                    cam.orthographicSize = min_camera_size;
                }
            }
            distance_between_touches = new_distance_between_touches;
		}
    }
}
