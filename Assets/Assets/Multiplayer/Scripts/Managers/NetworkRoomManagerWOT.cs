using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkRoomManagerWOT : NetworkRoomManager
{
	[SerializeField] private GameObject MapPrefab;
	[SerializeField] private GameObject onlineGameManagerPrefab;
	[SerializeField] private List<Color> playerColors;
	private bool[] colorsUsed;
	public int mapWidth;
	/*
	public Dictionary<int, string> playerNames = new Dictionary<int, string>();

	public void AddPlayerName(int index, string playerName)
	{
		if (playerNames.ContainsKey(index)) { return; }
		int i = 0;
		string playerNameOld = playerName;
		while (playerNames.ContainsValue(playerName))
		{
			i++;
			playerName = playerNameOld + "_" + i;
		}
		playerNames.Add(index, playerName);
	}

	public void UpdatePlayerNameIndex(int oldIndex, int newIndex, string playerName)
	{
		if (playerNames.ContainsKey(oldIndex))
		{
			playerNames.Remove(oldIndex);
			playerNames.Add(newIndex, playerName);
		}
	}
	*/
	public System.Action OnMapGenerationFinish;
	public System.Action<string> OnMapGenerationStatusChange;

	public LoadingScreenManager loadingScreenManager;

	public override void Awake()
	{
		networkSceneName = offlineScene;
		colorsUsed = new bool[playerColors.Count];
	}

	public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
	{
		Vector3 startPos = Vector3.zero;
		bool isValidPosToSpawn = false;
		string hexKey;
		do
		{
			int x = Random.Range(-Map.Instance.mapWidth + 1, Map.Instance.mapWidth);
			int y = Random.Range(-Map.Instance.mapWidth + 1, Map.Instance.mapWidth);
			int z = 0 - x - y;
			hexKey = x + "_" + y + "_" + z;
			if (Map.Instance.mapDictionary.ContainsKey(hexKey) && (Map.Instance.mapDictionary[hexKey].GetOccupierBuilding() == null) && (Map.Instance.mapDictionary[hexKey].terrainType == TerrainType.Plain))
			{
				isValidPosToSpawn = true;
				startPos = Map.Instance.mapDictionary[hexKey].transform.position;
			}
		} while (!isValidPosToSpawn);
		/*
		if(Map.Instance.mapDictionary["5_-5_0"].OccupierBuilding == null)
			startPos = Map.Instance.mapDictionary["5_-5_0"].transform.position;
		else
			startPos = Map.Instance.mapDictionary["-5_5_0"].transform.position;
		*/
		GameObject player = Instantiate(playerPrefab, startPos, Quaternion.Euler(0, -60, 0));
		int colorIndex;
		do
		{
			colorIndex = Random.Range(0, playerColors.Count);
		} while (colorsUsed[colorIndex] == true);
		TownCenter tempPlayer = player.GetComponent<TownCenter>();
		//tempPlayer.OccupiedHex = Map.Instance.mapDictionary[hexKey];
		OnlineGameManager.Instance.UpdateBuildingsOccupiedTerrain(tempPlayer, Map.Instance.mapDictionary[hexKey]);
		tempPlayer.OnTerrainOccupiersChange(OnlineGameManager.Instance.buildingsToOccupiedTerrains[tempPlayer].Key, 1);
		tempPlayer.playerColor = playerColors[colorIndex];
		colorsUsed[colorIndex] = true;
		return player;
	}

	public override void OnRoomServerSceneChanged(string sceneName)
	{
		if (sceneName == GameplayScene)
		{

			GameObject map = Instantiate(MapPrefab);
			NetworkServer.Spawn(map);
			Map.Instance.SetMapWidth(mapWidth);

			Map.Instance.GenerateMap();
			Map.Instance.CreateUndiscoveredBlocks();
			Instantiate(onlineGameManagerPrefab);
		}
	}

	public override void OnGUI()
	{
		if (!showRoomGUI)
			return;
	}
}
