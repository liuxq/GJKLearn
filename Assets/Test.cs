using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    ConvexData cd = new ConvexData();

    CAPSULE cap;

	// Use this for initialization
	void Start () {

        cd.Verts.Add(new Vector3(17.60511f, 26.17006f, -152.7192f));
        cd.Verts.Add(new Vector3(11.78428f, 27.02423f, -155.854f));
        cd.Verts.Add(new Vector3(15.43481f, 27.02345f, -154.2114f));
        cd.Verts.Add(new Vector3(16.62971f, 27.02312f, -153.5959f));
        cd.Verts.Add(new Vector3(0.3282423f, 26.17006f, -152.6952f));
        cd.Verts.Add(new Vector3(1.31353f, 27.02312f, -153.5549f));
        cd.Verts.Add(new Vector3(2.222675f, 27.02339f, -154.2698f));
        cd.Verts.Add(new Vector3(5.110615f, 27.02398f, -155.854f));
        cd.Verts.Add(new Vector3(13.44987f, 26.17006f, -146.3835f));
        cd.Verts.Add(new Vector3(8.920061f, 26.17006f, -145.1911f));
        cd.Verts.Add(new Vector3(4.400952f, 26.17006f, -146.4234f));
        cd.Verts.Add(new Vector3(1.103441f, 26.17006f, -149.7501f));
        cd.Verts.Add(new Vector3(16.77663f, 26.17006f, -149.681f));
        cd.Verts.Add(new Vector3(2.156608f, 27.02312f, -150.352f));
        cd.Verts.Add(new Vector3(5.012085f, 27.02312f, -147.4712f));
        cd.Verts.Add(new Vector3(8.925415f, 27.02312f, -146.4041f));
        cd.Verts.Add(new Vector3(12.84801f, 27.02312f, -147.4366f));
        cd.Verts.Add(new Vector3(15.72883f, 27.02312f, -150.2921f));


        
        
        cap.Center = new Vector3(3.174698f, 26.31351f, -3.662457f);
        //cap.Center = new Vector3(3.14838171f, 26.36351f, -3.66289163f);
        cap.HalfLen = 0.7f;
        cap.Radius = 0.2f;

        
	}

    void OnGUI()
    {
        if(GUILayout.Button("Collide"))
        {
            cap.Center = this.transform.position;
            float lambda = 1;
            Vector3 normal = Vector3.zero;
            Vector3 delta = new Vector3(0, -0.08f, 0);

            bool startSolid = false;
            bool ret = GJKRaycast._gjkLocalRayCast(cap, cd, delta, ref lambda, ref normal, ref startSolid);

            //GJKRaycast.GJKType ret = GJKRaycast.gjkLocalPenetration(cap, cd, 0, ref normal, ref lambda);
            Debug.Log("result:" + ret);
            Debug.Log("normal:" + normal);
            Debug.Log("lambda:" + lambda);
        }

        if (GUILayout.Button("Show Convex"))
        {
            GameObject con = GameObject.Find("Convex");
            if (con == null)
                con = new GameObject("Convex");

            LineRenderer lr = con.GetComponent<LineRenderer>();
            if (lr == null) lr = con.AddComponent<LineRenderer>();
            lr.positionCount = cd.Verts.Count;
            lr.SetPositions(cd.Verts.ToArray());
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.loop = true;
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
