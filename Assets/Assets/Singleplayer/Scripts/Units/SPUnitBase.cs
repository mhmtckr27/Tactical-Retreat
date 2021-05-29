using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SPUnitBase : MonoBehaviour
{
	public int currentHealth;
	public int currentArmor;
	public bool HasAttacked { get; set; }
	[SerializeField] public GameObject canvas;
	[SerializeField] protected Image healthBar;
	[SerializeField] protected Image armorBar;
	[SerializeField] public UnitProperties unitProperties;

	protected AudioSource audioSource;
	public bool IsMoving { get; set; }

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

	public List<SPTerrainHexagon> neighboursWithinRange;
	public List<SPTerrainHexagon> occupiedNeighboursWithinRange;
	public List<SPTerrainHexagon> path = new List<SPTerrainHexagon>();

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
		//TODO:
		OnIsInMoveModeChange(newIsInMoveMode);
	}

	public void OnIsInMoveModeChange(bool newValue)
	{
		if (!SPGameManager.Instance.GetPlayer(playerID).IsAI)
		{
			DisableHexagonOutlines();
		}

		if (newValue)
		{
			RequestToggleActionMode(this);
			if (!SPGameManager.Instance.GetPlayer(playerID).IsAI)
			{
				RequestReachableHexagons(this);
			}
			else
			{
				GetReachables();
			}
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
		//remainingMovesThisTurn = unitProperties.moveRange;
		HasAttacked = true;
		transform.eulerAngles = unitProperties.initialRotation;
		currentArmor = unitProperties.armor;
		currentHealth = unitProperties.health;
		audioSource = GetComponent<AudioSource>();
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

	public IEnumerator Move2(Vector3 to, int dummy)
	{
		SPTerrainHexagon[] tempPath = new SPTerrainHexagon[path.Count];
		path.CopyTo(tempPath, 0);
		yield return StartCoroutine(MoveRoutine(to + UnitProperties.positionOffsetOnHexagons));
	}

	public void SetIsMoving(bool isMoving)
	{
		this.IsMoving = isMoving;
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

	public IEnumerator ValidateAttack(SPUnitBase target)
	{
		/*if (IsMoving) { Debug.LogError("moving"); return; }
		if (!occupiedNeighboursWithinRange.Contains(target.occupiedHex)) { Debug.LogError("yakin degil"); return; }
		//TODO: visual feedback
		if (unitProperties.moveCostToAttack > remainingMovesThisTurn) { return; }
		*/
		if (!IsMoving && occupiedNeighboursWithinRange.Contains(target.occupiedHex) && unitProperties.moveCostToAttack <= remainingMovesThisTurn)
		{

			if (/*true*/!HasAttacked/* && targetIsInRange*/)
			{
				yield return StartCoroutine(AttackRoutines(target));
				if(target != null)
				{
					target.GetReachables();
					yield return StartCoroutine(target.ValidateAttack(this, true));
					target.HasAttacked = false;
					target.remainingMovesThisTurn = target.unitProperties.moveRange;
					//SPGameManager.Instance
				}
				SPGameManager.Instance.GetPlayer(playerID).SelectUnit(this); 
			}
		}
	}

	public IEnumerator ValidateAttack(SPUnitBase target, bool isSelfDefense)
	{
		Debug.LogWarning(!IsMoving);
		Debug.LogWarning(occupiedNeighboursWithinRange.Contains(target.occupiedHex));
		Debug.LogWarning(unitProperties.moveCostToAttack <= remainingMovesThisTurn);
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

	IEnumerator AttackRoutines(SPUnitBase target)
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

	private IEnumerator AttackValidated(SPUnitBase target)
	{
		if (!SPGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex) && !SPGameManager.Instance.GetPlayer(playerID).IsAI)
		{
			target.SeeAttacker(this, true);
		}
		if (!SPGameManager.Instance.GetPlayer(target.playerID).IsAI)
		{
			float x = (transform.position.x - target.transform.position.x) / 2 + target.transform.position.x - 5;
			float y = 0;
			float z = (transform.position.z - target.transform.position.z) / 2 + target.transform.position.z + 0.75f;
			target.StartCoroutine(SetAllCameraPositions(new Vector3(x, y, z)));
		}
		yield return StartCoroutine(AttackRoutine(target));
	}

	IEnumerator AttackRoutine(SPUnitBase target)
	{
		SetIsMoving(true);
		yield return new WaitForSeconds(.25f);
		Vector3 oldPos = transform.position;
		if (unitProperties.unitCombatType != UnitCombatType.RangedCombat)
		{
			yield return StartCoroutine(MoveRoutine(target.transform.position - (target.transform.position - transform.position).normalized * .5f));
			yield return StartCoroutine(Attack(target));
			yield return StartCoroutine(MoveRoutine(oldPos));
		}
		else
		{
			Quaternion oldRot = transform.rotation;
			Quaternion lookAt = Quaternion.LookRotation(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z) - transform.position);
			yield return StartCoroutine(RotateRoutine(lookAt));
			yield return StartCoroutine(Attack(target));
			yield return StartCoroutine(RotateRoutine(oldRot));
		}
		if (!SPGameManager.Instance.GetPlayer(target.playerID).IsAI)
		{
			SeeAttacker(target);
		}
		SetIsMoving(false);
	}

	//TODO Update
	public void SeeAttacker(SPUnitBase target)
	{
		if (target == null) { return; }
		if (!SPGameManager.Instance.PlayersToDiscoveredTerrains[target.playerID].Contains(occupiedHex))
		{
			target.SeeAttacker(this, false);
		}
	}


	public virtual IEnumerator Attack(SPUnitBase target)
	{
		HasAttacked = true;
		remainingMovesThisTurn -= unitProperties.moveCostToAttack;
		if(remainingMovesThisTurn <= 0)
		{
			SetIsInMoveMode(false);
		}
		else
		{
			GetReachablesVisual(null);
		}
		audioSource.clip = unitProperties.attackSound;
		audioSource.Play();
		bool isTargetDead = target.TakeDamage(this, unitProperties.damage);
		if (isTargetDead)
		{
			target.DisableHexagonOutlines();
			PlayDeathEffects(target.transform.position);
			UpdateOutlines();
		}
		yield return null;
	}

	protected void PlayDeathEffects(Vector3 pos)
	{
		Instantiate(unitProperties.deathParticle, pos, Quaternion.identity);
	}

	private void TakeDamage2(SPUnitBase attacker, Image bar, float fillAmount)
	{
		//healthBar.fillAmount = fillAmount;
		Instantiate(unitProperties.hitBloodParticle, healthBar.transform.position, Quaternion.identity);
		audioSource.clip = attacker.unitProperties.hitSound;
		if(audioSource.clip != null)
		{
			audioSource.Play();
		}
		StartCoroutine(TakeDamageRoutine(bar, fillAmount));
	}

	private void SeeAttacker(SPUnitBase attacker, bool see)
	{
		if (!SPGameManager.Instance.GetPlayer(attacker.playerID).IsAI)
		{
			attacker.gameObject.SetActive(see);
		}
	}

	IEnumerator TakeDamageRoutine(Image bar, float fillAmount)
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
		if (bar.transform.parent != null && bar.fillAmount == 0)
		{
			Destroy(bar.transform.parent.gameObject);
		}
	}

	//TODO update
	public bool TakeDamage(SPUnitBase attacker, int damage)
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
			TakeDamage2(attacker, armorBar, (float)currentArmor / unitProperties.armor);
		}
		if(damageToHealth > 0)
		{
			currentHealth = (currentHealth - damageToHealth) > 0 ? currentHealth - damageToHealth : 0;
			TakeDamage2(attacker, healthBar, (float)currentHealth / unitProperties.health);
			if (currentHealth <= 0)
			{
				currentHealth = 0;
				occupiedHex.OccupierUnit = null;
				SPGameManager.Instance.UnregisterUnit(playerID, this);
				audioSource.clip = unitProperties.deathSound;
				audioSource.Play();
				Destroy(gameObject);
				return true;
			}
		}
		return false;
	}

	public void GetReachablesVisual(SPUnitBase targetUnit)
	{
		//occupiedNeighboursWithinRange.Clear();
		//neighboursWithinRange = SPMap.Instance.GetReachableHexagons(occupiedHex, remainingMovesThisTurn, unitProperties.attackRange, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, occupiedNeighboursWithinRange);
		GetReachables();
		if (!SPGameManager.Instance.GetPlayer(targetUnit.playerID).IsAI)
		{
			EnableOutlines();
		}
	}

	public void GetReachables()
	{
		occupiedNeighboursWithinRange.Clear();
		neighboursWithinRange = SPMap.Instance.GetReachableHexagons(occupiedHex, remainingMovesThisTurn, unitProperties.attackRange, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, occupiedNeighboursWithinRange);
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
			//Debug.LogError((occupied_neighbour == null) + " " + (occupied_neighbour.OccupierUnit == null));
			if (occupied_neighbour.OccupierUnit.playerID != playerID && remainingMovesThisTurn >= unitProperties.moveCostToAttack && !HasAttacked)
			{
				EnableHexagonOutline(occupied_neighbour, 1, true);
			}
		}
	}

	public bool ValidateRequestToMove(SPTerrainHexagon to)
	{
		if (IsMoving) { return false; }
		if (!neighboursWithinRange.Contains(to)) { return false; }
		else
		{
			GetPath(to);
			if(remainingMovesThisTurn < path.Count - 1)
			{
				return false;
			}
			Move(to, path.Count - 1);
			return true;
		}
	}
	public IEnumerator ValidateRequestToMove(SPTerrainHexagon to, int dummy)
	{
		if(!IsMoving && neighboursWithinRange.Contains(to))
		{
			GetPath(to);
			if (remainingMovesThisTurn >= (path.Count - 1))
			{
				yield return Move(to, path.Count - 1, 0);
			}
		}
	}

	public void GetPath(SPTerrainHexagon to)
	{
		path.Clear();
		foreach (SPTerrainHexagon hex in SPMap.Instance.AStar(occupiedHex, to, unitProperties.blockedToMoveTerrains, unitProperties.blockedToAttackTerrains, null))
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
			SetIsInMoveMode(false);
		}
		else
		{
			UpdateOutlines();
		}
	}

	public IEnumerator Move(SPTerrainHexagon to, int cost, int dummy)
	{
		occupiedHex.OccupierUnit = null;
		occupiedHex = to;
		occupiedHex.OccupierUnit = this;
		remainingMovesThisTurn -= cost;
		yield return Move2(to.transform.position, dummy);
		if (remainingMovesThisTurn == 0)
		{
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
			GetReachablesVisual(this);
		}
	}

	public void RequestReachableHexagons(SPUnitBase targetUnit/*, TerrainHexagon blockUnder, int remainingMoves*/)
	{
		//in future, implement some logic that will prevent user from cheating. e.g. if it is not this player's turn, ignore this request.
		if ((targetUnit != null) && (IsInMoveMode) && (SPMap.Instance.UnitToMove == targetUnit) && (remainingMovesThisTurn > 0))
		{
			GetReachablesVisual(targetUnit);
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