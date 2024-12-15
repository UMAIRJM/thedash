using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class miniMenuController : MonoBehaviour
{
    public GameObject miniMenu;
    private characterMovement characterMovementScript;

    void Start()
    {
        characterMovementScript = FindObjectOfType<characterMovement>();
    }

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
        
        characterMovement.playerConstantMovementFlag = true;
        UIComtroller.playerScore = 0;
        SceneManager.LoadScene("MainMenu");
    }

    public void StartGame()
    {
        characterMovement.playerConstantMovementFlag = true;
        SceneManager.LoadScene("Game");
       
    }


    IEnumerator ResetCharacterAfterLoad()
    {
        yield return new WaitForSeconds(0.1f);
        characterMovementScript.ResetCharacter();
    }
    public void ResetHighScore()
    {
        PlayerPrefs.SetInt("score", 0);
    }
}
