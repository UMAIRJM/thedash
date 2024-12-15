using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioController : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource audio;

    public AudioClip backgroundMusic;

    int mute;

    public static bool muted;

    void Start()
    {
        mute = PlayerPrefs.GetInt("mute", 0);
        audio = GetComponent<AudioSource>();
        audio.clip = backgroundMusic;
        muted = mute == 0 ? false : true;
        if (muted)
        {
            audio.Stop();
        }
        

    }

    // Update is called once per frame
    void Update()
    {
        if (muted)
        {
            audio.Stop ();
        }
    }
}
