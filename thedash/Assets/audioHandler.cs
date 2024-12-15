using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class audioHandler : MonoBehaviour
{
    public Sprite muteSprite;
    public Sprite unmuteSprite;
    public Button mutueUnmuteButton;
    Image buttonImage;
    audioController ac;

    void Start()
    {
        int muted = PlayerPrefs.GetInt("mute", 0);
        ac = FindAnyObjectByType<audioController>();
        buttonImage = mutueUnmuteButton.GetComponent<Image>();
        if (muted == 0)
        {
            buttonImage.sprite = unmuteSprite;
        }
        else
        {
            buttonImage.sprite = muteSprite;
        }
    }
    public void muteUnmuteSound()
    {
        int status = PlayerPrefs.GetInt("mute", 0);
        print("status is : " + status);
        if (status == 0)
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
        PlayerPrefs.Save();
        ac.muteUmuteAudio();
    }
}
