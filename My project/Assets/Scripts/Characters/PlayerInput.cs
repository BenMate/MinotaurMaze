using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 moveInputs;

    // Mouse values
    [HideInInspector] public bool rightClicked;
    [HideInInspector] public bool isRickClicking;
    [HideInInspector] public bool leftClicked;
    [HideInInspector] public bool isLeftClicking;
    [HideInInspector] public float mouseWheel;

    // Interaction
    [HideInInspector] public bool interactPressed;

    void Update()
    {
        UpdateInputs();
    }

    void UpdateInputs()
    {
        // Movement
        moveInputs.x = Input.GetAxis("Horizontal");
        moveInputs.y = Input.GetAxis("Vertical");

        // Mouse
        mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        rightClicked = Input.GetMouseButtonDown(1);
        leftClicked = Input.GetMouseButtonDown(0);
        isLeftClicking = Input.GetMouseButton(0);

        // Interaction
        interactPressed = Input.GetKeyDown(KeyCode.E);
    }
}