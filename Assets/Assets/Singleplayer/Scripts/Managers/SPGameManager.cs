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
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] public List<GameObject> spawnablePrefabs;
    [SerializeField] private List<Color> playerColors;
    private bool[] colorsUsed;

    public int mapWidth = 6;
    public int aiPlayerCount = 1;
    private SPMap map;
    private int totalPlayerCount;
    private uint currentPlayerIndex;

    private Dictionary<uint, List<string>> playersToDiscoveredTerrains = new Dictionary<uint, List<string>>();
    public Dictionary<uint, List<string>> PlayersToDiscoveredTerrains { get => playersToDiscoveredTerrains; }
    private List<SPTownCenter> playerList = new List<SPTownCenter>();
    private Dictionary<uint, SPTownCenter> players = new Dictionary<uint, SPTownCenter>();
    private Dictionary<uint, List<SPUnitBase>> units = new Dictionary<uint, List<SPUnitBase>>();

    private bool canGiveTurnToNextPlayer = true;
    private int hasTurnIndex = 0;

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
        if (scene.name != "Singleplayer") { return; }
        totalPlayerCount = aiPlayerCount + 1;
        colorsUsed = new bool[playerColors.Count];
        currentPlayerIndex = 0;
        map = Instantiate(mapPrefab).GetComponent<SPMap>();
        map.mapWidth = mapWidth;
        map.SPGenerateMap();
        //map.SPDilateMap();
        map.SPCreateUndiscoveredBlocks();
        SpawnPlayers();
	}

	private void SpawnPlayers()
	{
        Vector3 startPos = Vector3.zero;
        bool isValidPosToSpawn = false;
        string hexKey;
        for(int i = 0; i < totalPlayerCount; i++)
		{
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

            GameObject player = Instantiate(playerPrefab, startPos, Quaternion.Euler(0, -60, 0));
            int colorIndex;
            do
            {
                colorIndex = Random.Range(0, playerColors.Count);
            } while (colorsUsed[colorIndex] == true);
            SPTownCenter tempPlayer = player.GetComponent<SPTownCenter>();
            tempPlayer.occupiedHex = SPMap.Instance.mapDictionary[hexKey];
            tempPlayer.PlayerColor = playerColors[colorIndex];
            tempPlayer.playerID = currentPlayerIndex;
            currentPlayerIndex++;
            colorsUsed[colorIndex] = true;
            RegisterPlayer(tempPlayer);
		}
        NextTurn();
    }

	public void RegisterPlayer(SPTownCenter player)
    {
        if (!players.ContainsKey(player.playerID))
        {
            players.Add(player.playerID, player);
            units.Add(player.playerID, new List<SPUnitBase>());
            playerList.Add(player);
            Debug.Log("Player-" + player.playerID + " has joined the game");
        }
    }

    public void UnregisterPlayer(SPTownCenter player)
    {
        if (players.ContainsKey(player.playerID))
        {
            players.Remove(player.playerID);
            units.Remove(player.playerID);
            if (playerList.Contains(player))
            {
                if (hasTurnIndex == playerList.IndexOf(player))
                {
                    hasTurnIndex++;
                }
                playerList.Remove(player);
            }
            Debug.Log("Player-" + player.playerID + " has left the game");
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

    public void UnregisterUnit(uint playerID, SPUnitBase unit)
    {
        if (units.ContainsKey(playerID))
        {
            if (units[playerID].Contains(unit))
            {
                units[playerID].Remove(unit);
                if ((units[playerID].Count == 0)/* && players[playerID].isConquered*/)
                {
                    //players[playerID].transform.localScale = Vector3.zero;
                    GameObject player = players[playerID].gameObject;
                    UnregisterPlayer(players[playerID]);
                    Destroy(player.gameObject);
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

    public void AddDiscoveredTerrains(uint playerID, string key, int distance)
    {
        bool shouldDiscover = false;
        List<SPTerrainHexagon> distantNeighbours = SPMap.Instance.GetDistantHexagons(SPMap.Instance.mapDictionary[key], distance);
        if (!PlayersToDiscoveredTerrains.ContainsKey(playerID))
        {
            PlayersToDiscoveredTerrains.Add(playerID, new List<string>());
        }
        foreach (SPTerrainHexagon hex in distantNeighbours)
        {
            if (!PlayersToDiscoveredTerrains[playerID].Contains(hex.Key))
            {
                PlayersToDiscoveredTerrains[playerID].Add(hex.Key);
                shouldDiscover = true;
            }
        }
        if (shouldDiscover)
        {
            players[playerID].ExploreTerrains(distantNeighbours, true);
        }
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
