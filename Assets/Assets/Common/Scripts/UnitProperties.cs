using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "ScriptableObjects/Unit", order = 2)]
public class UnitProperties : ScriptableObject
{
	[SerializeField] public static Vector3 positionOffsetOnHexagons = new Vector3(-0.365f, 0, 0);
	[SerializeField] public Vector3 initialRotation;
	[SerializeField] public UnitType unitType;
	[SerializeField] public UnitCombatType unitCombatType;
	[SerializeField] public int actionPointCostToCreate;
	[SerializeField] public int meatCostToCreate;
	[Header("Combat")]
	[SerializeField] public int moveCostToAttack;
	[SerializeField] public int health;
	[SerializeField] public int armor;
	[SerializeField] public int damage;
	[SerializeField] public int attackRange;
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
}
