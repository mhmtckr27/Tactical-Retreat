using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBase : MonoBehaviour
{
	[SerializeField] private UnitType unitType;
	[SerializeField] private int health;
	[SerializeField] private int armor;
	[SerializeField] private int damage;
	[SerializeField] private int maxMovesEachTurn;
	[SerializeField] private int attackRange;
	private bool hasAttacked = false;
	private TerrainHexagon blockUnder;
	public int playerID;
	public List<TerrainType> blockedTerrains;

	private bool isInMoveMode = false;
	public bool IsInMoveMode
	{
		get => isInMoveMode;
		set
		{
			isInMoveMode = value;
			if (isInMoveMode)
			{
				EnterActionMode();
			}
			else
			{
				ExitActionMode();
			}
			UpdateOutlines();
		}
	}

	private int remainingMovesThisTurn;
	List<TerrainHexagon> neighboursWithinRange;
	List<TerrainHexagon> occupiedNeighboursWithinRange;
	public TerrainHexagon BlockUnder 
	{ 
		get => blockUnder; 
		set 
		{
			if(blockUnder != null)
			{
				blockUnder.OccupierUnit = null;
			}
			blockUnder = value;
			blockUnder.OccupierUnit = this;
		} 
	}


	private void Awake()
	{
		neighboursWithinRange = new List<TerrainHexagon>();
		occupiedNeighboursWithinRange = new List<TerrainHexagon>();
		remainingMovesThisTurn = maxMovesEachTurn;
		transform.eulerAngles = new Vector3(0, -90, 0);

		RaycastHit hit;
		if(Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out hit, .2f))
		{
			BlockUnder = hit.collider.GetComponent<TerrainHexagon>();
		}
	}

/*	private void OnMouseUpAsButton()
	{
		isInMoveMode = !isInMoveMode;
		if (IsInMoveMode)
		{
			EnterActionMode();
		}
		else
		{
			ExitActionMode();
		}
		UpdateOutlines();
	}*/
	public bool TryMoveTo(TerrainHexagon hex)
	{
		if (!neighboursWithinRange.Contains(hex))
		{
			return false;
		}
		else
		{
			List<TerrainHexagon> path = Map.Instance.AStar(BlockUnder, hex, blockedTerrains);
			remainingMovesThisTurn -= path.Count - 1;
			transform.position = hex.transform.position;
			BlockUnder = hex;
			if(remainingMovesThisTurn == 0)
			{
				IsInMoveMode = false;
			}
			else
			{
				UpdateOutlines();
			}
			return true;
		}
	}

	private void UpdateOutlines()
	{
		DisableOutlines();
		if (IsInMoveMode)
		{
			occupiedNeighboursWithinRange.Clear();
			neighboursWithinRange = Map.Instance.GetReachableHexagons(BlockUnder, remainingMovesThisTurn, blockedTerrains, occupiedNeighboursWithinRange);
			EnableOutlines();
		}
	}

	public void EnableOutlines()
	{
		if (IsInMoveMode && (remainingMovesThisTurn > 0))
		{
			foreach (TerrainHexagon neighbour in neighboursWithinRange)
			{
				neighbour.ToggleOutlineVisibility(0, true);
			}
			foreach(TerrainHexagon occupied_neighbour in occupiedNeighboursWithinRange)
			{
				Debug.Log(occupiedNeighboursWithinRange.IndexOf(occupied_neighbour));
				occupied_neighbour.ToggleOutlineVisibility(1, true);
			}
		}
	}

	public void DisableOutlines()
	{
		foreach (TerrainHexagon neighbour in neighboursWithinRange)
		{
			neighbour.ToggleOutlineVisibility(0, false);
		}
		foreach(TerrainHexagon occupied in occupiedNeighboursWithinRange)
		{
			occupied.ToggleOutlineVisibility(1, false);
		}
	}

	public void EnterActionMode()
	{
		Map.Instance.currentState = State.UnitAction;
		Map.Instance.UnitToMove = this;
	}

	public void ExitActionMode()
	{
		Map.Instance.currentState = State.None;
		Map.Instance.UnitToMove = null;
	}

	public void TakeDamage(int damage)
	{
		damage = (damage - armor) > 0 ? (damage - armor) : 0;
		health -= damage;
		if(health <= 0)
		{
			//GameController.Instance.teams[playerID].UnitDied(this);
			Destroy(gameObject);
		}
	}

	public void Attack(UnitBase unitToAttack)
	{
		if (!hasAttacked)
		{
			unitToAttack.TakeDamage(damage);
			hasAttacked = true;
		}
	}
}

public enum UnitType
{
	Peasant,
	Warrior,
	Archer
}
