using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OnlineGameManager : NetworkBehaviour
{
	private static OnlineGameManager instance;
	public static OnlineGameManager Instance { get => instance; }

	[Server]
	private void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
	}

	private Dictionary<uint, TownCenter> players = new Dictionary<uint, TownCenter>();
	private Dictionary<uint, List<UnitBase>> units = new Dictionary<uint, List<UnitBase>>();

	[Server]
	public void RegisterPlayer(TownCenter player)
	{
		if (!players.ContainsKey(player.netId))
		{
			players.Add(player.netId, player);
			units.Add(player.netId, new List<UnitBase>());
			Debug.Log("Player-" + player.netId + " has joined the game");
		}
	}

	[Server]
	public void UnregisterPlayer(TownCenter player)
	{
		if (players.ContainsKey(player.netId))
		{
			players.Remove(player.netId);
			Debug.Log("Player-" + player.netId + " has left the game");
		}
	}

	[Server]
	public void RegisterUnit(uint playerID, UnitBase unit)
	{
		if (units.ContainsKey(playerID))
		{
			if (!units[playerID].Contains(unit))
			{
				units[playerID].Add(unit);
			}
		}
	}

	[Server]
	public void UnregisterUnit(uint playerID, UnitBase unit)
	{
		if (units.ContainsKey(playerID))
		{
			if (units[playerID].Contains(unit))
			{
				units[playerID].Remove(unit);
				if (units[playerID].Count == 0)
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
}
