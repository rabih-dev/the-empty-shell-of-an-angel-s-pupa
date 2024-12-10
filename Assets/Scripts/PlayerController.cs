using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;
//using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{


    [Header("Player Values")]
    [SerializeField]private float playerHeight;

    [Header("Movement Values")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [HideInInspector] public float currentMoveSpeed;

    private UnityEngine.Vector3 movementInput;

    [Header("Jumping and Gliding Values")]
    public float currentFallingMultiplier;
    [SerializeField] private Vector2 glidingMultiplier;
    [SerializeField] private float fallingMultiplier;

    [SerializeField] private float airMultiplier;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCutAmount;

    private Vector3 playerVelocity;

    [Header("Jump Buffer")]
    [Range(0f, 5f)]
    [SerializeField] private float bufferRange;
    private bool hasBufferedJump;
    
   

    [Header("GroundChecking")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDrag;
    private bool isGrounded;

    [Header("References")]
    public Rigidbody rb;
    public Transform visionLinePos;
    public Transform orientation;

    [Header("Keybinds")]
    public KeyCode jumpButton;
    public KeyCode sprintButton;

    //Dash
    [SerializeField] private float dashForce;

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
    public enum PlayerStates
    {
    walking,
    sprinting,
    airborne,
    talking,
    gliding
    }

    void Start()
    {
        rb.freezeRotation = true;
        currentMoveSpeed = walkSpeed;
    }
    void Update()
    {
        GroundChecking();
        DragControl();
        JumpCheckings();

        StateControl();
        SpeedCapping();

        Interactions();
        LockToTalk();

        Dash();
    }


    private void FixedUpdate()
    {

        FallingControl();
        Movement();
    }

    private void StateControl()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.z = Input.GetAxisRaw("Vertical");


        if (isGrounded)
        {

            currentMoveSpeed = walkSpeed;
            currentState = PlayerStates.walking;
        }

       else if (Input.GetKey(sprintButton) && isGrounded)
        {
            currentMoveSpeed = sprintSpeed;
            currentState = PlayerStates.sprinting;
        }

        else if (!isGrounded)
        {
            currentState = PlayerStates.airborne;
        }

        //only airborne
        if (currentState == PlayerStates.airborne)
        {
            //airborne and moving down
            if (rb.velocity.y <= 0)
            {
                currentFallingMultiplier = fallingMultiplier;

                if (Input.GetKey(jumpButton))
                {
                    currentFallingMultiplier = glidingMultiplier.y;
                    currentState = PlayerStates.gliding;
                }
            }

            if (rb.velocity.y > 0)
            {
                if (Input.GetKeyUp(jumpButton))
                {
                    rb.velocity = new Vector3(rb.velocity.x,  rb.velocity.y * jumpCutAmount, rb.velocity.z);
                }
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

    private void FallingControl()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += transform.up * Physics.gravity.y *  (currentFallingMultiplier  -1) * Time.fixedDeltaTime;
        }
    }

    public void Movement()
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

    void JumpCheckings()
    {
        JumpBuffer();
    
        if (Input.GetButtonDown("Jump") && isGrounded)
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

    void JumpBuffer()
    {
        bool isInBufferRange = Physics2D.Raycast(transform.position, Vector3.down ,playerHeight + bufferRange ,groundMask);
        if (Input.GetButtonDown("Jump") && isInBufferRange )
        {
            hasBufferedJump = true;
        }
    }

    private void Dash()
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
                rb.AddForce(direction * dashForce, ForceMode.Impulse);
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


    #region Checkings and Clamps

    private void GroundChecking()
    {
        isGrounded = Physics.Raycast(transform.position, UnityEngine.Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);
    }

    private void DragControl()
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

        if (horizontalVelocity.magnitude > 30)
        {
            print("a movespeed é: " + currentMoveSpeed);
            Vector3 limitedVelocity = horizontalVelocity.normalized * currentMoveSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    public void LockToTalk()
    {
        if (isTalking)
        {
            currentMoveSpeed = 0;

        }

        else
        {
            currentMoveSpeed = walkSpeed;

        }
    }

    #endregion Checkings and Clamps


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

        void OnDrawGizmos()
        {

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector3.down * playerHeight * 0.5f * 0.2f * bufferRange);

        Gizmos.color = Color.magenta;
           Gizmos.DrawRay(transform.position,Vector3.down * (bufferRange + playerHeight));
        }
    } 

