using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
	[SerializeField] public GameObject townCenterBuildingMenu;

    private static UIController instance;
    public static UIController Instance
	{
		get
		{
            return instance;
		}
		set
		{
            instance = value;
		}
	}

	private void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}	
		else if(instance != this)
		{
			Destroy(gameObject);
		}
	}
}
