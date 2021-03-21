using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MyNetworkRoomManager : NetworkRoomManager
{
	private static MyNetworkRoomManager instance;
	public static MyNetworkRoomManager Instance
	{
		get
		{
			return instance;
		}
	}
	public GameObject MapPrefab;
	public override void Awake()
	{
		base.Awake();
		if(instance == null)
		{
			instance = this;
		}
		else if(instance != this)
		{
			Destroy(gameObject);
		}
	}

	public override void OnRoomServerSceneChanged(string sceneName)
	{
		if (sceneName == "Assets/Scenes/Online.unity")
		{
			Instantiate(MapPrefab);
			Map.Instance.GenerateMap();
			Map.Instance.DilateMap();
		}
	}
}
