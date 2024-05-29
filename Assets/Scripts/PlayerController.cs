using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Values")]
    [SerializeField] private float walkSpeed;
    [HideInInspector] public float currentWalkSpeed;
    [SerializeField] private float turnSpeed;

    private UnityEngine.Vector3 playerMovementFoward;
    private UnityEngine.Vector3 playerMovementSideways;

    private CharacterController cc;

    //Dialogue
    [HideInInspector]
    public bool isTalking;
    public bool canInteract;
    private GameObject objToInteract;


    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        currentWalkSpeed = walkSpeed;

    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Interactions();
        LockToTalk();
    }

    public void Movement()
    {


        playerMovementFoward = transform.forward * Input.GetAxisRaw("Vertical") * currentWalkSpeed * Time.deltaTime;

        playerMovementSideways = transform.right * Input.GetAxisRaw("Horizontal") * currentWalkSpeed * Time.deltaTime;

        //Applying gravity
        //playerMovement.y -= playerMovement.y * Physics.gravity.y * Time.deltaTime;

        cc.Move(playerMovementFoward + playerMovementSideways);
        transform.Rotate(UnityEngine.Vector3.up, Input.GetAxisRaw("Mouse X") * turnSpeed * Time.deltaTime, 0);
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

    public void LockToTalk()
    {
        if (isTalking)
        {
            currentWalkSpeed = 0;
        }

        else
        {
            currentWalkSpeed = walkSpeed;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        print(other.name);
        if (other.CompareTag("Interactible"))
        {

            objToInteract = other.gameObject;
            canInteract = true;

        }
    }

    private void OnTriggerExtit(Collider other)
    {
        
        if (other.CompareTag("Interactible"))
        {

            objToInteract = null;
            canInteract = false;

        }
    }
}
