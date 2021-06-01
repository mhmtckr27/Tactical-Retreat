using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class UnitBase : NetworkBehaviour
{
	public bool isPendingDead;
	public int currentHealth;
	public int currentArmor;
	public bool HasAttacked { get; set; }

	[SerializeField] public GameObject canvas;
	[SerializeField] protected Image healthBar;
	[SerializeField] protected Image armorBar;
	[SerializeField] public UnitProperties unitProperties;

	protected AudioSource audioSource;

	public bool IsMoving { get; set; }

	[SyncVar(hook = nameof(OnPlayerColorChange))] public Color playerColor;
	
	public void OnPlayerColorChange(Color oldColor, Color newColor)
	{
		GetComponent<Renderer>().materials[0].color = newColor;
	}

	[SyncVar][HideInInspector] public uint playerID;

	protected List<TerrainHexagon> neighboursWithinRange;
	protected List<TerrainHexagon> occupiedNeighboursWithinRange;
	public SyncList<TerrainHexagon> path = new SyncList<TerrainHexagon>();

	public TerrainHexagon occupiedHex;
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
		currentHealth = unitProperties.health;
	}
	#region Client
	private void Awake()
	{
		neighboursWithinRange = new List<TerrainHexagon>();
		occupiedNeighboursWithinRange = new List<TerrainHexagon>();
		//remainingMovesThisTurn = unitProperties.moveRange;
		transform.eulerAngles = unitProperties.initialRotation;
		HasAttacked = true;
		currentArmor = unitProperties.armor;
		currentHealth = unitProperties.health;
		audioSource = GetComponent<AudioSource>();
	}
	private void Start()
	{
		if (!hasAuthority) { return; }
		InitCmd();
	}

	[Command]
	public void InitCmd()
	{
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, unitProperties.exploreRange);
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


	//TODO: Ienumarator yap ve yield return yap
	[TargetRpc]
	public void RpcMove(Vector3 to)
	{
		TerrainHexagon[] tempPath = new TerrainHexagon[path.Count];
		path.CopyTo(tempPath,0);
		StartCoroutine(MoveRoutine(to + UnitProperties.positionOffsetOnHexagons));
	}


	[Command(requiresAuthority = false)]
	public void SetIsMovingCmd(bool isMoving)
	{
		SetIsMoving(isMoving);
	}

	[Server]
	public void SetIsMoving(bool isMoving)
	{
		this.IsMoving = isMoving;
	}

	[TargetRpc]
	protected void RotateRoutineWrapper(Quaternion lookAt)
	{
		StartCoroutine(RotateRoutine(lookAt));
	}
	protected IEnumerator RotateRoutine(Quaternion lookAt)
	{
		yield return StartCoroutine(RotateRoutine_Inner(lookAt));
	}

	protected IEnumerator RotateRoutine_Inner(Quaternion lookAt)
	{
		while (true)
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, lookAt, Time.smoothDeltaTime * unitProperties.turnSpeed);
			if (Quaternion.Angle(transform.rotation, lookAt) < 5f)
			{
				transform.rotation = lookAt;
				break;
			}
			yield return new WaitForSeconds(unitProperties.waitBetweenMovement);
		}
	}

	protected IEnumerator MoveRoutine(Vector3 moveToPosition)
	{
		SetIsMovingCmd(true);
		Quaternion oldRot = transform.rotation;
		Quaternion lookAt = Quaternion.LookRotation(new Vector3(moveToPosition.x, transform.position.y, moveToPosition.z) - transform.position);
		//yield return StartCoroutine(RotateRoutineWrapper(lookAt));
		RotateRoutineWrapper(lookAt);
		while (true)
		{
			Vector3 oldPos = transform.position;
			transform.position = Vector3.Lerp(oldPos, moveToPosition, unitProperties.lerpSpeed);
			if(Vector3.Distance(transform.position, moveToPosition) < unitProperties.snapToPositionThreshold)
			{
				transform.position = moveToPosition;
				break;
			}	
			yield return new WaitForSeconds(unitProperties.waitBetweenMovement);
		}
		ExploreTerrains();
		//yield return StartCoroutine(RotateRoutine(oldRot));
		RotateRoutineWrapper(oldRot);
		SetIsMovingCmd(false);
	}

	[Command]
	public void ExploreTerrains()
	{
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, unitProperties.exploreRange);
	}

	[TargetRpc]
	public void RpcEnableHexagonOutline(TerrainHexagon hexagon, int outlineIndex, bool enable)
	{
		hexagon.ToggleOutlineVisibility(outlineIndex, enable);
	}

	[TargetRpc]
	private void SetAllCameraPositions(Vector3 position)
	{
		StartCoroutine(SetAllCameraPositions_Inner(position));
	}

	private IEnumerator SetAllCameraPositions_Inner(Vector3 position)
	{
		Camera[] cams = Camera.allCameras;
		while (true)
		{
			foreach (Camera cam in cams)
			{
				cam.transform.position = Vector3.Lerp(cam.transform.position, new Vector3(position.x, cam.transform.position.y, position.z), 0.1f);
			}
			yield return new WaitForSeconds(0.01f);
			if (Vector3.Distance(cams[0].transform.position, new Vector3(position.x, cams[0].transform.position.y, position.z)) < 0.2f)
			{
				break;
			}
		}
		yield return null;
	}

	[Server]
	public IEnumerator ValidateAttack(UnitBase target)
	{
		if (!HasAttacked)
		{
			if (!IsMoving && occupiedNeighboursWithinRange.Contains(target.occupiedHex) && unitProperties.moveCostToAttack <= remainingMovesThisTurn)
			{
				/*if (!OnlineGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex))
				{
					target.SeeAttacker(this, true);
				}*/
				target.SetAllCameraPositions(new Vector3(target.transform.position.x - 5, 0, target.transform.position.z + 0.75f));
				//AttackRpc(target);
				yield return StartCoroutine(AttackRoutines(target));

				if (target != null)
				{
					target.GetReachables(target);
					yield return StartCoroutine(target.ValidateAttack(this, true));
					target.HasAttacked = false;
					target.remainingMovesThisTurn = target.unitProperties.moveRange;
				}
				OnlineGameManager.Instance.GetPlayer(playerID).SelectUnit(this);
			}
		}
	}

	[Server]
	public IEnumerator ValidateAttack(UnitBase target, bool isSelfDefense)
	{
		/*Debug.LogWarning(!IsMoving);
		Debug.LogWarning(occupiedNeighboursWithinRange.Contains(target.occupiedHex));
		Debug.LogWarning(unitProperties.moveCostToAttack <= remainingMovesThisTurn);*/
		if (!IsMoving && occupiedNeighboursWithinRange.Contains(target.occupiedHex) && unitProperties.moveCostToAttack <= remainingMovesThisTurn)
		{
			if (/*true*/!HasAttacked || isSelfDefense/* && targetIsInRange*/)
			{
				yield return StartCoroutine(AttackRoutines(target));
			}
		}
		else
		{
			yield return null;
		}
	}

	[Server]
	IEnumerator AttackRoutines(UnitBase target)
	{
		yield return StartCoroutine(AttackValidated(target));
		/*if(target != null)
		{
			target.GetReachables();
			yield return StartCoroutine(target.ValidateAttack(this, 0));
			target.HasAttacked = false;
			target.remainingMovesThisTurn = target.unitProperties.moveRange;
		}*/
	}

	[Server]
	private IEnumerator AttackValidated(UnitBase target)
	{
		if (!OnlineGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex))
		{
			target.SeeAttacker(this, true);
		}
		float x = (transform.position.x - target.transform.position.x) / 2 + target.transform.position.x - 5;
		float y = 0;
		float z = (transform.position.z - target.transform.position.z) / 2 + target.transform.position.z + 0.75f;
		target.SetAllCameraPositions(new Vector3(x, y, z));
		//AttackRpc(target);
		yield return StartCoroutine(AttackRoutineWrapper(target));
		//yield return null;
	}

	//TODO: Ienumarator yap ve yield return yap
	[TargetRpc]
	public void AttackRpc(UnitBase target)
	{
		StartCoroutine(AttackRoutineWrapper(target));
	}

	private IEnumerator AttackRoutineWrapper(UnitBase target)
	{
		yield return StartCoroutine(AttackRoutine(target));
	}

	private IEnumerator AttackRoutine(UnitBase target)
	{
		SetIsMovingCmd(true);
		yield return new WaitForSeconds(.25f);
		Vector3 oldPos = transform.position;
		if(unitProperties.unitCombatType != UnitCombatType.RangedCombat)
		{
			yield return StartCoroutine(MoveRoutine(target.transform.position - (target.transform.position - transform.position).normalized * .5f));
			AttackCmd(target);
			yield return StartCoroutine(MoveRoutine(oldPos));
		}
		else
		{
			Quaternion oldRot = transform.rotation;
			Quaternion lookAt = Quaternion.LookRotation(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z) - transform.position);
			//yield return StartCoroutine(RotateRoutine(lookAt));
			RotateRoutineWrapper(lookAt);
			AttackCmd(target);
			//yield return StartCoroutine(RotateRoutine(oldRot));
			RotateRoutineWrapper(oldRot);
		}
		SeeAttackerCmd(target);
		SetIsMovingCmd(false);
	}



	[Command(requiresAuthority = false)]
	public void SeeAttackerCmd(UnitBase target)
	{
		if(target == null) { return; }
		if (!OnlineGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex))
		{
			target.SeeAttacker(this, false);
		}
	}


	//TODO: Ienumarator yap ve yield return yap
	[Command(requiresAuthority = false)]
	public void AttackCmd(UnitBase target)
	{
		StartCoroutine(Attack(target));
		HasAttacked = true;
		remainingMovesThisTurn -= unitProperties.moveCostToAttack;
	}

	[Server]
	public virtual IEnumerator Attack(UnitBase target)
	{
		/*HasAttacked = true;
		remainingMovesThisTurn -= unitProperties.moveCostToAttack;*/
		if(remainingMovesThisTurn <= 0)
		{
			SetIsInMoveMode(false);
		}
		else
		{
			GetReachablesVisual(null);
		}

		PlayAttackEffectsRpc();
		bool isTargetDead = target.TakeDamage(this, unitProperties.damage);
		target.isPendingDead = isTargetDead;
		if(isTargetDead)
		{
			target.DisableHexagonOutlines();
			UpdateOutlinesServer();
			/*PlayDeathEffectsRpc(netIdentity.connectionToClient, target.transform.position);
			PlayDeathEffectsRpc(target.netIdentity.connectionToClient, target.transform.position);*/
		}
		yield return null;
	}

	[TargetRpc]
	public void PlayAttackEffectsRpc()
	{
		audioSource.clip = unitProperties.attackSound;
		audioSource.Play();
	}

	[Server]
	public void PlayDeathEffectsWrapperRpc(Vector3 pos)
	{
		PlayDeathEffectsRpc(pos);
	}

	[ClientRpc]
	protected void PlayDeathEffectsRpc(Vector3 pos)
	{
		StartCoroutine(PlayDeathEffects(pos));
	}

	protected IEnumerator PlayDeathEffectsWrapper(Vector3 pos)
	{
		yield return StartCoroutine(PlayDeathEffects(pos));
	}

	protected IEnumerator PlayDeathEffects(Vector3 pos)
	{
		audioSource.clip = unitProperties.deathSound;
		audioSource.Play();
		while (audioSource.isPlaying)
		{
			yield return new WaitForSeconds(0.05f);
		}
		ParticleSystem particle = Instantiate(unitProperties.deathParticle, pos, Quaternion.identity, null).GetComponent<ParticleSystem>();

		NetworkServer.Destroy(gameObject);
		Destroy(gameObject);
	}

	[Server]
	public bool TakeDamage(UnitBase attacker, int damage)
	{
		int damageToHealth = (damage - unitProperties.armor) > 0 ? (damage - unitProperties.armor) : 0;
		if(damage > currentArmor)
		{
			damageToHealth = damage - currentArmor;
			currentArmor = 0;
		}
		else
		{
			damageToHealth = 0;
			currentArmor -= damage;
		}

		if(armorBar != null)
		{
			TakeDamageRpc(attacker, UnitBarType.ArmorBar, (float)currentArmor / unitProperties.armor);
			TakeDamageRpc2(attacker);
		}

		if (damageToHealth > 0)
		{
			currentHealth = (currentHealth - damageToHealth) > 0 ? (currentHealth - damageToHealth) : 0;
			TakeDamageRpc(attacker, UnitBarType.HealthBar, (float)currentHealth / unitProperties.health);
			TakeDamageRpc2(attacker);
			if (currentHealth <= 0)
			{
				currentHealth = 0;
				occupiedHex.OccupierUnit = null;
				OnlineGameManager.Instance.UnregisterUnit(playerID, this);
				PlayDeathEffectsWrapperRpc(transform.position);
				return true;
			}
		}
		return false;
	}

	[ClientRpc]
	private void TakeDamageRpc(UnitBase attacker, UnitBarType barType, float fillAmount)
	{
		Instantiate(unitProperties.hitBloodParticle, healthBar.transform.position, Quaternion.identity);
		StartCoroutine(TakeDamageRoutine(barType == UnitBarType.ArmorBar ? armorBar : healthBar, fillAmount));
	}

	[TargetRpc]
	private void TakeDamageRpc2(UnitBase attacker)
	{
		audioSource.clip = attacker.unitProperties.hitSound;
		if (audioSource.clip != null)
		{
			audioSource.Play();
		}
	}

	protected IEnumerator TakeDamageRoutine(Image bar, float fillAmount)
	{
		while (true)
		{
			bar.fillAmount = Mathf.Lerp(bar.fillAmount, fillAmount, 0.05f);
			if (bar.fillAmount < 0.25f)
			{
				bar.color = Color.red;
			}
			if (Mathf.Abs(bar.fillAmount - fillAmount) < 0.05f)
			{
				bar.fillAmount = fillAmount;
				break;
			}
			yield return new WaitForSeconds(unitProperties.waitBetweenMovement);
		}
		if(bar.transform.parent != null && bar.fillAmount == 0)
		{
			//NetworkServer.Destroy(bar.transform.parent.gameObject);
			Destroy(bar.transform.parent.gameObject);
		}
	}



	[TargetRpc]
	private void SeeAttacker(UnitBase attacker, bool see)
	{
		attacker.gameObject.SetActive(see);
	}


	#endregion

	#region Server
	[Server]
	public void SetIsInMoveMode(bool newIsInMoveMode)
	{
		isInMoveMode = newIsInMoveMode;
	}

	[Server]
	public void GetReachablesVisual(UnitBase targetUnit)
	{
		GetReachables(targetUnit);
		EnableOutlines();
	}

	[Server]
	public void GetReachables(UnitBase targetUnit)
	{
		occupiedNeighboursWithinRange.Clear();
		neighboursWithinRange = Map.Instance.GetReachableHexagons(occupiedHex, remainingMovesThisTurn, unitProperties.attackRange, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, occupiedNeighboursWithinRange);
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
			if (occupied_neighbour.OccupierUnit.playerID != playerID && remainingMovesThisTurn >= unitProperties.moveCostToAttack && !HasAttacked)
			{
				RpcEnableHexagonOutline(occupied_neighbour, 1, true);
			}
		}
	}

	[Server]
	public bool ValidateRequestToMove(TerrainHexagon to)
	{
		if (IsMoving) { return false; }
		if (!neighboursWithinRange.Contains(to)) { return false; }
		else
		{
			GetPath(to);
			if(remainingMovesThisTurn < path.Count - 1) { return false; }
			Move(to, path.Count - 1);
			return true;
		}
	}

	[Server]
	public void GetPath(TerrainHexagon to)
	{
		path.Clear();
		foreach(TerrainHexagon hex in Map.Instance.AStar(occupiedHex, to, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, null))
		{
			path.Add(hex);
		}
	}

	[Server]
	public void Move(TerrainHexagon to, int cost)
	{
		occupiedHex.OccupierUnit = null;
		occupiedHex = to;
		occupiedHex.OccupierUnit = this;
		remainingMovesThisTurn -= cost;
		RpcMove(to.transform.position);
		if (remainingMovesThisTurn == 0)
		{
			//isInMoveMode = false;
			SetIsInMoveMode(false);
		}
		else
		{
			UpdateOutlinesServer();
			//UpdateOutlinesClient();
		}
	}

	[Server]
	protected void UpdateOutlinesServer()
	{
		DisableHexagonOutlines();
		if (isInMoveMode)
		{
			GetReachablesVisual(this);
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
			GetReachablesVisual(targetUnit);
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

public enum UnitCombatType
{
	CloseCombat,
	RangedCombat
}

public enum UnitBarType
{
	ArmorBar,
	HealthBar
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