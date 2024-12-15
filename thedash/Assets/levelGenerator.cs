using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class levelGenerator : MonoBehaviour
{
    public Transform character;
    public Transform level1;
    public Transform endpoint;
    private float payload = 0;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        difference(character.position.x, endpoint.position.x);
    }

    void difference(float currentPosition,float endpointPosiotn)
    {
        
        float diff = (endpointPosiotn) - currentPosition;
        if (diff <= 0) {
            payload += 171;
            Vector3 moveAmount = new Vector3(endpointPosiotn, 0, 0);
            level1.position = moveAmount;
           // endpoint.position += moveAmount;

        }
    }

    
}
