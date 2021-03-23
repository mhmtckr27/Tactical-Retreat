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
	/*[SyncVar(hook = nameof(dene))] public TerrainHexagon occupiedHexagon;
	public void dene(TerrainHexagon old, TerrainHexagon yeni)
	{
		CmdTest(":reflected to client");
	}*/

	[SyncVar]public uint playerID;
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

	[SyncVar] private int remainingMovesThisTurn;
	List<TerrainHexagon> neighboursWithinRange;
	List<TerrainHexagon> occupiedNeighboursWithinRange;

	#region Client
	private void Awake()
	{
		neighboursWithinRange = new List<TerrainHexagon>();
		occupiedNeighboursWithinRange = new List<TerrainHexagon>();
		remainingMovesThisTurn = maxMovesEachTurn;
		transform.eulerAngles = new Vector3(0, -90, 0);
	}

	public bool TryMoveTo(TerrainHexagon hex)
	{
		if(!hasAuthority) { return false; }
		if (!neighboursWithinRange.Contains(hex))
		{
			return false;
		}
		else
		{
			CmdGetPath(this, occupiedHexagon, hex);
			return true;
		}
	}

	[TargetRpc]
	public void RpcMove(NetworkConnection target, TerrainHexagon to, int pathLength)
	{
		remainingMovesThisTurn -= pathLength - 1;
		transform.position = to.transform.position;
		if (remainingMovesThisTurn == 0)
		{
			IsInMoveMode = false;
		}
		else
		{
			Invoke("UpdateOutlines", Time.fixedDeltaTime);
			//UpdateOutlines();
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

	public void DisableOutlines()
	{
		foreach (TerrainHexagon neighbour in neighboursWithinRange)
		{
			neighbour.ToggleOutlineVisibility(0, false);
			neighbour.ToggleOutlineVisibility(1, false);
		}
		foreach (TerrainHexagon occupied in occupiedNeighboursWithinRange)
		{
			occupied.ToggleOutlineVisibility(0, false);
			occupied.ToggleOutlineVisibility(1, false);
		}
	}

	public void EnableOutlines()
	{
		if (IsInMoveMode && (remainingMovesThisTurn > 0))
		{
			foreach (TerrainHexagon neighbour in neighboursWithinRange)
			{
				if ((neighbour.OccupierBuilding != null) && (neighbour.OccupierBuilding.playerID != playerID))
				{
					neighbour.ToggleOutlineVisibility(1, true);
				}
				else
				{
					neighbour.ToggleOutlineVisibility(0, true);
				}
			}
			foreach (TerrainHexagon occupied_neighbour in occupiedNeighboursWithinRange)
			{
				if (occupied_neighbour.occupierUnit.playerID != playerID)
				{
					occupied_neighbour.ToggleOutlineVisibility(1, true);
				}
			}

		}
	}

	[TargetRpc]
	public void RpcGetReachables(NetworkConnection target, List<TerrainHexagon> reachables, List<TerrainHexagon> occupieds)
	{
		neighboursWithinRange = reachables;
		occupiedNeighboursWithinRange = occupieds;
		EnableOutlines();
	}

	[TargetRpc]
	public void TryAttackRpc(NetworkConnection target, UnitBase attacker, UnitBase defender)
	{
		ValidateAttackCmd(this, defender);
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
	#endregion

	#region Server
	[Command]
	public void CmdGetPath(UnitBase unit, TerrainHexagon from, TerrainHexagon to)
	{
		List<TerrainHexagon> tempPath = Map.Instance.AStar(from, to, blockedTerrains);
		NetworkIdentity target = unit.GetComponent<NetworkIdentity>();
		////////////////////////////////////////
		///
		to.occupierUnit = unit;
		from.occupierUnit = null;
		occupiedHexagon = to;
		//transform.position = to.transform.position;

		//Debug.LogWarning(Time.timeSinceLevelLoad + ":setted in server");
		RpcMove(target.connectionToClient, to, tempPath.Count);
		//from.occupierUnit = null;
		//to.occupierUnit = unit;
		//remainingMovesThisTurn -= tempPath.Count - 1;
		///
		////////////////////////////////////////
	}

	[Command]
	public void GetReachablesCmd(UnitBase targetUnit, TerrainHexagon blockUnder, int remainingMoves)
	{
		List<TerrainHexagon> occupieds = new List<TerrainHexagon>();
		List<TerrainHexagon> tempReachables = Map.Instance.GetReachableHexagons(blockUnder, remainingMoves, blockedTerrains, occupieds);
		NetworkIdentity target = targetUnit.GetComponent<NetworkIdentity>();
		RpcGetReachables(target.connectionToClient, tempReachables, occupieds);
	}

	[Command]
	public void ToggleActionModeCmd(UnitBase unit)
	{
		if (unit == null)
		{
			Map.Instance.currentState = State.None;
		}
		else
		{
			Map.Instance.currentState = State.UnitAction;
		}
		Map.Instance.UnitToMove = unit;
	}

	[Command]
	public void ValidateAttackCmd(UnitBase attacker, UnitBase target)
	{
		if (/*!attacker.hasAttacked*/ true)
		{
			NetworkIdentity attackerIdentity = attacker.GetComponent<NetworkIdentity>();
			attacker.hasAttacked = true;
			int damage = (attacker.damage - target.armor) > 0 ? (attacker.damage - target.armor) : 0;
			target.health -= damage;
			if (target.health < 0)
			{
				target.health = 0;
			}
			if (target.health == 0)
			{
				TerrainHexagon tempHexagon = target.occupiedHexagon;
				///Handle Death
				OnlineGameManager.Instance.UnregisterUnit(target.playerID, target);
				tempHexagon.occupierUnit = null;
				NetworkServer.Destroy(target.gameObject);
				EndAttackRpc(attackerIdentity.connectionToClient, true, tempHexagon);
				///
			}
		}
	}
	#endregion
}

public enum UnitType
{
	Peasant,
	Warrior,
	Archer
}