using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ConvexData cd = new ConvexData();
        cd.Verts.Add(new Vector3(107.729836f, 34.54181f, 75.3554153f));//0
        cd.Verts.Add(new Vector3(101.2302f, 34.54181f, 75.42438f));//1
        cd.Verts.Add(new Vector3(101.2302f, 31.54181f, 75.42438f));
        cd.Verts.Add(new Vector3(107.729836f, 31.54181f, 75.3554153f));
        cd.Verts.Add(new Vector3(101.1288f, 36.36845f, 65.86682f));
        cd.Verts.Add(new Vector3(101.1288f, 33.36845f, 65.86682f));
        cd.Verts.Add(new Vector3(107.628433f, 33.36845f, 65.79786f));
        cd.Verts.Add(new Vector3(107.628433f, 36.36845f, 65.79786f));



        //LineRenderer lr = GetComponent<LineRenderer>();
        //lr.positionCount = 8;
        //lr.SetPositions(cd.Verts.ToArray());
        

        


        CAPSULE cap;
        cap.Center = new Vector3(103.3638f, 36.91232f, 70.32143f);
        cap.HalfLen = 0.7f;
        cap.Radius = 0.2f;

        float lambda = 1;
        Vector3 normal = Vector3.zero;
        Vector3 delta = new Vector3(0, -0.5f, 0);
        bool ret = GJKRaycast._gjkLocalRayCast(cap, cd,  delta, ref lambda, ref normal);
        if(ret)
        {
            Debug.Log("Success!" + lambda);
            Debug.Log("Success!" + normal);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
