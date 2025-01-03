using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class characterMovement : MonoBehaviour
{
    public Transform cameraTransform;
    public Rigidbody2D rb;
    
    private float playerSpeed = 10f;
    public float jumpForce = 10f;
    private bool isJumping = false;
    private float jumpStartRotation;
    private float jumpTime = 1f; // Time in seconds to complete one spin
    private float jumpTimer;
    public static bool playerConstantMovementFlag = true;
    public int tempScore = 0;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        FollowPlayerWithCamera();
        jumpStartRotation = transform.rotation.eulerAngles.z;
        


    }

    
    void Update()
    {
        playerConstantMovement();
        FollowPlayerWithCamera();
        HandleJump();
        UpdateRotation();
        if((UIComtroller.playerScore - tempScore) >=200)
        {
            tempScore = UIComtroller.playerScore;
            playerSpeed += 4f;
        }

    }
    private void HandleJump()
    {
        if (Input.GetMouseButton(0) && Mathf.Abs(rb.velocity.y) < 0.15f)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = true;
            
            jumpTimer = 0f;
            rb.constraints = RigidbodyConstraints2D.None;
        }
    }
    public void playerConstantMovement()
    {
        
       if (playerConstantMovementFlag)
        {
            print("I am inside player contant movement flag" + playerConstantMovementFlag);
          //  Vector2 veocity = new Vector2(playerSpeed, rb.velocity.y);
           // rb.velocity = veocity;
            rb.velocity = new Vector2(playerSpeed, rb.velocity.y);
        }
       
    }


    public void FollowPlayerWithCamera()
    {
        Vector3 cameraPOsition = cameraTransform.position;
        cameraPOsition.x  = transform.position.x + 6;
        cameraTransform.position = cameraPOsition;
    }

    private void UpdateRotation()
    {
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float progress = jumpTimer / jumpTime;

            // Calculate the rotation during the jump
            float newRotation = jumpStartRotation - 360f * progress;
            transform.rotation = Quaternion.Euler(0, 0, newRotation);

            // Check if the jump is complete and the character is back on the ground
            if (Mathf.Abs(rb.velocity.y) < 0.19f)
            {
                isJumping = false;
                transform.rotation = Quaternion.Euler(0, 0, jumpStartRotation);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    public void ResetCharacter()
    {
        rb.velocity = Vector2.zero;
        isJumping = false;
        transform.rotation = Quaternion.Euler(0, 0, jumpStartRotation);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
}
