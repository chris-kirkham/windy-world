using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{
    public Vector3 p0, p1;

    void Reset()
    {
        p0 = Vector3.zero;
        p1 = new Vector3(0f, 0f, 1f);
    }
}
