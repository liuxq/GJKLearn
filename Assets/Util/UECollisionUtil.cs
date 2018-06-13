using UnityEngine;


    public class UECollisionUtil
    {
        public static float EPSILON_COLLISION = 0.0001f;
        public static float EPSILON_DISTANCE = 0.01f;

        private static Vector3 mvN;
        private static Vector3 mvMaxT = new Vector3(-1.0f, -1.0f, -1.0f);
        private static float[] maSign = new float[3];

        private static Vector3 mRTTriDir;
        private static Vector3 mRTTritvec;
        private static Vector3 mRTTriqvec;
        private static float mRTTriu;
        private static float mRTTriv;
        private static float mRTTrit;
        private static float mRTTrifErr;
        private static float mRTTrifErr2 = 0.001f;
        /*	Check whether a ray collision with a 3D AABB, if true, calcualte the hit point.

	    Return true if ray collision with AABB, otherwise return false.

	    vStart: ray's start point.
	    vDelta: ray's moving delta (just is vEnd-vStart, not to be normalized)
	    vMins, vMaxs: 3D Axis-Aligned Bounding Box
	    vPoint (out): receive collision point.
	    pfFraction (out): hit fraction.
	    vNormal (out): hit plane's normal if true is returned. If start point is laid in
					    specified AABB,	vNormal will be (0, 0, 0);
        */
        public static bool RayToAABB3(Vector3 start, Vector3 delta, Vector3 mins, Vector3 maxs, out Vector3 point, out float fraction, out Vector3 normal)
        {
            point = start;

            //Vector3 vN;
            mvMaxT = new Vector3(-1.0f, -1.0f, -1.0f);
            bool bInside = true;
            int i;
            //float[] aSign = new float[3];
            maSign[0] = 0f;
            maSign[1] = 0f;
            maSign[2] = 0f;

            //	Search candidate plane
            for (i = 0; i < 3; i++)
            {
                if (start[i] < mins[i])
                {
                    point[i] = mins[i];
                    bInside = false;
                    maSign[i] = 1.0f;

                    //	Calcualte T distance to candidate plane
                    if (delta[i] != 0.0f)
                        mvMaxT[i] = (mins[i] - start[i]) / delta[i];
                }
                else if (start[i] > maxs[i])
                {
                    point[i] = maxs[i];
                    bInside = false;
                    maSign[i] = -1.0f;

                    //	Calcualte T distance to candidate plane
                    if (delta[i] != 0.0f)
                        mvMaxT[i] = (maxs[i] - start[i]) / delta[i];
                }
            }

            if (bInside)
            {
                point = start;
                fraction = 0;
                normal = Vector3.zero;
                return true;
            }

            //	Get largest of the maxT's for final choice of intersection
            int iWhichPlane = 0;
            mvN = new Vector3(-maSign[0], 0.0f, 0.0f);

            if (mvMaxT[1] > mvMaxT[iWhichPlane])
            {
                iWhichPlane = 1;
                mvN = new Vector3(0.0f, -maSign[1], 0.0f);
            }

            if (mvMaxT[2] > mvMaxT[iWhichPlane])
            {
                iWhichPlane = 2;
                mvN = new Vector3(0.0f, 0.0f, -maSign[2]);
            }

            //	Check final candidate actually inside box
            if (mvMaxT[iWhichPlane] < 0)
            {
                point = start;
                fraction = 0f;
                normal = Vector3.zero;
                return false;
            }

            for (i = 0; i < 3; i++)
            {
                if (i != iWhichPlane)
                {
                    point[i] = start[i] + mvMaxT[iWhichPlane] * delta[i];

                    if (point[i] < mins[i] - EPSILON_COLLISION ||
                        point[i] > maxs[i] + EPSILON_COLLISION)
                    {
                        point = start;
                        fraction = 0f;
                        normal = Vector3.zero;
                        return false;
                    }
                }
            }

            fraction = mvMaxT[iWhichPlane];
            normal = mvN;

            return true;
        }

        /*	Check whether ray intersect with triangle using the algorithm introduced by
        Tomas Toller and Ben Thumbore in "Fast, Minimum Storage Ray/Triangle Intersection".
        As they said, this algorithm is the fartest ray/triangle routine for triangles which
        havn't precomputed plane equations.

        vStart: start point of ray
        vDelta: ray's moving delta (needn't to be normalized)
        v0, v1, v2: three vertice of triangle
        vPoint (out): used to receive hit point.
        b2Sides: true, triangle is two sides, false, cull triangle which is back to ray
        pfFraction (out): hit fraction, can be NULL
        */
        public static bool RayToTriangle(Vector3 start, Vector3 delta, Vector3 v0,
                               Vector3 v1, Vector3 v2, out Vector3 point, bool b2Sides, out float fraction)
        {
            //float u, v, t, fErr = 0.00001f;
            mRTTriu = 0.00001f;
            mRTTriv = 0.00001f;
            mRTTrit = 0.00001f;
            mRTTrifErr = 0.00001f;

            mRTTriDir = delta;
            float fDist = mRTTriDir.magnitude;
            mRTTriDir.Normalize();

            //	Find vectors for two edges sharing vert0
            Vector3 vEdge1 = v1 - v0;
            Vector3 vEdge2 = v2 - v0;

            //	Begin calculating determinant - also used to calculate U parameter
            //Vector3 tvec, qvec;
            Vector3 pvec = Vector3.Cross(mRTTriDir, vEdge2);

            //	If determinant is near zero, ray lies in plane of triangle
            float fDet = Vector3.Dot(vEdge1, pvec);

            {
                //	Our little changed version
                if (!b2Sides)
                {
                    if (fDet < mRTTrifErr)
                    {
                        point = Vector3.zero;
                        fraction = 0f;
                        return false;
                    }
                }
                else
                {
                    if (fDet < mRTTrifErr && fDet > -mRTTrifErr)
                    {
                        point = Vector3.zero;
                        fraction = 0f;
                        return false;
                    }
                }

                //	Calculate distance from vert0 to ray origin
                mRTTritvec = start - v0;

                //	Calculate U parameter and test bounds
                mRTTriu = Vector3.Dot(mRTTritvec, pvec);
                if (mRTTriu < -mRTTrifErr || mRTTriu > fDet + mRTTrifErr)
                {
                    point = Vector3.zero;
                    fraction = 0f;
                    return false;
                }

                //	Prepare to test V parameter
                mRTTriqvec = Vector3.Cross(mRTTritvec, vEdge1);

                //	Calculate V parameter and test bounds
                mRTTriv = Vector3.Dot(mRTTriDir, mRTTriqvec);
                if (mRTTriv < -mRTTrifErr || mRTTriu + mRTTriv > fDet + mRTTrifErr)
                {
                    point = Vector3.zero;
                    fraction = 0f;
                    return false;
                }

                //	Calculate t, the distance to trangle from vStart
                mRTTrit = Vector3.Dot(vEdge2, mRTTriqvec) / fDet;
            }

            //	Ben Thumbore's algorithm check the insection of a unlimited ray and triangle.
            //	t means the distance from vStart position to trangle. But what we want to test
            //	here is the intersection of a line segment (NOT a whole ray) and triangle.
            //	so ignore the case when t < 0.0 or t > fDist
            //float fErr2 = 0.001f;
            if (mRTTrit < -mRTTrifErr2 || mRTTrit > fDist)
            {
                point = Vector3.zero;
                fraction = 0f;
                return false;
            }

            if (mRTTrit < mRTTrifErr2)
            {
                mRTTrit = mRTTrifErr2;
            }
            float fFraction = (mRTTrit - mRTTrifErr2) / fDist;
            point = start + delta * fFraction;

            //if (pfFraction)
            fraction = fFraction;

            return true;
        }

        /*	AABB-Sphere overlap test routine
	        Return true if boxes overlap.
	        aabb: aabb's inforamtion, only Center and Extents will be used
	        vCenter: sphere's center
	        radius: sphere's radius
        */
        public static bool AABBSphereOverlap(Bounds aabb, Vector3 vCenter, float radius)
        {
            float d = 0.0f;
            float radius2 = radius * radius;

            float tmp = vCenter.x - aabb.center.x;
            float s = tmp + aabb.extents.x;

            if (s < 0.0f)
            {
                if ((d += s * s) > radius2)
                    return false;
            }
            else
            {
                s = tmp - aabb.extents.x;
                if (s > 0.0f)
                {
                    if ((d += s * s) > radius2)
                        return false;
                }
            }

            tmp = vCenter.y - aabb.center.y;
            s = tmp + aabb.extents.y;

            if (s < 0.0f)
            {
                if ((d += s * s) > radius2)
                    return false;
            }
            else
            {
                s = tmp - aabb.extents.y;
                if (s > 0.0f)
                {
                    if ((d += s * s) > radius2)
                        return false;
                }
            }

            tmp = vCenter.z - aabb.center.z;
            s = tmp + aabb.extents.z;

            if (s < 0.0f)
            {
                if ((d += s * s) > radius2)
                    return false;
            }
            else
            {
                s = tmp - aabb.extents.z;
                if (s > 0.0f)
                {
                    if ((d += s * s) > radius2)
                        return false;
                }
            }

            return d <= radius2;
        }

        /*	AABB-AABB intersection routine.

	        Return true if two AABB collision, otherwise return false.

	        vCenter1: first AABB's center.
	        vExt1: first AABB's extents.
	        vCenter1: second AABB's center.
	        vExt1: second AABB's extents.
        */
        public static bool AABBAABBOverlap(Vector3 vCenter1, Vector3 vExt1,
                                 Vector3 vCenter2, Vector3 vExt2)
        {
            float fDist;

            //	X axis
            fDist = vCenter1.x - vCenter2.x;
            if (fDist < 0)
                fDist = -fDist;

            if (vExt1.x + vExt2.x < fDist)
                return false;

            //	Y axis
            fDist = vCenter1.y - vCenter2.y;
            if (fDist < 0)
                fDist = -fDist;

            if (vExt1.y + vExt2.y < fDist)
                return false;

            //	Z axis
            fDist = vCenter1.z - vCenter2.z;
            if (fDist < 0)
                fDist = -fDist;

            if (vExt1.z + vExt2.z < fDist)
                return false;

            return true;
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

        public static bool IsSphereOutsideCH(Vector3 centroid, float radius, ConvexData ch)
        {
            int nFaces = ch.GetFaceNum();
            for (int i = 0; i < nFaces; ++i)
            {
                HalfSpace hs = new HalfSpace(ch.GetFace(i));
                hs.Translate(radius);
                if (hs.Outside(centroid))
                    return true;
            }
            return false;
        }

        public static bool IsVertexOutsideCH(Vector3 v, ConvexData ch)
        {
            return IsSphereOutsideCH(v, 0.0f, ch);
        }

        //////////////////////////////////////////////////
        // Dynamic Collide with CH
        // To note is that the hitPos is not the contact point 
        // but the position of sphere center when collision occurs!
        //////////////////////////////////////////////////
        public static bool SphereCollideWithCH(Vector3 start, Vector3 delta, float radius, ConvexData ch, ref CovFace hitFace, ref Vector3 hitPos, ref float fraction)
        {
            CovFace face = null;
            int nAllFace = ch.GetFaceNum();
            for (int i = 0; i < nAllFace; ++i)
            {
                face = ch.GetFace(i);

                if (SphereIntersectFacePreTest(start, delta, radius, face) &&
                    SphereIntersectFaceExactTest(start, delta, radius, face, ref hitPos, ref fraction))
                {
                    hitFace = face;
                    return true;
                }
            }

            return false;
        }

        public static bool RayIntersectWithCH(Vector3 start, Vector3 delta, ConvexData ch, ref CovFace hitFace, ref Vector3 hitPos, ref float fraction)
        {
            return SphereCollideWithCH(start, delta, 0.0f, ch, ref hitFace, ref hitPos, ref fraction);
        }

        static bool SphereIntersectFacePreTest(Vector3 start, Vector3 delta, float radius, CovFace face)
        {
            Vector3 end = start + delta;

            bool result = face.Dist2Plane(start) > (radius - UEMathUtil.FLOAT_EPSILON)
                && face.Dist2Plane(end) < (radius + UEMathUtil.FLOAT_EPSILON);

            return result;
        }

        // test if the sphere intersects with the polygon
        static bool SphereIntersectPolygonTest(Vector3 start, Vector3 delta, float radius, CovFace face, ref Vector3 hitpos, ref float fraction)
        {
            Vector3 end = start + delta;
            float d0 = face.Dist2Plane(start);

            if (Vector3.Dot(delta, face.Normal) > 0.0f)
                return false;

            if (Mathf.Abs(d0) < radius + UEMathUtil.FLOAT_EPSILON)
            {
                for (int i = 0; i < face.GetEdgeNum(); i++)
                {
                    HalfSpace hs = new HalfSpace(face.GetEdgeHalfSpace(i));
                    hs.Translate(radius);			//向外膨胀一个球半径的距离
                    if (hs.Outside(start))		    //在某一个hs的外部
                        return false;
                }
                // 如果有额外面片，也必须考虑
                for (int i = 0; i < face.GetExtraHSNum(); i++)
                {
                    HalfSpace hs = new HalfSpace(face.GetExtraHalfSpace(i));
                    hs.Translate(radius);			//向外膨胀一个球半径的距离
                    if (hs.Outside(start))		    //在某一个hs的外部
                        return false;
                }

                fraction = 0.0f;
                hitpos = start;

                return true;
            }

            float d1 = face.Dist2Plane(end);

            //parallel
            if (Mathf.Abs(d0 - d1) < UEMathUtil.FLOAT_EPSILON)
            {
                return false;
            }

            bool D0_POSITIVE_INTER = ((d0 > radius + UEMathUtil.FLOAT_EPSILON) && (d1 < radius + UEMathUtil.FLOAT_EPSILON));

            if (D0_POSITIVE_INTER)
            {
                fraction = (d0 - radius) / (d0 - d1);
                hitpos = start * (1.0f - fraction) + end * fraction;

                Vector3 planeHit = hitpos - face.Normal * radius;

                for (int i = 0; i < face.GetEdgeNum(); i++)
                {
                    HalfSpace hs = new HalfSpace(face.GetEdgeHalfSpace(i));
                    if (hs.Outside(planeHit))		//在某一个hs的外部
                        return false;
                }

                // 如果有额外面片，也必须考虑
                for (int i = 0; i < face.GetExtraHSNum(); i++)
                {
                    HalfSpace hs = new HalfSpace(face.GetExtraHalfSpace(i));
                    if (hs.Outside(planeHit))		//在某一个hs的外部
                        return false;
                }
                return true;
            }

            return false;
        }

        static bool SphereIntersectFaceExactTest(Vector3 start, Vector3 delta, float radius, CovFace face, ref Vector3 hitPos, ref float fraction)
        {
            //求解碰撞交点
            float fdp = Vector3.Dot(face.Normal, delta);
            if (Mathf.Abs(fdp) < UEMathUtil.FLOAT_EPSILON)
                return false;

            fraction = face.Dist + radius - Vector3.Dot(face.Normal, start);
            fraction /= fdp;

            if (fraction < 0.0f || fraction > 1.0f)
                return false;

            hitPos = start + fraction * delta;

            //测试交点是否在FACE内部
            int nbEdges = face.GetEdgeNum();
            for (int i = 0; i < nbEdges; ++i)
            {
                HalfSpace hs = new HalfSpace(face.GetEdgeHalfSpace(i));
                hs.Translate(radius);			//向外膨胀一个球半径的距离
                if (hs.Outside(hitPos))		    //在某一个hs的外部
                    return false;
            }

            // 如果有额外面片，也必须考虑
            int nbExtraHSs = face.GetExtraHSNum();
            for (int i = 0; i < nbExtraHSs; ++i)
            {
                HalfSpace hs = new HalfSpace(face.GetExtraHalfSpace(i));
                hs.Translate(radius);			//向外膨胀一个球半径的距离
                if (hs.Outside(hitPos))		    //在某一个hs的外部
                    return false;
            }

            return true;
        }
    }

