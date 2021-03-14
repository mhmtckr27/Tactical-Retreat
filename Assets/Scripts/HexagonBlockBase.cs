using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonBlockBase : MonoBehaviour
{
	[SerializeField] public BlockType block_type;
	public List<HexagonBlockBase> neighbours;

	private Outline outline;

	private void Awake()
	{
		outline = GetComponent<Outline>();
	}

	public void ToggleOutlineVisibility(bool show_outline)
	{
		outline.enabled = show_outline;
	}
}

public enum BlockType
{
	Ground,
	Water
}