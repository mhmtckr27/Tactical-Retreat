using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPArcher : SPUnitBase
{
	[Header("Combat")]
	[SerializeField] private Transform arrowSpawnPoint;
	[SerializeField] private GameObject arrowProjectile;

	public override void Attack(SPUnitBase target)
	{
		HasAttacked = true;
		remainingMovesThisTurn -= unitProperties.moveCostToAttack;

		if (remainingMovesThisTurn <= 0)
		{
			SetIsInMoveMode(false);
		}
		else
		{
			UpdateOutlines();
		}

		Vector3 lookRotBegin = target.transform.position;
		Vector3 lookRotEnd = new Vector3(arrowSpawnPoint.position.x, target.transform.position.y, arrowSpawnPoint.position.z);
		Quaternion lookRot = Quaternion.LookRotation(lookRotBegin - lookRotEnd);
		GameObject arrow = Instantiate(arrowProjectile, arrowSpawnPoint.transform.position, lookRot);

		StartCoroutine(ArrowThrowedRoutine(target, arrow.transform));
	}

	IEnumerator ArrowThrowedRoutine(SPUnitBase target, Transform arrowTransform)
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

	public void OnArrowHit(SPUnitBase target, GameObject arrow)
	{
		Destroy(arrow);
		bool isTargetDead = target.TakeDamage(unitProperties.damage);
		if (isTargetDead)
		{
			PlayDeathEffects(target.transform.position);
			UpdateOutlines();
		}
	}
}
