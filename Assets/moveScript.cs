using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveScript : MonoBehaviour {

	void Update()
    {
        if(Input.GetKey(KeyCode.W))
        {
            GetComponent<CharacterController>().Move(Vector3.forward * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            GetComponent<CharacterController>().Move(Vector3.back * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            GetComponent<CharacterController>().Move(Vector3.left * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            GetComponent<CharacterController>().Move(Vector3.right * Time.deltaTime);
        }
    }
}
