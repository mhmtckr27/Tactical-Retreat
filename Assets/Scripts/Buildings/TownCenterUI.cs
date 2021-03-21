using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TownCenterUI : MonoBehaviour
{
	public TownCenter townCenter;

	public void CreateUnit(GameObject unitToCreate)
	{
		townCenter.CreateUnit(unitToCreate);
	}
}
