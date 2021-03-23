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

	[SyncVar]public uint playerID;
	public List<TerrainType> blockedTerrains;

	
	[SyncVar(hook = nameof(OnIsInMoveModeChange))] public bool isInMoveMode = false;
	public void SetIsInMoveMode(bool newIsInMoveMode)
	{
		CmdSetIsInMoveMode(newIsInMoveMode);
	}
	public void CmdSetIsInMoveMode(bool newIsInMoveMode)
	{
		isInMoveMode = newIsInMoveMode;
	}	
	
	public void OnIsInMoveModeChange(bool oldValue, bool newValue)
	{
		if (!hasAuthority) { return; }

		if (newValue)
		{
			CmdToggleActionMode(this);
		}
		else
		{
			CmdToggleActionMode(null);
		}
		UpdateOutlinesClient();
	}

	[SyncVar] private int remainingMovesThisTurn;
	[SyncVar] List<TerrainHexagon> neighboursWithinRange;
	[SyncVar] List<TerrainHexagon> occupiedNeighboursWithinRange;

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
		if(!hasAuthority) { return false; }
		CmdRequestToMove(to);
		return true;
	}

	[Server]
	public void CmdRequestToMove(TerrainHexagon to)
	{
		if(!hasAuthority) { return; }
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
			GetPath(to);
		}
	}

	[Server]
	public void GetPath(TerrainHexagon to)
	{
		List<TerrainHexagon> tempPath = Map.Instance.AStar(occupiedHexagon, to, blockedTerrains);

		occupiedHexagon.occupierUnit = null;
		occupiedHexagon = to;
		occupiedHexagon.occupierUnit = this;
		remainingMovesThisTurn -= tempPath.Count - 1;
		RpcMove(to);
		if(remainingMovesThisTurn == 0)
		{
			isInMoveMode = false;
		}
		else
		{
			UpdateOutlinesServer();
		}
	}

	[TargetRpc]
	public void RpcSetIsInMoveMode(bool newIsInMoveMode)
	{
		SetIsInMoveMode(newIsInMoveMode);
	}

	[TargetRpc]
	public void RpcMove(TerrainHexagon to)
	{
		transform.position = to.transform.position;

		/*{
			Invoke("UpdateOutlines", Time.fixedDeltaTime);
			//UpdateOutlines();
		}*/
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

	
	private void UpdateOutlinesClient()
	{
		CmdRequestDisableHexagonOutlines();
		if (isInMoveMode)
		{
			CmdRequestReachableHexagons(this);
		}
	}

	[Command]
	public void CmdRequestDisableHexagonOutlines()
	{
		//oyuncunun hile yapmasini engellemek gelecekte buraya kosullar eklenebilir o yuzden Server tarafina istek atiyorum.
		DisableHexagonOutlines();
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

	[TargetRpc]
	public void RpcEnableHexagonOutline(TerrainHexagon hexagon, int outlineIndex, bool enable)
	{
		hexagon.ToggleOutlineVisibility(outlineIndex, enable);
	}

	[TargetRpc]
	public void TryAttackRpc(NetworkConnection target, UnitBase attacker, UnitBase defender)
	{
		CmdValidateAttack(this, defender);
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

	[Command]
	public void CmdRequestReachableHexagons(UnitBase targetUnit/*, TerrainHexagon blockUnder, int remainingMoves*/)
	{
		//in future, implement some logic that will prevent user from cheating. e.g. if it is not this player's turn, ignore this request.
		if ((targetUnit != null) && (isInMoveMode) && (Map.Instance.UnitToMove == targetUnit) && (remainingMovesThisTurn > 0))
		{
			GetReachables(targetUnit);
		}
	}

	[Server]
	public void GetReachables(UnitBase targetUnit)
	{
		occupiedNeighboursWithinRange.Clear();
		neighboursWithinRange = Map.Instance.GetReachableHexagons(occupiedHexagon, remainingMovesThisTurn, blockedTerrains, occupiedNeighboursWithinRange);
		EnableOutlines();
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

	[Command]
	public void CmdToggleActionMode(UnitBase unit)
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
	#endregion
}

public enum UnitType
{
	Peasant,
	Warrior,
	Archer
}