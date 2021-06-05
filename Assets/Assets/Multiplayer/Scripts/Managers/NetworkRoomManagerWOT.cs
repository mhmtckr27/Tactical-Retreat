using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkRoomManagerWOT : NetworkRoomManager
{
	[SerializeField] private GameObject MapPrefab;
	[SerializeField] private GameObject onlineGameManagerPrefab;
	[SerializeField] private List<Color> playerColors;
	private bool[] colorsUsed;

	public override void Awake()
	{
		base.Awake();
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
			Map.Instance.GenerateMap();
			Map.Instance.CreateUndiscoveredBlocks();
			//Map.Instance.DilateMap();
			Instantiate(onlineGameManagerPrefab);
		}
	}

	public override void OnGUI()
	{
		if (!showRoomGUI)
			return;

		/*if (NetworkServer.active && IsSceneActive(GameplayScene))
		{
			GUILayout.BeginArea(new Rect(Screen.width - 150f, 10f, 140f, 30f));
			if (GUILayout.Button("Return to Room"))
				ServerChangeScene(RoomScene);
			GUILayout.EndArea();
		}*/
		
		if (IsSceneActive(RoomScene))
			GUI.Box(new Rect(10f, 180f, 520f, 150f), "PLAYERS");
	}
}
