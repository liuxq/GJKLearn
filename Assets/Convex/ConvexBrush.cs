
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

 

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
        public float Fraction;		//	Fraction
        public CDBrush HitObject;	//  The traced object
        public CollidePoints HitPoints;
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
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;
        }

        public void Init(CAPSULE start, Vector3 delta, uint flags = 0xffffffff)
        {
            Start = start;
            Delta = delta;
            ChkFlags = flags;

            StartSolid = false;
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
            Plane = new HalfSpace();
            Plane.Normal = normal;
            Plane.Dist = dist;
            Bevel = bevel;
        }
    }

    public class CDBrush
    {
        public uint Flags { get; set; }
        public List<CDSide> LstSides { get; set; }
        public AABB BoundAABB { get; set; }
        public ConvexData cd;

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
                CDSide side = new CDSide(src.LstSides[i].Plane, src.LstSides[i].Bevel);
                LstSides.Add(side);
            }
            Flags = src.Flags;
        }

        public void Release()
        {
            LstSides.Clear();
            BoundAABB.Clear();
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
            Release();
            BoundAABB = EditSLBinary.LoadAABB(fs);
            Flags = fs.Reader.ReadUInt32();
            int numsides = fs.Reader.ReadInt32();
            for (int i = 0; i < numsides; i++)
            {
                Vector3 normal = EditSLBinary.LoadVector3(fs);
                float dist = fs.Reader.ReadSingle();
                bool bevel = EditSLBinary.LoadBool(fs);
                CDSide side = new CDSide();
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

		public bool CapsuleTraceBrush(CapsuleTraceBrushInfo info)
        {
            if ((Flags & info.ChkFlags) > 0)
                return false;

            bool ret = false;

            if (UECollisionUtil.AABBAABBOverlap(BoundAABB.Center, BoundAABB.Extents, info.Bound.Center, info.Bound.Extents))
            {
                ret = GJKRaycast._gjkLocalRayCast(info.Start, this.cd, info.Delta, ref info.Fraction, ref info.Normal, ref info.StartSolid, ref info.HitPoints);
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

        public void Release()
        {

        }


        public void DebugRender(bool debug = true)
        {
            if (!debug)
            {
                //ClearDebugRender();
                return;
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

            //for (int j = 0; j < vlist.Count; ++j)
            //{
            //    _DebugRender.SetPoint(j, vlist[j]);
            //}
               
            //_DebugRender.CreateLineRender();

            //_DebugRender.AABB = GetAABB();
            //_DebugRender.Create(vlist, ilist);
        }

        public bool AddBrushBevels(ConvexData chData)
        {
            Release();
            CHData = chData;

            if (!FlushCH())
                return false;

            return true;
        }

        public void Export(CDBrush cdBrush)
        {
            if (cdBrush == null)
            {
                return;
            }
            cdBrush.Release();
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

