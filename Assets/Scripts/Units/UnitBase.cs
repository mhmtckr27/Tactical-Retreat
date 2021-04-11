using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class UnitBase : NetworkBehaviour
{
	public static Vector3 positionOffsetOnHexagons = new Vector3(-0.365f, 0, 0);
	[SerializeField] private UnitType unitType;
	[SerializeField] private Vector3 initialRotation;
	[SerializeField] private int maxHealth;
	private int currentHealth;
	[Header("Combat")]
	[SerializeField] private int armor;
	[SerializeField] private int damage;
	[SerializeField] private int moveRange;
	[SerializeField] private int attackRange;
	[SerializeField] private bool hasAttacked = false;
	[SerializeField] private Image healthBar;
	[SerializeField] private GameObject hitBloodParticle;	
	[SerializeField] private GameObject deathParticle;
	[Header("Movement")]
	[SerializeField] private int explorationDistance = 2;
	[SerializeField] private float moveSpeed = 0.2f;
	[SerializeField] private float turnSpeed = 100f;
	[SerializeField] private float snapToPositionThreshold = 0.1f;
	[SerializeField] private float waitBetweenMovement = 0.02f;
	
	private bool isMoving;

	[SyncVar(hook = nameof(OnPlayerColorChange))] public Color playerColor;
	
	public void OnPlayerColorChange(Color oldColor, Color newColor)
	{
		GetComponent<Renderer>().materials[0].color = newColor;
	}

	[SyncVar][HideInInspector] public uint playerID;

	/*[SyncVar]*/ List<TerrainHexagon> neighboursWithinRange;
	/*[SyncVar]*/ List<TerrainHexagon> occupiedNeighboursWithinRange;
	public SyncList<TerrainHexagon> path = new SyncList<TerrainHexagon>();

	public List<TerrainType> blockedTerrains;
	public TerrainHexagon occupiedHexagon;
	[SyncVar] public int remainingMovesThisTurn;
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
	public override void OnStartServer()
	{
		base.OnStartServer();
		currentHealth = maxHealth;
	}
	#region Client
	private void Awake()
	{
		neighboursWithinRange = new List<TerrainHexagon>();
		occupiedNeighboursWithinRange = new List<TerrainHexagon>();
		remainingMovesThisTurn = moveRange;
		transform.eulerAngles = initialRotation;
	}

	/*public bool TryMoveTo(TerrainHexagon to)
	{
		if (!hasAuthority) { return false; }
		CmdRequestToMove(to);
		return true;
	}*/
	private void UpdateOutlinesClient()
	{
		CmdRequestDisableHexagonOutlines();
		if (isInMoveMode)
		{
			CmdRequestReachableHexagons(this);
		}
	}

	[TargetRpc]
	public void RpcMove(Vector3 to)
	{
		TerrainHexagon[] tempPath = new TerrainHexagon[path.Count];
		path.CopyTo(tempPath,0);
		StartCoroutine(MoveRoutine(to + positionOffsetOnHexagons));
	}	
	
	[Command]
	public void SetIsMovingCmd(bool isMoving)
	{
		this.isMoving = isMoving;
	}

	IEnumerator RotateRoutine(Quaternion lookAt)
	{
		while (true)
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, lookAt, Time.smoothDeltaTime * turnSpeed);
			if(Quaternion.Angle(transform.rotation, lookAt) < 5f)
			{
				transform.rotation = lookAt;
				break;
			}
			yield return new WaitForSeconds(waitBetweenMovement);
		}
	}

	IEnumerator MoveRoutine(Vector3 moveToPosition)
	{
		SetIsMovingCmd(true);
		Quaternion oldRot = transform.rotation;
		Quaternion lookAt = Quaternion.LookRotation(new Vector3(moveToPosition.x, transform.position.y, moveToPosition.z) - transform.position);
		yield return StartCoroutine(RotateRoutine(lookAt));
		while (true)
		{
			Vector3 oldPos = transform.position;
			transform.position = Vector3.Lerp(oldPos, moveToPosition, moveSpeed);
			if(Vector3.Distance(transform.position, moveToPosition) < snapToPositionThreshold)
			{
				transform.position = moveToPosition;
				break;
			}	
			yield return new WaitForSeconds(waitBetweenMovement);
		}
		DiscoverTerrains();
		yield return StartCoroutine(RotateRoutine(oldRot));
		SetIsMovingCmd(false);
	}

	[Command]
	public void DiscoverTerrains()
	{
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHexagon.Key, explorationDistance);
	}

	[TargetRpc]
	public void RpcEnableHexagonOutline(TerrainHexagon hexagon, int outlineIndex, bool enable)
	{
		hexagon.ToggleOutlineVisibility(outlineIndex, enable);
	}

	[Server]
	public void ValidateAttack(UnitBase target)
	{
		if(isMoving) { return; }
		if(!occupiedNeighboursWithinRange.Contains(target.occupiedHexagon)) { return; }
		
		if (/*!hasAttacked && targetIsInRange*/ true)
		{
			AttackRpc(target);
		}
	}

	[TargetRpc]
	public void AttackRpc(UnitBase target)
	{
		StartCoroutine(AttackRoutine(target));
	}

	IEnumerator AttackRoutine(UnitBase target)
	{
		Vector3 oldPos = transform.position;
		SetIsMovingCmd(true);
		yield return StartCoroutine(MoveRoutine(target.transform.position - (target.transform.position - transform.position).normalized * .5f));
		AttackCmd(target);
		yield return StartCoroutine(MoveRoutine(oldPos));
	}

	[Command]
	public void AttackCmd(UnitBase target)
	{
		Attack(target);
	}

	[Server]
	public void Attack(UnitBase target)
	{
		hasAttacked = true;
		bool isTargetDead = target.TakeDamage(damage);
		if(isTargetDead)
		{
			DieRpc();
			UpdateOutlinesServer();
		}
	}

	[ClientRpc]
	private void DieRpc()
	{
		Instantiate(deathParticle, transform.position, Quaternion.identity);
	}

	[ClientRpc]
	private void TakeDamageRpc(float fillAmount)
	{
		//healthBar.fillAmount = fillAmount;
		Instantiate(hitBloodParticle, healthBar.transform.position, Quaternion.identity);
		StartCoroutine(TakeDamageRoutine(fillAmount));
	}

	IEnumerator TakeDamageRoutine(float fillAmount)
	{
		while (true)
		{
			healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, fillAmount, moveSpeed);
			if(healthBar.fillAmount < 0.25f)
			{
				healthBar.color = Color.red;
			}
			if(Mathf.Abs(healthBar.fillAmount - fillAmount) < 0.05f)
			{
				healthBar.fillAmount = fillAmount;
				break;
			}
			yield return new WaitForSeconds(waitBetweenMovement);
		}
	}

	[Server]
	public bool TakeDamage(int damage)
	{
		int damageToApply = (damage - armor) > 0 ? (damage - armor) : 0;
		currentHealth = (currentHealth - damageToApply) > 0 ? currentHealth - damageToApply : 0;
		TakeDamageRpc((float)currentHealth / maxHealth);
		if (currentHealth <= 0)
		{
			currentHealth = 0;
		//	TakeDamageRpc(0);
			occupiedHexagon.OccupierUnit = null;
			OnlineGameManager.Instance.UnregisterUnit(playerID, this);
			NetworkServer.Destroy(gameObject);
			return true;
		}
		return false;
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
		neighboursWithinRange = Map.Instance.GetReachableHexagons(occupiedHexagon, remainingMovesThisTurn, attackRange, blockedTerrains, occupiedNeighboursWithinRange);
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
			if (occupied_neighbour.OccupierUnit.playerID != playerID)
			{
				RpcEnableHexagonOutline(occupied_neighbour, 1, true);
			}
		}
	}

	[Server]
	public bool ValidateRequestToMove(TerrainHexagon to)
	{
		if (isMoving) { return false; }
		if (!neighboursWithinRange.Contains(to)) { return false; }
		else
		{
			GetPath(to);
			Move(to, path.Count - 1);
			return true;
		}
	}

	[Server]
	public void GetPath(TerrainHexagon to)
	{
		path.Clear();
		foreach(TerrainHexagon hex in Map.Instance.AStar(occupiedHexagon, to, blockedTerrains))
		{
			path.Add(hex);
		}
	}

	[Server]
	public void Move(TerrainHexagon to, int cost)
	{
		occupiedHexagon.OccupierUnit = null;
		occupiedHexagon = to;
		occupiedHexagon.OccupierUnit = this;
		remainingMovesThisTurn -= cost;
		RpcMove(to.transform.position);
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

public enum UnitActionType
{
	Move,
	Attack
}
/*

public static class CustomReadWriteFunctions3
{
	public static void WriteUnitBase(this NetworkWriter writer, UnitBase value)
	{
		if (value == null) { return; }

		NetworkIdentity networkIdentity = value.GetComponent<NetworkIdentity>();
		writer.WriteNetworkIdentity(networkIdentity);

		writer.WriteColor(value.playerColor);
		writer.WriteUInt32(value.playerID);
		writer.WriteTerrainHexagon(value.occupiedHexagon);
		writer.WriteInt32(value.remainingMovesThisTurn);
	}

	public static UnitBase ReadUnitBase(this NetworkReader reader)
	{
		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		UnitBase unitBase = networkIdentity != null
			? networkIdentity.GetComponent<UnitBase>()
			: null;

		if(unitBase == null) { return null; }

		unitBase.playerColor = reader.ReadColor();
		unitBase.playerID = reader.ReadUInt32();
		unitBase.occupiedHexagon = reader.ReadTerrainHexagon();
		unitBase.remainingMovesThisTurn = reader.ReadInt32();
		
		return unitBase;
	}
}
*/