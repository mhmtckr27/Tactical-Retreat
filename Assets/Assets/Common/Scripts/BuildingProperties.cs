using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "ScriptableObjects/Building", order = 3)]
public class BuildingProperties : ScriptableObject
{
	[SerializeField] public GameObject buildingPrefab;
	[SerializeField] public string buildingName;
	[SerializeField] public string buildingDescription;
	[SerializeField] public Sprite buildingIcon;
	[SerializeField] public static Vector3 positionOffsetOnHexagons = new Vector3(-0.365f, 0, 0);
	[SerializeField] public Vector3 initialRotation;
	[SerializeField] public BuildingType buildingType;
	[SerializeField] public int woodCostToCreate;
	[SerializeField] public int meatCostToCreate;
	[SerializeField] public int populationCostToCreate;
	[SerializeField] public int actionPointCostToCreate;
	[SerializeField] public List<TerrainType> blockedTerrainsToBuild;

	/*[Header("Combat")]
	[SerializeField] public int moveCostToAttack;
	[SerializeField] public int health;
	[SerializeField] public int armor;
	[SerializeField] public int damage;
	[SerializeField] public int attackRange;
	[SerializeField] public List<TerrainType> blockedToAttackTerrains;
	[SerializeField] public GameObject hitBloodParticle;
	[SerializeField] public GameObject deathParticle;
	[SerializeField] public AudioClip attackSound;
	[SerializeField] public AudioClip hitSound;
	[SerializeField] public AudioClip deathSound;
	[Header("Movement")]
	[SerializeField] public int moveRange;
	[SerializeField] public int exploreRange = 2;
	[SerializeField] public float lerpSpeed = 0.2f;
	[SerializeField] public float turnSpeed = 100f;
	[SerializeField] public float snapToPositionThreshold = 0.1f;
	[SerializeField] public float waitBetweenMovement = 0.02f;
	[SerializeField] public List<TerrainType> blockedToMoveTerrains;
	*/
}
