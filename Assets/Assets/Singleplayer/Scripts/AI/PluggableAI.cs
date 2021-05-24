using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PluggableAI : MonoBehaviour
{
	public SPTownCenter TownCenter { get; set; }
	public bool IsPlaying { get; set; }
	private Priority currentPriority;
	//parameterize these
	public int townCenterDangerRadius = 3;
	//
	int prevIterationActionPoint = -1;
	public void Think()
	{
		while (!MustFinishTurn(TownCenter.actionPoint))
		{
			DeterminePriority();
			Play();
		}
		TownCenter.FinishTurn();
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
	}

	private void Play()
	{
		switch (currentPriority)
		{
			case Priority.RetakeTownCenter:
				break;
			case Priority.DefendTownCenter:
				DefendTownCenter();
				break;
		}
		Think();
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
			if(hex.OccupierUnit && hex.OccupierUnit.playerID != TownCenter.PlayerID && TownCenter.OccupiedHex.OccupierUnit == null)
			{
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