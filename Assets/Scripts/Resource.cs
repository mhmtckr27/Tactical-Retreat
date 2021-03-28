using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "ScriptableObjects/Resource", order = 1)]
public class Resource : ScriptableObject
{
	public ResourceType resourceName;
	public TerrainType obtainedFromTerrainType;
	public string description;
	public string collectText;
	public Sprite resourceIcon;
	public Sprite obtainedFromTerrainIcon;
	public Sprite costIcon;
	public int resourceCount;
	public int costToCollect;
}

public enum ResourceType
{
	None,
	Wood,
	Meat
}