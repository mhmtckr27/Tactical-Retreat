using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenterUI : MonoBehaviour
{
	public TownCenter townCenter;

	public void CreateUnit(GameObject unitToCreate)
	{
		townCenter.CreateUnit(unitToCreate);
	}
}
