using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UnitBase : NetworkBehaviour
{
	[SerializeField] private UnitType unitType;
	[SerializeField] private int health;
	[SerializeField] private int armor;
	[SerializeField] private int damage;
	[SerializeField] private int maxMovesEachTurn;
	[SerializeField] private int attackRange;
	private bool hasAttacked = false;
	private TerrainHexagon blockUnder;
	public uint playerID;
	public List<TerrainType> blockedTerrains;

	public bool isInMoveMode = false;
	public bool IsInMoveMode
	{
		get => isInMoveMode;
		set
		{
			isInMoveMode = value;
			if (isInMoveMode)
			{
				ToggleActionModeCmd(this);
			}
			else
			{
				ToggleActionModeCmd(null);
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
	private void Start()
	{
		Debug.LogError(playerID);
		
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
		if(!hasAuthority) { return false; }
		if (!neighboursWithinRange.Contains(hex))
		{
			return false;
		}
		else
		{
			Debug.LogWarning("varki");
			GetPath(this, BlockUnder, hex);
			return true;
		}
	}

	[Command]
	public void GetPath(UnitBase unit, TerrainHexagon from, TerrainHexagon to)
	{
		List<TerrainHexagon> tempPath = Map.Instance.AStar(from, to, blockedTerrains);
		Debug.LogWarning(tempPath.Count);
		NetworkIdentity target = unit.GetComponent<NetworkIdentity>();
		RpcMove(target.connectionToClient, to, tempPath);
	}

	[TargetRpc]
	public void RpcMove(NetworkConnection target, TerrainHexagon to, List<TerrainHexagon> path)
	{
		remainingMovesThisTurn -= path.Count - 1;
		transform.position = to.transform.position;
		BlockUnder = to;
		if (remainingMovesThisTurn == 0)
		{
			IsInMoveMode = false;
		}
		else
		{
			UpdateOutlines();
		}
	}

	[Client]
	private void UpdateOutlines()
	{
		DisableOutlines();
		if (IsInMoveMode)
		{
			occupiedNeighboursWithinRange.Clear();
			GetReachablesCmd(this, BlockUnder, remainingMovesThisTurn);
		}
	}

	[Command]
	public void GetReachablesCmd(UnitBase targetUnit, TerrainHexagon blockUnder, int remainingMoves)
	{
		List<TerrainHexagon> occupieds = new List<TerrainHexagon>();
		List<TerrainHexagon> tempReachables = Map.Instance.GetReachableHexagons(blockUnder, remainingMoves, blockedTerrains, occupieds);
		NetworkIdentity target = targetUnit.GetComponent<NetworkIdentity>();
		RpcGetReachables(target.connectionToClient, tempReachables, occupieds);
	}

	[TargetRpc]
	public void RpcGetReachables(NetworkConnection target, List<TerrainHexagon> reachables, List<TerrainHexagon> occupieds)
	{
		neighboursWithinRange = reachables;
		occupiedNeighboursWithinRange = occupieds;
		EnableOutlines();
	}

	public void EnableOutlines()
	{
		//Debug.LogWarning(neighboursWithinRange.Count);

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
	
	[Command]
	public void ToggleActionModeCmd(UnitBase unit)
	{
		if(unit == null)
		{
			Map.Instance.currentState = State.None;
		}
		else
		{
			Map.Instance.currentState = State.UnitAction;
		}
		Map.Instance.UnitToMove = unit;
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
