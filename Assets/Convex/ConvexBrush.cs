
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

    public class CapsuleTraceBrushInfo
    {
        //////////////////////////////////////////////////////////////////////////
        //	In
        //////////////////////////////////////////////////////////////////////////
        public CAPSULE Start;			//	Start Capsule
        public Vector3 Delta;			//	Move delta
        public Bounds Bound = new Bounds();
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
        public Vector3 CloseB;

        public CapsuleTraceBrushInfo()
        {
            Start.Init(Vector3.zero, 0, 0, Quaternion.identity);
            Delta = Vector3.zero;
            Bound.Clear();
            ChkFlags = 0u;
            Epsilon = 1E-5f;

            StartSolid = false;
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;
            HitPoints.size = 0;
            HitPoints.a = 0;
            HitPoints.b = 0;
            HitPoints.c = 0;
            Normal = Vector3.zero;
        }

        public void Init(Vector3 cStart, float cHalfLen, float cRadius, Quaternion rot, Vector3 delta, uint flags = 0xffffffff, bool ray = false)
        {
            Start.Init(cStart, cHalfLen, cRadius, rot);
            Delta = delta;
            ChkFlags = flags;

            StartSolid = false;
            Fraction = 1.0f;
            HitObject = null;
            HitFlags = 0u;

            HitPoints.size = 0;
            HitPoints.a = 0;
            HitPoints.b = 0;
            HitPoints.c = 0;
            Normal = Vector3.zero;

            Bound.Clear();
            Bound.Encapsulate(Start.P0);
            Bound.Encapsulate(Start.P1);
            Bound.Encapsulate(Start.P0 + Delta);
            Bound.Encapsulate(Start.P1 + Delta);
            Bound.Expand(Start.Radius);
            Bound.Expand(Epsilon);
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
        public Bounds BoundAABB { get; set; }
        public ConvexData cd;

        // constructor, deconstructor and releaser
        public CDBrush()
        {
            BoundAABB = new Bounds();
            LstSides = new List<CDSide>();
            Flags = 0;
        }

        public CDBrush(CDBrush src)
        {
            BoundAABB = src.BoundAABB;
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
        public bool Load(LBinaryFile fs)
        {
            Release();
            BoundAABB = SLBinary.LoadAABB(fs);
            Flags = fs.Reader.ReadUInt32();
            int numsides = fs.Reader.ReadInt32();
            for (int i = 0; i < numsides; i++)
            {
                Vector3 normal = SLBinary.LoadVector3(fs);
                float dist = fs.Reader.ReadSingle();
                bool bevel = SLBinary.LoadBool(fs);
                CDSide side = new CDSide();
                side.Init(normal, dist, bevel);
                LstSides.Add(side);
            }

            return true;
        }

        public bool Save(LBinaryFile fs)
        {
            SLBinary.SaveAABB(fs, BoundAABB);
            fs.Writer.Write(Flags);
            int numsides = LstSides.Count;
            fs.Writer.Write(numsides);
            for (int i = 0; i < numsides; i++)
            {
                SLBinary.SaveVector3(fs, LstSides[i].Plane.Normal);
                fs.Writer.Write(LstSides[i].Plane.Dist);
                SLBinary.SaveBool(fs, LstSides[i].Bevel);
            }

            return true;
        }

		public bool CapsuleTraceBrush(CapsuleTraceBrushInfo info)
        {
            if ((Flags & info.ChkFlags) > 0)
                return false;

            bool ret = false;

            if (BoundsExtansions.AABBAABBOverlap(BoundAABB, info.Bound))
            {
                ret = GJKRaycast.GjkLocalRayCast_CapsuleConvex(info.Start, this.cd, info.Delta, ref info.Fraction, ref info.Normal, ref info.StartSolid, ref info.HitPoints, ref info.CloseB);
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

    }

