using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TerrainHexagonUI : MonoBehaviour
{
    public List<Resource> resources;
    private Resource currentResource;
    [SerializeField] private Image terrainIcon;
    [SerializeField] private Text terrainName;
    [SerializeField] private Text description;
    [SerializeField] private Image resourceIcon;
    [SerializeField] private Text resourceCount;
    [SerializeField] private Image costIcon;
    [SerializeField] private Text costCount;
    [SerializeField] private Button collectButton;
    [SerializeField] private Text collectButtonText;
    [SerializeField] private GameObject nextTurnButton;
    [SerializeField] private GameObject bottomBar; //disable it for terrains that does not contain any collectibles.
    [SerializeField] private Color enoughAPToCollectColor;
    [SerializeField] private Color notEnoughAPToCollectColor;
    public UIManager uiManager;

	private void OnEnable()
	{
        if(currentResource == null) { return; }
        terrainIcon.sprite = currentResource.obtainedFromTerrainIcon;
        terrainName.text = currentResource.obtainedFromTerrainName;
        description.text = currentResource.description;

		if (currentResource.canBeCollected)
		{
            resourceIcon.sprite = currentResource.resourceIcon;
            resourceCount.text = currentResource.resourceCount.ToString();
            costIcon.sprite = currentResource.costIcon;
            costCount.text = currentResource.costToCollect.ToString();
            costCount.color = currentResource.costToCollect <= uiManager.townCenterUI.townCenter.actionPoint ?
                    enoughAPToCollectColor :
                    notEnoughAPToCollectColor;
            collectButtonText.text = currentResource.collectText;
            collectButton.interactable = currentResource.costToCollect <= uiManager.townCenterUI.townCenter.actionPoint;
        }
        bottomBar.SetActive(currentResource.canBeCollected);
        nextTurnButton.SetActive(false);
    }

    private void OnDisable()
    {
        nextTurnButton.SetActive(true);
    }

    public void OnCloseButton()
	{
        uiManager.townCenterUI.townCenter.ClearSelectedHexagonCmd();
        SetEnable(-1, false);
	}

    public void OnCollectButton()
	{
        //uiManager.townCenterUI.townCenter.UpdateResourceCountCmd(currentResource.resourceType, currentResource.resourceCount, currentResource.costToCollect);
        uiManager.townCenterUI.townCenter.CollectResource(currentResource);
        OnCloseButton();
	}

    public void SetEnable(int terrainType, bool enable)
	{
        SetCurrentResource(terrainType);
        gameObject.SetActive(enable);
	}

    public void SetCurrentResource(int terrainType)
	{
        foreach(Resource resource in resources)
		{
            if (resource.obtainedFromTerrainType == (TerrainType)terrainType)
			{
                currentResource = resource;
                return;
			}
		}
        currentResource = null;
        return;
	}
}
