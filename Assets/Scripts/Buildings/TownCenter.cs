using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenter : MonoBehaviour
{
	public TerrainHexagon occupiedHex;
	public int playerID;

	private bool menu_visible = false;
	private TownCenterUI townCenterUI;
	private bool isConquered = false;

	//if player loses all players AND loses town center, he/she will lose the game.
	private List<UnitBase> units;
	public List<UnitBase> Units { get => units; set => units = value; }

	private void Awake()
	{
		Units = new List<UnitBase>();
		RaycastHit hit;
		if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out hit, .2f))
		{
			occupiedHex = hit.collider.GetComponent<TerrainHexagon>();
		}
		townCenterUI = UIController.Instance.townCenterBuildingMenu.GetComponent<TownCenterUI>();
		townCenterUI.townCenter = this;
	}
	public void ToggleBuildingMenu()
	{
		menu_visible = !menu_visible;
		townCenterUI.gameObject.SetActive(menu_visible);
	}
	public void CreateUnit(GameObject unitToCreate)
	{
		if (!occupiedHex.OccupierUnit)
		{
			UnitBase temp = Instantiate(unitToCreate, transform.position, Quaternion.identity).GetComponent<UnitBase>();
			temp.playerID = playerID;
			units.Add(temp);
		}
	}

	public void UnitDied(UnitBase unitBase)
	{
		Units.Remove(unitBase);
		if(Units.Count == 0 && isConquered)
		{
			//Game over for this player.
		}
	}
}