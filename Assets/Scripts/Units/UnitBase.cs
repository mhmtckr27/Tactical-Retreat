using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBase : MonoBehaviour
{
	[SerializeField] private UnitType unit_type;
	[SerializeField] private int max_moves_each_turn;
	private HexagonBlockBase block_under;

	private bool is_in_move_mode = false;
	public bool Is_in_move_mode
	{
		get => is_in_move_mode;
		set
		{
			is_in_move_mode = value;
			UpdateOutlines();
		}
	}

	private int remaining_moves_this_turn;
	List<HexagonBlockBase> neighbours_within_range = new List<HexagonBlockBase>();

	public HexagonBlockBase Block_under 
	{ 
		get => block_under; 
		set 
		{
			if(block_under != null)
			{
				block_under.occupier_unit = null;
			}
			block_under = value;
			block_under.occupier_unit = this;
		} 
	}


	private void Awake()
	{
		remaining_moves_this_turn = max_moves_each_turn;
	}
	
	private void OnMouseUpAsButton()
	{
		Is_in_move_mode = !Is_in_move_mode;
		if (Is_in_move_mode)
		{
			EnterMoveMode();
		}
		else
		{
			ExitMoveMode();
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
			remaining_moves_this_turn -= Map.Instance.GetDistanceBetweenTwoBlocks(hex, Block_under);
			Debug.Log(remaining_moves_this_turn);
			Block_under = hex;
			UpdateOutlines();
			return true;
		}
	}

	private void UpdateOutlines()
	{
		DisableOutlines();
		if (Is_in_move_mode)
		{
			neighbours_within_range = Map.Instance.GetReachableHexagons(Block_under, remaining_moves_this_turn);
			EnableOutlines();
		}
	}

	public void EnableOutlines()
	{
		if (Is_in_move_mode && (remaining_moves_this_turn > 0))
		{
			foreach (HexagonBlockBase neighbour in neighbours_within_range)
			{
				neighbour.ToggleOutlineVisibility(true);
			}
		}
	}

	public void DisableOutlines()
	{
		foreach (HexagonBlockBase neighbour in neighbours_within_range)
		{
			neighbour.ToggleOutlineVisibility(false);
		}
	}

	public void EnterMoveMode()
	{
		Map.Instance.current_state = State.MoveUnitMode;
		Map.Instance.Unit_to_move = this;
	}

	public void ExitMoveMode()
	{
		Map.Instance.current_state = State.None;
		Map.Instance.Unit_to_move = null;
	}
}

public enum UnitType
{
	Peasant,
	Warrior,
	Archer
}
