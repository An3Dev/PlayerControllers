using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] float speed = 4;
    [SerializeField] float acceleration = 0.2f; // the time it takes to get to full speed
    [SerializeField] float deceleration;
    [SerializeField] float jumpHeight = 2f;

    [SerializeField] Transform camera;
    [SerializeField] Transform groundChecker;
    [SerializeField] LayerMask ground;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float airStrafeSpeedMultiplier = 0.1f;
    [SerializeField] float airDragMultiplier = 1.5f;
    CharacterController controller;


    public bool isGrounded = true;

    Vector3 velocity = Vector3.zero;
    Vector3 positionLastFrame;

    Vector3 positionChange = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        positionLastFrame = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        isGrounded = Physics.CheckSphere(groundChecker.position, 0.2f, ground, QueryTriggerInteraction.Ignore);
        
        Vector3 moveDirection = xInput * camera.right + zInput * camera.forward;

        if (!isGrounded)
        {
            moveDirection *= airStrafeSpeedMultiplier;
        }

        velocity += moveDirection * speed;

        if ((velocity - new Vector3(0, velocity.y, 0)).magnitude > 0.01f)
        {
            if (xInput == 0 && )
            {
                velocity -= speed * transform.right * deceleration;
            }
            else if (zInput == 0)
            {
                velocity -= speed * transform.forward * deceleration;
            }
        }     

        if (isGrounded)
        { 
            if (velocity.y < 0)
            {
                velocity.y = 0;
            }
        }

        velocity.y += gravity * Time.deltaTime;

        if (!isGrounded)
        {
            velocity.x *= airDragMultiplier;
            velocity.z *= airDragMultiplier;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = 0;
            velocity.y += Mathf.Sqrt(jumpHeight * -2f * gravity);

            isGrounded = false;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void LateUpdate()
    {
        positionLastFrame = transform.position;
    }
}
