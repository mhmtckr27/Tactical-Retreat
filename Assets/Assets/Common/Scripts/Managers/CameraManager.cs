using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraManager : NetworkBehaviour
{
    [SerializeField] private float minCamSize = 2;
    [SerializeField] private float maxCamSize = 10;
    [SerializeField] private float dragSpeed = 50;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private CameraClearFlags clearFlags;
    private bool isSingleplayer;

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

    public void UpdateCameraSizes()
	{
        if(Map.Instance.mapWidth > 4)
		{
            maxCamSize *= (float)Map.Instance.mapWidth / 4;
            cam.nearClipPlane = -Map.Instance.mapWidth * 2.5f;
        }
    }

    public void SPUpdateCameraSizes()
    {
        if (SPMap.Instance.mapWidth > 4)
        {
            maxCamSize *= (float)SPMap.Instance.mapWidth / 4;
            cam.nearClipPlane = -SPMap.Instance.mapWidth * 2.5f;
        }
    }

    private void Update()
    {
#if UNITY_ANDROID
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
#else
		shouldMove = Input.GetMouseButton(0);
		zoomAmount = Input.GetAxis("Mouse ScrollWheel");
#endif
	}

    private void LateUpdate()
	{
		if (shouldMove)
		{
#if UNITY_ANDROID
			speed = (cam.orthographicSize / initialCamSize) * .25f * Time.deltaTime;
			Vector3 newCamPos = cam.transform.position - new Vector3(Input.touches[0].deltaPosition.y * speed, 0, -Input.touches[0].deltaPosition.x * speed);

			newCamPos = CalculateNewCameraPosition(newCamPos);

			cam.transform.position = newCamPos;
#else
			speed = (cam.orthographicSize / initialCamSize) * dragSpeed * Time.deltaTime;
			Vector3 newCamPos = cam.transform.position - new Vector3(Input.GetAxis("Mouse Y") * speed, 0, -Input.GetAxis("Mouse X") * speed);

			newCamPos = CalculateNewCameraPosition(newCamPos);

			cam.transform.position = newCamPos;
#endif
		}
		Zoom();
    }

	private Vector3 CalculateNewCameraPosition(Vector3 calculatedNewCamPos)
	{
        int mapWidth = Map.Instance.mapWidth;

        if (calculatedNewCamPos.x > (mapWidth * 2 - 1) / 2 * Map.blockHeight - 4)
		{
			calculatedNewCamPos.x = (mapWidth * 2 - 1) / 2 * Map.blockHeight - 4;
		}
		else if (calculatedNewCamPos.x < (-1 * (mapWidth * 2 - 1) / 2 * Map.blockHeight - 8))
		{
			calculatedNewCamPos.x = -1 * (mapWidth * 2 - 1) / 2 * Map.blockHeight - 8;
		}

		string leftMostHexKey = "-" + (mapWidth - 1) + "_" + (mapWidth - 1) + "_0";
		string rightMostHexKey = (mapWidth - 1) + "_" + "-" + (mapWidth - 1) + "_0";
		
		GetHexagonPositionCmd(leftMostHexKey, 0);
		GetHexagonPositionCmd(rightMostHexKey, 1);
		if (calculatedNewCamPos.z > pos.z)
		{
			calculatedNewCamPos.z = pos.z;
		}
		else if (calculatedNewCamPos.z < pos2.z)
		{
			calculatedNewCamPos.z = pos2.z;
		}
		
		return calculatedNewCamPos;
	}

	[SyncVar] public Vector3 pos;
	[SyncVar] public Vector3 pos2;

	[Command (requiresAuthority = false)]
	private void GetHexagonPositionCmd(string key, int index)
	{
		if (Map.Instance.mapDictionary.ContainsKey(key))
		{
			if (index == 0)
			{
				pos = Map.Instance.mapDictionary[key].transform.position;
			}
			else
			{
				pos2 = Map.Instance.mapDictionary[key].transform.position;
			}
		}
		else
		{
			if(index == 0)
			{
				pos = Vector3.zero;
			}
			else
			{
				pos2 = Vector3.zero;
			}
		}
	}

	private void Zoom()
	{
        if (zoomAmount != 0)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - zoomAmount * zoomSpeed, minCamSize, maxCamSize);
        }
    }
}
