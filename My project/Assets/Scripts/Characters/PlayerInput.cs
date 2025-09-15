using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 moveInputs;

    void Update()
    {
        UpdateInputs();
    }

    void UpdateInputs()
    {
        //Mouse
        moveInputs.x = Input.GetAxis("Horizontal");
        moveInputs.y = Input.GetAxis("Vertical");
    }
}
