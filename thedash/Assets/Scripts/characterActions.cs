using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class characterControls : MonoBehaviour
{
    public GameObject GameOverImage;
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "spikes")
        {
            int curerntScore = UIComtroller.playerScore;
            int savedScore = PlayerPrefs.GetInt("score", 0);
            if(curerntScore > savedScore)
            {
                PlayerPrefs.SetInt("score",curerntScore);
            }
            print("collided with spike");
            characterMovement.playerConstantMovementFlag = false;
            StartCoroutine(naviagteToMain());
            GameOverImage.SetActive(true);
         

        }
    }

    IEnumerator naviagteToMain()
    {
        yield return new WaitForSeconds(2);
        UIComtroller.playerScore = 0;
        characterMovement.playerConstantMovementFlag = true;
        SceneManager.LoadScene("MainMenu");
    }
}
