using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    [Header("Camera")]
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private float minVerticalAngle;
    [SerializeField] private float maxVerticalAngle;

    [Header("References")]
    [SerializeField] private Transform playerEyesPosition;
    [SerializeField] private Transform orientation;
    private Vector2 mousePosition;



    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
       
    }

    // Update is called once per frame
    void Update()
    {
        // Looking around
        mousePosition.x += Input.GetAxisRaw("Mouse X");
        mousePosition.y += Input.GetAxisRaw("Mouse Y");

        transform.position = playerEyesPosition.position;

        transform.localRotation = Quaternion.Euler(Mathf.Clamp(-mousePosition.y, minVerticalAngle, maxVerticalAngle), mousePosition.x, 0);
        orientation.rotation = Quaternion.Euler(0, mousePosition.x, 0);
    }
}

