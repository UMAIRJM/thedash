using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class characterControls : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "spikes")
        {
            print("collided with spike");
        //    characterMovement.playerConstantMovementFlag = false;
        }
    }
}
