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

	public Dictionary<uint, List<UnitBase>> Units { get => units; }

	public GameObject MapPrefab;
	private Dictionary<uint, TownCenter> players = new Dictionary<uint, TownCenter>();
	private Dictionary<uint, List<UnitBase>> units = new Dictionary<uint, List<UnitBase>>();

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

	public void RegisterPlayer(TownCenter player)
	{
		if (!players.ContainsKey(player.netId))
		{
			players.Add(player.netId, player);
			Units.Add(player.netId, new List<UnitBase>());
		}
	}

	public void UnregisterPlayer(TownCenter player)
	{
		if (players.ContainsKey(player.netId))
		{
			players.Remove(player.netId);
		}
	}

	public void RegisterUnit(uint playerID, UnitBase unit)
	{
		if (Units.ContainsKey(playerID))
		{
			if (!Units[playerID].Contains(unit))
			{
				Units[playerID].Add(unit);
				unitCount++;
			}
		}
	}
	int unitCount = 0;
	public void UnregisterUnit(uint playerID, UnitBase unit)
	{
		if (Units.ContainsKey(playerID))
		{
			if (Units[playerID].Contains(unit))
			{
				Units[playerID].Remove(unit);
				if (canDie)
				{
					players[playerID].transform.localScale = Vector3.zero;
				}
			}
			else
			{
				Debug.LogWarning("no unit found");
			}
		}
		else
		{
			Debug.LogWarning("no player found: " + playerID);
		}
	}
	bool canDie = false;
	public void Update()
	{
		if (unitCount == 2) canDie = true;
		foreach(KeyValuePair<uint, List<UnitBase>> keyValuePair in units)
		{
			//Debug.LogError(keyValuePair.Key);
			foreach(UnitBase unit in keyValuePair.Value)
			{
				Debug.LogError(keyValuePair.Key + " : " + unit.name);
			}
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
