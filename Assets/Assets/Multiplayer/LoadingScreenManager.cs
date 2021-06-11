using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenManager : NetworkBehaviour
{
	[SerializeField] public Text statusText;
	[SerializeField] public Image progressBarImage;

	[SyncVar] float progress;

	public override void OnStartClient()
	{
		base.OnStartClient();
		StartCoroutine(UpdateProgressRoutine());
	}

	public IEnumerator UpdateProgressRoutine()
	{
		while(progress < 1)
		{
			yield return new WaitForSeconds(0.01f);
			Debug.LogError(progress);
			GetMapGenerationProgress();
		}
		while(progressBarImage.fillAmount < 1)
		{
			yield return new WaitForSeconds(0.01f);
			progressBarImage.fillAmount += 0.05f;
		}
		if(isServer)
		OnlineGameManager.Instance.StartGame();
		Destroy(gameObject);
	}

	public void GetMapGenerationProgress()
	{
		progress = (float)(Map.Instance.currentlyInstantiatedTerrainHexagonCount) / (Map.Instance.totalTerrainHexagonCount * 2);
	}
}
