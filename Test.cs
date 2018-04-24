using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ConvexData cd = new ConvexData();
        cd.Verts.Add(new Vector3(0, 0, 0));
        cd.Verts.Add(new Vector3(0, 0, 1));
        cd.Verts.Add(new Vector3(0, 1, 0));
        cd.Verts.Add(new Vector3(0, 1, 1));
        cd.Verts.Add(new Vector3(1, 0, 0));
        cd.Verts.Add(new Vector3(1, 0, 1));
        cd.Verts.Add(new Vector3(1, 1, 0));
        cd.Verts.Add(new Vector3(1, 1, 1));

        CAPSULE cap;
        cap.Center = new Vector3(-1f, 0, 0);
        cap.HalfLen = 0.5f;
        cap.Radius = 0.2f;

        float lambda = 0;
        Vector3 normal = Vector3.zero;
        Vector3 delta = new Vector3(1, 0, 0);
        bool ret = GJKRaycast._gjkLocalRayCast(cap, cd,  delta, ref lambda, ref normal);
        if(ret)
        {
            Debug.Log("Success!" + lambda);
            Debug.Log("Success!" + normal);
        }

        cap.Center = cap.Center + lambda * delta;
        delta = new Vector3(0, -1, 0);
        ret = GJKRaycast._gjkLocalRayCast(cap, cd, delta, ref lambda, ref normal);
        if (ret)
        {
            Debug.Log("Success2!" + lambda);
            Debug.Log("Success2!" + normal);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
