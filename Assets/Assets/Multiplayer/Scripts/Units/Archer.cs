using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Archer : UnitBase
{
	[Header("Combat")]
	[SerializeField] private Transform arrowSpawnPoint;
	[SerializeField] private GameObject arrowProjectile;

	[Server]
	public override IEnumerator Attack(UnitBase target)
	{
		HasAttacked = true;
		remainingMovesThisTurn -= unitProperties.moveCostToAttack;

		if(remainingMovesThisTurn <= 0)
		{
			SetIsInMoveMode(false);
		}
		else
		{
			UpdateOutlinesServer();
		}

		PlayAttackEffectsRpc();

		Vector3 lookRotBegin = target.transform.position;
		Vector3 lookRotEnd = new Vector3(arrowSpawnPoint.position.x, target.transform.position.y, arrowSpawnPoint.position.z);
		Quaternion lookRot = Quaternion.LookRotation(lookRotBegin - lookRotEnd);
		yield return new WaitForSeconds(0.2f);
		GameObject arrow = Instantiate(arrowProjectile, arrowSpawnPoint.transform.position, lookRot);
		NetworkServer.Spawn(arrow);
		yield return StartCoroutine(ArrowThrowedRoutine(target, arrow.transform));
	}

	[Server]
	IEnumerator ArrowThrowedRoutine(UnitBase target, Transform arrowTransform)
	{
		Vector3 targetPos;
		Vector3 arrowPos;
		do
		{
			yield return new WaitForSeconds(0.05f);
			targetPos = target.transform.position;
			arrowPos = arrowTransform.position;
		}
		while (Vector3.Distance(new Vector3(targetPos.x, 0, targetPos.z), new Vector3(arrowPos.x, 0, arrowPos.z)) > .5f);
		OnArrowHit(target, arrowTransform.gameObject);
		yield return null;
	}

	[Server]
	public void OnArrowHit(UnitBase target, GameObject arrow)
	{
		NetworkServer.Destroy(arrow);
		bool isTargetDead = target.TakeDamage(this, unitProperties.damage);
		target.isPendingDead = isTargetDead;
		if (isTargetDead)
		{
			/*PlayDeathEffectsRpc(netIdentity.connectionToClient, target.transform.position);
			PlayDeathEffectsRpc(target.netIdentity.connectionToClient, target.transform.position);*/
			target.DisableHexagonOutlines();
			UpdateOutlinesServer();
		}
	}
}
