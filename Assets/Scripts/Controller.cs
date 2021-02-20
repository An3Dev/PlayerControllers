// Written by Andres Nedilskyj
// Date: 02/19/2021

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Player Controller Script using Unity's Character Controller
public class Controller : MonoBehaviour
{
    [SerializeField] float mouseXSensitivity = 1, mouseYSensitivity = 1;
    [SerializeField] float speed = 7;
    [SerializeField] float accelerationTime = 2f; // the higher, the longer it takes to get to full speed
    [SerializeField] float airAccelerationTime = 2.7f;

    [SerializeField] float decelerationTime = 2;
    [SerializeField] float jumpForce = 8.2f, jumpSlideDecelerationTime = 20;
    [SerializeField] float minFallVelocity = -20;

    [SerializeField] float counterStrafeMaxVelocity = 5;

    [SerializeField] Transform camera;
    [SerializeField] Transform groundChecker;
    [SerializeField] LayerMask ground;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float airDrag = 1.5f;
    CharacterController controller;

    private int lastXDir;
    private int lastZDir;

    public bool isGrounded = true;

    Vector3 localVelocity = Vector3.zero;
    Vector3 positionLastFrame;
    Vector3 worldVelocityBeforeBeingAirborne;

    Vector3 positionChange = Vector3.zero;

    float xInput, zInput;

    Vector3 moveDirection;
    bool slideFromJump = true;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        positionLastFrame = transform.position;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Look();

        Move();

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Break();
        }
    }

    public void SetGrounded(bool isGrounded)
    {
        // if was in the air last frame
        if (!this.isGrounded && isGrounded)
        {
            slideFromJump = true;
        } 

        this.isGrounded = isGrounded;
        if (!isGrounded)
        {
            worldVelocityBeforeBeingAirborne = LocalToWorldVelocity(localVelocity);
        }
    }

    private void Look()
    {
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X") * mouseXSensitivity, 0));

        camera.Rotate(new Vector3(-Input.GetAxis("Mouse Y") * mouseYSensitivity, 0, 0));

        ClampCameraXRot();
    }

    private void ClampCameraXRot()
    {
        if (camera.localRotation.x > 0.707f)
        {
            camera.localRotation = new Quaternion(0.707f, camera.localRotation.y, 0, 0.707f);
        }
        else if (camera.localRotation.x < -0.707f)
        {
            camera.localRotation = new Quaternion(-0.707f, camera.localRotation.y, 0, 0.707f);
        }
    }

    private void Move()
    {
        xInput = Input.GetAxis("Horizontal");
        zInput = Input.GetAxis("Vertical");

        // we use lastDir variables when applying deceleration
        if (xInput != 0)
        {
            lastXDir = (int)xInput;

            // we don't want the player to slide from the jump because the user is overriding the momentum with this input
            slideFromJump = false;
        }
        if (zInput != 0)
        {
            lastZDir = (int)zInput;
            slideFromJump = false;
        }

        moveDirection = new Vector3(xInput, 0, zInput).normalized;// direction relative to the player. Not in world space

        if (!isGrounded)
        {
            AirMovement(moveDirection);
        }
        else
        {
            GroundMovement(moveDirection);
        }
    }
    void AirMovement(Vector3 moveDirection)
    {
        // if there's no movement input
        if (xInput == 0 && zInput == 0)
        {
            // keep moving player in direction of its momentum, instead of relative to the player forward.
            ApplyGravity();
            controller.Move(new Vector3(worldVelocityBeforeBeingAirborne.x, localVelocity.y, worldVelocityBeforeBeingAirborne.z) * Time.deltaTime);
        }
        else
        {
            //moveDirection.x *= airStrafeSpeedMultiplier;
            //moveDirection.z *= airStrafeSpeedMultiplier;

            localVelocity += moveDirection / airAccelerationTime;

            ClampVelocity(speed);
            ApplyGravity();

            localVelocity.x /= airDrag + 1;
            localVelocity.z /= airDrag + 1;

            Vector3 worldVelocity = LocalToWorldVelocity(localVelocity);
            worldVelocityBeforeBeingAirborne = worldVelocity;

            controller.Move(worldVelocity * Time.deltaTime);
        }
    }

    private void GroundMovement(Vector3 moveDirection)
    {
        CounterStrafe();
        // applies "force"
        localVelocity += moveDirection / accelerationTime;

        ClampVelocity(speed);
        ApplyGravity();
        
        if (slideFromJump)
        {
            DecelerateWorldSpace(jumpSlideDecelerationTime);
        } else
        {
            worldVelocityBeforeBeingAirborne = Vector3.zero;
            Decelerate(decelerationTime);
        }
        if (localVelocity.y < 0)
        {
            localVelocity.y = 0;
        }
       
        bool jumped = false;
        if (Input.GetButtonDown("Jump"))
        {
            localVelocity.y = 0;
            localVelocity.y = jumpForce;

            isGrounded = false;
            jumped = true;
        }

        Vector3 worldVelocity = LocalToWorldVelocity(localVelocity);
        if (jumped)
        {
            worldVelocityBeforeBeingAirborne = worldVelocity;
        }

        controller.Move(worldVelocity * Time.deltaTime);
    }

    // Resets velocity if moving in opposite direction of input
    void CounterStrafe()
    {
        if (Mathf.Abs(localVelocity.x) <= counterStrafeMaxVelocity && (localVelocity.x > 0 && xInput < 0 || localVelocity.x < 0 && xInput > 0))
        {
            localVelocity.x = 0;
        }

        if (Mathf.Abs(localVelocity.z) <= counterStrafeMaxVelocity && (localVelocity.z > 0 && zInput < 0 || localVelocity.z < 0 && zInput > 0))
        {
            localVelocity.z = 0;
        }
    }

    Vector3 LocalToWorldVelocity(Vector3 localVel)
    {
        // forward normalized vector of the camera
        Vector3 flatForward = new Vector3(camera.forward.x, 0, camera.forward.z).normalized;
        Vector3 flatRight = new Vector3(camera.right.x, 0, camera.right.z).normalized;

        // if camera is facing straight down, then let's use its up vector as the world space direction to move forward in
        if (flatForward == Vector3.zero)
        {
            flatForward = camera.up;
        }

        return localVel.x * flatRight + localVel.z * flatForward + localVel.y * Vector3.up;
    }
    public void ApplyGravity()
    {
        localVelocity.y += gravity;
        if (localVelocity.y < minFallVelocity)
        {
            localVelocity.y = minFallVelocity;
        }
    }

    private void DecelerateWorldSpace(float decelerationTime)
    {
        //Debug.Log("World Vel: " + worldVelocityBeforeBeingAirborne + " Local Vel: " + transform.InverseTransformVector(worldVelocityBeforeBeingAirborne));
        Vector3 localVector = transform.InverseTransformVector(worldVelocityBeforeBeingAirborne);

        localVector.x = DecelerateAxis(localVector.x, jumpSlideDecelerationTime);
        localVector.z = DecelerateAxis(localVector.z, jumpSlideDecelerationTime);

        // inverse transform vector changes the velocity from world space to local space.
        localVelocity = new Vector3(localVector.x, localVelocity.y, localVector.z);
        worldVelocityBeforeBeingAirborne = transform.TransformVector(localVelocity);
    }
    
    private void Decelerate(float decelerationTime)
    {
        if (xInput == 0)
        {
            localVelocity.x = DecelerateAxis(localVelocity.x, decelerationTime);           
        }
        if (zInput == 0)
        {
            localVelocity.z = DecelerateAxis(localVelocity.z, decelerationTime);
        }
    }

    float DecelerateAxis(float value, float decelerationTime)
    {
        if (Mathf.Abs(value) < 1 / decelerationTime)
        {
            value = 0;
        }
        else
        {
            int decelerationDir = 0;
            // if moving forward currently
            if (value > 0)
            {
                decelerationDir = -1;
            }
            else if (value < 0)
            {
                decelerationDir = 1;
            }

            value += 1 / decelerationTime * decelerationDir;
        }
        return value;
    }
    void ClampVelocity(float maxSpeed)
    {
        // if velocity is greater than max speed
        if (Mathf.Abs(localVelocity.x) > maxSpeed)
        {
            // if going right, set x velocity to positive speed
            if (localVelocity.x > 0)
                localVelocity.x = maxSpeed;
            // if going left, set x velocity to negative speed
            else
                localVelocity.x = -maxSpeed;
        }
        // same as above but for z velocity
        if (Mathf.Abs(localVelocity.z) > maxSpeed)
        {
            if (localVelocity.z > 0)
                localVelocity.z = maxSpeed;
            else
                localVelocity.z = -maxSpeed;
        }
    }

    private void LateUpdate()
    {
        positionLastFrame = transform.position;
    }
}
