using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OnlineGameManager : NetworkBehaviour
{
	[SerializeField] private int totalPlayerCount;
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

	private List<TownCenter> playerList = new List<TownCenter>();
	private Dictionary<uint, TownCenter> players = new Dictionary<uint, TownCenter>();
	private Dictionary<uint, List<UnitBase>> units = new Dictionary<uint, List<UnitBase>>();
	private Dictionary<uint, List<BuildingBase>> buildings = new Dictionary<uint, List<BuildingBase>>();

	[Server]
	private void Start()
	{
		Invoke(nameof(NextTurn), .25f);
	}

	[Server]
	public override void OnStartServer()
	{
		base.OnStartServer();
	}

	[Server]
	public void RegisterPlayer(TownCenter player)
	{
		if (!players.ContainsKey(player.netId))
		{
			players.Add(player.netId, player);
			units.Add(player.netId, new List<UnitBase>());
			playerList.Add(player);
			Debug.Log("Player-" + player.netId + " has joined the game");
		}
	}

	[Server]
	public void UnregisterPlayer(TownCenter player)
	{
		if (players.ContainsKey(player.netId))
		{
			players.Remove(player.netId);
			units.Remove(player.netId);
			if (playerList.Contains(player))
			{
				if(hasTurnIndex == playerList.IndexOf(player))
				{
					hasTurnIndex++;
				}
				playerList.Remove(player);
			}
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

	[Server]
	public void RegisterBuilding(uint playerID, BuildingBase building)
	{
		if (buildings.ContainsKey(playerID))
		{
			if (!buildings[playerID].Contains(building))
			{
				buildings[playerID].Add(building);
			}
		}
	}

	[Server]
	public void UnregisterBuilding(uint playerID, BuildingBase building)
	{
		if (buildings.ContainsKey(playerID))
		{
			if (buildings[playerID].Contains(building))
			{
				buildings[playerID].Remove(building);
			}
			else
			{
				Debug.LogWarning("no building found");
			}
		}
		else
		{
			Debug.LogWarning("no player found: " + playerID);
		}
	}

	[Server]
	public List<BuildingBase> GetBuildings(uint playerID)
	{
		if (buildings.ContainsKey(playerID))
		{
			return buildings[playerID];
		}
		return null;
	}

	private bool canGiveTurnToNextPlayer = true;
	private int hasTurnIndex = 0;

	[Server]
	public void NextTurn()
	{
		if(!canGiveTurnToNextPlayer) { return; }
		GiveTurnToPlayer(playerList[hasTurnIndex], true);
	}

	[Server]
	public void GiveTurnToPlayer(TownCenter player, bool giveTurn)
	{
		canGiveTurnToNextPlayer = false;
		player.SetHasTurn(true);
	}

	[Server]
	public void PlayerFinishedTurn(TownCenter player)
	{
		hasTurnIndex = (hasTurnIndex == (playerList.Count - 1)) ? 0 : (hasTurnIndex + 1);
		canGiveTurnToNextPlayer = true;
		player.SetHasTurn(false);
		NextTurn();
	}
}
