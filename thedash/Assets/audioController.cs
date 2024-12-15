using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioController : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource audio;

    public AudioClip backgroundMusic;


    public static bool muted = false;

    void Start()
    {
        audio = GetComponent<AudioSource>();
        audio.clip = backgroundMusic;
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
