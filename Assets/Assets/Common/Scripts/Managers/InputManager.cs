using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
	private const float tapTimeThreshold = 0.2f;
	private const int totalFingerCount = 1;
	private float[] touchBeganTime;
	private bool[] touchDidMove;

	private void Start()
	{
		touchBeganTime = new float[totalFingerCount];
		touchDidMove = new bool[totalFingerCount];
	}

	public bool HasValidTap()
	{
		if(Input.touchCount != 1) { return false; }
		foreach (Touch touch in Input.touches)
		{
			if (touch.phase == TouchPhase.Began)
			{
				touchBeganTime[touch.fingerId] = Time.timeSinceLevelLoad;
				touchDidMove[touch.fingerId] = false;
			}
			else if (touch.phase == TouchPhase.Moved)
			{
				touchDidMove[touch.fingerId] = true;
			}
			else if (touch.phase == TouchPhase.Ended)
			{
				float tapTime = Time.timeSinceLevelLoad - touchBeganTime[touch.fingerId];
				if (tapTime < tapTimeThreshold && touchDidMove[touch.fingerId] == false)
				{
					return true;
				}
			}
		}
		return false;
	}
}
