
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public static class BoundsExtansions
{
    public static void Clear(this Bounds aabb)
    {
        aabb.min = new Vector3(999999f, 999999f, 999999f);
        aabb.max = new Vector3(-999999f, -999999f, -999999f);
    }

    //	Merge two aabb
    public static void Merge(this Bounds aabb, Bounds aabb2)
    {
        aabb.Encapsulate(aabb2);
    }

    //	Check whether a point is in this aabb
    public static bool IsPointIn(this Bounds aabb, Vector3 v, float offset)
    {
        if (v.x > aabb.max.x + offset || v.x < aabb.min.x - offset ||
            v.y > aabb.max.y + offset || v.y < aabb.min.y - offset ||
            v.z > aabb.max.z + offset || v.z < aabb.min.z - offset)
        {
            return false;
        }

        return true;
    }

    //	Build AABB from vertices
    public static void Build(this Bounds aabb, Vector3[] lstVerPos)
    {
        aabb.Clear();
        for (int i = 0; i < lstVerPos.Length; i++)
        {
            aabb.Encapsulate(lstVerPos[i]);
        }
    }

    public static bool AABBAABBOverlap(Bounds aabb1, Bounds aabb2)
    {
        if (aabb1.min.x > aabb2.max.x || aabb2.min.x > aabb1.max.x)
            return false;
        if (aabb1.min.y > aabb2.max.y || aabb2.min.y > aabb1.max.y)
            return false;
        if (aabb1.min.z > aabb2.max.z || aabb2.min.z > aabb1.max.z)
            return false;
        return true;
    }

}
   

    ///////////////////////////////////////////////////////////////////////////
    //	
    //	Class OBB
    //	
    ///////////////////////////////////////////////////////////////////////////

    //	Oriented Bounding Box
    public class OBB
    {
	    private Vector3 mCenter;
	    private Vector3 mXAxis;
	    private Vector3 mYAxis;
	    private Vector3 mZAxis;
	    private Vector3 mExtX;
	    private Vector3 mExtY;
	    private Vector3 mExtZ;
	    private Vector3 mExtents;

	    public Vector3 Center
        {
            get { return mCenter; }
            set { mCenter = value; }
        }

	    public Vector3 XAxis
        {
            get { return mXAxis; }
            set { mXAxis = value; }
        }

	    public Vector3 YAxis
        {
            get { return mYAxis; }
            set { mYAxis = value; }
        }

	    public Vector3 ZAxis
        {
            get { return mZAxis; }
            set { mZAxis = value; }
        }

	    public Vector3 ExtX
        {
            get { return mExtX; }
            set { mExtX = value; }
        }

	    public Vector3 ExtY
        {
            get { return mExtY; }
            set { mExtY = value; }
        }

	    public Vector3 ExtZ
        {
            get { return mExtZ; }
            set { mExtZ = value; }
        }

	    public Vector3 Extents
        {
            get { return mExtents; }
            set { mExtents = value; }
        }

        public OBB()
        {
        }

	    public OBB(OBB obb)
        {
            mCenter = obb.mCenter;
            mXAxis = obb.mXAxis;
            mYAxis = obb.mYAxis;
            mZAxis = obb.mZAxis;
            mExtX = obb.mExtX;
            mExtY = obb.mExtY;
            mExtZ = obb.mExtZ;
            mExtents = obb.mExtents;
        }

        //	Clear obb
        void Clear()
        {
            mCenter.Set(0.0f, 0.0f, 0.0f);
            mXAxis.Set(0.0f, 0.0f, 0.0f);
            mYAxis.Set(0.0f, 0.0f, 0.0f);
            mZAxis.Set(0.0f, 0.0f, 0.0f);
            mExtX.Set(0.0f, 0.0f, 0.0f);
            mExtY.Set(0.0f, 0.0f, 0.0f);
            mExtZ.Set(0.0f, 0.0f, 0.0f);
            mExtents.Set(0.0f, 0.0f, 0.0f);
        }

        //	Compute ExtX, ExtY, ExtZ
        public void CompleteExtAxis()
        {
            mExtX = mXAxis * mExtents.x;
            mExtY = mYAxis * mExtents.y;
            mExtZ = mZAxis * mExtents.z;
        }

        //	Check whether a point is in this obb
        public bool IsPointIn(Vector3 v)
        {
	        Vector3 vd = v - mCenter;

	        //	Transfrom point to obb space
	        float d = Vector3.Dot(mXAxis, vd);
	        if (d < -mExtents.x || d > mExtents.x)
		        return false;

	        d = Vector3.Dot(mYAxis, vd);
	        if (d < -mExtents.y || d > mExtents.y)
		        return false;

	        d = Vector3.Dot(mZAxis, vd);
	        if (d < -mExtents.z || d > mExtents.z)
		        return false;

	        return true;
        }

        //	Build obb from aabb
        public void Build(Bounds aabb)
        {
	        mCenter = aabb.center;
	        mXAxis = Vector3.right;
	        mYAxis = Vector3.up;
	        mZAxis = Vector3.forward;
	        mExtents = aabb.extents;
	        CompleteExtAxis();
        }

        //	Build obb from vertices
        public void Build(List<Vector3> lstVertPos)
        {
            Clear();

            if (lstVertPos.Count <= 0)
                return;

            Matrix3x3 matTransform = GetOBBOrientation(lstVertPos);

            //	For matTransform is orthogonal, so the inverse matrix is just rotate it;
            matTransform.Transpose();

            Vector3 vecMax = new Vector3(lstVertPos[0].x, lstVertPos[0].y, lstVertPos[0].z) * matTransform;
            Vector3 vecMin = vecMax;

            for (int i = 1; i < lstVertPos.Count; i++)
            {
                Vector3 vecThis = new Vector3(lstVertPos[i].x, lstVertPos[i].y, lstVertPos[i].z) * matTransform;
                vecMax = Vector3.Max(vecMax, vecThis);
                vecMin = Vector3.Min(vecMin, vecThis);
            }

            matTransform.Transpose();

            Vector3 row0 = matTransform.GetRow(0);
            Vector3 row1 = matTransform.GetRow(1);
            Vector3 row2 = matTransform.GetRow(2);

            mCenter = 0.5f * (vecMax + vecMin) * matTransform;
            mXAxis = Vector3.Normalize(row0);
            mYAxis = Vector3.Normalize(row1);
            mZAxis = Vector3.Normalize(row2);
            mExtents = 0.5f * (vecMax - vecMin);

            CompleteExtAxis();
        }

        //	Build obb from two obbs
        public void Build(OBB obb1, OBB obb2)
        {
	        Clear();

	        List<Vector3> lstVertPos = new List<Vector3>();
            obb1.GetVertices(lstVertPos, null, true);
            List<Vector3> lstTmp = new List<Vector3>();
            obb2.GetVertices(lstTmp, null, true);
            lstVertPos.AddRange(lstTmp);

            Build(lstVertPos);
        }
        
	    private static short[] mIndexTriangle = new short[]
	    {
		    0, 1, 3, 3, 1, 2, 
		    2, 1, 6, 6, 1, 5, 
		    5, 1, 4, 4, 1, 0,
		    0, 3, 4, 4, 3, 7, 
		    3, 2, 7, 7, 2, 6, 
		    7, 6, 4, 4, 6, 5
	    };

	    private static short[] mIndexWire = new short[]
        {
		    0, 1, 1, 2, 2, 3, 3, 0, 
		    0, 4, 1, 5, 2, 6, 3, 7,
		    4, 5, 5, 6, 6, 7, 7, 4  
        };

	    //	Get vertices of obb
	    public void GetVertices(List<Vector3> lstVertPos, List<short> lstIndices, bool wire)
        {
	        if (lstVertPos != null)
	        {
                lstVertPos.Clear();

		        //	Up 4 vertex;
		        //	Left Up corner;
		        lstVertPos[0] = mCenter - mExtX + mExtY + mExtZ;
		        //	right up corner;
		        lstVertPos[1] = lstVertPos[0] + 2.0f * mExtX;
		        //	right bottom corner;
		        lstVertPos[2] = lstVertPos[1] - 2.0f * mExtZ;
		        //	left bottom corner;
		        lstVertPos[3] = lstVertPos[2] - 2.0f * mExtX;

		        //	Down 4 vertex;
		        //	Left up corner;
		        lstVertPos[4] = mCenter - mExtX - mExtY + mExtZ;
		        //	right up corner;
		        lstVertPos[5] = lstVertPos[4] + 2.0f * mExtX;
		        //	right bottom corner;
		        lstVertPos[6] = lstVertPos[5] - 2.0f * mExtZ;
		        //	left bottom corner;
		        lstVertPos[7] = lstVertPos[6] - 2.0f * mExtX;
	        }

	        if (lstIndices != null)
	        {
                lstIndices.Clear();
                if (wire)
                    lstIndices.AddRange(mIndexWire);
                else
                    lstIndices.AddRange(mIndexTriangle);
	        }
        }

        public static Matrix3x3 GetOBBOrientation(List<Vector3> lstVertPos)
        {
            Matrix3x3 cov = new Matrix3x3();
            if (lstVertPos == null || lstVertPos.Count <= 0)
                return cov;

            cov = GetConvarianceMatrix(lstVertPos);

            // now get eigenvectors
            Matrix3x3 Evecs;
            Vector3 Evals;
            GetEigenVectors(out Evecs, out Evals, cov);

            return Evecs;
        }

        public static Matrix3x3 GetConvarianceMatrix(List<Vector3> lstVertPos)
        {
	        Matrix3x3 cov = new Matrix3x3();
            if (lstVertPos == null || lstVertPos.Count <= 0)
                return cov;

	        double[] s1 = new double[3];
	        double[,] s2 = new double[3,3];

	        s1[0] = s1[1] = s1[2] = 0.0;
	        s2[0,0] = s2[1,0] = s2[2,0] = 0.0;
	        s2[0,1] = s2[1,1] = s2[2,1] = 0.0;
	        s2[0,2] = s2[1,2] = s2[2,2] = 0.0;

	        // get center of mass
            for (int i = 0; i < lstVertPos.Count; i++)
	        {
		        s1[0] += lstVertPos[i].x;
		        s1[1] += lstVertPos[i].y;
		        s1[2] += lstVertPos[i].z;

		        s2[0,0] += lstVertPos[i].x * lstVertPos[i].x;
		        s2[1,1] += lstVertPos[i].y * lstVertPos[i].y;
		        s2[2,2] += lstVertPos[i].z * lstVertPos[i].z;
		        s2[0,1] += lstVertPos[i].x * lstVertPos[i].y;
		        s2[0,2] += lstVertPos[i].x * lstVertPos[i].z;
		        s2[1,2] += lstVertPos[i].y * lstVertPos[i].z;
	        }

	        float n = (float)lstVertPos.Count;
	        // now get covariances
	        cov[0,0] = (float)(s2[0,0] - s1[0]*s1[0] / n) / n;
	        cov[1,1] = (float)(s2[1,1] - s1[1]*s1[1] / n) / n;
	        cov[2,2] = (float)(s2[2,2] - s1[2]*s1[2] / n) / n;
	        cov[0,1] = (float)(s2[0,1] - s1[0]*s1[1] / n) / n;
	        cov[1,2] = (float)(s2[1,2] - s1[1]*s1[2] / n) / n;
	        cov[0,2] = (float)(s2[0,2] - s1[0]*s1[2] / n) / n;
	        cov[1,0] = cov[0,1];
	        cov[2,0] = cov[0,2];
	        cov[2,1] = cov[1,2];

	        return cov;
        }

        public static void Rotate(ref Matrix3x3 a, ref double g, ref double h, double s, double tau, int i, int j, int k, int l)
        {
            g = a[i,j]; 
            h = a[k,l];
            a[i,j] = (float)(g - s * (h + g * tau)); 
            a[k,l] = (float)(h + s * (g - h * tau));
        }

        public static void GetEigenVectors(out Matrix3x3 vout, out Vector3 dout, Matrix3x3 a)
        {
	        int n = 3;
	        int j, iq, ip, i;
	        double tresh, theta, tau, t, sm, s, h, g, c;
	        int nrot;
	        Vector3 b = Vector3.zero;
            Vector3 z = Vector3.zero;
            Matrix3x3 v = Matrix3x3.zero;
            Vector3 d = Vector3.zero;

	        v.Identity();
	        for (ip = 0; ip < n; ip++) 
	        {
		        b[ip] = a[ip,ip];
		        d[ip] = a[ip,ip];
		        z[ip] = 0.0f;
	        }  
	        nrot = 0;
            for (i = 0; i < 50; i++)
	        {
		        sm = 0.0;
                for (ip = 0; ip < n; ip++)
                    for (iq = ip + 1; iq < n; iq++)
                        sm += Mathf.Abs(a[ip, iq]);

		        if (sm == 0.0)
		        {
			        v.Transpose();
			        vout = v;
			        dout = d;
			        return;
		        }

                if (i < 3)
                    tresh = 0.2 * sm / (n * n);
                else
                    tresh = 0.0;
      
		        for (ip = 0; ip < n; ip++) 
		        {
			        for (iq = ip+1; iq < n; iq++)
			        {
				        g = 100.0 * Mathf.Abs(a[ip,iq]);
                        if (i > 3 && (Mathf.Abs(d[ip]) + g == Mathf.Abs(d[ip])) && (Mathf.Abs(d[iq]) + g == Mathf.Abs(d[iq]))) 
                            a[ip, iq] = 0.0f;
                        else if (Mathf.Abs(a[ip, iq]) > tresh)
                        {
                            h = d[iq] - d[ip];
                            if (g == 0.0)
                                t = (a[ip, iq]) / h;
                            else
                            {
                                theta = 0.5 * h / a[ip, iq];
                                t = 1.0 / (Mathf.Abs((float)theta) + Mathf.Sqrt((float)(1.0 + theta * theta)));
                                if (theta < 0.0)
                                    t = -t;
                            }
                            c = 1.0 / Mathf.Sqrt((float)(1 + t * t));
                            s = t * c;
                            tau = s / (1.0 + c);
                            h = t * a[ip, iq];
                            z[ip] -= (float)h;
                            z[iq] += (float)h;
                            d[ip] -= (float)h;
                            d[iq] += (float)h;
                            a[ip, iq] = 0.0f;
                            for (j = 0; j < ip; j++)
                            {
                                Rotate(ref a, ref g, ref h, s, tau, j, ip, j, iq);
                            }
                            for (j = ip + 1; j < iq; j++)
                            {
                                Rotate(ref a, ref g, ref h, s, tau, ip, j, j, iq);
                            }
                            for (j = iq + 1; j < n; j++)
                            {
                                Rotate(ref a, ref g, ref h, s, tau, ip, j, iq, j);
                            }
                            for (j = 0; j < n; j++)
                            {
                                Rotate(ref v, ref g, ref h, s, tau, j, ip, j, iq);
                            }
                            nrot++;
                        }
			        }
		        }

                for (ip = 0; ip < n; ip++)
		        {
                    b[ip] += z[ip];
                    d[ip] = b[ip];
                    z[ip] = 0.0f;
		        }
            }

	        v.Transpose();
	        vout = v;
	        dout = d;
	        return;
        }
    }


    ///////////////////////////////////////////////////////////////////////////
    //	
    //	struct CAPSULE
    //	
    ///////////////////////////////////////////////////////////////////////////

    //	Capsule
    public struct CAPSULE
    {
        private Vector3 mCenter;
        private float mHalfLen;
        private float mRadius;

        private Vector3 p0;
        private Vector3 p1;
        private bool dirty;

        public Vector3 Center
        {
            get { return mCenter; }
            set { mCenter = value; dirty = true; }
        }
        public float HalfLen
        {
            get { return mHalfLen; }
            set { mHalfLen = value; dirty = true; }
        }
        public float Radius
        {
            get { return mRadius; }
            set { mRadius = value; }
        }
        public float ExtendY
        {
            get { return mRadius + mHalfLen; }
        }

        public CAPSULE(Vector3 center, float halfLen, float radius)
        {
            mCenter = center;
            mHalfLen = halfLen;
            mRadius = radius;
            p0 = mCenter + mHalfLen * Vector3.up;
            p1 = mCenter + mHalfLen * Vector3.down;
            dirty = false;
        }

	    public CAPSULE(CAPSULE src)
        {
            mCenter = src.Center;
            mHalfLen = src.HalfLen;
            mRadius = src.Radius;
            p0 = mCenter + mHalfLen * Vector3.up;
            p1 = mCenter + mHalfLen * Vector3.down;
            dirty = false;
        }


        private void updateP0P1()
        {
            if(dirty)
            {
                p0 = mCenter + mHalfLen * Vector3.up;
                p1 = mCenter + mHalfLen * Vector3.down;
            }
        }

        public float getMargin()
        {
            return mRadius;
        }

        //This function is used in epa
        //dir is in the shape space
        public Vector3 supportSweepLocal(Vector3 dir)
        {
            updateP0P1();
            return (Vector3.Dot(p0, dir) > Vector3.Dot(p1, dir))? p0 : p1;
        }

	    //	Check whether a point is in this capsule
	    public bool IsPointIn(Vector3 pos)
        {
            Vector3 delta = pos - Center;
            
            if (float.Equals(HalfLen, 0.0f))
            {
                //	The capped cylinder Is a sphere
                return (delta.sqrMagnitude <= Radius * Radius);
            }

            if (delta.x > Radius || delta.x < -Radius)  // Quick check
                return false;

            if (delta.z > Radius || delta.z < -Radius)  // Quick check
                return false;

            if (delta.x * delta.x + delta.z * delta.z > Radius * Radius)
                return false;

            if (delta.y >= -HalfLen && delta.y <= HalfLen)  // Quick check
                return true;

            if (delta.y > 0.0f)
            {
                pos.y -= HalfLen;
                delta = pos - Center;
            }
            else
            {
                pos.y += HalfLen;
                delta = pos - Center;
            }

            if (delta.sqrMagnitude <= Radius * Radius)
                return true;

            return false;
        }
    }

    ///////////////////////////////////////////////////////////////////////////
    //	
    //	class CYLINDER
    //	
    ///////////////////////////////////////////////////////////////////////////

    //	Cylinder
    public class CYLINDER
    {
	    public Vector3 Center { get; set; }
	    public Vector3 AxisX { get; set; }
	    public Vector3 AxisY { get; set; }
	    public Vector3 AxisZ { get; set; }
	    public float HalfLen { get; set; }		//	Half length (on Y axis)
	    public float Radius { get; set; }	    

	    public CYLINDER() 
        {
        }

	    public CYLINDER(CYLINDER src)
	    {
            Center = src.Center;
            AxisX = src.AxisX;
            AxisY = src.AxisY;
            AxisZ = src.AxisZ;
            HalfLen = src.HalfLen;
            Radius = src.Radius;
	    }

	    //	Check whether a point is in this cylinder
	    public bool IsPointIn(Vector3 pos)
        {
            Vector3 vDelta = pos - Center;

            float fpx = (float)Mathf.Abs(Vector3.Dot(vDelta, AxisX));
            if (fpx > Radius)
                return false;

            float fpz = (float)Mathf.Abs(Vector3.Dot(vDelta, AxisZ));
            if (fpz > Radius)
                return false;

            if (fpx * fpx + fpz * fpz > Radius * Radius)
                return false;

            float fpy = Vector3.Dot(vDelta, AxisY);
            if (Mathf.Abs(fpy) > HalfLen)
                return false;

            return true;
        }
    }

