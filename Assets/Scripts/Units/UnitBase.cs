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
	private void Awake()
	{

		remaining_moves_this_turn = max_moves_each_turn;
		remaining_moves_this_turn = 1;
	}
	
	private void OnMouseUpAsButton()
	{
		is_in_move_mode = !is_in_move_mode;
		List<HexagonBlockBase> neighbours = Map.Instance.GetNeighboursWithinDistance(block_under, 3);
		if (is_in_move_mode)
		{
			foreach(HexagonBlockBase neighbour in neighbours)
			{
				neighbour.ToggleOutlineVisibility(true);
			}
		}
		else
		{
			foreach (HexagonBlockBase neighbour in neighbours)
			{
				neighbour.ToggleOutlineVisibility(false);
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
