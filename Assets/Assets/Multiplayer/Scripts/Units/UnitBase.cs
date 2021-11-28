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

	[SyncVar] [HideInInspector] public uint playerID;

	protected List<TerrainHexagon> neighboursWithinRange;
	protected List<TerrainHexagon> occupiedNeighboursWithinRange;
	public SyncList<string> path = new SyncList<string>();

	//public TerrainHexagon occupiedHex;
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
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, OnlineGameManager.Instance.unitsToOccupiedTerrains[this].Key, unitProperties.exploreRange);
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
		//TerrainHexagon[] tempPath = new TerrainHexagon[path.Count];
		//path.CopyTo(tempPath, 0);
		StartCoroutine(MoveRoutine(to + UnitProperties.positionOffsetOnHexagons, false));
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

	//[TargetRpc]
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
			if (Quaternion.Angle(transform.rotation, lookAt) < 20)
			{
				transform.rotation = lookAt;
				break;
			}
			yield return new WaitForSeconds(unitProperties.waitBetweenMovement);
		}
	}

	protected IEnumerator MoveRoutine(Vector3 moveToPosition, bool isAttacking)
	{
		SetIsMovingCmd(true);
		Quaternion oldRot = transform.rotation;
		Quaternion lookAt = Quaternion.LookRotation(new Vector3(moveToPosition.x, transform.position.y, moveToPosition.z) - transform.position);
		//yield return StartCoroutine(RotateRoutineWrapper(lookAt));
		yield return StartCoroutine(RotateRoutine(lookAt));
		while (true)
		{
			Vector3 oldPos = transform.position;
			transform.position = Vector3.Lerp(oldPos, moveToPosition, unitProperties.lerpSpeed);
			if (Vector3.Distance(transform.position, moveToPosition) < unitProperties.snapToPositionThreshold)
			{
				transform.position = moveToPosition;
				break;
			}
			yield return new WaitForSeconds(unitProperties.waitBetweenMovement);
		}
		if (!isAttacking)
		{
			ExploreTerrains();
		}
		//yield return StartCoroutine(RotateRoutine(oldRot));
		yield return StartCoroutine(RotateRoutine(oldRot));
		SetIsMovingCmd(false);
	}

	[Command]
	public void ExploreTerrains()
	{
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, OnlineGameManager.Instance.unitsToOccupiedTerrains[this].Key, unitProperties.exploreRange);
	}

	[TargetRpc]
	public void RpcEnableHexagonOutline(TerrainHexagon hexagon, int outlineIndex, bool enable)
	{
		hexagon.ToggleOutlineVisibility(outlineIndex, enable);
	}

	[Server]
	private IEnumerator SetAllCameraPositions(Vector3 position)
	{
		SetAllCameraPositions_Inner(position);
		yield return null;
	}

	[TargetRpc]
	private void SetAllCameraPositions_Inner(Vector3 position)
	{
		StartCoroutine(SetAllCameraPositions_Inner_Inner(position));
	}

	private IEnumerator SetAllCameraPositions_Inner_Inner(Vector3 position)
	{
		yield return StartCoroutine(SetAllCameraPositions_Inner_Inner_Inner(position));
	}

	private IEnumerator SetAllCameraPositions_Inner_Inner_Inner(Vector3 position)
	{
		Camera[] cams = Camera.allCameras;
		while (true)
		{
			foreach (Camera cam in cams)
			{
				cam.transform.position = Vector3.Lerp(cam.transform.position, new Vector3(position.x, cam.transform.position.y, position.z), 0.2f);
			}
			yield return new WaitForSeconds(0.01f);
			if (Vector3.Distance(cams[0].transform.position, new Vector3(position.x, cams[0].transform.position.y, position.z)) < 0.3f)
			{
				break;
			}
		}
		yield return null;
	}

	[Command(requiresAuthority = false)]
	public void ValidateAttackWrapperCmd(UnitBase target, bool isSelfDefense)
	{
		GetReachables();
		if (!IsMoving && occupiedNeighboursWithinRange.Contains(OnlineGameManager.Instance.unitsToOccupiedTerrains[target]) && unitProperties.moveCostToAttack <= remainingMovesThisTurn)
		{
			StartCoroutine(AttackValidated(target, isSelfDefense));
			HasAttacked = false;
			remainingMovesThisTurn = unitProperties.moveRange;
		}
	}

	[Server]
	public IEnumerator ValidateAttackWrapper(UnitBase target, bool isSelfDefense)
	{
		if (!HasAttacked)
		{
			if (!IsMoving && occupiedNeighboursWithinRange.Contains(OnlineGameManager.Instance.unitsToOccupiedTerrains[target]) && unitProperties.moveCostToAttack <= remainingMovesThisTurn)
			{
				float x = (transform.position.x - target.transform.position.x) / 2 + target.transform.position.x - 5;
				float y = 0;
				float z = (transform.position.z - target.transform.position.z) / 2 + target.transform.position.z + 0.75f;
				yield return StartCoroutine(target.SetAllCameraPositions(new Vector3(x, y, z)));
				yield return StartCoroutine(SetAllCameraPositions(new Vector3(x, y, z)));
				yield return StartCoroutine(AttackValidated(target, isSelfDefense));

				/*if (target != null)
				{
					target.GetReachables(target);
					yield return StartCoroutine(target.AttackValidated(this));
					target.HasAttacked = false;
					target.remainingMovesThisTurn = target.unitProperties.moveRange;
				}*/
				//OnlineGameManager.Instance.GetPlayer(playerID).SelectUnit(this);
			}
		}
	}

	[Server]
	private IEnumerator AttackValidated(UnitBase target, bool isSelfDefense)
	{
		if (!OnlineGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(OnlineGameManager.Instance.unitsToOccupiedTerrains[this]))
		{
			target.SeeAttacker(this, true);
		}
		float x = (transform.position.x - target.transform.position.x) / 2 + target.transform.position.x - 5;
		float y = 0;
		float z = (transform.position.z - target.transform.position.z) / 2 + target.transform.position.z + 0.75f;
		yield return StartCoroutine(target.SetAllCameraPositions(new Vector3(x, y, z)));
		yield return StartCoroutine(SetAllCameraPositions(new Vector3(x, y, z)));
		//AttackRpc(target);
		yield return StartCoroutine(AttackRoutine(target, isSelfDefense));
		//yield return null;
	}

	private IEnumerator AttackRoutine(UnitBase target, bool isSelfDefense)
	{
		AttackRoutine_Inner(target, isSelfDefense);
		yield return null;
	}

	[TargetRpc]
	private void AttackRoutine_Inner(UnitBase target, bool isSelfDefense)
	{
		StartCoroutine(AttackRoutine_Inner_Inner(target, isSelfDefense));
	}

	private IEnumerator AttackRoutine_Inner_Inner(UnitBase target, bool isSelfDefense)
	{
		SetIsMovingCmd(true);
		yield return new WaitForSeconds(.25f);
		Vector3 oldPos = transform.position;
		if (unitProperties.unitCombatType != UnitCombatType.RangedCombat)
		{
			yield return StartCoroutine(MoveRoutine(target.transform.position - (target.transform.position - transform.position).normalized * .5f, true));
			AttackCmd(target);
			yield return StartCoroutine(MoveRoutine(oldPos, true));
		}
		else
		{
			Quaternion oldRot = transform.rotation;
			Quaternion lookAt = Quaternion.LookRotation(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z) - transform.position);
			//yield return StartCoroutine(RotateRoutine(lookAt));
			//yield return StartCoroutine(MoveRoutine(target.transform.position - (target.transform.position - transform.position) * .999f));
			RotateRoutineWrapper(lookAt);
			yield return new WaitForSeconds(0.2f);
			AttackCmd(target);
			//yield return StartCoroutine(MoveRoutine(oldPos));
			//yield return StartCoroutine(RotateRoutine(oldRot));
			RotateRoutineWrapper(oldRot);
		}
		SeeAttackerCmd(target);
		SetIsMovingCmd(false);

		if (target != null && !target.isPendingDead && !isSelfDefense)
		{
			target.ValidateAttackWrapperCmd(this, true);
		}
	}

	[Command(requiresAuthority = false)]
	public void SeeAttackerCmd(UnitBase target)
	{
		if(target == null || target.isPendingDead) { return; }
		if (!OnlineGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(OnlineGameManager.Instance.unitsToOccupiedTerrains[this]))
		{
			target.SeeAttacker(this, false);
		}
	}


	//TODO: Ienumarator yap ve yield return yap
	[Command(requiresAuthority = false)]
	public void AttackCmd(UnitBase target)
	{
		StartCoroutine(Attack(target));
	}

	[Server]
	public virtual IEnumerator Attack(UnitBase target)
	{
		HasAttacked = true;
		remainingMovesThisTurn -= unitProperties.moveCostToAttack;

		if(remainingMovesThisTurn <= 0)
		{
			SetIsInMoveMode(false);
		}
		else
		{
			GetReachablesVisual();
		}

		PlayAttackEffectsRpc();

		yield return StartCoroutine(target.TakeDamage(this, unitProperties.damage));

		if(target.isPendingDead)
		{
			target.DisableHexagonOutlines();
			UpdateOutlinesServer();
			/*PlayDeathEffectsRpc(netIdentity.connectionToClient, target.transform.position);
			PlayDeathEffectsRpc(target.netIdentity.connectionToClient, target.transform.position);*/
		}
		yield return null;
	}

	[ClientRpc]
	public void PlayAttackEffectsRpc()
	{
		audioSource.clip = unitProperties.attackSound;
		audioSource.Play();
	}

	[Server]
	public IEnumerator PlayDeathEffectsWrapper(Vector3 pos)
	{
		//PlayDeathEffectsRpc(pos);
		yield return StartCoroutine(PlayDeathEffects(pos));
		yield return new WaitForSeconds(0.5f);
		OnlineGameManager.Instance.UnregisterUnit(playerID, this);
		yield return null;
	}

	[Server]
	public IEnumerator PlayDeathEffects(Vector3 pos)
	{
		PlayDeathEffectsRpc(pos);
		OnlineGameManager.Instance.GetPlayer(playerID).DeselectEverything();
		yield return null;
	}

	[ClientRpc]
	protected void PlayDeathEffectsRpc(Vector3 pos)
	{
		StartCoroutine(PlayDeathEffects_Inner(pos));
	}

	protected IEnumerator PlayDeathEffects_Inner(Vector3 pos)
	{
		yield return StartCoroutine(PlayDeathEffects_Inner_Inner(pos));
	}

	protected IEnumerator PlayDeathEffects_Inner_Inner(Vector3 pos)
	{
		audioSource.clip = unitProperties.deathSound;
		audioSource.Play();
		while (audioSource.isPlaying)
		{
			yield return new WaitForSeconds(0.05f);
		}
		ParticleSystem particle = Instantiate(unitProperties.deathParticle, pos, Quaternion.identity, null).GetComponent<ParticleSystem>();
		//particle.
	}

	[Server]
	public IEnumerator TakeDamage(UnitBase attacker, int damage)
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
				isPendingDead = true;
				//occupiedHex.OccupierUnit = null;
				yield return StartCoroutine(PlayDeathEffectsWrapper(transform.position));
				//return true;
			}
		}
		//return false;
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
	public void GetReachablesVisual()
	{
		GetReachables();
		EnableOutlines();
	}

	[Server]
	public void GetReachables()
	{
		occupiedNeighboursWithinRange.Clear();
		neighboursWithinRange = Map.Instance.GetReachableHexagons(OnlineGameManager.Instance.unitsToOccupiedTerrains[this], remainingMovesThisTurn, unitProperties.attackRange, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, occupiedNeighboursWithinRange);
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
			if ((neighbour.GetOccupierBuilding() != null) && (neighbour.GetOccupierBuilding().playerID != playerID))
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
			if (occupied_neighbour.GetOccupierUnit().playerID != playerID && remainingMovesThisTurn >= unitProperties.moveCostToAttack && !HasAttacked)
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
		foreach(TerrainHexagon hex in Map.Instance.AStar(OnlineGameManager.Instance.unitsToOccupiedTerrains[this], to, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, null))
		{
			path.Add(hex.Key);
		}
	}

	[Server]
	public void Move(TerrainHexagon to, int cost)
	{
		//occupiedHex.OccupierUnit = null;
		//occupiedHex = to;
		//occupiedHex.OccupierUnit = this;

		OnlineGameManager.Instance.UpdateUnitsOccupiedTerrain(this, to);

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
			GetReachablesVisual();
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
			GetReachablesVisual();
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