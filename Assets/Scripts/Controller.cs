using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] float speed = 4;
    [SerializeField] float acceleration = 0.2f; // the time it takes to get to full speed
    [SerializeField] float deceleration;
    [SerializeField] float decelerationOffJump = 0.1f;
    [SerializeField] float jumpHeight = 2f;

    [SerializeField] Transform camera;
    [SerializeField] Transform groundChecker;
    [SerializeField] LayerMask ground;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float gravityBoost = 5f;
    [SerializeField] float airStrafeSpeedMultiplier = 0.1f;
    [SerializeField] float airDragMultiplier = 1.5f;
    CharacterController controller;

    private int lastXDir;
    private int lastZDir;

    public bool isGrounded = true;

    Vector3 velocity = Vector3.zero;
    Vector3 positionLastFrame;

    Vector3 positionChange = Vector3.zero;

    float xInput, zInput;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        positionLastFrame = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        xInput = Input.GetAxis("Horizontal");
        zInput = Input.GetAxis("Vertical");

        isGrounded = Physics.CheckSphere(groundChecker.position, 0.2f, ground, QueryTriggerInteraction.Ignore);

        Vector3 flatForward = new Vector3(camera.forward.x, 0, camera.forward.z).normalized;
        Vector3 flatRight = new Vector3(camera.right.x, 0, camera.right.z).normalized;

        Vector3 moveDirection = new Vector3(xInput, 0, zInput);// direction relative to the player. Not in world space

        // normalize the movement dir vector so that player doesn't move faster diagonally
        if (moveDirection.sqrMagnitude > 1)
        {
            moveDirection.Normalize();
        }

        // slow down speed while in the air
        if (!isGrounded)
        {
            moveDirection.x *= airStrafeSpeedMultiplier;
            moveDirection.z *= airStrafeSpeedMultiplier;
        }

        if (xInput != 0)
        {
            lastXDir = (int)xInput;
        }
        if (zInput != 0)
        {
            lastZDir = (int)zInput;
        }

        velocity += moveDirection * acceleration;

        ClampVelocity();

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Break();
        }

        velocity.y += gravity * Time.deltaTime;

        if (isGrounded)
        {
            Decelerate(flatRight, flatForward);
            if (velocity.y < 0)
            {
                velocity.y = 0;
            }
        } else
        {
            velocity.x *= airDragMultiplier;
            velocity.z *= airDragMultiplier;
            if (velocity.y < 0.01f)
            {
                velocity.y -= gravityBoost * Time.deltaTime;
            }
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = 0;
            velocity.y += Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            isGrounded = false;
        }

        Vector3 worldVelocity = velocity.x * flatRight + velocity.z * flatForward + velocity.y* Vector3.up;
        controller.Move(worldVelocity * Time.deltaTime);
    }

    private void Decelerate(Vector3 flatRight, Vector3 flatForward)
    {
        if (xInput == 0)
        {
            if (Mathf.Abs(velocity.x) < deceleration)
            {
                velocity.x = 0;
            } else
            {
                int decelerationDir = 0;
                // if moving right currently
                if (velocity.x > 0)
                {
                    // apply deceleration to the left, to make the player eventually come to a complete stop.
                    decelerationDir = -1;
                }
                else if (velocity.x < 0)
                {
                    decelerationDir = 1;
                }

                velocity += flatRight * deceleration * decelerationDir;
            }      
        }

        if (zInput == 0)
        {
            if (Mathf.Abs(velocity.z) < deceleration)
            {
                velocity.z = 0;
            } else
            {
                int decelerationDir = 0;
                // if moving forward currently
                if (velocity.z > 0)
                {
                    decelerationDir = -1;
                }
                else if (velocity.z < 0)
                {
                    decelerationDir = 1;
                }

                velocity += flatForward * deceleration * decelerationDir;
            }         
        }
    }

    void ClampVelocity()
    {
        // if velocity is greater than max speed
        if (Mathf.Abs(velocity.x) > speed)
        {
            // if going right, set x velocity to positive speed
            if (velocity.x > 0)
                velocity.x = speed;
            // if going left, set x velocity to negative speed
            else
                velocity.x = -speed;
        }
        // same as above but for z velocity
        if (Mathf.Abs(velocity.z) > speed)
        {
            if (velocity.z > 0)
                velocity.z = speed;
            else
                velocity.z = -speed;
        }
    }

    private void LateUpdate()
    {
        positionLastFrame = transform.position;
    }
}
