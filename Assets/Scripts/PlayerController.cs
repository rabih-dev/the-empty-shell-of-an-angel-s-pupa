using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;
using static UnityEngine.UI.Image;
//using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{


    [Header("Player")]
    [SerializeField] private float playerHeight;
    [SerializeField] private float playerRadius;
    private bool canInput;

    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float dashSpeed;
    [HideInInspector] public float currentMoveSpeed;
    private float lastMoveSpeed;

    private Vector3 movementInput;

    [Header("Dash")]
    [SerializeField] private float dashDuration;
    [SerializeField] private float dashForce;
    private bool isDashing;

    [Header("Jump")]
    [SerializeField] private float airMultiplier;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCutAmount;

    [Header("Glide")]
    [SerializeField] private Vector2 glidingMultiplier;

    [Header("Fall")]
    public float currentFallingMultiplier;
    [SerializeField] private float fallingMultiplier;


    [Header("Wall Slide")]
    [SerializeField] private float wallSlideFallingMultiplier;

    [Header("WallJump")]
    [SerializeField] private Vector3 wallJumpForce;
    [SerializeField] private bool isWalljumping;

    [Header("Wallrun")]
    [SerializeField] private float wallrunTime;
    [SerializeField] private float wallrunSpeed;
    [SerializeField] private bool isWallrunning;
    [SerializeField] private float wallrunCheckDistance;
    private Vector3 wallFoward;

    [Header("Keeping Momentum")]
    [SerializeField] private float smoothingMultiplier;

    [Header("Jump Buffer")]
    [Range(0f, 1f)]
    [Tooltip("It's an increment to the ground checker, that will be acapted as an jump input when it hits the ground")]
    [SerializeField] private float bufferRange;
    private bool hasBufferedJump;
    private bool isInBufferRange;

    [Header("GroundChecking")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDrag;
    private bool isGrounded;

    [Header("WallCheking")]
    [SerializeField] private LayerMask wallMask;
    Vector3 wallDirection;
    RaycastHit wallHit;
    Vector3 wallHitDirection;

    Vector3 sphereCastOrigin;
    private bool isOnWall;

    [Header("References")]
    public Rigidbody rb;
    public Transform visionLinePos;
    public Transform orientation;

    [Header("Keybinds")]
    public KeyCode jumpButton;
    public KeyCode sprintButton;


    //reading double tap inputs for dashing
    private const float DOUBLE_TAP_TIME = 0.25f;
    private float lastTapTime;
    private float lastTapDeltaTime;

    private int fowardDashTaps;
    private int backwardDashTaps;
    private int rightDashTaps;
    private int leftDashTaps;

    private int tapCount;
    private Vector3 dashDir;

    //Dialogue
    [HideInInspector]
    public bool isTalking;
    public bool canInteract;
    private GameObject objToInteract;


    public PlayerStates currentState;
    private PlayerStates lastState;
    public enum PlayerStates
    {
        walking,
        sprinting,
        dashing,
        airborne,
        wallSliding,
        walljumping,
        wallrunning,
        talking,
        gliding
    }

    void Start()
    {
        rb.freezeRotation = true;
        currentMoveSpeed = walkSpeed;
        isOnWall = false;
        canInput = true;
    }
    void Update()
    {
        Vector3 test = Vector3.Cross(new Vector3(0, 0, -1), new Vector3(0, 1, 0));
        Debug.Log(test);  // Deve retornar (1, 0, 0)

        DoubleTapToDash();
        WallChecking();
        GroundChecking();

        if (lastMoveSpeed > currentMoveSpeed)
        {
            // StartCoroutine(nameof(KeepMomentum));
        }

        WallMovement();

        SpeedCapping();
        JumpCheckings();
        StateControl();

        Interactions();

        ApplyDrag();
    }


    private void FixedUpdate()
    {
        DirectionalMovement();
        ApplyFallingMultiplier();
    }

    //handy functions, like suspending inputs for x amount of time
    #region Handy Functions
    IEnumerator SuspendInputs(float time)
    {
        canInput = false;
        yield return new WaitForSeconds(time);
        canInput = true;

    }
    #endregion Handy Functions

    //state handler and the directional movement
    #region Movement and States
    //in this function we get the directional inputs and control the speed and the variables changes upon state change
    private void StateControl()
    {
        //getting directional inputs
        if (canInput)
        {
            movementInput.x = Input.GetAxisRaw("Horizontal");
            movementInput.z = Input.GetAxisRaw("Vertical");
        }

        //setting the setting the diferent speeds and gravity forces along the states

        //TALKING
        if (isTalking)
        {
            currentMoveSpeed = 0;
            currentState = PlayerStates.talking;
        }

        //DASHING
        else if (isDashing)
        {

            lastMoveSpeed = currentMoveSpeed;
            currentMoveSpeed = dashSpeed;
            currentState = PlayerStates.dashing;
        }

        //SPRINTING
        else if (Input.GetKey(sprintButton) && isGrounded)
        {
            lastMoveSpeed = currentMoveSpeed;
            currentMoveSpeed = sprintSpeed;
            currentState = PlayerStates.sprinting;
            isOnWall = false;
        }

        //WALKING
        else if (isGrounded)
        {
            isOnWall = false;
            isWalljumping = false;
            lastMoveSpeed = currentMoveSpeed;
            currentMoveSpeed = walkSpeed;
            currentFallingMultiplier = fallingMultiplier;
            currentState = PlayerStates.walking;

        }
        //WALLJUMPING
        else if (isWalljumping)
        {
            currentState = PlayerStates.walljumping;
            currentFallingMultiplier = fallingMultiplier;
        }

        //WALL SLIDING
        else if (isOnWall && !isGrounded && !isWalljumping)
        {
            currentState = PlayerStates.wallSliding;
            currentFallingMultiplier = wallSlideFallingMultiplier;
        }

        //AIRBORNE
        else if (!isGrounded)
        {
            currentState = PlayerStates.airborne;
            currentFallingMultiplier = fallingMultiplier;
        }

        //only airborne
        if (currentState == PlayerStates.airborne)
        {

            if (Input.GetKeyUp(jumpButton))
            {
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * jumpCutAmount, rb.velocity.z);
            }

            //airborne and moving down without any wall
            if (rb.velocity.y <= 0 && !isOnWall)
            {
                currentFallingMultiplier = fallingMultiplier;

                if (Input.GetKey(jumpButton) && !isInBufferRange)
                {
                    currentFallingMultiplier = glidingMultiplier.y;
                    currentState = PlayerStates.gliding;
                }
            }

            else if (isOnWall)
            {
                currentState = PlayerStates.wallSliding;
                currentFallingMultiplier = wallSlideFallingMultiplier;
            }

        }

        //only gliding
        if (currentState == PlayerStates.gliding)
        {
            if (Input.GetKeyUp(jumpButton))
            {
                currentFallingMultiplier = glidingMultiplier.y;
            }
        }
    }
    public void DirectionalMovement()
    {
        //Rotate to match the camera;
        transform.rotation = UnityEngine.Quaternion.Euler(0, Camera.main.transform.localRotation.y, 0);

        //Walking and running
        Vector3 move = orientation.right * movementInput.x + orientation.forward * movementInput.z;

        if (isGrounded)
        {
            rb.AddForce(move.normalized * currentMoveSpeed * 10f, ForceMode.Force);
        }

        else
        {
            if (currentState == PlayerStates.gliding)
            {
                rb.AddForce(move.normalized * currentMoveSpeed * airMultiplier * glidingMultiplier.x * 10f, ForceMode.Force);
            }

            else
            {
                rb.AddForce(move.normalized * currentMoveSpeed * airMultiplier * 10f, ForceMode.Force);
            }
        }
    }
    #endregion Movement and States

    //in this region, you can find the functions that works on improving how the physics feel, not envolving any inputs
    #region PhysicsEnhancements
    private void ApplyFallingMultiplier()
    {
        if (rb.velocity.y <= 0 && !isGrounded)
        {
            rb.velocity += transform.up * Physics.gravity.y * (currentFallingMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
    private void ApplyDrag()
    {
        if (currentState == PlayerStates.walking || currentState == PlayerStates.sprinting)
        {
            rb.drag = groundDrag;
        }

        else
        {
            rb.drag = 0;
        }

    }

    private void SpeedCapping()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (horizontalVelocity.magnitude > currentMoveSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * currentMoveSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    private IEnumerator KeepMomentum()
    {

        float startTime = 0;
        float speedDifference = Mathf.Abs(lastMoveSpeed - currentMoveSpeed);
        float startingMoveSpeed = currentMoveSpeed;

        while (startTime < speedDifference)
        {
            currentMoveSpeed = Mathf.Lerp(lastMoveSpeed, currentMoveSpeed, startTime / speedDifference);
            print("suavizando..." + currentMoveSpeed);

            startTime += Time.deltaTime * smoothingMultiplier;
            yield return null;
        }


    }
    #endregion PhysicsEnhancements

    #region Jump
    void JumpCheckings()
    {
        JumpBuffer();

        if (Input.GetButtonDown("Jump") && isGrounded && !hasBufferedJump)
        {
            Jump();
        }

        else if (isGrounded && hasBufferedJump)
        {
            hasBufferedJump = false;
            Jump();
        }
    }


    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    #endregion Jump

    //in this region, you'll find things like jump buffering, coyote time, this type of stuff
    #region Input Correction
    void JumpBuffer()
    {
        isInBufferRange = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f + bufferRange, groundMask);
        if (Input.GetButtonDown("Jump") && isInBufferRange)
        {
            hasBufferedJump = true;
        }
    }
    #endregion

    //in this region, we read the double tap directional inputs in order to dash in its direction
    #region DoubleTap Inputs
    private void DoubleTapToDash()
    {
        // Atualizando o tempo do último toque
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
        {
            lastTapDeltaTime = Time.time - lastTapTime;
            lastTapTime = Time.time;
        }

        // Definindo direções e contadores de toques
        Vector3 direction = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W))
        {
            IncrementDashTaps(ref fowardDashTaps, ref backwardDashTaps,
                              ref rightDashTaps, ref leftDashTaps);
            direction = orientation.forward;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            IncrementDashTaps(ref leftDashTaps, ref fowardDashTaps,
                              ref backwardDashTaps, ref rightDashTaps);
            direction = -orientation.right;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            IncrementDashTaps(ref backwardDashTaps, ref fowardDashTaps,
                              ref rightDashTaps, ref leftDashTaps);
            direction = -orientation.forward;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            IncrementDashTaps(ref rightDashTaps, ref fowardDashTaps,
                              ref backwardDashTaps, ref leftDashTaps);
            direction = orientation.right;
        }

        // Executa o dash se as condições forem atendidas
        if (lastTapDeltaTime <= DOUBLE_TAP_TIME && GetTotalTapCount() >= 2)
        {
            if (fowardDashTaps >= 2) direction = orientation.forward;
            else if (backwardDashTaps >= 2) direction = -orientation.forward;
            else if (rightDashTaps >= 2) direction = orientation.right;
            else if (leftDashTaps >= 2) direction = -orientation.right;

            if (direction != Vector3.zero)
            {
                ResetTapCount();
                Dash(direction);
            }
        }
    }

    // Método auxiliar para incrementar os toques e resetar os outros
    private void IncrementDashTaps(ref int mainTap, ref int tap1, ref int tap2, ref int tap3)
    {
        mainTap++;
        tap1 = tap2 = tap3 = 0;
    }

    // Método auxiliar para calcular o total de toques
    private int GetTotalTapCount()
    {
        return fowardDashTaps + backwardDashTaps + rightDashTaps + leftDashTaps;
    }

    private void ResetTapCount()
    {
        fowardDashTaps = 0;
        backwardDashTaps = 0;
        rightDashTaps = 0;
        leftDashTaps = 0;
    }
    #endregion DoubleTap Inputs

    //the dash behaviour itself
    #region Dash
    private void Dash(Vector3 direction)
    {
        isDashing = true;
        rb.AddForce(direction * dashForce, ForceMode.Impulse);
        Invoke(nameof(StopDash), dashDuration);
    }

    private void StopDash()
    {
        isDashing = false;
    }
    #endregion

    //the interaction behaviour
    #region Interactions
    public void Interactions()
    {
        if (canInteract)
        {
            if (Input.GetMouseButtonDown(0) && !isTalking)
            {
                Interactible interactibleContent = objToInteract.GetComponent<Interactible>();
                Singleton.GetInstance.dialogueManager.StartDialogue(interactibleContent.dialogue);
                isTalking = true;
            }

            else if (Input.GetMouseButtonDown(0) && isTalking)
            {
                if (Singleton.GetInstance.dialogueManager.finishedSentence)
                {
                    Singleton.GetInstance.dialogueManager.NextSentence();
                }

                else
                {
                    Singleton.GetInstance.dialogueManager.SkipLetterByLetter();
                }
            }
        }
    }
    #endregion Interactions

    //checks if the player is grounded or in the wall
    #region Checking
    private void GroundChecking()
    {
        isGrounded = Physics.Raycast(transform.position, UnityEngine.Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);
    }

    private void WallChecking()
    {
        //line values calculation, need to press towards the wall
        float wallCheckerRange = playerRadius + 0.2f;
        wallDirection = orientation.right * movementInput.x + orientation.forward * movementInput.z;
        wallDirection = wallDirection.normalized;


        //if is dashing, can start a wallrun by colliding at the right angle
        if (isDashing)
        {
            if (Physics.Raycast(transform.position, orientation.forward, out wallHit, wallrunCheckDistance, wallMask))
            {
                float angle = Mathf.Abs(Vector3.Angle(orientation.forward, wallHit.normal));

                print(angle);

                if (angle >= 45 && !isWallrunning)
                {
                    //need to store wallhit in a variable in order to maintain it, because of "out wallHit" on the raycast declaration
                    isWallrunning = true;
                    Vector3 wallNormal = wallHit.normal;
                    Vector3 wallFoward = Vector3.Cross(wallNormal, transform.up);
                    print("logo após o calculo, wallfoward era: " + wallFoward);
                    StopDash();
                }
            }
        }

        //if on wall, no need to press direction to continue
        else if (isOnWall && !isWallrunning)
        {
            if (wallDirection.magnitude > 0)
            {
                wallHitDirection = wallDirection;
            }

            isOnWall = Physics.Raycast(transform.position, wallHitDirection, out wallHit, wallCheckerRange, wallMask);
        }

        //if youre not, u need do input the wall direction to start to slide;
        else if (!isWallrunning)
        {
            isOnWall = Physics.Raycast(transform.position, wallDirection, out wallHit, wallCheckerRange, wallMask);
        }

    }



    //the walljump itself
    private void Walljump()
    {
        //checks the opposite direction to the wall as well as the force to be applied
        Vector3 oppositeWallhitDirection = wallHit.normal;
        Vector3 force = new Vector3(oppositeWallhitDirection.x * wallJumpForce.x, wallJumpForce.y, oppositeWallhitDirection.z * wallJumpForce.z);

        //resets the current vertical speed and applies the force
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(force, ForceMode.Impulse);


        //reset the wallhit, detaches from the wall and suspend inputs for a brief moment, in order to not get caught on the wall again
        wallHit = new RaycastHit();
        isOnWall = false;
        StartCoroutine(SuspendInputs(0.25f));
    }

    private void WallMovement()
    {
        //WALLJUMP
        if (isOnWall && Input.GetKeyDown(jumpButton) && canInput)
        {
            Walljump();
        }

        //WALLRUN
        if (isWallrunning)
        {
            Wallrun(wallFoward);  
        }
    }

    private void Wallrun(Vector3 direction)
    {
        rb.AddForce(direction * wallrunSpeed, ForceMode.Force);

    }

    #endregion Checking

    //unity colliders functions
    #region OnTrigger and OnCollision functions
    private void OnTriggerStay(Collider other)
        {
            print(other.name);
            if (other.CompareTag("Interactible"))
            {

                objToInteract = other.gameObject;
                canInteract = true;

        }
    }

    private void OnTriggerExit(Collider other)

    {

        print(other.tag);
        if (other.CompareTag("Interactible"))
        {

            objToInteract = null;
            canInteract = false;

        }
    }
    #endregion OnTrigger and OnCollision functions

    void OnDrawGizmos()
    {
        float bufferLength = playerHeight * 0.5f + 0.2f + bufferRange;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector3.down * bufferLength);

        float groundLength = playerHeight * 0.5f + 0.2f;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, Vector3.down * groundLength);

        float wallLength = playerRadius + 0.2f;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, wallDirection * wallLength);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sphereCastOrigin, wallLength);
        
    }
}

