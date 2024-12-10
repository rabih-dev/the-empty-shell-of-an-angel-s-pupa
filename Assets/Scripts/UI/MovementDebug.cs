using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MovementDebug : MonoBehaviour
{
    public Text playerVelXTXT;
    public Text playerVelYTXT;
    public Text gravityTXT;
    public Text stateTXT;

    public Rigidbody playerRB;
    public PlayerController playerController;

    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        playerVelXTXT.text = "X: ";
        playerVelXTXT.text += playerRB.velocity.x;

        playerVelYTXT.text = "Y: ";
        playerVelYTXT.text += playerRB.velocity.y;

        gravityTXT.text = "Gravity: ";
        gravityTXT.text += playerController.currentFallingMultiplier;

        stateTXT.text = "State: ";
        stateTXT.text += playerController.currentState;

    }
}
