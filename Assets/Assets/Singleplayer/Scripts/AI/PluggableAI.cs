using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PluggableAI : MonoBehaviour
{
	public SPTownCenterAI TownCenter { get; set; }
	public bool IsPlaying { get; set; }
	private Priority currentPriority;
	//parameterize these
	public int townCenterDangerRadius = 3;
	//
	int prevIterationActionPoint = -1;

	SPUnitBase closestEnemyToTownCenter;
	public void Think()
	{
		PlayTown();
		PlayUnits();
		/*while (!MustFinishTurn(TownCenter.actionPoint))
		{
			DeterminePriority();
			Play();
		}
		TownCenter.FinishTurn();*/
		TownCenter.FinishTurn();
	}

	private void PlayUnits()
	{
		List<SPUnitBase> units = SPGameManager.Instance.GetUnits(TownCenter.PlayerID);
		List<bool> areUnitsPlayed = new List<bool>(units.Count);

		foreach(SPUnitBase unit in units)
		{
			//DeterminePriority()
		}
	}

	private void PlayTown()
	{
		string unitName = SPGameManager.Instance.spawnablePrefabs[Random.Range(0, SPGameManager.Instance.spawnablePrefabs.Count)].name;
		TownCenter.CreateUnit(unitName);
		CollectResource();
	}

	private void CollectResource()
	{
		List<SPTerrainHexagon> exploreds = SPGameManager.Instance.PlayersToDiscoveredTerrains[TownCenter.PlayerID];
		foreach (SPTerrainHexagon explored in exploreds)
		{
			if (explored.resource != null && explored.resource.canBeCollected)
			{
				SPMap.Instance.selectedHexagon = explored;
				TownCenter.UpdateResourceCount(explored.resource);
			}
		}
	}

	private bool MustFinishTurn(int thisIterationActionPoint)
	{
		if(thisIterationActionPoint == prevIterationActionPoint)
		{
			return true;
		}
		prevIterationActionPoint = thisIterationActionPoint;
		return false;
	}

	private void DeterminePriority()
	{
		//if(we are getting counquered){
		//{
		//	currentPriority = Priority.RetakeTownCenter;
		//}	
		if (IsTownCenterInDanger())
		{
			currentPriority = Priority.DefendTownCenter;
		}
		//else if()
		//else if()
		else
		{
			currentPriority = Priority.ExploreMap;
		}
	}

	private void Play()
	{
		switch (currentPriority)
		{
			case Priority.RetakeTownCenter:
				break;
			case Priority.DefendTownCenter:
				DefendTownCenter();
				Attack(closestEnemyToTownCenter);
				break;
			case Priority.AttackUnit:
				//Attack()
				break;
			case Priority.ExploreMap:
				ExploreMap();
				break;
		}
		Think();
	}

	private void ExploreMap()
	{
		List<SPUnitBase> units = SPGameManager.Instance.GetUnits(TownCenter.PlayerID);
		if(units.Count == 0) { return; }
		do
		{
			TownCenter.SelectUnit(units[UnityEngine.Random.Range(0, units.Count)]);
		} while (!SPMap.Instance.UnitToMove.CanMove());

		SPTerrainHexagon to;
		List<SPTerrainHexagon> distants = SPMap.Instance.GetDistantHexagons(SPMap.Instance.UnitToMove.occupiedHex, SPMap.Instance.UnitToMove.remainingMovesThisTurn);

		do
		{
			to = distants[Random.Range(0, distants.Count)];
		} while (SPMap.Instance.AStar(SPMap.Instance.UnitToMove.occupiedHex, to, SPMap.Instance.UnitToMove.blockedTerrains) == null);

		SPMap.Instance.UnitToMove.ValidateRequestToMove(to);
	}

	private void Attack(SPUnitBase target)
	{
		if(TownCenter.OccupiedHex.OccupierUnit != null)
		{
			TownCenter.SelectUnit(TownCenter.OccupiedHex.OccupierUnit);
			TownCenter.OccupiedHex.OccupierUnit.ValidateAttack(target);
		}
	}

	private void DefendTownCenter()
	{
		if (TownCenter.CreateUnit("SPWarrior_New_Variant"))
		{
			return;
		}
		if(TownCenter.actionPoint == 0) { return; }
		if(TownCenter.OccupiedHex.OccupierUnit == null)
		{
			int minDistance = int.MaxValue;
			int unitIndex = -1;
			List<SPUnitBase> units = SPGameManager.Instance.GetUnits(TownCenter.PlayerID);
			foreach (SPUnitBase unit in units)
			{
				int currentDistance = SPMap.Instance.AStar(unit.occupiedHex, TownCenter.OccupiedHex, unit.blockedTerrains).Count;
				if ((currentDistance < minDistance) && (unit.remainingMovesThisTurn > 0))
				{
					unitIndex = units.IndexOf(unit);
					minDistance = currentDistance;
				}
			}
			if(unitIndex == -1) { return; }
			TownCenter.SelectUnit(units[unitIndex]);
			List<SPTerrainHexagon> path = SPMap.Instance.AStar(units[unitIndex].occupiedHex, TownCenter.OccupiedHex, units[unitIndex].blockedTerrains);
			if(units[unitIndex].remainingMovesThisTurn <= (path.Count - 1))
			{
				units[unitIndex].ValidateRequestToMove(TownCenter.OccupiedHex);
			}
			else
			{
				units[unitIndex].ValidateRequestToMove(path[units[unitIndex].remainingMovesThisTurn]);
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
}

public enum Priority
{
	ExploreMap,
	AttackUnit,
	ConquerTownCenter,
	ReinforceUnit,
	ObtainResource,
	EscapeUnit,
	DefendTownCenter,
	RetakeTownCenter
}