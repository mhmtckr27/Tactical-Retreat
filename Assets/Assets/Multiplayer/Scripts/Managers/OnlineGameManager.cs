using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OnlineGameManager : NetworkBehaviour
{
	[SerializeField] private int totalPlayerCount;
	private static OnlineGameManager instance;
	public static OnlineGameManager Instance { get => instance; }
	public SyncDictionary<uint, List<string>> PlayersToDiscoveredTerrains { get => playersToDiscoveredTerrains; }

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
	private SyncDictionary<uint, List<string>> playersToDiscoveredTerrains = new SyncDictionary<uint, List<string>>();

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
	public void AddDiscoveredTerrains(uint playerID, string key, int distance)
	{
		bool shouldDiscover = false;
		List<TerrainHexagon> distantNeighbours = Map.Instance.GetDistantHexagons(Map.Instance.mapDictionary[key], distance);
		if (!PlayersToDiscoveredTerrains.ContainsKey(playerID))
		{
			PlayersToDiscoveredTerrains.Add(playerID, new List<string>());
		}
		foreach (TerrainHexagon hex in distantNeighbours)
		{
			if (!PlayersToDiscoveredTerrains[playerID].Contains(hex.Key))
			{
				PlayersToDiscoveredTerrains[playerID].Add(hex.Key);
				shouldDiscover = true;
			}
		}
		if (shouldDiscover)
		{
			players[playerID].ExploreTerrainsRpc(distantNeighbours, true);
		}
	}

	[Server]
	public void RegisterPlayer(TownCenter player, bool isHost)
	{
		if (!players.ContainsKey(player.netId))
		{
			players.Add(player.netId, player);
			units.Add(player.netId, new List<UnitBase>());
			playerList.Add(player);
			players[player.netId].isHost = isHost;
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
				if ((units[playerID].Count == 0)/* && players[playerID].isConquered*/)
				{
					//players[playerID].transform.localScale = Vector3.zero;
					if (!players[playerID].isHost)
					{
						GameObject player = players[playerID].gameObject;
						UnregisterPlayer(players[playerID]);
						NetworkServer.Destroy(player);
					}
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
