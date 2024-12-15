using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class miniMenuController : MonoBehaviour
{
    public GameObject miniMenu;
    

    public void pauseButtonController()
    {
        Time.timeScale = 0f;
        miniMenu.SetActive(true);
    }

    public void resumeTheGame()
    {
        Time.timeScale = 1f;
        miniMenu.SetActive(false);
    }

    public void exitTheGame()
    {
        Application.Quit();
    }

    public void navigatetoMainMenu()
    {
        UIComtroller.playerScore = 0;
        SceneManager.LoadScene("MainMenu");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }
    public void ResetHighScore()
    {
        PlayerPrefs.SetInt("score", 0);
    }
}
