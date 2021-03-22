using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UnitBase : NetworkBehaviour
{
	[SerializeField] private UnitType unitType;
	[SerializeField][SyncVar] private int health;
	[SerializeField] private int armor;
	[SerializeField] private int damage;
	[SerializeField] private int maxMovesEachTurn;
	[SerializeField] private int attackRange;
	[SerializeField][SyncVar] private bool hasAttacked = false;
	[SyncVar] public TerrainHexagon occupiedHexagon;

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


	private void Awake()
	{
		neighboursWithinRange = new List<TerrainHexagon>();
		occupiedNeighboursWithinRange = new List<TerrainHexagon>();
		remainingMovesThisTurn = maxMovesEachTurn;
		transform.eulerAngles = new Vector3(0, -90, 0);
	}

	public bool TryMoveTo(TerrainHexagon hex)
	{
		Debug.LogWarning("giris");
		if(!hasAuthority) { return false; }
		Debug.LogWarning("otoriter");
		if (!neighboursWithinRange.Contains(hex))
		{
			Debug.LogWarning("yakinda yok");
			return false;
		}
		else
		{
			GetPath(this, occupiedHexagon, hex);
			return true;
		}
	}
	
	[Command]
	public void GetPath(UnitBase unit, TerrainHexagon from, TerrainHexagon to)
	{
		List<TerrainHexagon> tempPath = Map.Instance.AStar(from, to, blockedTerrains);
		NetworkIdentity target = unit.GetComponent<NetworkIdentity>();
		////////////////////////////////////////
		///
		from.occupierUnit = null;
		unit.occupiedHexagon = to;
		to.occupierUnit = unit;
		///
		////////////////////////////////////////
		RpcMove(target.connectionToClient, to, tempPath.Count);
	}

	[TargetRpc]
	public void RpcMove(NetworkConnection target, TerrainHexagon to, int pathLength)
	{
		remainingMovesThisTurn -= pathLength - 1;
		transform.position = to.transform.position;
		////////////////////////////////////////
		occupiedHexagon.occupierUnit = null;
		occupiedHexagon = to;
		occupiedHexagon.occupierUnit = this;
		////////////////////////////////////////
		if (remainingMovesThisTurn == 0)
		{
			IsInMoveMode = false;
		}
		else
		{
			UpdateOutlines();
		}
	}

	private void UpdateOutlines()
	{
		DisableOutlines();
		if (IsInMoveMode)
		{
			occupiedNeighboursWithinRange.Clear();
			GetReachablesCmd(this, occupiedHexagon, remainingMovesThisTurn);
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
		if (IsInMoveMode && (remainingMovesThisTurn > 0))
		{
			foreach (TerrainHexagon neighbour in neighboursWithinRange)
			{
				if((neighbour.OccupierBuilding != null) && (neighbour.OccupierBuilding.playerID != playerID))
				{
					neighbour.ToggleOutlineVisibility(1, true);
				}
				else
				{
					neighbour.ToggleOutlineVisibility(0, true);
				}
			}
			foreach(TerrainHexagon occupied_neighbour in occupiedNeighboursWithinRange)
			{
				if(occupied_neighbour.occupierUnit.playerID != playerID)
				{
					occupied_neighbour.ToggleOutlineVisibility(1, true);
				}
			}
		}
	}

	public void DisableOutlines()
	{
		foreach (TerrainHexagon neighbour in neighboursWithinRange)
		{
			neighbour.ToggleOutlineVisibility(0, false);
			neighbour.ToggleOutlineVisibility(1, false);
		}
		foreach(TerrainHexagon occupied in occupiedNeighboursWithinRange)
		{
			occupied.ToggleOutlineVisibility(0, false);
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
			Die();
		}
	}

	public void Die()
	{
		//GameController.Instance.teams[playerID].UnitDied(this);
		
		Destroy(gameObject);
	}

	[TargetRpc]
	public void TryAttackRpc(NetworkConnection target, UnitBase attacker, UnitBase defender)
	{
		ValidateAttackCmd(this, defender);
	}

	[Command]
	public void ValidateAttackCmd(UnitBase attacker, UnitBase target)
	{
		if (/*!attacker.hasAttacked*/ true)
		{
			NetworkIdentity targetIdentity = attacker.GetComponent<NetworkIdentity>();
			attacker.hasAttacked = true;
			int damage = (attacker.damage - target.armor) > 0 ? (attacker.damage - target.armor) : 0;
			target.health -= damage;
			if(target.health < 0)
			{
				target.health = 0;
			}
			if(target.health == 0)
			{
				TerrainHexagon temp = target.occupiedHexagon;
				///Handle Death
				temp.occupierUnit = null;
				NetworkServer.Destroy(target.gameObject);
				EndAttackRpc(targetIdentity.connectionToClient, true, temp);
				///
			}
		}
	}

	[TargetRpc]
	public void EndAttackRpc(NetworkConnection targetConn, bool targetUnitIsDead, TerrainHexagon targetHexagon)
	{
		if (targetUnitIsDead)
		{
			//targetHexagon.occupierUnit = null;
			UpdateOutlines();
		}
	}
}

public enum UnitType
{
	Peasant,
	Warrior,
	Archer
}
