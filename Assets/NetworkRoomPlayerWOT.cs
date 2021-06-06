using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRoomPlayerWOT : NetworkRoomPlayer
{



	public override void OnGUI()
	{
		NetworkRoomManager room = NetworkManagerHUDWOT.manager as NetworkRoomManager;
		if (room)
		{
			if (!NetworkRoomManagerWOT.IsSceneActive(room.RoomScene))
			{
				return;
			}

			DrawPlayerReadyState();
			DrawPlayerReadyButton();
		}
	}

	public override void DrawPlayerReadyState()
	{
		base.DrawPlayerReadyState();
	}

	public override void DrawPlayerReadyButton()
	{
		base.DrawPlayerReadyButton();
	}

}
