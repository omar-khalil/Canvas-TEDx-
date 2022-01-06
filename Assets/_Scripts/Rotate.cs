using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour {

    public float rotateSpeed;

    Quaternion targetRotation;

    private void Start()
    {
        targetRotation = transform.rotation;

    }

    // Update is called once per frame
    void Update () {
        //transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, targetRotation, Time.deltaTime * rotateSpeed);
        transform.localRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
	}

    public void RotateButton(bool left)
    {
        float y = targetRotation.eulerAngles.y;
        y += left ? 90 : -90;
        targetRotation = Quaternion.Euler(transform.eulerAngles.x, y, transform.eulerAngles.z);
    }
}
