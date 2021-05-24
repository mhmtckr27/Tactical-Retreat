using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SPUnitBase : MonoBehaviour
{
	public int currentHealth;
	public int currentArmor;
	public bool HasAttacked { get; set; }
	[SerializeField] protected Image healthBar;
	[SerializeField] public UnitProperties unitProperties;

	protected bool isMoving;

	private Color playerColor;
	public Color PlayerColor { get => playerColor;
		set
		{
			OnPlayerColorChange(playerColor, value);
			playerColor = value;
		}
	}

	public void OnPlayerColorChange(Color oldColor, Color newColor)
	{
		GetComponent<Renderer>().materials[0].color = newColor;
	}

	[HideInInspector] public uint playerID;

	protected List<SPTerrainHexagon> neighboursWithinRange;
	protected List<SPTerrainHexagon> occupiedNeighboursWithinRange;
	public List<SPTerrainHexagon> path = new List<SPTerrainHexagon>();

	public List<TerrainType> blockedTerrains;
	public SPTerrainHexagon occupiedHex;
	public int remainingMovesThisTurn;

	public bool CanMove()
	{
		return remainingMovesThisTurn > 0;
	}

	private bool isInMoveMode = false;
	public bool IsInMoveMode
	{
		get => isInMoveMode;
		set
		{
			isInMoveMode = value;
		}
	}

	public void SetIsInMoveMode(bool newIsInMoveMode)
	{
		IsInMoveMode = newIsInMoveMode;
		if (!SPGameManager.Instance.GetPlayer(playerID).IsAI)
		{
			OnIsInMoveModeChange(newIsInMoveMode);
		}
	}

	public void OnIsInMoveModeChange(bool newValue)
	{
		DisableHexagonOutlines();
		if (newValue)
		{
			RequestToggleActionMode(this);
			RequestReachableHexagons(this);
		}
		else
		{
			RequestToggleActionMode(null);
		}
	}

	private void Awake()
	{
		neighboursWithinRange = new List<SPTerrainHexagon>();
		occupiedNeighboursWithinRange = new List<SPTerrainHexagon>();
		remainingMovesThisTurn = unitProperties.moveRange;
		transform.eulerAngles = unitProperties.initialRotation;
		currentArmor = unitProperties.armor;
		currentHealth = unitProperties.health;
	}

	//TODO update
	private void Start()
	{
		SPGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, unitProperties.exploreRange);
	}


	/*public bool TryMoveTo(TerrainHexagon to)
	{
		RequestToMove(to);
		return true;
	}*/


	public void Move2(Vector3 to)
	{
		SPTerrainHexagon[] tempPath = new SPTerrainHexagon[path.Count];
		path.CopyTo(tempPath, 0);
		StartCoroutine(MoveRoutine(to + UnitProperties.positionOffsetOnHexagons));
	}

	public void SetIsMoving(bool isMoving)
	{
		this.isMoving = isMoving;
	}

	protected IEnumerator RotateRoutine(Quaternion lookAt)
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
		SetIsMoving(true);
		Quaternion oldRot = transform.rotation;
		Quaternion lookAt = Quaternion.LookRotation(new Vector3(moveToPosition.x, transform.position.y, moveToPosition.z) - transform.position);
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
		ExploreTerrains();
		yield return StartCoroutine(RotateRoutine(oldRot));
		SetIsMoving(false);
	}

	//TODO update
	public void ExploreTerrains()
	{
		SPGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, unitProperties.exploreRange);
	}

	public void EnableHexagonOutline(SPTerrainHexagon hexagon, int outlineIndex, bool enable)
	{
		hexagon.ToggleOutlineVisibility(outlineIndex, enable);
	}

	IEnumerator SetAllCameraPositions(Vector3 position)
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

	public void ValidateAttack(SPUnitBase target)
	{
		if (isMoving) { return; }
		if (!occupiedNeighboursWithinRange.Contains(target.occupiedHex)) { return; }
		//TODO: visual feedback
		if (unitProperties.actionPointCostToAttack > SPGameManager.Instance.GetPlayer(playerID).actionPoint) { return; }

		if (true/*!HasAttacked/* && targetIsInRange*/)
		{
			if (!SPGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex.Key))
			{
				target.SeeAttacker(this, true);
			}
			if (!SPGameManager.Instance.GetPlayer(target.playerID).IsAI)
			{
				target.StartCoroutine(SetAllCameraPositions(new Vector3(target.transform.position.x - 5, 0, target.transform.position.z + 0.75f)));
			}
			StartCoroutine(AttackRoutine(target));
		}
	}

	IEnumerator AttackRoutine(SPUnitBase target)
	{
		yield return new WaitForSeconds(.25f);
		Vector3 oldPos = transform.position;
		if (unitProperties.unitCombatType != UnitCombatType.RangedCombat)
		{
			yield return StartCoroutine(MoveRoutine(target.transform.position - (target.transform.position - transform.position).normalized * .5f));
			Attack(target);
			yield return StartCoroutine(MoveRoutine(oldPos));
		}
		else
		{
			Quaternion oldRot = transform.rotation;
			Quaternion lookAt = Quaternion.LookRotation(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z) - transform.position);
			yield return StartCoroutine(RotateRoutine(lookAt));
			Attack(target);
			yield return StartCoroutine(RotateRoutine(oldRot));
		}
		SeeAttacker(target);
	}

	//TODO Update
	public void SeeAttacker(SPUnitBase target)
	{
		if (target == null) { return; }
		if (!SPGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex.Key))
		{
			target.SeeAttacker(this, false);
		}
	}

	public virtual void Attack(SPUnitBase target)
	{
		HasAttacked = true;
		bool isTargetDead = target.TakeDamage(unitProperties.damage);
		if (isTargetDead)
		{
			PlayDeathEffects(target.transform.position);
			UpdateOutlines();
		}
	}

	protected void PlayDeathEffects(Vector3 pos)
	{
		Instantiate(unitProperties.deathParticle, pos, Quaternion.identity);
	}

	private void TakeDamage2(float fillAmount)
	{
		//healthBar.fillAmount = fillAmount;
		Instantiate(unitProperties.hitBloodParticle, healthBar.transform.position, Quaternion.identity);
		StartCoroutine(TakeDamageRoutine(fillAmount));
	}

	private void SeeAttacker(SPUnitBase attacker, bool see)
	{
		if (!SPGameManager.Instance.GetPlayer(attacker.playerID).IsAI)
		{
			attacker.gameObject.SetActive(see);
		}
	}

	IEnumerator TakeDamageRoutine(float fillAmount)
	{
		while (true)
		{
			healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, fillAmount, 0.05f);
			if (healthBar.fillAmount < 0.25f)
			{
				healthBar.color = Color.red;
			}
			if (Mathf.Abs(healthBar.fillAmount - fillAmount) < 0.05f)
			{
				healthBar.fillAmount = fillAmount;
				break;
			}
			yield return new WaitForSeconds(unitProperties.waitBetweenMovement);
		}
	}

	//TODO update
	public bool TakeDamage(int damage)
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
		if(damageToHealth > 0)
		{
			currentHealth = (currentHealth - damageToHealth) > 0 ? currentHealth - damageToHealth : 0;
			TakeDamage2((float)currentHealth / unitProperties.health);
			if (currentHealth <= 0)
			{
				currentHealth = 0;
				occupiedHex.OccupierUnit = null;
				SPGameManager.Instance.UnregisterUnit(playerID, this);
				Destroy(gameObject);
				return true;
			}
		}
		return false;
	}

	public void GetReachables(SPUnitBase targetUnit)
	{
		occupiedNeighboursWithinRange.Clear();
		neighboursWithinRange = SPMap.Instance.GetReachableHexagons(occupiedHex, remainingMovesThisTurn, unitProperties.attackRange, blockedTerrains, occupiedNeighboursWithinRange);
		EnableOutlines();
	}

	public void DisableHexagonOutlines()
	{
		foreach (SPTerrainHexagon neighbour in neighboursWithinRange)
		{
			EnableHexagonOutline(neighbour, 0, false);
			EnableHexagonOutline(neighbour, 1, false);
		}
		foreach (SPTerrainHexagon occupied in occupiedNeighboursWithinRange)
		{
			EnableHexagonOutline(occupied, 0, false);
			EnableHexagonOutline(occupied, 1, false);
		}
	}

	public void EnableOutlines()
	{
		foreach (SPTerrainHexagon neighbour in neighboursWithinRange)
		{
			if ((neighbour.OccupierBuilding != null) && (neighbour.OccupierBuilding.PlayerID != playerID))
			{
				EnableHexagonOutline(neighbour, 1, true);
			}
			else
			{
				EnableHexagonOutline(neighbour, 0, true);
			}
		}
		foreach (SPTerrainHexagon occupied_neighbour in occupiedNeighboursWithinRange)
		{
			if (occupied_neighbour.OccupierUnit.playerID != playerID)
			{
				EnableHexagonOutline(occupied_neighbour, 1, true);
			}
		}
	}

	public bool ValidateRequestToMove(SPTerrainHexagon to)
	{
		if (isMoving) { return false; }
		if (!neighboursWithinRange.Contains(to)) { return false; }
		//TODO: visual feedback that indicates why it cant move there.
		if (unitProperties.actionPointCostToMove > SPGameManager.Instance.GetPlayer(playerID).actionPoint) { return false; }
		else
		{
			GetPath(to);
			Move(to, path.Count - 1);
			return true;
		}
	}

	public void GetPath(SPTerrainHexagon to)
	{
		path.Clear();
		foreach (SPTerrainHexagon hex in SPMap.Instance.AStar(occupiedHex, to, blockedTerrains))
		{
			path.Add(hex);
		}
	}

	public void Move(SPTerrainHexagon to, int cost)
	{
		occupiedHex.OccupierUnit = null;
		occupiedHex = to;
		occupiedHex.OccupierUnit = this;
		remainingMovesThisTurn -= cost;
		Move2(to.transform.position);
		if (remainingMovesThisTurn == 0)
		{
			IsInMoveMode = false;
			SetIsInMoveMode(false);
		}
		else
		{
			UpdateOutlines();
		}
	}

	protected void UpdateOutlines()
	{
		DisableHexagonOutlines();
		if (IsInMoveMode)
		{
			GetReachables(this);
		}
	}

	public void RequestReachableHexagons(SPUnitBase targetUnit/*, TerrainHexagon blockUnder, int remainingMoves*/)
	{
		//in future, implement some logic that will prevent user from cheating. e.g. if it is not this player's turn, ignore this request.
		if ((targetUnit != null) && (IsInMoveMode) && (SPMap.Instance.UnitToMove == targetUnit) && (remainingMovesThisTurn > 0))
		{
			GetReachables(targetUnit);
		}
	}

	public void RequestToggleActionMode(SPUnitBase unit)
	{
		if (unit == null)
		{
			SPMap.Instance.currentState = State.None;
			SPMap.Instance.UnitToMove = unit;
		}
		else if (unit == this)
		{
			SPMap.Instance.currentState = State.UnitAction;
			SPMap.Instance.UnitToMove = unit;
		}
	}
}