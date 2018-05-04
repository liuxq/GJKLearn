using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ConvexData cd = new ConvexData();
        cd.Verts.Add(new Vector3(14.3568859f, 24.96267f, 27.4215775f));//0
        cd.Verts.Add(new Vector3(3.15342522f, 24.96267f, 27.4215775f));//1
        cd.Verts.Add(new Vector3(3.15342522f, 24.96267f, -106.553719f));
        cd.Verts.Add(new Vector3(14.3568859f, 24.96267f, -106.553719f));

        cd.Verts.Add(new Vector3(3.15342522f, 25.46267f, 27.4215775f));
        cd.Verts.Add(new Vector3(3.15342522f, 25.46267f, -106.553719f));
        cd.Verts.Add(new Vector3(14.3568859f, 25.46267f, -106.553719f));
        cd.Verts.Add(new Vector3(14.3568859f, 25.46267f, 27.4215775f));


        //LineRenderer lr = GetComponent<LineRenderer>();
        //lr.positionCount = 8;
        //lr.SetPositions(cd.Verts.ToArray());
        

        CAPSULE cap;
        //cap.Center = new Vector3(3.174698f, 26.36351f, -3.662457f);
        cap.Center = new Vector3(3.14838171f, 26.36351f, -3.66289163f);
        cap.HalfLen = 0.7f;
        cap.Radius = 0.2f;

        float lambda = 1;
        Vector3 normal = Vector3.zero;
        Vector3 delta = new Vector3(0, -0.08f, 0);

        bool startSolid = false;
        //bool ret = GJKRaycast._gjkLocalRayCast(cap, cd, delta, ref lambda, ref normal, ref startSolid);

        GJKRaycast.GJKType ret = GJKRaycast.gjkLocalPenetration(cap, cd, 0, ref normal, ref lambda);
        if(true)
        {
            Debug.Log("Success!" + lambda);
            Debug.Log("Success!" + normal);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
