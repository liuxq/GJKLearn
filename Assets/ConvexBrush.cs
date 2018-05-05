/*
 * CREATED:     2014-12-31 14:43:43
 * PURPOSE:     
 * AUTHOR:      Wangrui
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace UEEngine
{
    //////////////////////////////////////////////////////////////////////////
    // define a new compact Convex Hull data type which is used in Client!
    //////////////////////////////////////////////////////////////////////////
    public class BrushTraceInfo
    {
        //////////////////////////////////////////////////////////////////////////
        //	In
        //////////////////////////////////////////////////////////////////////////
        public Vector3 Start;			//	Start point
        public Vector3 Delta;			//	Move delta
        public Vector3 Extents;
        public AABB Bound = new AABB();
        // Dist Epsilon
        public uint ChkFlags;		//  for details refer to CConvexBrush::BrushTrace() in ConvexBrush.h
        public float Epsilon;	//	Dist Epsilon
        public bool IsPoint;	//	raytrace

        //////////////////////////////////////////////////////////////////////////
        //	Out
        //////////////////////////////////////////////////////////////////////////
        public bool StartSolid;	    //	Collide something at start point
        public bool AllSolid;		//	All in something
        public HalfSpace ClipPlane;	//	Clip plane
        public int ClipPlaneIdx;	//	Clip plane's index
        public float Fraction;		//	Fraction
        public object HitObject;	//  The traced object
        public uint HitFlags;     //  the flags of hit convex

        public BrushTraceInfo()
        {
            Start = Vector3.zero;
            Delta = Vector3.zero;
            Extents = Vector3.zero;
            Bound.Clear();
            ChkFlags = 0u;
            Epsilon = 1E-5f;
            IsPoint = true;
            StartSolid = false;
            AllSolid = false;
            ClipPlane = null;
            ClipPlaneIdx = -1;
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;
        }

        public void Init(Vector3 start, Vector3 delta, Vector3 extents, uint flags = 0xffffffff, bool ray = false)
        {
            Start = start;
            Delta = delta;
            Extents = extents;
            ChkFlags = flags;
            IsPoint = ray;

            StartSolid = false;
            AllSolid = false;
            ClipPlane = null;
            ClipPlaneIdx = -1;
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;

            Bound.Clear();
            if (IsPoint)
            {
                Extents = Vector3.zero;
                Bound.AddVertex(start);
                Bound.AddVertex(start + delta);
                Epsilon = 1E-5f;
            }
            else
            {
                Bound.AddVertex(start);
                Bound.AddVertex(start + extents);
                Bound.AddVertex(start - extents);
                Bound.AddVertex(start + delta);
                Bound.AddVertex(start + delta + extents);
                Bound.AddVertex(start + delta - extents);
                Bound.Extend(Epsilon);
            }
            Bound.CompleteCenterExts();
        }

        public bool HasClipPlane()
        {
            return ClipPlane != null;
        }

        public void GetPorjectPos(Vector3 pos, out float x, out float y, out float z)
        {
            if(null == ClipPlane)
            {
                x = 0; y = 0; z = 0;
            }

            Vector3 tmp = ClipPlane.GetPorjectPos(pos);
            x = tmp.x; y = tmp.y; z = tmp.z;
        }

        public void ClipPlaneNormal(out float x, out float y, out float z)
        {
            if (null == ClipPlane)
            {
                x = 0; y = 0; z = 0;
            }

            x = ClipPlane.Normal.x; y = ClipPlane.Normal.y; z = ClipPlane.Normal.z;
        }

        public static bool RayAABBCollision(BrushTraceInfo pInfo, AABB aabb)
        {
            Vector3 p1 = pInfo.Start;
            Vector3 p2 = pInfo.Start + pInfo.Delta;

            float left = aabb.Mins.x - pInfo.Epsilon, right = aabb.Maxs.x + pInfo.Epsilon;
            float front = aabb.Mins.z - pInfo.Epsilon, back = aabb.Maxs.z + pInfo.Epsilon;
            float top = aabb.Maxs.y + pInfo.Epsilon, bottom = aabb.Mins.y - pInfo.Epsilon;
	        if ((p1.x > right && p2.x > right) || (p1.x < left && p2.x < left)
		        ||(p1.y > top && p2.y > top) || (p1.y < bottom && p2.y < bottom)
		        ||(p1.z > back && p2.z > back) || (p1.z < front && p2.z < front))
		        return false;
	        return true;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    // Trace the QBrush!
    //////////////////////////////////////////////////////////////////////////
    public class AABBBrushTraceInfo : BrushTraceInfo
    {
        //////////////////////////////////////////////////////////////////////////
        //	In
        //////////////////////////////////////////////////////////////////////////
        // Convex hull data
        public List<QBrush> LstBrush { get; set; }

        public AABBBrushTraceInfo()
        {
        }

        public void Init(Vector3 start, Vector3 delta, Vector3 ext, List<QBrush> lstBrush = null, uint flags = 0xffffffff, bool ray = false)
        {
            base.Init(start, delta, ext, flags, ray);
            LstBrush = lstBrush;
        }
    }

    public class CapsuleTraceBrushInfo
    {
        //////////////////////////////////////////////////////////////////////////
        //	In
        //////////////////////////////////////////////////////////////////////////
        public CAPSULE Start;			//	Start Capsule
        public Vector3 Delta;			//	Move delta
        public AABB Bound = new AABB();
        // Dist Epsilon
        public uint ChkFlags;		//  for details refer to CConvexBrush::BrushTrace() in ConvexBrush.h
        public float Epsilon;	//	Dist Epsilon

        //////////////////////////////////////////////////////////////////////////
        //	Out
        //////////////////////////////////////////////////////////////////////////
        public bool StartSolid;	    //	Collide something at start point
        public bool AllSolid;		//	All in something
        public HalfSpace ClipPlane;	//	Clip plane
        public int ClipPlaneIdx;	//	Clip plane's index
        public float Fraction;		//	Fraction
        public CDBrush HitObject;	//  The traced object
        public uint HitFlags;     //  the flags of hit convex
        public Vector3 Normal;

        public CapsuleTraceBrushInfo()
        {
            Start.Center = Vector3.zero;
            Start.HalfLen = 0;
            Start.Radius = 0;
            Delta = Vector3.zero;
            Bound.Clear();
            ChkFlags = 0u;
            Epsilon = 1E-5f;

            StartSolid = false;
            AllSolid = false;
            ClipPlane = null;
            ClipPlaneIdx = -1;
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;
        }

        public void Init(CAPSULE start, Vector3 delta, uint flags = 0xffffffff, bool ray = false)
        {
            Start = start;
            Delta = delta;
            ChkFlags = flags;

            StartSolid = false;
            AllSolid = false;
            ClipPlane = null;
            ClipPlaneIdx = -1;
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;

            Bound.Clear();
            Bound.AddVertex(Start.Center - Vector3.up * Start.HalfLen);
            Bound.AddVertex(Start.Center + Vector3.up * Start.HalfLen);
            Bound.AddVertex(Start.Center - Vector3.up * Start.HalfLen + Delta);
            Bound.AddVertex(Start.Center + Vector3.up * Start.HalfLen + Delta);
            Bound.Extend(Start.Radius);
            Bound.Extend(Epsilon);
            
            Bound.CompleteCenterExts();
        }

        public bool HasClipPlane()
        {
            return ClipPlane != null;
        }

        public void GetPorjectPos(Vector3 pos, out float x, out float y, out float z)
        {
            if (null == ClipPlane)
            {
                x = 0; y = 0; z = 0;
            }

            Vector3 tmp = ClipPlane.GetPorjectPos(pos);
            x = tmp.x; y = tmp.y; z = tmp.z;
        }

        public void ClipPlaneNormal(out float x, out float y, out float z)
        {
            if (null == ClipPlane)
            {
                x = 0; y = 0; z = 0;
            }

            x = ClipPlane.Normal.x; y = ClipPlane.Normal.y; z = ClipPlane.Normal.z;
        }

        public static bool RayAABBCollision(BrushTraceInfo pInfo, AABB aabb)
        {
            Vector3 p1 = pInfo.Start;
            Vector3 p2 = pInfo.Start + pInfo.Delta;

            float left = aabb.Mins.x - pInfo.Epsilon, right = aabb.Maxs.x + pInfo.Epsilon;
            float front = aabb.Mins.z - pInfo.Epsilon, back = aabb.Maxs.z + pInfo.Epsilon;
            float top = aabb.Maxs.y + pInfo.Epsilon, bottom = aabb.Mins.y - pInfo.Epsilon;
            if ((p1.x > right && p2.x > right) || (p1.x < left && p2.x < left)
                || (p1.y > top && p2.y > top) || (p1.y < bottom && p2.y < bottom)
                || (p1.z > back && p2.z > back) || (p1.z < front && p2.z < front))
                return false;
            return true;
        }
    }

    public class CDSide
    {
        public HalfSpace Plane { get; set; }
        public bool Bevel { get; set; }

        public CDSide()
        {

        }

        public CDSide(HalfSpace hs, bool bevel)
        {
            Init(hs, bevel);
        }

        public void Init(HalfSpace hs, bool bevel)
        {
            Plane = hs;
            Bevel = bevel;
        }

        public void Init(Vector3 normal, float dist, bool bevel)
        {
            if(null == Plane)
            {
                Plane = new HalfSpace();
            }
            Plane.Normal = normal;
            Plane.Dist = dist;
            Bevel = bevel;
        }

        public void Reset()
        {
            Bevel = false;
            Plane.Reset();
        }
    }

    public class CDBrush
    {
        public uint Flags { get; set; }
        public List<CDSide> LstSides { get; set; }
        public AABB BoundAABB { get; set; }
        public ConvexData cd;

        private static List<CDSide> mCachedLstSides = new List<CDSide>();

        // constructor, deconstructor and releaser
        public CDBrush()
        {
            BoundAABB = new AABB();
            LstSides = new List<CDSide>();
            Flags = 0;
        }

        public CDBrush(CDBrush src)
        {
            BoundAABB = new AABB(src.BoundAABB);
            LstSides = new List<CDSide>();
            for (int i = 0; i < src.LstSides.Count; i++)
            {
                CDSide side = GetCachedCDSide(); //new CDSide(src.LstSides[i].Plane, src.LstSides[i].Bevel);
                side.Init(src.LstSides[i].Plane, src.LstSides[i].Bevel);
                LstSides.Add(side);
            }
            Flags = src.Flags;
        }

        public void Reset()
        {
            foreach(CDSide side in LstSides)
            {
                side.Reset();
                mCachedLstSides.Add(side);
            }
            LstSides.Clear();

            BoundAABB = new AABB();
            Flags = 0;
        }

        public CDBrush Clone(CDBrush src)
        {
            return new CDBrush(src);
        }

        public CDSide GetSide(int i)
        {
            return (i >= 0 && i < LstSides.Count) ? LstSides[i] : null;
        }

        // Load and save method
        public bool Load(UEBinaryFile fs)
        {
            Reset();
            BoundAABB = EditSLBinary.LoadAABB(fs);
            Flags = fs.Reader.ReadUInt32();
            int numsides = fs.Reader.ReadInt32();
            for (int i = 0; i < numsides; i++)
            {
                Vector3 normal = EditSLBinary.LoadVector3(fs);
                float dist = fs.Reader.ReadSingle();
                bool bevel = EditSLBinary.LoadBool(fs);
                CDSide side = GetCachedCDSide();
                side.Init(normal, dist, bevel);
                LstSides.Add(side);
            }

            return true;
        }

        public bool Save(UEBinaryFile fs)
        {
            EditSLBinary.SaveAABB(fs, BoundAABB);
            fs.Writer.Write(Flags);
            int numsides = LstSides.Count;
            fs.Writer.Write(numsides);
            for (int i = 0; i < numsides; i++)
            {
                EditSLBinary.SaveVector3(fs, LstSides[i].Plane.Normal);
                fs.Writer.Write(LstSides[i].Plane.Dist);
                EditSLBinary.SaveBool(fs, LstSides[i].Bevel);
            }

            return true;
        }

        private static CDSide GetCachedCDSide()
        {
            CDSide cdside = null;
            if (mCachedLstSides.Count > 0)
            {
                cdside = mCachedLstSides[0];
                mCachedLstSides.RemoveAt(0);
            }

            if (null == cdside)
            {
                cdside = new CDSide();
            }

            return cdside;
        }

        /*
         *	trace with aabb or ray
         *  @param  rInfo: the trace info (both input and output) @ref class BrushTraceInfo
         *  @return  false if no collision
         *	
         *	there are 3 steps in the function:
         *		1. return false, if not pass the flags-masks test   
         *		2. return false, if not pass the AABB test
         *		3. do the real ray-trace/collision with the CDBrush
         */
        public bool Trace(BrushTraceInfo info)
        {
            if ((Flags & info.ChkFlags) > 0)
                return false;

            bool ret = false;
            if (info.IsPoint)
            {
                float fraction;
                Vector3 hitPos, hitNormal;
                if (UECollisionUtil.RayToAABB3(info.Start, info.Delta, BoundAABB.Mins, BoundAABB.Maxs, out hitPos, out fraction, out hitNormal))
                    ret = CDBrush.RayTraceBrush(info, LstSides);
            }
            else
            {
                if (UECollisionUtil.AABBAABBOverlap(BoundAABB.Center, BoundAABB.Extents, info.Bound.Center, info.Bound.Extents))
                {
                    Profiler.BeginSample("brush");
                    ret = ClipBrush(info, LstSides);
                    Profiler.EndSample();
                }
                    
            }

            if (ret)
            {
                info.HitObject = this;
                info.HitFlags = Flags;
            }

            return ret;
        }
		
		public bool CapsuleTraceBrush(CapsuleTraceBrushInfo info)
        {
            if ((Flags & info.ChkFlags) > 0)
                return false;

            bool ret = false;

            if (UECollisionUtil.AABBAABBOverlap(BoundAABB.Center, BoundAABB.Extents, info.Bound.Center, info.Bound.Extents))
            {
                Profiler.BeginSample("gjk");
                //ret = CapsuleTraceBrush(info, LstSides);
                ret = GJKRaycast._gjkLocalRayCast(info.Start, this.cd, info.Delta, ref info.Fraction, ref info.Normal, ref info.StartSolid);

                Profiler.EndSample();
            }
                

            if (ret)
            {
                info.HitObject = this;
                info.HitFlags = Flags;
            }

            return ret;
        }

        public bool PointInBrush(Vector3 p, float offset)
        {
            if ((Flags & ConvexData.CHFLAG_SKIP_MOVETRACE) > 0)
                return false;

            if (!BoundAABB.IsPointIn(p, offset))
                return false;
            return PointInBrush(p, offset, LstSides);
        }

        public static bool PointInBrush(Vector3 p, float offset, List<CDSide> lstSides)
        {
            HalfSpace plane;
            float dist;
            float d;
            
            if (lstSides == null || lstSides.Count <= 0)
                return false;

            for (int i = 0; i < lstSides.Count; i++)
            {
                //skip the bevel
                if (lstSides[i].Bevel)
                    continue;

                plane = lstSides[i].Plane;
                Vector3 normal = plane.Normal;

                dist = plane.Dist;
                d = Vector3.Dot(p, normal) - dist;

                if (d > offset)
                    return false;
            }
            return true;
        }

        public static bool RayTraceBrush(BrushTraceInfo info, List<CDSide> lstSides)
        {
            HalfSpace plane, clipplane;
            float dist;
            float enterfrac, leavefrac;
            //Vector3 ofs;
            float d1, d2;
            bool getout, startout;
            float f;
            if (lstSides == null || lstSides.Count <= 0 || info == null || !info.IsPoint)
                return false;

            info.ClipPlaneIdx = -1;
            enterfrac = -1.0f;
            leavefrac = 1.0f;
            clipplane = null;
            getout = false;
            startout = false;

            Vector3 p1 = info.Start;
            Vector3 p2 = info.Start + info.Delta;

            for (int i = 0; i < lstSides.Count; i++)
            {
                //skip the bevel
                if (lstSides[i].Bevel)
                    continue;

                plane = lstSides[i].Plane;
                Vector3 normal = plane.Normal;

                dist = plane.Dist;
                d1 = Vector3.Dot(p1, normal) - dist;
                d2 = Vector3.Dot(p2, normal) - dist;

                if (d2 > 0)
                    getout = true;	    // endpoint is not in solid
                if (d1 > 0)
                    startout = true;

                // if completely in front of face, no intersection
                float limit = d1;
                UEMathUtil.ClampRoof(ref limit, 0.1f);
                if (d1 > 0 && d2 >= limit)
                    return false;

                if (d1 <= 0 && d2 <= 0)
                    continue;

                // crosses face
                if (d1 > d2)
                {
                    f = (d1 - info.Epsilon) / (d1 - d2);
                    if (f < 0.0f)
                    {
                        f = 0.0f;
                    }
                    if (f > enterfrac)
                    {
                        enterfrac = f;
                        clipplane = plane;
                        info.ClipPlaneIdx = i;
                    }
                }
                else
                {	// leave
                    f = (d1 + info.Epsilon) / (d1 - d2);
                    if (f > 1.0f)
                    {
                        f = 1.0f;
                    }

                    if (f < leavefrac)
                        leavefrac = f;
                }
            }

            //@note: gx modify for startsolid not in convex//
            if (!startout)
            {
                info.StartSolid = true;
                if (!getout)
                    info.AllSolid = true;

                info.Fraction = 0.0f;
                return false;
            }

            //@note : In some singular situations, eg. the volume of the convex hull is zero, 
            //        two or more sides are opposite,  enterfrac will be very close to leavefrac.//
            if (enterfrac < leavefrac)
            {
                if (enterfrac > -1 && enterfrac < 1)
                {
                    if (enterfrac < 0)
                        enterfrac = 0;
                    info.Fraction = enterfrac;
                    info.ClipPlane = clipplane;
                    return true;
                }
            }

            return false;
        }

        /*
         *	dynamic collision detect between aabb and brush 
         *  @note:  only consider the aabb, for raytrace case @ref RayTraceBrush
         *  @todo:  refine me and RayTraceBrush!  It is not a good way to keep duplicate code!!!  
         *  @author: 
         *  @ref:   quake2, quake3 source code and  RayTraceBrush
         */
        public static bool ClipBrush(BrushTraceInfo info, List<CDSide> lstSides)
        {
            HalfSpace plane, clipplane;
            float enterfrac, leavefrac;
            float d1, d2;
            bool getout, startout;
            float f;

            if (lstSides == null || lstSides.Count <= 0 || info == null || info.IsPoint)
                return false;

            Vector3 maxs = info.Extents;
            Vector3 mins = -info.Extents;

            info.ClipPlaneIdx = -1;
            enterfrac = -1;
            leavefrac = 1;
            clipplane = null;

            getout = false;
            startout = false;
            //Vector3 p1 = info.Start;
            Vector3 p2 = info.Start + info.Delta;

            for (int i = 0; i < lstSides.Count; i++)
            {
                plane = lstSides[i].Plane;
                Vector3 normal = plane.Normal;

                // push the plane out apropriately for mins/maxs
                // FIXME: use signbits into 8 way lookup for each mins/maxs
                Vector3 ofs = Vector3.zero;
                for (int j = 0; j < 3; j++)
                {
                    ofs[j] = (normal[j] < 0) ? maxs[j] : mins[j];
                }

                Vector3 pofs1 = info.Start + ofs;
                Vector3 pofs2 = p2 + ofs;
                d1 = plane.Dist2Plane(pofs1);
                d2 = plane.Dist2Plane(pofs2);
                if (d2 >= 0)
                    getout = true;	    // endpoint is not in solid

                if (d1 >= 0)
                    startout = true;    // startpoint is not in solid

                // if completely in front of face, no intersection
                float limit = d1;
                UEMathUtil.ClampRoof(ref limit, 0.1f);

                //Vector3 targetnormal = new Vector3(0.6818372f, -0.3770991f, 0.6268128f);

                if (d1 >= 0 && d2 >= limit)
                    return false;

                //d1 == 0 or d2 == 0 is on the plan,then it is not in solid//
                if (d1 < 0 && d2 < 0)
                {
                    //if (plane.Normal == targetnormal)
                    //{
                    //    UELogMan.LogMsg("solid occurs, d1:" + d1.ToString("f8") + " d2:" + d2.ToString("f8"));
                    //}
                    continue;
                }

                if (d1 >= 0 && d2 >= 0)
                {
                    // move direction is almost parallel the face
                    if (UEMathUtil.FloatEqual(d1, d2, info.Epsilon))
                        return false;
                }

                // crosses face
                if (d1 > d2)
                {
                    // enter
                    f = (d1 - info.Epsilon) / (d1 - d2);
                    if (f < 0.0f)
                        f = 0.0f;

                    if (f > enterfrac)
                    {
                        enterfrac = f;
                        clipplane = plane;
                        info.ClipPlaneIdx = i;
                    }
                }
                else
                {
                    // leave
                    f = (d1 + info.Epsilon) / (d1 - d2);
                    if (f > 1.0f)
                        f = 1.0f;

                    if (f < leavefrac)
                        leavefrac = f;
                }
            }

            if (!startout)
            {	// original point was inside brush
                info.StartSolid = true;
                if (!getout)
                    info.AllSolid = true;

                info.Fraction = 0.0f;
                return true;
            }

            if (enterfrac < leavefrac)
            {
                // if (enterfrac > -1 && enterfrac < trace.fraction)
                if (enterfrac > -1 && enterfrac < 1)
                {
                    if (enterfrac < 0)
                        enterfrac = 0;
                    info.Fraction = enterfrac;
                    info.ClipPlane = clipplane;
                    return true;
                }
            }

            return false;
        }
		
		public static bool CapsuleTraceBrush(CapsuleTraceBrushInfo info, List<CDSide> lstSides)
        {
            HalfSpace plane, clipplane;
            float enterfrac, leavefrac;
            float d1, d2;
            bool getout, startout;
            float f;

            if (lstSides == null || lstSides.Count <= 0 || info == null)
                return false;

            info.ClipPlaneIdx = -1;
            enterfrac = -1;
            leavefrac = 1;
            clipplane = null;

            getout = false;
            startout = false;

            Vector3 p1Up = info.Start.Center + Vector3.up * info.Start.HalfLen;
            Vector3 p1Down = info.Start.Center - Vector3.up * info.Start.HalfLen;
            Vector3 p2Up = p1Up + info.Delta;
            Vector3 p2Down = p1Down + info.Delta;

            float r = info.Start.Radius;

            for (int i = 0; i < lstSides.Count; i++)
            {
                plane = lstSides[i].Plane;
                Vector3 normal = plane.Normal;

                if (normal.y > 0)
                {
                    d1 = plane.Dist2Plane(p1Down) - r;
                    d2 = plane.Dist2Plane(p2Down) - r;
                }
                else
                {
                    d1 = plane.Dist2Plane(p1Up) - r;
                    d2 = plane.Dist2Plane(p2Up) - r;
                }

                if (d2 >= 0)
                    getout = true;	    // endpoint is not in solid

                if (d1 >= 0)
                    startout = true;    // startpoint is not in solid

                // if completely in front of face, no intersection
                float limit = d1;
                UEMathUtil.ClampRoof(ref limit, 0.1f);

                //Vector3 targetnormal = new Vector3(0.6818372f, -0.3770991f, 0.6268128f);

                if (d1 >= 0 && d2 >= limit)
                    return false;

                //d1 == 0 or d2 == 0 is on the plan,then it is not in solid//
                if (d1 < 0 && d2 < 0)
                {
                    //if (plane.Normal == targetnormal)
                    //{
                    //    UELogMan.LogMsg("solid occurs, d1:" + d1.ToString("f8") + " d2:" + d2.ToString("f8"));
                    //}
                    continue;
                }

                if (d1 >= 0 && d2 >= 0)
                {
                    // move direction is almost parallel the face
                    if (UEMathUtil.FloatEqual(d1, d2, info.Epsilon))
                        return false;
                }

                // crosses face
                if (d1 > d2)
                {
                    // enter
                    f = (d1 - info.Epsilon) / (d1 - d2);
                    if (f < 0.0f)
                        f = 0.0f;

                    if (f > enterfrac)
                    {
                        enterfrac = f;
                        clipplane = plane;
                        info.ClipPlaneIdx = i;
                    }
                }
                else
                {
                    // leave
                    f = (d1 + info.Epsilon) / (d1 - d2);
                    if (f > 1.0f)
                        f = 1.0f;

                    if (f < leavefrac)
                        leavefrac = f;
                }
            }

            if (!startout)
            {	// original point was inside brush
                info.StartSolid = true;
                if (!getout)
                    info.AllSolid = true;

                info.Fraction = 0.0f;
                return true;
            }

            if (enterfrac < leavefrac)
            {
                // if (enterfrac > -1 && enterfrac < trace.fraction)
                if (enterfrac > -1 && enterfrac < 1)
                {
                    if (enterfrac < 0)
                        enterfrac = 0;
                    info.Fraction = enterfrac;
                    info.ClipPlane = clipplane;
                    return true;
                }
            }

            return false;
        }
    }

    public class QBrush
    {
        public class QPlane
        {
            public HalfSpace HS;
            public int CHIndex;
            public bool Bevel;
            public Vector3 Vert;        // for debug
        }

        public const int MAX_FACE_IN_HULL = 200;

        public ConvexData CHData { get; set; }

        private List<QPlane> mLstQPlanes = new List<QPlane>();
        //debug
        private UEDebugMeshRender _DebugRender;

        public QBrush()
        {
        }

        public int GetSideNum()
        {
            return mLstQPlanes.Count;
        }

        public HalfSpace GetSide(int i)
        {
            if (0 > i || i >= mLstQPlanes.Count)
                return null;
            return mLstQPlanes[i].HS;
        }

        public bool IsBevel(int i)
        {
            if (0 > i || i >= mLstQPlanes.Count)
                return false;
            return mLstQPlanes[i].Bevel;
        }

        public void Reset()
        {
            mLstQPlanes.Clear();
            CHData = null;
            ClearDebugRender();
        }

        public void Update()
        {
            if(null != _DebugRender)
            {
                _DebugRender.Update();
            }
        }

        public void ClearDebugRender()
        {
            //debug
            if (null != _DebugRender)
            {
                _DebugRender.Destroy();
                _DebugRender = null;
            }
        }

        public void DebugRender(bool debug = true)
        {
            if (!debug)
            {
                ClearDebugRender();
                return;
            }

            if (null == _DebugRender)
            {
                _DebugRender = new UEDebugMeshRender();
            }

            List<Vector3> vlist = new List<Vector3>();
            //int iindex = 0;
            for (int i = 0; i < mLstQPlanes.Count; ++i)
            {
                QPlane cf = mLstQPlanes[i];

                CovFace face = CHData.GetFace(cf.CHIndex);
                int VNum = face.GetVNum();
                Vector3 center = Vector3.zero;
                for (int j = 0; j < VNum; j++)
                {
                    int index = face.GetVID(j);
                    Vector3 v0 = CHData.GetVertex(index);
                    vlist.Add(v0); center += v0;
                    int k = (j + 1) % VNum;
                    index = face.GetVID(k);
                    Vector3 v1 = CHData.GetVertex(index);
                    vlist.Add(v1);center += v1;
                    k = (k + 1) % VNum;
                    Vector3 v2 = CHData.GetVertex(face.GetVID(k));
                    vlist.Add(v2); center += v2;
                }
                //_DebugRender.NameList.Add(center/3);
            }

            for (int j = 0; j < vlist.Count; ++j)
            {
                _DebugRender.SetPoint(j, vlist[j]);
            }
               
            _DebugRender.CreateLineRender();

            //_DebugRender.AABB = GetAABB();
            //_DebugRender.Create(vlist, ilist);
        }

        public bool AddBrushBevels(ConvexData chData)
        {
            Reset();
            CHData = chData;

            if (!FlushCH())
                return false;

            int axis, dir;
            int i, order;
            Vector3 normal;
            float dist;

            AABB aabb;
            bool ret = CHData.GetAABB(out aabb);
            if (!ret)
            {
                return false;
            }
            QPlane sidetemp = null;
            
            // add the axial bevel planes
            order = 0;
            for (axis = 0; axis < 3; axis++)
            {
                for (dir = -1; dir <= 1; dir += 2, order++)
                {
                    // see if the plane is allready present
                    for (i = 0; i < GetSideNum(); i++)
                    {
                        normal = mLstQPlanes[i].HS.Normal;
                        if (dir == normal[axis])
                        {
                            break;
                        }
                    }

                    if (i == GetSideNum())
                    {
                        // add a new side
                        if (i >= MAX_FACE_IN_HULL)
                        {
                            UELogMan.LogError("add convex brush bevel error: side >= 200!");
                            return false;
                        }
                        normal = Vector3.zero;
                        normal[axis] = (float)dir;
                        if (dir == 1)
                        {
                            dist = aabb.Maxs[axis];
                        }
                        else
                        {
                            dist = -aabb.Mins[axis];
                        }
                        QPlane qplane = new QPlane();
                        qplane.HS = new HalfSpace();
                        qplane.HS.Normal = normal;
                        qplane.HS.Dist = dist;
                        qplane.Bevel = true;
                        mLstQPlanes.Add(qplane);
                    }

                    // if the plane is not in it canonical order, swap it
                    if (i != order)
                    {
                        sidetemp = mLstQPlanes[order];
                        mLstQPlanes[order] = mLstQPlanes[i];
                        mLstQPlanes[i] = sidetemp;
                    }
                }
            }

            // add the edge bevels
            if (GetSideNum() == 6)
            {
                return true; // pure axial
            }

            AddAngleBevel();

            return true;
        }

        private bool AddObseluteAngleBevel()
        {
            // test the non-axial plane edges
            for (int i = 0; i < GetSideNum(); i++)
            {
                QPlane qplane = mLstQPlanes[i];
                if (qplane.Bevel)
                    continue;

                CovFace face = CHData.GetFace(qplane.CHIndex);
                int VNum = face.GetVNum();
                for (int j = 0; j < VNum; j++)
                {
                    int index = face.GetVID(j);
                    Vector3 v0 = CHData.GetVertex(index);
                    int k = (j + 1) % VNum;
                    index = face.GetVID(k);
                    Vector3 v1 = CHData.GetVertex(index);
                    Vector3 edgedir = v1 - v0;
                    float mag = UEMathUtil.Normalize(ref edgedir);
                    if (mag < 0.5f)
                    {
                        continue;
                    }

                    UEMathUtil.Snap(ref edgedir);
                    for (k = 0; k < 3; k++)
                    {
                        if (edgedir[k] == -1.0f || edgedir[k] == 1.0f)
                        {
                            break; //axial
                        }
                    }

                    if (k != 3)
                    {
                        continue; // only test non-axial edges
                    }

                    // try the six possible slanted axials from this edge
                    for (int axis = 0; axis < 3; axis++)
                    {
                        for (int dir = -1; dir <= 1; dir += 2)
                        {
                            v1 = Vector3.zero;
                            v1[axis] = (float)dir;
                            Vector3 normal = Vector3.Cross(edgedir, v1);
                            mag = UEMathUtil.Normalize(ref normal);
                            if (mag < 0.5)
                            {
                                continue;
                            }
                            float dist = Vector3.Dot(v0, normal);
                            // if all the points on all the sides are
                            // behind this plane, it is a proper edge bevel
                            for (k = 0; k < GetSideNum(); k++)
                            {
                                QPlane qp = mLstQPlanes[k];
                                Vector3 hsNormal = qp.HS.Normal;
                                float hsDist = qp.HS.Dist;
                                if (UEMathUtil.FloatEqual(hsNormal.x, normal.x, 0.01f)
                                    && UEMathUtil.FloatEqual(hsNormal.y, normal.y, 0.01f)
                                    && UEMathUtil.FloatEqual(hsNormal.z, normal.z, 0.01f)
                                    && UEMathUtil.FloatEqual(hsDist, dist, 0.01f))
                                {
                                    break;
                                }
                                if (qp.Bevel)
                                {
                                    continue;
                                }
                                CovFace chFace = CHData.GetFace(qp.CHIndex);
                                int l = -1;
                                for (l = 0; l < chFace.GetVNum(); l++)
                                {
                                    int idx = chFace.GetVID(l);
                                    Vector3 vert = CHData.GetVertex(idx);
                                    float d = Vector3.Dot(vert, normal) - dist;
                                    if (d > 0.1f)
                                    {
                                        break; // point in front
                                    }
                                }

                                if (l != chFace.GetVNum())
                                {
                                    break;
                                }
                            }
                            if (k != GetSideNum())
                            {
                                continue;
                            }

                            // add this plane
                            if (GetSideNum() >= MAX_FACE_IN_HULL)
                            {
                                UELogMan.LogError("add convex brush bevel error: side >= 200!");
                                return false;
                            }

                            QPlane tmpqp = new QPlane();
                            tmpqp.HS = new HalfSpace();
                            tmpqp.HS.Normal = normal;
                            tmpqp.HS.Dist = dist;
                            tmpqp.Vert = v0;
                            tmpqp.Bevel = true;
                            mLstQPlanes.Add(tmpqp);
                        }
                    }
                }
            }
            return true;
        }

        private void AddAngleBevel()
        {
            for(int i = 0; i < CHData.GetFaceNum(); ++i)
            {
                CovFace pFace = CHData.GetFace(i);
			    Vector3 normal = pFace.Normal;
			    int j;
			    int vNum = pFace.GetVNum();
			    for (j = 0; j < vNum; j++) 
			    {
				    Vector3 v0, v1;
				    int index0 = pFace.GetVID(j);
				    v0 = CHData.GetVertex(index0);
				    int k = (j +1) % vNum;
				    int index1 = pFace.GetVID(k);
				    v1 = CHData.GetVertex(index1);
				
				    CovFace pNeigbor = CHData.GetNeighborFace(pFace, index0, index1);
				    if (pNeigbor == null)
					    continue;
				    Vector3 normal1 = pNeigbor.Normal;
				    if (Vector3.Dot(normal, normal1) < -0.33333f)//锐角两面夹角小于一定角度,误差控制为(sqrt(3)-1)r
				    {
					    Vector3 normal2 = (normal1 - normal);
					    Vector3 vDir = (v1 - v0); 
					    normal2 = Vector3.Cross(normal2, vDir);
					    normal2.Normalize();
					    if (Vector3.Dot(normal2, normal1 + normal) < 0)
						    normal = -normal2;
					    else
						    normal = normal2;
					
					    float fDist = Vector3.Dot(v0, normal);
					    fDist = Vector3.Dot(v1, normal);
					
					    for (k = 0;  k < GetSideNum(); k++) 
                        {
						    QPlane qplane = mLstQPlanes[k];
						    Vector3 vHSNormal = (qplane.HS.Normal);
						    float      fHSDist = qplane.HS.Dist;
                            if (UEMathUtil.FloatEqual(vHSNormal.x, normal.x, 0.01f)
                                    && UEMathUtil.FloatEqual(vHSNormal.y, normal.y, 0.01f)
                                    && UEMathUtil.FloatEqual(vHSNormal.z, normal.z, 0.01f)
                                    && UEMathUtil.FloatEqual(fHSDist, fDist, 0.01f))
                                {
                                    break;
                                }
					    }

					    if (k < mLstQPlanes.Count)
						    continue;
					    for(k = 0; k < CHData.GetVertexNum(); k++)
					    {
						    float d = Vector3.Dot(CHData.GetVertex(k), normal) - fDist;
						    if (d > 1E-4) 
							    break; // point in front						
					    }
					    if (k < CHData.GetVertexNum())
						    continue;						
					
					    if(mLstQPlanes.Count >= MAX_FACE_IN_HULL)
                        {
                            UELogMan.LogError("add convex angle bevel error: side >= 200!");
                            continue;
                        }

                        QPlane q = new QPlane();
                        q.HS = new HalfSpace();
					    q.HS.Normal = (normal);
					    q.HS.Dist = (fDist);
					    q.Bevel = true;
					
					    mLstQPlanes.Add(q);
				    }
			    }
            }
        }

        //void Transform(Matrix4x4 mtxTrans)
        //{
        //    for (int i = 0; i < GetSideNum(); ++i)
        //    {
        //        // the first-6 axial plane, we only transform the plane
        //        mLstQPlanes[i].HS.Transform(mtxTrans);
        //        if (i > 5)
        //        {
        //            // additionally, we should transform the vertex
        //            if (mLstQPlanes[i].Bevel)
        //            {
        //                mLstQPlanes[i].Vert = mtxTrans.MultiplyPoint3x4(mLstQPlanes[i].Vert);
        //            }
        //        }
        //    }
        //}

        public void Export(CDBrush cdBrush)
        {
            if (cdBrush == null)
            {
                return;
            }
            cdBrush.Reset();
            for (int i = 0; i < GetSideNum(); i++)
            {
                CDSide side = new CDSide();
                side.Init(mLstQPlanes[i].HS, mLstQPlanes[i].Bevel);
                cdBrush.LstSides.Add(side);
                cdBrush.cd = this.CHData;
            }

            AABB tmpAABB;
            CHData.GetAABB(out tmpAABB);
            if (tmpAABB != null)
            {
                cdBrush.BoundAABB = tmpAABB;
            }
            cdBrush.Flags = CHData.Flags;
        }

        private bool FlushCH()
        {
            if (CHData == null || CHData.IsEmpty())
            {
                return false;
            }

            if (mLstQPlanes.Count != 0)
            {
                return false;
            }

            int facenum = CHData.GetFaceNum();
            for (int i = 0; i < facenum; i++)
            {
                QPlane qplane = new QPlane();
                qplane.HS = new HalfSpace();
                qplane.HS.Normal = CHData.GetFace(i).Normal;
                qplane.HS.Dist = CHData.GetFace(i).Dist;
                qplane.CHIndex = i;
                qplane.Bevel = false;
                mLstQPlanes.Add(qplane);
            }
            return true;
        }
    }
}
