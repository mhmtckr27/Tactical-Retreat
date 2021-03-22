using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UIController : MonoBehaviour
{
	[SerializeField] private GameObject townCenterBuildingMenuPrefab;
	public TownCenterUI abc;

    private static UIController instance;
    public static UIController Instance
	{
		get
		{
            return instance;
		}
	}

	private void Awake()
	{
		/*if(instance == null)
		{
			instance = this;
		}	
		else if(instance != this)
		{
			Destroy(gameObject);
		}*/
		//abc = Instantiate(townCenterBuildingMenuPrefab, transform).GetComponent<TownCenterUI>();
	}


}
