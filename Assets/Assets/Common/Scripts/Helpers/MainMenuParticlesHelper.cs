using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuParticlesHelper : MonoBehaviour
{
	// Start is called before the first frame update

	private static MainMenuParticlesHelper instance;

	public static MainMenuParticlesHelper Instance { get; }

    void Awake()
    {
		if(instance == null)
		{
			instance = this;
		}
		else if(instance != this)
		{
			Destroy(gameObject);
		}

        DontDestroyOnLoad(gameObject);
    }

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneChanged;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneChanged;
	}

	private void OnSceneChanged(Scene newScene, LoadSceneMode mode)
	{
		if (newScene.name == "Room")
		{
			transform.position = new Vector3(0, 5, 0);
		}
		else
		{
			transform.position = Vector3.zero;
		}
		if (newScene.name == "Singleplayer" || newScene.name == "Online")
		{
			Destroy(gameObject);
		}
	}

	// Update is called once per frame
	void Update()
    {
        
    }


}
