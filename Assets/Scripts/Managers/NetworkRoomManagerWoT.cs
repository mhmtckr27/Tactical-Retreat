using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkRoomManagerWoT : NetworkRoomManager
{
	private static NetworkRoomManagerWoT instance;
	public static NetworkRoomManagerWoT Instance
	{
		get
		{
			return instance;
		}
	}

	[SerializeField] private GameObject MapPrefab;
	[SerializeField] private GameObject onlineGameManagerPrefab;

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
		if (sceneName == GameplayScene)
		{
			Instantiate(MapPrefab);
			Map.Instance.GenerateMap();
			Map.Instance.DilateMap();
			Instantiate(onlineGameManagerPrefab);
		}
	}
}
