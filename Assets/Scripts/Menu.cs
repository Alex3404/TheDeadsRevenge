using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class Menu : MonoBehaviour
    {
        public bool isMain = false;
        public bool CanGoToPrevious = false;
        public Menu parentMenu;
        public MenuManager menuManager;
        public GameObject firstSelected;

        public virtual void onMenuClose()
        {

        }

        public virtual void onMenuOpen()
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
}
