using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonBlockBase : MonoBehaviour
{
	[SerializeField] public BlockType block_type;
}

public enum BlockType
{
	Ground,
	Water
}