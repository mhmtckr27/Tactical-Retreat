using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenter : MonoBehaviour
{
	public TerrainHexagon occupiedHex;
	public int teamID;

	private bool menu_visible = false;
	private TownCenterUI townCenterUI;

	//if player loses all players AND loses town center, he/she will lose the game.
	private List<UnitBase> units;
	public List<UnitBase> Units { get => units; set => units = value; }

	private void Awake()
	{
		Units = new List<UnitBase>();
		occupiedHex = transform.parent.GetComponent<TerrainHexagon>();
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
			units.Add(Instantiate(unitToCreate, transform.position, Quaternion.identity).GetComponent<UnitBase>());
		}
	}
}