using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SPGameManager : MonoBehaviour
{
    [SerializeField] private GameObject mapPrefab;
    public int mapWidth = 6;
    public int aiPlayerCount = 1;
    private static SPGameManager instance;

    private Map map;

    public static SPGameManager Instance
    {
        get { return instance; }
    }

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
        map = Instantiate(mapPrefab).GetComponent<Map>();
        map.mapWidth = mapWidth;
        map.SPGenerateMap();
        map.SPDilateMap();
	}
}
