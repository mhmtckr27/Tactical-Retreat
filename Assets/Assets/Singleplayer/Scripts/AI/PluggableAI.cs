using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PluggableAI : MonoBehaviour
{
	[SerializeField] private PlayStyle playStyle;
	[SerializeField] private int resourcePriorityThreshold;
	public SPTownCenterAI TownCenter { get; set; }
	public bool IsPlaying { get; set; }
	private Priority currentPriority;
	//parameterize these
	public int townCenterDangerRadius = 3;
	//

	SPUnitBase closestEnemyToTownCenter;
	List<SPUnitBase> units;
	bool[] areUnitsPlayed;

	private SPUnitBase townCenterDefender;

	public IEnumerator Think()
	{
		IsPlaying = true;
		yield return StartCoroutine(PlayUnits());
		yield return StartCoroutine(DetermineTownPriority());
		yield return StartCoroutine(PlayTown(currentPriority));
		TownCenter.FinishTurn();
		IsPlaying = false;
	}

	private IEnumerator PlayTown(Priority townPriority)
	{
		//Debug.LogWarning("town priority: " + townPriority);
		switch (townPriority)
		{
			case Priority.CollectWood:
				if (CollectResource(ResourceType.Wood) == false)
				{
					PlayTown(Priority.CreateUnit);
				}
				break;
			case Priority.CollectMeat:
				if (CollectResource(ResourceType.Meat) == false)
				{
					PlayTown(Priority.CreateUnit);
				}				
				break;
			case Priority.CreateUnit:
				string unitName = SPGameManager.Instance.spawnablePrefabs[Random.Range(0, SPGameManager.Instance.spawnablePrefabs.Count)].name;
				TownCenter.CreateUnit(TownCenter, unitName);
				break;
		}
		yield return null;
	}

	private IEnumerator DetermineTownPriority()
	{
		int minResourceCount = int.MaxValue;
		ResourceType minResourceType = ResourceType.None;
		if(TownCenter.woodCount < minResourceCount)
		{
			minResourceCount = TownCenter.woodCount;
			minResourceType = ResourceType.Wood;
		}
		if(TownCenter.meatCount < minResourceCount)
		{
			minResourceCount = TownCenter.meatCount;
			minResourceType = ResourceType.Meat;
		}

		if (minResourceCount < resourcePriorityThreshold)
		{
			if(minResourceType == ResourceType.Wood)
			{
				currentPriority = Priority.CollectWood;
			}
			else
			{
				currentPriority = Priority.CollectMeat;
			}
		}
		else
		{
			currentPriority = Priority.CreateUnit;
		}

		yield return null;
	}

	private bool CollectResource(ResourceType resourceType)
	{
		List<SPTerrainHexagon> exploreds = SPGameManager.Instance.PlayersToDiscoveredTerrains[TownCenter.PlayerID];
		foreach (SPTerrainHexagon explored in exploreds)
		{
			if (explored.resource != null && explored.resource.resourceType == resourceType)
			{
				SPMap.Instance.selectedHexagon = explored;
				TownCenter.CollectResource(explored.resource);
				return true;
			}
		}
		return false;
	}

	private IEnumerator PlayUnits()
	{
		units = SPGameManager.Instance.GetUnits(TownCenter.PlayerID);
		areUnitsPlayed = new bool[units.Count];
		if (townCenterDefender == null && units.Count > 0)
		{
			townCenterDefender = units[Random.Range(0, units.Count)];
		}
		if(townCenterDefender != null)
		{
			yield return StartCoroutine(PlayTownCenterDefender());
			if(townCenterDefender != null && townCenterDefender.isPendingDead == false)
			{
				areUnitsPlayed[units.IndexOf(townCenterDefender)] = true;
			}
		}

		for (int i = 0; i < units.Count; i++)
		{
			//Debug.LogError(units.Count + " " + i);
			yield return StartCoroutine(PlayUnit(units[i]));
			//Debug.LogError(units.Count + " " + i);
		}
		yield return null;
	}

	private IEnumerator PlayUnit(SPUnitBase unit)
	{
		if (areUnitsPlayed[units.IndexOf(unit)] == false)
		{
			int unitIndex = units.IndexOf(unit);
			TownCenter.SelectUnit(unit);
			DeterminePriority(unit);
			//Debug.LogWarning(currentPriority.ToString() + " | unit: " + unit.name); ;
			switch (currentPriority)
			{
				case Priority.ConquerTownCenter:
					yield return StartCoroutine(Conquer());
					break;
				case Priority.OccupyTownCenter:
					yield return StartCoroutine(Occupy(GetClosestEnemyTownCenter(unit), unit));
					break;
				case Priority.AttackUnit:
					Debug.LogWarning("before attack");
					SPUnitBase target = GetClosestEnemy(unit);
					yield return StartCoroutine(Attack(unit, target));
					Debug.LogWarning("after attack");
					break;
				case Priority.ExploreMap:
					yield return StartCoroutine(ExploreMap(unit));
					break;
			}
			areUnitsPlayed[unitIndex] = true;
		}
		yield return null;
	}

	private IEnumerator Occupy(SPTownCenter closestEnemyTown, SPUnitBase unit)
	{
		TownCenter.SelectUnit(unit);
		List <SPTerrainHexagon> occupieds = new List<SPTerrainHexagon>();
		List<SPTerrainHexagon> reachables = SPMap.Instance.GetReachableHexagons(unit.occupiedHex, unit.unitProperties.moveRange, unit.unitProperties.attackRange, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds);
		if (closestEnemyTown.OccupiedHex.OccupierUnit == null)
		{
			List<SPTerrainHexagon> path = SPMap.Instance.AStar(unit.occupiedHex, closestEnemyTown.OccupiedHex, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds);
			if (unit.neighboursWithinRange.Contains(closestEnemyTown.OccupiedHex))
			{
				yield return StartCoroutine(unit.ValidateRequestToMove(closestEnemyTown.OccupiedHex, 0));
			}
			else
			{
				yield return StartCoroutine(unit.ValidateRequestToMove(path[unit.remainingMovesThisTurn], 0));
			}
		}
		else if (closestEnemyTown.OccupiedHex.OccupierUnit.playerID != unit.playerID)
		{
			if(reachables != null)
			{
				if (occupieds.Contains(closestEnemyTown.OccupiedHex))
				{
					yield return StartCoroutine(Attack(unit, closestEnemyTown.OccupiedHex.OccupierUnit)); // ValidateAttack(closestEnemyTown.OccupiedHex.OccupierUnit);
				}
				else
				{
					int minDist = int.MaxValue;
					SPTerrainHexagon closestHex = null;
					unit.GetReachables();
					foreach (SPTerrainHexagon hex in reachables)
					{
						int currentDist = SPMap.Instance.GetDistanceBetweenTwoBlocks(hex, closestEnemyTown.OccupiedHex);
						if (unit.neighboursWithinRange.Contains(hex) && currentDist < minDist)
						{
							minDist = currentDist;
							closestHex = hex;
						}
					}
					if(closestHex != null)
					{
						yield return StartCoroutine(unit.ValidateRequestToMove(closestHex, 0));
					}
				}
			}
		}
		else
		{
			int minDist = int.MaxValue;
			SPTerrainHexagon closestHex = null;
			unit.GetReachables();
			foreach (SPTerrainHexagon hex in reachables)
			{
				int currentDist = SPMap.Instance.GetDistanceBetweenTwoBlocks(hex, closestEnemyTown.OccupiedHex);
				if (unit.neighboursWithinRange.Contains(hex) && currentDist < minDist)
				{
					minDist = currentDist;
					closestHex = hex;
				}
			}
			if(closestHex != null)
			{
				yield return StartCoroutine(unit.ValidateRequestToMove(closestHex, 0));
			}
		}
	}

	private IEnumerator Conquer()
	{
		//CONQUER
		//simply destroy it.
		Debug.Log("conquering");
		yield return null;
	}

	private IEnumerator PlayTownCenterDefender()
	{
		DeterminePriority();
		switch (currentPriority)
		{
			case Priority.RetakeTownCenter:
				yield return StartCoroutine(RetakeTownCenter(townCenterDefender));
				break;
			case Priority.DefendTownCenter:
				yield return StartCoroutine(DefendTownCenter(townCenterDefender));
				break;
			default:
				yield return StartCoroutine(Patrol(townCenterDefender, TownCenter.OccupiedHex, townCenterDefender.unitProperties.attackRange));
				break;
		}
	}

	private IEnumerator Patrol(SPUnitBase explorer, SPTerrainHexagon origin, int distance)
	{
		TownCenter.SelectUnit(explorer);
		SPTerrainHexagon to = null;
		List<SPTerrainHexagon> distants = SPMap.Instance.GetDistantHexagons(TownCenter.OccupiedHex, distance);
		List<SPTerrainHexagon> occupieds = new List<SPTerrainHexagon>();
		List<SPTerrainHexagon> reachables = SPMap.Instance.GetReachableHexagons(explorer.occupiedHex, explorer.remainingMovesThisTurn, explorer.unitProperties.attackRange, explorer.unitProperties.blockedToMoveTerrains, explorer.unitProperties.blockedToAttackTerrains, occupieds);

		if(reachables.Count > 1)
		{
			do
			{
				to = distants[Random.Range(0, distants.Count)];

			} while ((SPMap.Instance.AStar(explorer.occupiedHex, to, explorer.unitProperties.blockedToMoveTerrains, explorer.unitProperties.blockedToAttackTerrains, occupieds) == null) || explorer.occupiedHex == to || TownCenter.OccupiedHex == to);
			Debug.Log(to.Key);
			if (!explorer.ValidateRequestToMove(to))
			{
				Debug.Log("cant move");
			}
			else
			{
				Debug.Log("moved");
			}
		}
		yield return null;
	}

	private IEnumerator RetakeTownCenter(SPUnitBase unit)
	{
		TownCenter.SelectUnit(unit);
		if (TownCenter.OccupiedHex.OccupierUnit == null)
		{
			if (unit.ValidateRequestToMove(TownCenter.OccupiedHex))
			{
				//TODO: send request to SPGameMAnager and retake town
				yield break;
			}
		}
		else if(TownCenter.OccupiedHex.OccupierUnit.playerID != TownCenter.PlayerID)
		{
			if (unit.occupiedNeighboursWithinRange.Contains(TownCenter.OccupiedHex))
			{
				if(unit.remainingMovesThisTurn >= unit.unitProperties.moveCostToAttack && !unit.HasAttacked)
				{
					yield return StartCoroutine(Attack(unit, TownCenter.OccupiedHex.OccupierUnit));
				}
			}
			else
			{
				//we cant attack because we are not near it.
			}
		}
		//unit could not manage to retake
	}

	private IEnumerator DefendTownCenter(SPUnitBase unit)
	{
		Debug.Log("defending");
		if (TownCenter.OccupiedHex.OccupierUnit == null)
		{
			Debug.Log("town center bos");
			TownCenter.SelectUnit(unit);
			List<SPTerrainHexagon> occupieds = new List<SPTerrainHexagon>();
			List<SPTerrainHexagon> path = SPMap.Instance.AStar(unit.occupiedHex, TownCenter.OccupiedHex, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds);
			if (unit.neighboursWithinRange.Contains(TownCenter.OccupiedHex))
			{
				Debug.Log("town centera gidebilirim");
				unit.ValidateRequestToMove(TownCenter.OccupiedHex);
			}
			else
			{
				Debug.LogError("path null: " + (path == null) + "| unit null: " + unit == null);
				Debug.Log("town centera gidememmm");
				unit.ValidateRequestToMove(path[unit.remainingMovesThisTurn]);
			}
		}
		else if (TownCenter.OccupiedHex.OccupierUnit.playerID != TownCenter.PlayerID)
		{
			Debug.Log("town centerda dusman var");
			yield return StartCoroutine(Attack(unit, TownCenter.OccupiedHex.OccupierUnit));
		}
		else
		{
			Debug.Log("town centerda dost var");
			yield return StartCoroutine(Attack(unit, closestEnemyToTownCenter));
			/*if (closestEnemyToTownCenter != null)
			{
				closestEnemyToTownCenter.GetReachables();
				yield return StartCoroutine(closestEnemyToTownCenter.ValidateAttack(unit, true));
				closestEnemyToTownCenter.HasAttacked = false;
				closestEnemyToTownCenter.remainingMovesThisTurn = closestEnemyToTownCenter.unitProperties.moveRange;
			}*/
		}
	}

	private void DeterminePriority()
	{
		if(IsTownCenterOccupied())
		{
			currentPriority = Priority.RetakeTownCenter;
		}	
		if(IsTownCenterInDanger())
		{
			currentPriority = Priority.DefendTownCenter;
		}
		else
		{
			currentPriority = Priority.ExploreMap;
		}
	}

	private void DeterminePriority(SPUnitBase unit)
	{
		if(unit.occupiedHex.OccupierBuilding && unit.occupiedHex.OccupierBuilding.PlayerID != unit.playerID)
		{
			//CONQUER
			currentPriority = Priority.ConquerTownCenter;
		}
		else /*if(true/*playStyle == PlayStyle.Aggressive)*/
		{
			SPTownCenter closestEnemyTown = GetClosestEnemyTownCenter(unit);
			SPUnitBase closestEnemy = GetClosestEnemy(unit);

			float opt = Random.Range(0, 1f);
			if (closestEnemyTown && closestEnemy)
			{
				if (opt <= 0.25f)
				{
					currentPriority = Priority.OccupyTownCenter;
				}
				else
				{
					currentPriority = Priority.AttackUnit;
				}
			}
			else if (closestEnemyTown)
			{
				if(opt <= 0.25f)
				{
					currentPriority = Priority.OccupyTownCenter;
				}
				else
				{
					currentPriority = Priority.ExploreMap;
				}
			}
			else if (closestEnemy)
			{
				currentPriority = Priority.AttackUnit;
			}
			else
			{
				currentPriority = Priority.ExploreMap;
			}
		}
	}

	private SPTownCenter GetClosestEnemyTownCenter(SPUnitBase unit)
	{
		int minDist = int.MaxValue;
		SPTownCenter closestEnemyTown = null;
		foreach(SPTownCenter enemyTown in TownCenter.exploredEnemyTowns)
		{
			if(unit.occupiedHex == enemyTown.OccupiedHex || (enemyTown.OccupiedHex.OccupierUnit && unit.playerID == enemyTown.OccupiedHex.OccupierUnit.playerID))
			{
				return enemyTown;
			}
			else
			{
				//TODO: optimize et. suanda blocked terrainleri hesaba katmiyor. kus ucusu mesafe buluyor.
				int currentDist = SPMap.Instance.GetDistanceBetweenTwoBlocks(unit.occupiedHex, enemyTown.OccupiedHex);
				if (currentDist < minDist)
				{
					minDist = currentDist;
					closestEnemyTown = enemyTown;
				}
			}
		}
		return closestEnemyTown;
	}

	private SPUnitBase GetClosestEnemy(SPUnitBase unit)
	{
		foreach (SPTerrainHexagon hex in SPMap.Instance.GetDistantHexagons(unit.occupiedHex, unit.unitProperties.attackRange))
		{
			if (hex.OccupierUnit && hex.OccupierUnit.playerID != unit.playerID && unit.remainingMovesThisTurn >= unit.unitProperties.moveCostToAttack)
			{
				return hex.OccupierUnit;
			}
		}
		return null;
	}

	private IEnumerator ExploreMap(SPUnitBase unit)
	{
		TownCenter.SelectUnit(unit);
		SPTerrainHexagon to = null;
		//List<SPTerrainHexagon> distants = SPMap.Instance.GetDistantHexagons(unit.occupiedHex, unit.remainingMovesThisTurn);
		List<SPTerrainHexagon> occupieds = new List<SPTerrainHexagon>();
		List<SPTerrainHexagon> reachables = SPMap.Instance.GetReachableHexagons(unit.occupiedHex, unit.remainingMovesThisTurn, unit.unitProperties.attackRange, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds); ;

		List<SPTerrainHexagon> unexploreds = new List<SPTerrainHexagon>();
		List<SPTerrainHexagon> unexploredDistants = new List<SPTerrainHexagon>();

		foreach(KeyValuePair<string, SPTerrainHexagon> kvp in SPMap.Instance.mapDictionary)
		{
			if (SPGameManager.Instance.PlayersToDiscoveredTerrains[TownCenter.PlayerID].Contains(kvp.Value) == false)
			{
				unexploreds.Add(kvp.Value);
				if (reachables.Contains(kvp.Value))
				{
					unexploredDistants.Add(kvp.Value);
				}
			}
		}

		List<SPTerrainHexagon> path = null;
		//ulasabilecegim hexlerden kesfetmediklerim varsa oraya git
		if(unexploredDistants.Count > 0)
		{
			to = unexploredDistants[Random.Range(0, unexploredDistants.Count)];
		}
		//yakinimda kesfetmedigim yoksa, en yakin kesfetmedigimi bul ona yaklas.
		else if (unexploreds.Count > 0)
		{
			int minDist = int.MaxValue;
			foreach(SPTerrainHexagon hex in unexploreds)
			{
				int currentDist = SPMap.Instance.GetDistanceBetweenTwoBlocks(unit.occupiedHex, hex);
				if (currentDist < minDist)
				{
					path = SPMap.Instance.AStar(unit.occupiedHex, hex, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds);
					if (path != null)
					{
						minDist = currentDist;
						to = hex;
					}
				}
			}
			if(to != null)
			{
				//Debug.LogError(path.Count + " " + unit.remainingMovesThisTurn);
				yield return StartCoroutine(unit.ValidateRequestToMove(path[unit.remainingMovesThisTurn], 0));
			}
			else
			{
				if (reachables.Count > 0)
				{
					do
					{
						to = reachables[Random.Range(0, reachables.Count)];
					} while ((SPMap.Instance.AStar(unit.occupiedHex, to, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds) == null) || to == unit.occupiedHex);
					yield return StartCoroutine(unit.ValidateRequestToMove(to, 0));
				}
			}
		}
		//kesfetmedigim yer yoksa yakinimdakilerden rastgele birine git
		else if(reachables.Count > 0)
		{
			do
			{
				to = reachables[Random.Range(0, reachables.Count)];
			} while ((SPMap.Instance.AStar(unit.occupiedHex, to, unit.unitProperties.blockedToMoveTerrains, unit.unitProperties.blockedToAttackTerrains, occupieds) == null) || to == unit.occupiedHex);
			yield return StartCoroutine(unit.ValidateRequestToMove(to, 0));
		}
	}

	/*private IEnumerator ValidateAttack(SPUnitBase attacker, SPUnitBase target)
	{
		//if(TownCenter.OccupiedHex.OccupierUnit != null && TownCenter.OccupiedHex.OccupierUnit.playerID != TownCenter.PlayerID)
		{
			yield return StartCoroutine(Attack(attacker, target));
		}
	}*/

	private IEnumerator Attack(SPUnitBase attacker, SPUnitBase target)
	{
		TownCenter.SelectUnit(attacker);
		//Debug.LogError("deniyorum ama olmuyor" + closestEnemyToTownCenter.occupiedHex.Key);
		bool hasAttackedBefore = attacker.HasAttacked;
		yield return StartCoroutine(attacker.ValidateAttack(target, false));
		bool hasAttackedAfter = attacker.HasAttacked;
		if(hasAttackedBefore == false && hasAttackedAfter == true)
		{
			if (target != null && target.isPendingDead == false)
			{
				target.GetReachables();
				yield return StartCoroutine(target.ValidateAttack(attacker, true));
				target.HasAttacked = false;
				target.remainingMovesThisTurn = target.unitProperties.moveRange;
			}
		}
	}

	//TODO: ne kadar fazla dusman birlik varsa tehlike o kadar buyuktur ve
	//o kadar fazla birlik takviyesi gerekir town centera.
	private bool IsTownCenterInDanger()
	{
		foreach(SPTerrainHexagon hex in SPMap.Instance.GetDistantHexagons(TownCenter.OccupiedHex, townCenterDangerRadius))
		{
			if(hex.OccupierUnit && hex.OccupierUnit.playerID != TownCenter.PlayerID)
			{
				closestEnemyToTownCenter = hex.OccupierUnit;
				return true;
			}
		}
		return false;
	}

	private bool IsTownCenterOccupied()
	{
		//TODO: occupy ederken birakan birisi var mi onu da kontrol et.
		return (TownCenter.OccupiedHex.OccupierUnit != null && TownCenter.OccupiedHex.OccupierUnit.playerID != TownCenter.PlayerID);
	}
}

public enum Priority
{
	//town center defender unit related.
	RetakeTownCenter,
	DefendTownCenter,

	//unit related
	AttackUnit,
	EscapeUnit,
	//send unit to nearest enemy town center
	OccupyTownCenter,
	//unit is already on enemy town center, start conquer process
	ConquerTownCenter,
	ExploreMap,

	//town related
	CollectWood,
	CollectMeat,
	CreateUnit,
}

public enum PlayStyle
{
	Aggressive,
	Defensive
}