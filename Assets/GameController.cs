using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
	public TownCenter[] teams;
	public int playerID;

	private static GameController instance;
	public static GameController Instance
	{
		get
		{
			return instance;
		}
	}

	private void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}
		else if(instance != this)
		{
			Destroy(gameObject);
		}
		teams = new TownCenter[1];
	}

	private void Start()
	{
		//teams[0] = UIController.Instance.townCenterBuildingMenu.GetComponent<TownCenterUI>().townCenter;
		teams = FindObjectsOfType<TownCenter>();
		for(int i = 0; i < teams.Length; i++)
		{
			teams[i].playerID = i;
		}
	}
	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			RaycastHit hit;
			if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
			{
				UnitBase temp = hit.collider.GetComponent<UnitBase>();
				if(temp != null)
				{
					if(Map.Instance.UnitToMove == null)
					{
						if(playerID == temp.playerID)
						{
							temp.IsInMoveMode = true;
						}
					}
					else if(Map.Instance.UnitToMove != temp)
					{
						if(playerID == temp.playerID)
						{
							Map.Instance.UnitToMove.IsInMoveMode = false;
							temp.IsInMoveMode = true;
						}
						else
						{
							Map.Instance.UnitToMove.Attack(temp);
						}
					}
					else
					{
						Map.Instance.UnitToMove.IsInMoveMode = false;
					}
				}
				else
				{
					TownCenter temp2 = hit.collider.GetComponent<TownCenter>();
					if (temp2 != null)
					{
					Debug.Log("var");
						if (Map.Instance.currentState != State.UnitAction)
						{
							temp2.ToggleBuildingMenu();
						}
						else
						{
							if(Map.Instance.UnitToMove.playerID == temp2.playerID)
							{
								Map.Instance.UnitToMove.TryMoveTo(temp2.occupiedHex);
							}
							else
							{
								if(temp2.occupiedHex.OccupierUnit != null)
								{
									if (temp2.occupiedHex.OccupierUnit.playerID == Map.Instance.UnitToMove.playerID)
									{
										Map.Instance.UnitToMove.IsInMoveMode = false;
										temp2.occupiedHex.OccupierUnit.IsInMoveMode = true;
									}
									else
									{
										Map.Instance.UnitToMove.Attack(temp2.occupiedHex.OccupierUnit);
									}
								}
								else
								{
									//try to occupy the building.
								}
							}
						}
					}
				}
			}
		}

		if (Map.Instance.UnitToMove != null && Input.GetMouseButtonDown(0))
		{
			RaycastHit hit;
			if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
			{
				UnitBase temp = hit.collider.GetComponent<UnitBase>();
				if (temp != null && Map.Instance.UnitToMove.playerID != temp.playerID)
				{
					Debug.Log("saldirildi");
				}
			}
		}
	}
}
