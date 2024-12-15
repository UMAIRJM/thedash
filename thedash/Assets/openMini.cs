using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class openMini : MonoBehaviour
{
    public GameObject miniMenu;
    public void mainMenuMiniMenu()
    {
        miniMenu.SetActive(true);
    }
    public void closeMiniMenu()
    {
        miniMenu.SetActive(false);
    }
}
