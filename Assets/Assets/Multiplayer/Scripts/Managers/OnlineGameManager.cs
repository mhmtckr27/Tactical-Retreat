using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OnlineGameManager : NetworkBehaviour
{
	private static OnlineGameManager instance;
	public static OnlineGameManager Instance { get => instance; }

	[SerializeField] private int totalPlayerCount;

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
	//if sth goes wrong, make it syncdictionary :D
	private Dictionary<uint, List<TerrainHexagon>> playersToDiscoveredTerrains = new Dictionary<uint, List<TerrainHexagon>>();
	public Dictionary<uint, List<TerrainHexagon>> PlayersToDiscoveredTerrains { get => playersToDiscoveredTerrains; }
	private List<TownCenter> playerList = new List<TownCenter>();
	private Dictionary<uint, TownCenter> players = new Dictionary<uint, TownCenter>();
	private Dictionary<uint, List<UnitBase>> units = new Dictionary<uint, List<UnitBase>>();
	private Dictionary<uint, List<BuildingBase>> buildings = new Dictionary<uint, List<BuildingBase>>();

	public Dictionary<BuildingBase, TerrainHexagon> buildingsToOccupiedTerrains = new Dictionary<BuildingBase, TerrainHexagon>();
	public Dictionary<UnitBase, TerrainHexagon> unitsToOccupiedTerrains = new Dictionary<UnitBase, TerrainHexagon>();

	private bool canGiveTurnToNextPlayer = true;
	private int hasTurnIndex = 0;

	[Server]
	private void Start()
	{
		Invoke(nameof(StartGame), .25f);
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
			PlayersToDiscoveredTerrains.Add(playerID, new List<TerrainHexagon>());
		}
		foreach (TerrainHexagon hex in distantNeighbours)
		{
			if (!PlayersToDiscoveredTerrains[playerID].Contains(hex))
			{
				PlayersToDiscoveredTerrains[playerID].Add(hex);
				shouldDiscover = true;
			}
		}
		if (shouldDiscover)
		{
			players[playerID].ExploreTerrains(distantNeighbours, true);
		}
	}

	[Server]
	public void RegisterPlayer(TownCenter player, bool isHost)
	{
		if (!players.ContainsKey(player.netId))
		{
			players.Add(player.netId, player);
			units.Add(player.netId, new List<UnitBase>());
			buildings.Add(player.playerID, new List<BuildingBase>());
			playerList.Add(player);
			players[player.netId].isHost = isHost;
			buildings[player.playerID].Add(player);
			//UpdateBuildingsOccupiedTerrain(player, player.OccupiedHex);
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
			buildings.Remove(player.netId);
			if (buildingsToOccupiedTerrains[player] != null)
			{
				buildingsToOccupiedTerrains[player].SetOccupierBuilding(null);
			}
			buildingsToOccupiedTerrains.Remove(player);
			if (playerList.Contains(player))
			{
				if(hasTurnIndex == playerList.IndexOf(player))
				{
					hasTurnIndex++;
				}
				playerList.Remove(player);
				NetworkServer.Destroy(player.gameObject);
				Destroy(player.gameObject);
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
				//UpdateUnitsOccupiedTerrain(unit, buildingsToOccupiedTerrains[players[playerID]]);
			}
		}
	}

	[Server]
	public void UpdateUnitsOccupiedTerrain(UnitBase unit, TerrainHexagon newOccupied)
	{
		if (!unitsToOccupiedTerrains.ContainsKey(unit))
		{
			unitsToOccupiedTerrains.Add(unit, newOccupied);
		}
		else
		{
			if(unitsToOccupiedTerrains[unit] != null)
			{
				unitsToOccupiedTerrains[unit].SetOccupierUnit(null);
			}
			unitsToOccupiedTerrains[unit] = newOccupied;
		}
		if (newOccupied != null)
		{
			newOccupied.SetOccupierUnit(unit);
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
				//UpdateBuildingsOccupiedTerrain(building, building.OccupiedHex);
			}
		}
	}

	[Server]
	public void UpdateBuildingsOccupiedTerrain(BuildingBase building, TerrainHexagon newOccupied)
	{
		if (!buildingsToOccupiedTerrains.ContainsKey(building))
		{
			buildingsToOccupiedTerrains.Add(building, newOccupied);
		}
		else
		{
			if(buildingsToOccupiedTerrains[building] != null)
			{
				buildingsToOccupiedTerrains[building].SetOccupierBuilding(null);
			}
			buildingsToOccupiedTerrains[building] = newOccupied;
		}
		if(newOccupied != null)
		{
			newOccupied.SetOccupierBuilding(building);
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
				if(unitsToOccupiedTerrains[unit] != null)
				{
					unitsToOccupiedTerrains[unit].SetOccupierUnit(null);
				}
				unitsToOccupiedTerrains.Remove(unit);
				players[playerID].UpdateResourceCount(ResourceType.CurrentPopulation, -1);
				NetworkServer.Destroy(unit.gameObject);
				Destroy(unit.gameObject);
				if ((units[playerID].Count == 0)/* && players[playerID].isConquered*/)
				{
					//players[playerID].transform.localScale = Vector3.zero;
					/*if (!players[playerID].isHost)
					{
						GameObject player = players[playerID].gameObject;
						UnregisterPlayer(players[playerID]);
						NetworkServer.Destroy(player);
					}*/
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
	public void UnregisterBuilding(uint playerID, BuildingBase building)
	{
		if (buildings.ContainsKey(playerID))
		{
			if (buildings[playerID].Contains(building))
			{
				buildings[playerID].Remove(building);
				if (buildingsToOccupiedTerrains[building] != null)
				{
					buildingsToOccupiedTerrains[building].SetOccupierBuilding(null);
				}
				buildingsToOccupiedTerrains.Remove(building);
				if ((buildings[playerID].Count == 0)/* && players[playerID].isConquered*/)
				{
					/*//players[playerID].transform.localScale = Vector3.zero;
                     GameObject player = players[playerID].gameObject;
                     UnregisterPlayer(players[playerID]);
                     Destroy(player.gameObject);*/
				}
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
	public List<UnitBase> GetUnits(uint playerID)
	{
		if (units.ContainsKey(playerID))
		{
			return units[playerID];
		}
		return null;
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

	public TownCenter GetPlayer(uint playerID)
	{
		if (units.ContainsKey(playerID))
		{
			return players[playerID];
		}
		return null;
	}

	[Server]
	public void StartGame()
	{
		foreach (TownCenter player in playerList)
		{
			player.EnableNextTurnButton(false);
		}
		NextTurn();
	}

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
