using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class UIComtroller : MonoBehaviour
{
    // Start is called before the first frame update
    public static int playerScore = 0;
    public Transform player;
    public Text score;
    public Text highScore;
    public Sprite muteSprite;
    public Sprite unmuteSprite;
    public Button mutueUnmuteButton;
    Image buttonImage;

    void Start()
    {
        int muted = PlayerPrefs.GetInt("mute",0);
        score.text = ":   0";
        highScore.text = ":   0";
        buttonImage = mutueUnmuteButton.GetComponent<Image>();
        if(muted == 0)
        {
            buttonImage.sprite = unmuteSprite;
        }
        else
        {
            buttonImage.sprite = muteSprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        playerScore = (int)player.position.x < 0 ? 0 : (int)player.position.x;
        updateScoreText();


    }

    void updateScoreText()
    {
        score.text = ":   " + playerScore.ToString(); 
    }

   public void muteUnmuteSound()
    {
        int status = PlayerPrefs.GetInt("mute", 0);
        if(status == 0)
        {
            PlayerPrefs.SetInt("mute", 1);
            audioController.muted = true;
            buttonImage.sprite = muteSprite;
        }
        else
        {
            PlayerPrefs.SetInt("mute", 0);
            audioController.muted = false;
            buttonImage.sprite = unmuteSprite;

        }
    }
}
