using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private Vector2 hvAxis;
    private float jump;

    private float mouseX = 0f;
    private float mouseY = 0f;

    public Vector2 GetHVAxis()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        return new Vector2(horizontal, vertical);
    }

    public Vector2 GetMouseAxis()
    {
        mouseX += Input.GetAxis("Mouse X");
        mouseY -= Input.GetAxis("Mouse Y");
        return new Vector2(mouseX, mouseY);
    }

    public Vector2 GetMouseAxisChange()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    public bool GetJump()
    {
        return Input.GetKey(KeyCode.Space);
    }



}
