using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkRoomManagerWOT : NetworkRoomManager
{
	private static NetworkRoomManagerWOT instance;
	public static NetworkRoomManagerWOT Instance
	{
		get
		{
			return instance;
		}
	}
	[SerializeField] private GameObject MapPrefab;
	[SerializeField] private GameObject onlineGameManagerPrefab;
	[SerializeField] private List<Color> playerColors;
	private bool[] colorsUsed;


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
		colorsUsed = new bool[playerColors.Count];
	}
	
	public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
	{
		Vector3 startPos = Vector3.zero;
		bool isValidPosToSpawn = false;
		do
		{
			int x = Random.Range(-Map.Instance.mapWidth + 1, Map.Instance.mapWidth);
			int y = Random.Range(-Map.Instance.mapWidth + 1, Map.Instance.mapWidth);
			int z = 0 - x - y;
			string hexKey = x + "_" + y + "_" + z;
			if (Map.Instance.mapDictionary.ContainsKey(hexKey) && (Map.Instance.mapDictionary[hexKey].OccupierBuilding == null) && (Map.Instance.mapDictionary[hexKey].terrainType == TerrainType.Plain))
			{
				isValidPosToSpawn = true;
				startPos = Map.Instance.mapDictionary[hexKey].transform.position;
			}
		} while (!isValidPosToSpawn);

		GameObject player = Instantiate(playerPrefab, startPos, Quaternion.identity);
		int colorIndex;
		do
		{
			colorIndex = Random.Range(0, playerColors.Count);
			Debug.LogWarning(colorIndex);
		} while (colorsUsed[colorIndex] == true);
		//player.GetComponent<Renderer>().materials[1].color = playerColors[colorIndex];
		player.GetComponent<TownCenter>().playerColor = playerColors[colorIndex];
		colorsUsed[colorIndex] = true;
		return player;
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
