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
    [SerializeField] private Text collectButtonText;
    public UIManager uiManager;
	private void OnEnable()
	{
        if(currentResource == null) { return; }
        terrainIcon.sprite = currentResource.obtainedFromTerrainIcon;
        terrainName.text = currentResource.obtainedFromTerrainType.ToString();
        description.text = currentResource.description;

		if (currentResource.canBeCollected)
		{
            resourceIcon.sprite = currentResource.resourceIcon;
            resourceCount.text = currentResource.resourceCount.ToString();
            costIcon.sprite = currentResource.costIcon;
            costCount.text = currentResource.costToCollect.ToString();
            collectButtonText.text = currentResource.collectText;
		}
        resourceIcon.transform.parent.gameObject.SetActive(currentResource.canBeCollected);
	}

    public void OnCloseButton()
	{
        uiManager.townCenterUI.townCenter.ClearSelectedHexagonCmd();
        SetEnable(-1, false);
	}

    public void OnCollectButton()
	{
        uiManager.townCenterUI.townCenter.UpdateResourceCountCmd(currentResource.resourceType, currentResource.resourceCount, currentResource.costToCollect);
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
