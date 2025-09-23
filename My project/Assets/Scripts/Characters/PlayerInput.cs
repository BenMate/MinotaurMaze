using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 moveInputs;
    
    //mouse values
    [HideInInspector] public bool rightClicked;
    [HideInInspector] public bool isRickClicking;
    [HideInInspector] public bool leftClicked;
    [HideInInspector] public bool isLeftClicking;
    [HideInInspector] public float mouseWheel;

    void Update()
    {
        UpdateInputs();
    }

    void UpdateInputs()
    {
        //Mouse
        moveInputs.x = Input.GetAxis("Horizontal");
        moveInputs.y = Input.GetAxis("Vertical");
        mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        rightClicked = Input.GetMouseButtonDown(1);
        leftClicked = Input.GetMouseButtonDown(0);
        isLeftClicking = Input.GetMouseButton(0);

        //Mouse
        moveInputs.x = Input.GetAxis("Horizontal");
        moveInputs.y = Input.GetAxis("Vertical");
    }
}
