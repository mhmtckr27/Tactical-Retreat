using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WoodcutterCottage : BuildingBase
{
	

	[Server]
	public int CollectResource()
	{
		return 1;
	}
}
