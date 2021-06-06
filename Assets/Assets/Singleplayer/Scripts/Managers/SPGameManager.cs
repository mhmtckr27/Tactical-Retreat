using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SPGameManager : MonoBehaviour
{
    private static SPGameManager instance;
    public static SPGameManager Instance
    {
        get { return instance; }
    }

    [SerializeField] private GameObject mapPrefab;
    [SerializeField] public bool enableMapVisibilityHack;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject aiPlayerPrefab;
    [SerializeField] private PluggableAI pluggableAIPrefab;
    [SerializeField] public List<GameObject> spawnablePrefabs;
    [SerializeField] private List<Color> playerColors;
    private bool[] colorsUsed;

    public int mapWidth = 6;
    public int aiPlayerCount = 1;
    private int totalPlayerCount;
    private uint currentPlayerIndex;

    private Dictionary<uint, List<SPTerrainHexagon>> playersToDiscoveredTerrains = new Dictionary<uint, List<SPTerrainHexagon>>();
    public Dictionary<uint, List<SPTerrainHexagon>> PlayersToDiscoveredTerrains { get => playersToDiscoveredTerrains; }
    private List<SPTownCenter> playerList = new List<SPTownCenter>();
    private Dictionary<uint, SPTownCenter> players = new Dictionary<uint, SPTownCenter>();
    private Dictionary<uint, List<SPUnitBase>> units = new Dictionary<uint, List<SPUnitBase>>();
    private Dictionary<uint, List<SPBuildingBase>> buildings = new Dictionary<uint, List<SPBuildingBase>>();

    private bool canGiveTurnToNextPlayer = true;
    private int hasTurnIndex;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

	private void Start()
	{
        hasTurnIndex = 0;
    }

	private void OnEnable()
	{
        SceneManager.sceneLoaded += OnSingleplayerGameStart;
    }

	private void OnDisable()
	{
        SceneManager.sceneLoaded -= OnSingleplayerGameStart;
    }

	public void OnSingleplayerGameStart(Scene scene, LoadSceneMode loadSceneMode)
	{
        if(scene.name == "Start") { Destroy(gameObject); }
        if (scene.name != "Singleplayer") { return; }
        totalPlayerCount = aiPlayerCount + 1;
        colorsUsed = new bool[playerColors.Count];
        currentPlayerIndex = 0;
        Instantiate(mapPrefab).GetComponent<SPMap>();
        SPMap.Instance.mapWidth = mapWidth;
        SPMap.Instance.SPGenerateMap();
        //map.SPDilateMap();
		if (!enableMapVisibilityHack)
		{
            SPMap.Instance.SPCreateUndiscoveredBlocks();
		}
        SpawnPlayers();
	}

    public void AddDiscoveredTerrains(uint playerID, string key, int distance)
    {
        bool shouldDiscover = false;
        List<SPTerrainHexagon> distantNeighbours = SPMap.Instance.GetDistantHexagons(SPMap.Instance.mapDictionary[key], distance);
        if (!PlayersToDiscoveredTerrains.ContainsKey(playerID))
        {
            PlayersToDiscoveredTerrains.Add(playerID, new List<SPTerrainHexagon>());
        }
        foreach (SPTerrainHexagon hex in distantNeighbours)
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

    private void SpawnPlayers()
	{
        Vector3 startPos = Vector3.zero;
        bool isValidPosToSpawn = false;
        string hexKey;
        for(int i = 0; i < totalPlayerCount; i++)
		{
            isValidPosToSpawn = false;
            do
            {
                int x = Random.Range(-SPMap.Instance.mapWidth + 1, SPMap.Instance.mapWidth);
                int y = Random.Range(-SPMap.Instance.mapWidth + 1, SPMap.Instance.mapWidth);
                int z = 0 - x - y;
                hexKey = x + "_" + y + "_" + z;
                if (SPMap.Instance.mapDictionary.ContainsKey(hexKey) && (SPMap.Instance.mapDictionary[hexKey].OccupierBuilding == null) && (SPMap.Instance.mapDictionary[hexKey].terrainType == TerrainType.Plain))
                {
                    isValidPosToSpawn = true;
                    startPos = SPMap.Instance.mapDictionary[hexKey].transform.position;
                }
            } while (!isValidPosToSpawn);
            if (i == 0)
            {
                hexKey = "5_-5_0";
                startPos = SPMap.Instance.mapDictionary[hexKey].transform.position;
            }
			else
			{
                hexKey = "0_0_0";
                startPos = SPMap.Instance.mapDictionary[hexKey].transform.position;
            }

            GameObject player;
            SPTownCenter tempPlayer;
            int colorIndex;

            do
            {
                colorIndex = Random.Range(0, playerColors.Count);
            } while (colorsUsed[colorIndex] == true);

            if (i == 0)
			{
                player = Instantiate(playerPrefab, startPos, Quaternion.Euler(0, -60, 0));
                tempPlayer = player.GetComponent<SPTownCenter>();
                tempPlayer.IsAI = false;
            }
            else
            {
                player = Instantiate(aiPlayerPrefab, startPos, Quaternion.Euler(0, -60, 0));
                tempPlayer = player.GetComponent<SPTownCenterAI>();
                tempPlayer.IsAI = true;
                tempPlayer.PluggableAI = Instantiate(pluggableAIPrefab, tempPlayer.transform).GetComponent<PluggableAI>();
            }

            tempPlayer.OccupiedHex = SPMap.Instance.mapDictionary[hexKey];
            tempPlayer.PlayerColor = playerColors[colorIndex];
            tempPlayer.PlayerID = currentPlayerIndex;

            currentPlayerIndex++;
            colorsUsed[colorIndex] = true;
            RegisterPlayer(tempPlayer);
		}
        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
	{
        yield return new WaitForEndOfFrame();
        NextTurn();
	}

	public void RegisterPlayer(SPTownCenter player)
    {
        if (!players.ContainsKey(player.PlayerID))
        {
            players.Add(player.PlayerID, player);
            units.Add(player.PlayerID, new List<SPUnitBase>());
            buildings.Add(player.PlayerID, new List<SPBuildingBase>());
            playerList.Add(player);
            Debug.Log("Player-" + player.PlayerID + " has joined the game");
        }
    }

    public void UnregisterPlayer(SPTownCenter player)
    {
        if (players.ContainsKey(player.PlayerID))
        {
            players.Remove(player.PlayerID);
            units.Remove(player.PlayerID);
            buildings.Remove(player.PlayerID);
            if (playerList.Contains(player))
            {
                if (hasTurnIndex == playerList.IndexOf(player))
                {
                    hasTurnIndex++;
                }
                playerList.Remove(player);

                foreach(SPTownCenterAI tc in playerList)
				{
                    //TODO: may throw error, check .Contains first.
                    tc.exploredEnemyTowns.Remove(player);
				}
                Destroy(player.gameObject);
            }
            Debug.Log("Player-" + player.PlayerID + " has left the game");
        }
    }
    public void RegisterUnit(uint playerID, SPUnitBase unit)
    {
        if (units.ContainsKey(playerID))
        {
            if (!units[playerID].Contains(unit))
            {
                units[playerID].Add(unit);
            }
        }
    }
    public void RegisterBuilding(uint playerID, SPBuildingBase building)
    {
        if (buildings.ContainsKey(playerID))
        {
            if (!buildings[playerID].Contains(building))
            {
                buildings[playerID].Add(building);
            }
        }
    }
    public void UnregisterUnit(uint playerID, SPUnitBase unit)
    {
        if (units.ContainsKey(playerID))
        {
            if (units[playerID].Contains(unit))
            {
                units[playerID].Remove(unit);
                players[playerID].UpdateResourceCount(ResourceType.CurrentPopulation, -1);
                if ((units[playerID].Count == 0)/* && players[playerID].isConquered*/)
                {
                   /*//players[playerID].transform.localScale = Vector3.zero;
                    GameObject player = players[playerID].gameObject;
                    UnregisterPlayer(players[playerID]);
                    Destroy(player.gameObject);*/
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

    public void UnregisterBuilding(uint playerID, SPBuildingBase building)
    {
        if (buildings.ContainsKey(playerID))
        {
            if (buildings[playerID].Contains(building))
            {
                buildings[playerID].Remove(building);
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

    public List<SPUnitBase> GetUnits(uint playerID)
	{
		if (units.ContainsKey(playerID))
		{
            return units[playerID];
		}
        return null;
	}

    public List<SPBuildingBase> GetBuildings(uint playerID)
	{
		if (buildings.ContainsKey(playerID))
		{
            return buildings[playerID];
		}
        return null;
	}

    public SPTownCenter GetPlayer(uint playerID)
	{
		if (units.ContainsKey(playerID))
		{
            return players[playerID];
		}
        return null;
	}

    public void NextTurn()
    {
        if (!canGiveTurnToNextPlayer) { return; }
        GiveTurnToPlayer(playerList[hasTurnIndex], true);
    }

    public void GiveTurnToPlayer(SPTownCenter player, bool giveTurn)
    {
        canGiveTurnToNextPlayer = false;
        player.SetHasTurn(true);
    }

    public void PlayerFinishedTurn(SPTownCenter player)
    {
        hasTurnIndex = (hasTurnIndex == (playerList.Count - 1)) ? 0 : (hasTurnIndex + 1);
        canGiveTurnToNextPlayer = true;
        player.SetHasTurn(false);
        NextTurn();
    }

}
