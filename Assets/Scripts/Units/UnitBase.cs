using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UnitBase : NetworkBehaviour
{
	public static Vector3 positionOffsetOnHexagons = Vector3.left * 0.5f;
	[SerializeField] private UnitType unitType;
	[SerializeField][SyncVar] private int health;
	[SerializeField] private int armor;
	[SerializeField] private int damage;
	[SerializeField] private int maxMovesEachTurn;
	[SerializeField] private int attackRange;
	[SerializeField][SyncVar] private bool hasAttacked = false;
	[SyncVar]public uint playerID;

	[SyncVar] List<TerrainHexagon> neighboursWithinRange;
	[SyncVar] List<TerrainHexagon> occupiedNeighboursWithinRange;
	public List<TerrainType> blockedTerrains;
	[SyncVar] public TerrainHexagon occupiedHexagon;
	[SyncVar] private int remainingMovesThisTurn;
	[Server]
	public bool CanMoveCmd() 
	{
		return remainingMovesThisTurn > 0;
	}

	[SyncVar(hook = nameof(OnIsInMoveModeChange))] public bool isInMoveMode = false;	
	public void OnIsInMoveModeChange(bool oldValue, bool newValue)
	{
		if (!hasAuthority) { return; }

		if (newValue)
		{
			CmdRequestToggleActionMode(this);
		}
		else
		{
			CmdRequestToggleActionMode(null);
		}
		UpdateOutlinesClient();
	}
	
	#region Client
	private void Awake()
	{
		neighboursWithinRange = new List<TerrainHexagon>();
		occupiedNeighboursWithinRange = new List<TerrainHexagon>();
		remainingMovesThisTurn = maxMovesEachTurn;
		transform.eulerAngles = new Vector3(0, -90, 0);
	}

	public bool TryMoveTo(TerrainHexagon to)
	{
		if (!hasAuthority) { return false; }
		CmdRequestToMove(to);
		return true;
	}
	private void UpdateOutlinesClient()
	{
		CmdRequestDisableHexagonOutlines();
		if (isInMoveMode)
		{
			CmdRequestReachableHexagons(this);
		}
	}

	[TargetRpc]
	public void RpcMove(TerrainHexagon to)
	{
		if(to.OccupierBuilding != null)
		{
			transform.position = to.transform.position + positionOffsetOnHexagons;
		}
		else
		{
			transform.position = to.transform.position;
		}
	}	
	
	[TargetRpc]
	public void RpcEnableHexagonOutline(TerrainHexagon hexagon, int outlineIndex, bool enable)
	{
		hexagon.ToggleOutlineVisibility(outlineIndex, enable);
	}

	[Server]
	public void ValidateAttack(UnitBase target)
	{
		bool targetIsInRange = (GetPath(target.occupiedHexagon).Count - 1) <= attackRange;

		if (/*!hasAttacked && targetIsInRange*/ true)
		{
			Attack(target);
		}
	}

	[Server]
	public void Attack(UnitBase target)
	{
		hasAttacked = true;
		bool isTargetDead = target.TakeDamage(damage);
		if(isTargetDead)
		{
			UpdateOutlinesServer();
		}
	}

	[Server]
	public bool TakeDamage(int damage)
	{
		int damageToApply = (damage - armor) > 0 ? (damage - armor) : 0;
		health -= damageToApply;
		if(health <= 0)
		{
			health = 0;
			occupiedHexagon.occupierUnit = null;
			OnlineGameManager.Instance.UnregisterUnit(playerID, this);
			NetworkServer.Destroy(gameObject);
			return true;
		}
		return false;
	}

	[Command]
	public void CmdValidateAttack(UnitBase attacker, UnitBase target)
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

	[TargetRpc]
	public void EndAttackRpc(NetworkConnection targetConn, bool targetUnitIsDead, TerrainHexagon targetHexagon)
	{
		if (targetUnitIsDead)
		{
			//targetHexagon.occupierUnit = null;
			UpdateOutlinesClient();
		}
	}
	#endregion

	#region Server
	[Server]
	public void SetIsInMoveMode(bool newIsInMoveMode)
	{
		isInMoveMode = newIsInMoveMode;
	}

	[Server]
	public void GetReachables(UnitBase targetUnit)
	{
		occupiedNeighboursWithinRange.Clear();
		neighboursWithinRange = Map.Instance.GetReachableHexagons(occupiedHexagon, remainingMovesThisTurn, blockedTerrains, occupiedNeighboursWithinRange);
		EnableOutlines();
	}

	[Server]
	public void DisableHexagonOutlines()
	{
		foreach (TerrainHexagon neighbour in neighboursWithinRange)
		{
			RpcEnableHexagonOutline(neighbour, 0, false);
			RpcEnableHexagonOutline(neighbour, 1, false);
		}
		foreach (TerrainHexagon occupied in occupiedNeighboursWithinRange)
		{
			RpcEnableHexagonOutline(occupied, 0, false);
			RpcEnableHexagonOutline(occupied, 1, false);
		}
	}

	[Server]
	public void EnableOutlines()
	{
		foreach (TerrainHexagon neighbour in neighboursWithinRange)
		{
			if ((neighbour.OccupierBuilding != null) && (neighbour.OccupierBuilding.playerID != playerID))
			{
				RpcEnableHexagonOutline(neighbour, 1, true);
			}
			else
			{
				RpcEnableHexagonOutline(neighbour, 0, true);
			}
		}
		foreach (TerrainHexagon occupied_neighbour in occupiedNeighboursWithinRange)
		{
			if (occupied_neighbour.occupierUnit.playerID != playerID)
			{
				RpcEnableHexagonOutline(occupied_neighbour, 1, true);
			}
		}
	}

	[Server]
	public void CmdRequestToMove(TerrainHexagon to)
	{
		if (!hasAuthority) { return; }
		ValidateRequestToMove(to);
	}

	[Server]
	public void ValidateRequestToMove(TerrainHexagon to)
	{
		if (!neighboursWithinRange.Contains(to))
		{
			return;
		}
		else
		{
			List<TerrainHexagon> path = GetPath(to);
			Move(to, path.Count - 1);
		}
	}

	[Server]
	public List<TerrainHexagon> GetPath(TerrainHexagon to)
	{
		return Map.Instance.AStar(occupiedHexagon, to, blockedTerrains);
	}

	[Server]
	public void Move(TerrainHexagon to, int cost)
	{
		occupiedHexagon.occupierUnit = null;
		occupiedHexagon = to;
		occupiedHexagon.occupierUnit = this;
		remainingMovesThisTurn -= cost;
		RpcMove(to);
		if (remainingMovesThisTurn == 0)
		{
			isInMoveMode = false;
		}
		else
		{
			UpdateOutlinesServer();
		}
	}

	[Server]
	private void UpdateOutlinesServer()
	{
		DisableHexagonOutlines();
		if (isInMoveMode)
		{
			GetReachables(this);
		}
	}
	#endregion

	#region Command (Sending requests to server from client)
	[Command]
	public void CmdRequestReachableHexagons(UnitBase targetUnit/*, TerrainHexagon blockUnder, int remainingMoves*/)
	{
		//in future, implement some logic that will prevent user from cheating. e.g. if it is not this player's turn, ignore this request.
		if ((targetUnit != null) && (isInMoveMode) && (Map.Instance.UnitToMove == targetUnit) && (remainingMovesThisTurn > 0))
		{
			GetReachables(targetUnit);
		}
	}
	[Command]
	public void CmdRequestDisableHexagonOutlines()
	{
		//oyuncunun hile yapmasini engellemek gelecekte buraya kosullar eklenebilir o yuzden Server tarafina istek atiyorum.
		DisableHexagonOutlines();
	}

	[Command]
	public void CmdRequestToggleActionMode(UnitBase unit)
	{
		if (unit == null)
		{
			Map.Instance.currentState = State.None;
			Map.Instance.UnitToMove = unit;
		}
		else if(unit == this)
		{
			Map.Instance.currentState = State.UnitAction;
			Map.Instance.UnitToMove = unit;
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