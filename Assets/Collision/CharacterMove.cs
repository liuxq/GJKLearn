using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour {
    public Bounds aabb;
    private MoveCDR cdr = null;

	// Use this for initialization
	void Start () {
        cdr = new MoveCDR();
        cdr.Center = this.transform.position;
        cdr.TPNormal = Vector3.up;
        cdr.Radius = 0.5f;
        cdr.HalfLen = 0.5f;
	}
	
	// Update is called once per frame
	void Update () {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
		if(v != 0 || h != 0)
        {
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            Vector3 dir = forward * v + right * h;
            dir.y = 0;
            dir = dir.normalized;
            transform.position = GroundMove(new Vector3(dir.x, 0, dir.z), 5f, (uint)(Time.deltaTime * 1000));
        }
	}

    public Vector3 GroundMove(Vector3 dirH, float speedH, uint time, float speedV = 0.0f)
    {
        Vector3 realDirH = dirH;
        if (Mathf.Abs(realDirH.y) > UEMathUtil.FLOAT_EPSILON)
        {
            realDirH.y = 0;
            realDirH.Normalize();
        }

        // OnGroundMove only accept positive speed
        if (speedH < 0.0f)
        {
            realDirH = -dirH;
            speedH = -speedH;
        }


        cdr.Center = transform.position;
        cdr.VelDirH = dirH;
        cdr.Speed = speedH;
        cdr.TimeSec = time * 0.001f;
        cdr.ClipVel.y += speedV;

        UECollision.OnGroundMove(cdr);

        //if (cdr.OnSurface && cdr.TPNormal.sqrMagnitude > UEMathUtil.FLOAT_EPSILON)
        //    mHostPlayer.GroundNormal = cdr.TPNormal;
        //else
        //    mHostPlayer.GroundNormal = Vector3.up;

        // do ground move
        Vector3 newpos = cdr.Center;
        //mMoveTime += time;
        //mMoveBlocked = cdr.Blocked;

        return newpos;
    }
}
