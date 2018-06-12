/*
 * CREATED:     2015.05.06
 * PURPOSE:     raytrace data
 * AUTHOR:      gx
 */

using UnityEngine;


	public class ESENT_TYPE
	{
		public const uint ESENT_NONE = 0;
		public const uint ESENT_TERRAIN = 1;
        public const uint ESENT_SCENEOBJ = 2;
		public const uint ESENT_PLAYER = 3;
		public const uint ESENT_NPC = 4;
		public const uint ESENT_MATTER = 5;
		public const uint ESENT_SUBOBJ = 6;
	}

	public class UETraceRt
	{
        public Vector3 hitPos;      //返回位置//
        public Vector3 hitNormal;   //返回法线//
        public float hitDist;       //返回距离//
        public uint hitEntity;      //返回碰撞类型//
        public double objectID;     //返回目标的id//
        public Vector2 hituv;       //返回uv，编辑器用//
        public bool StartSolid;     //是否在凸包内部//

		public UETraceRt()
		{
			Reset();
		}

		public void Reset()
		{
			hitPos = Vector3.zero;
			hitNormal = Vector3.up;
			hitDist = float.MaxValue;
			hitEntity = ESENT_TYPE.ESENT_NONE;
            StartSolid = false;
			objectID = 0;
		}
	}

	public class UERayTrace
	{
		public Ray ray = new Ray();
		public float traceDist;
		public UETraceRt traceRt = new UETraceRt();

        public float HitDist { get { return traceRt.hitDist; } }
        public bool StartSolid { get { return traceRt.StartSolid; } }
        public Vector3 HitPos { get { return traceRt.hitPos; } }
        public Vector3 HitNormal { get { return traceRt.hitNormal; } }
        public float HitPosX { get { return traceRt.hitPos.x; } }
        public float HitPosY { get { return traceRt.hitPos.y; } }
        public float HitPosZ { get { return traceRt.hitPos.z; } }

        public float HitNormalX { get { return traceRt.hitPos.x; } }
        public float HitNormalY { get { return traceRt.hitPos.y; } }
        public float HitNormalZ { get { return traceRt.hitPos.z; } }

        public void SetRayInfo(Vector3 ori,Vector3 dir)
        {
            ray.origin = ori;
            ray.direction = dir;
        }

        public void SetRayInfoF(float orix,float oriy, float oriz, float dirx, float diry, float dirz)
        {
            ray.origin = new Vector3(orix,oriy,oriz);
            ray.direction = new Vector3(dirx, diry, dirz);
        }

        public void SetTraceRTInfo(double objectid, uint hitentity, float hitdist)
        {
            traceRt.objectID = objectid;
            traceRt.hitEntity = hitentity;
            traceRt.hitDist = hitdist;
        }

        public void Reset()
        {
            traceRt.Reset();
        }
	}