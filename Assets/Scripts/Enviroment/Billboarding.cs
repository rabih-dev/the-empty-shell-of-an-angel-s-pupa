using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboarding : MonoBehaviour
{
    Vector3 cameraDirection;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cameraDirection = Camera.main.transform.forward;
        cameraDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(cameraDirection);
    }
}
