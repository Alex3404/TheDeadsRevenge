using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public bool active = true;
    public Menu[] menus;
    public Menu currentMenu;
    public bool onMainMenu = true;

    public void Start()
    {
        foreach (Menu menu in menus)
        {
            menu.menuManager = this;
            if (menu.isMain)
            {
                currentMenu = menu;
                menu.gameObject.SetActive(true);
            }
        }
    }
    public void OpenMenu(string name)
    {
        foreach (Menu menu in menus)
        {
            menu.gameObject.SetActive(false);
            if (menu.gameObject.name == name)
            {
                if(currentMenu!=null)
                    currentMenu.onMenuClose();
                currentMenu = menu;
                onMainMenu = menu.isMain;
                menu.gameObject.SetActive(true);
                menu.onMenuOpen();
            }
        }
    }

    public void GoBack()
    {
        if(currentMenu.CanGoToPrevious && currentMenu.parentMenu != null)
            OpenMenu(currentMenu.parentMenu.gameObject.name);
    }

    public void Update()
    {
        if (active)
        {
            if (Input.GetAxisRaw("Cancel") == 1 && currentMenu.CanGoToPrevious && currentMenu.parentMenu != null)
                OpenMenu(currentMenu.parentMenu.gameObject.name);
        }
    }
}
