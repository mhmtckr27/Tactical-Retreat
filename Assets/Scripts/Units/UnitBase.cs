using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBase : MonoBehaviour
{
	[SerializeField] private UnitType unit_type;
	[SerializeField] private int max_moves_each_turn;
	public HexagonBlockBase block_under;

	private bool is_in_move_mode = false;
	private int remaining_moves_this_turn;
	List<HexagonBlockBase> neighbours_within_range = new List<HexagonBlockBase>();
	private void Awake()
	{
		remaining_moves_this_turn = max_moves_each_turn;
	}
	
	private void OnMouseUpAsButton()
	{
		is_in_move_mode = !is_in_move_mode;
		if (is_in_move_mode)
		{
			Map.Instance.current_state = State.MoveUnitMode;
			Map.Instance.unit_to_move = this;
		}
		else
		{
			Map.Instance.current_state = State.None;
			Map.Instance.unit_to_move = null;
		}
		UpdateOutlines();
	}

	public bool TryMoveTo(HexagonBlockBase hex)
	{
		if (!neighbours_within_range.Contains(hex))
		{
			return false;
		}
		else
		{
			transform.position = hex.transform.position;
			remaining_moves_this_turn -= Map.Instance.GetDistanceBetweenTwoBlocks(hex, block_under);
			Debug.Log(remaining_moves_this_turn);
			block_under = hex;
			
			UpdateOutlines();
			return true;
		}
	}

	private void UpdateOutlines()
	{
		foreach (HexagonBlockBase neighbour in neighbours_within_range)
		{
			neighbour.ToggleOutlineVisibility(false);
		}
		neighbours_within_range = Map.Instance.GetReachableHexagons(block_under, remaining_moves_this_turn);
		//neighbours_within_range = Map.Instance.GetNeighboursWithinDistance(block_under, remaining_moves_this_turn);
		if (is_in_move_mode && (remaining_moves_this_turn > 0))
		{
			foreach (HexagonBlockBase neighbour in neighbours_within_range)
			{
				neighbour.ToggleOutlineVisibility(true);
			}
		}
	}
}

public enum UnitType
{
	Peasant,
	Warrior,
	Archer
}
