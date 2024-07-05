using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public GameObject shopItemPrefab;
    public GameObject upgradeItemPrefab;
    public Transform shopItemList;
    public Transform upgradeItemList;

    public TextMeshProUGUI cashText;
    public TextMeshProUGUI selectedShopName;
    public TextMeshProUGUI selectedShopButtonText;
    public Button selectedShopButton;
    public Image selectedShopImage;

    public Upgradable[] PlayerUpgrades;
    public Upgradable[] MinionUpgrade;

    bool minionSelected = false;
    Minion selectedMinion = null;
    WeaponData selectedWep = null;

    public void Start()
    {
        UpdateCashText();
        {
            GameObject shopItem = Instantiate(shopItemPrefab);
            shopItem.transform.SetParent(shopItemList, false);
            shopItem.GetComponentInChildren<TextMeshProUGUI>().text = "Player";
            shopItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedShopButton.interactable = false;
                selectedShopButtonText.text = "Can't be sold";
                selectedShopName.text = "Player";
                AddUpgradeButtons(PlayerUpgrades, "Player", true);
            });
        }

        {
            GameObject shopItem = Instantiate(shopItemPrefab);
            shopItem.transform.SetParent(shopItemList, false);
            shopItem.GetComponentInChildren<TextMeshProUGUI>().text = "Minions";
            shopItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedShopButton.interactable = false;
                selectedShopButtonText.text = "Can't be sold";
                selectedShopName.text = "Minions";
                AddUpgradeButtons(MinionUpgrade, "", true);
            });
        }
        foreach (WeaponData wep in GameManager.Instance.weapons)
        {
            GameObject shopItem = Instantiate(shopItemPrefab);
            shopItem.transform.SetParent(shopItemList, false);
            shopItem.GetComponentInChildren<TextMeshProUGUI>().text = wep.name;
            shopItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                UpdateSelected(wep);
            });
        }

        foreach (Minion min in GameManager.Instance.minions)
        {
            GameObject shopItem = Instantiate(shopItemPrefab);
            shopItem.transform.SetParent(shopItemList, false);
            shopItem.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = min.name;
            shopItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                UpdateSelected(min);
            });
        }

        selectedShopButton.onClick.AddListener(() =>
        {
            int currectcash = PlayerPrefs.GetInt("Cash");
            if (!minionSelected)
            {
                if (selectedWep != null && !(PlayerPrefs.GetInt(selectedWep.name, 0) == 1 || selectedWep.givenByDefault))
                {
                    if (selectedWep.WeaponCost <= currectcash)
                    {
                        PlayerPrefs.SetInt(selectedWep.name, 1);
                        UpdateSelected(selectedWep);
                        PlayerPrefs.SetInt("Cash", currectcash - selectedWep.WeaponCost);
                        UpdateCashText();
                    }
                }
            }
            else
            {
                if (selectedMinion != null && !(PlayerPrefs.GetInt(selectedMinion.name, 0) == 1 || selectedMinion.givenByDefault))
                {
                    if (selectedMinion.cost <= currectcash)
                    {
                        PlayerPrefs.SetInt(selectedMinion.name, 1);
                        UpdateSelected(selectedMinion);
                        PlayerPrefs.SetInt("Cash", currectcash - selectedMinion.cost);
                        UpdateCashText();
                    }
                }
            }
        });
    }

    public void UpdateCashText()
    {
        cashText.text = System.String.Format("{0:n0}", PlayerPrefs.GetInt("Cash"));
    }

    public void UpdateSelected(WeaponData wep)
    {
        minionSelected = false;
        selectedWep = wep;
        bool wepOwned = wep.givenByDefault || PlayerPrefs.GetInt(wep.name, 0) == 1;
        selectedShopButton.interactable = !wepOwned;
        selectedShopButtonText.text = wepOwned ? 
            (wep.CanBeSold ? "Sell ($" + selectedWep.WeaponCost / 2 + ")" : "Can't be sold") :
            "Buy ($" + selectedWep.WeaponCost + ")";
        selectedShopName.text = wep.name;
        AddUpgradeButtons(wep.upgrades, wep.name, wepOwned);
    }

    public void UpdateSelected(Minion minion)
    {
        minionSelected = true;
        selectedMinion = minion;
        bool minOwned = PlayerPrefs.GetInt(selectedMinion.name, 0) == 1 || selectedMinion.givenByDefault;
        selectedShopButton.interactable = !minOwned;
        selectedShopButtonText.text = minOwned ?
            (minion.CanBeSold ? "Sell ($" + selectedMinion.cost / 2 + ")" : "Can't be sold") :
            "Buy ($" + selectedMinion.cost + ")";
        selectedShopName.text = selectedMinion.name;
        AddUpgradeButtons(minion.upgrades, minion.name, minOwned);
    }

    public void AddUpgradeButtons(Upgradable[] upgrades, string name, bool owned)
    {
        ClearUpgradeList();
        foreach (Upgradable upgrade in upgrades)
        {
            GameObject upgradeItem = Instantiate(upgradeItemPrefab);
            upgradeItem.GetComponent<Button>().interactable = owned;
            TextMeshProUGUI text = upgradeItem.GetComponentInChildren<TextMeshProUGUI>();
            upgradeItem.transform.SetParent(upgradeItemList, false);
            float currentval = PlayerPrefs.GetFloat(name + upgrade.name, upgrade.defaultValue);
            text.text = upgrade.name + " " + (upgrade.Increment < 0 ? "-" + upgrade.Increment : "+" + upgrade.Increment) + "\n" +
                (upgrade.maxValue == 0  || currentval < upgrade.maxValue ? "Cost: $" + GetPrice(upgrade, currentval) : "Maxed");
            upgradeItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                float value = PlayerPrefs.GetFloat(name + upgrade.name, upgrade.defaultValue);
                int price = GetPrice(upgrade, value);
                int currentCash = PlayerPrefs.GetInt("Cash", 0);
                if (price <= currentCash && (value < upgrade.maxValue || upgrade.maxValue == 0))
                {
                    PlayerPrefs.SetInt("Cash", currentCash - price);
                    PlayerPrefs.SetFloat(name + upgrade.name, value + upgrade.Increment);
                    text.text = upgrade.name + " " + (upgrade.Increment < 0 ? "-" + upgrade.Increment : "+" + upgrade.Increment) + "\n" +
                        (upgrade.maxValue == 0 || value + upgrade.Increment < upgrade.maxValue ? "Cost: $" + GetPrice(upgrade, value + upgrade.Increment) : "Maxed");
                    UpdateCashText();
                }
            });
        }
    }

    public int GetPrice(Upgradable upgrade, float value)
    {
        return (int)(
            ((value - upgrade.defaultValue) /
            upgrade.Increment * upgrade.startPrice * upgrade.priceMultiplier)
            +upgrade.startPrice);
    }

    public void ClearUpgradeList()
    {
        foreach (Transform transform in upgradeItemList.GetComponentsInChildren<Transform>(true))
            if (transform.gameObject != null && transform!=upgradeItemList)
                Destroy(transform.gameObject);
        upgradeItemList.DetachChildren();
    }
}
