using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!GameManager.Instance.isPlayerWhite)
        {
            transform.RotateAround(Vector3.zero, Vector3.up, 180f);
            Debug.Log("camera flipped");
        }
    }

}
