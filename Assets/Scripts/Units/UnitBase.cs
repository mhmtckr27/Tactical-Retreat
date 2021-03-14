using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBase : MonoBehaviour
{
	[SerializeField] private UnitType unit_type;
	[SerializeField] private int max_moves_each_turn;

	private HexagonBlockBase current_block;
	private bool is_in_move_mode = false;
	private int remaining_moves_this_turn;
	[SerializeField] private bool[] can_move_to_blocks;

	private void Awake()
	{
		//sonradan degistir daha efektif bi yol bul.
		can_move_to_blocks = new bool[System.Enum.GetValues(typeof(BlockType)).Length];
		for(int i = 0; i < can_move_to_blocks.Length; i++)
		{
			can_move_to_blocks[i] = true;
			if(i == (int)BlockType.Water)
			{
				can_move_to_blocks[i] = false;
			}
		}
		////////////////////////////////////////////

		remaining_moves_this_turn = max_moves_each_turn;
		RaycastHit hit;
		if(Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit))
		{
			current_block = hit.collider.GetComponent<HexagonBlockBase>();
		}
	}

	private void OnMouseUpAsButton()
	{
		is_in_move_mode = !is_in_move_mode;
		if (is_in_move_mode)
		{
			foreach(HexagonBlockBase neighbour in current_block.neighbours)
			{
				if (can_move_to_blocks[(int)neighbour.block_type])
				{
					neighbour.ToggleOutlineVisibility(true);
				}
			}
			current_block.ToggleOutlineVisibility(true);
		}
		else
		{
			foreach (HexagonBlockBase neighbour in current_block.neighbours)
			{
				neighbour.ToggleOutlineVisibility(false);
				current_block.ToggleOutlineVisibility(false);
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
