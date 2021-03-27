using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenterUI : MonoBehaviour
{
	public TownCenter townCenter;

	public void CreateUnitRequest(GameObject unitToCreate)
	{
		townCenter.CreateUnitCmd(unitToCreate.name);
	}
}
