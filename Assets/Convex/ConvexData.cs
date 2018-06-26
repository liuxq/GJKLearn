
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


    [Serializable]
    public class ConvexData
    {
        public const uint CHFLAG_BAD_DETOUR_FIT = 0x00000001;
        public const uint CHFLAG_BLOCK_NPC = 0x00000002;
        public const uint CHFLAG_SKIP_MOVETRACE = 0x00000004;
        public const uint CHFLAG_SKIP_RAYTRACE = 0x00000008;
        public const uint CHFLAG_SKIP_CAMTRACE = 0x00000010;

        private const string _Key_ConvexDataHead = "=========Convex_Data_Start=========";
        private const string _Key_ConvexFlag = "ConvexFlag";
        private const string _Key_VertexNum = "VertexNum";
        private const string _Key_Vertex = "Vertex";
        private const string _Key_FaceNum = "FaceNum";

        public enum MIRROR_TYPE
        {
            MIRROR_AXIS_X = 0,
            MIRROR_AXIS_Y,
            MIRROR_AXIS_Z,
            MIRROR_AXIS_XY,
            MIRROR_AXIS_YZ,
            MIRROR_AXIS_XZ,
            MIRROR_AXIS_NUM,
        };

        public static float VERT_VALUE_MAX = 100000; //used by IsValidte(); vertex should between (-MAX_VERT_NUM, MAX_VERT_NUM)

        public uint Flags
        {
            get { return mFlags; }
            set { mFlags = value; }
        }

        private bool mAABBDirty;
        public Bounds mAABB = new Bounds();		// keep record of convex hull's AABB.
        public List<Vector3> mLstVertices = new List<Vector3>();
        public List<CovFace> mLstCovFace = new List<CovFace>();
        public uint mFlags;

        //debug
        public UEDebugMeshRender _DebugRender;
        public ConvexData()
        {
            Flags = 0;
            mAABBDirty = false;
        }

        public ConvexData(ConvexData chData)
        {
            Set(chData);
        }

        public void Set(ConvexData src)
        {
            Reset();
            int i;
            for (i = 0; i < src.GetVertexNum(); i++)
                AddVertex(src.mLstVertices[i]);
            for (i = 0; i < src.GetFaceNum(); i++)
                AddFace(src.mLstCovFace[i]);

            Flags = src.Flags;
            mAABB = src.mAABB;
        }

        public ConvexData Clone()
        {
            return new ConvexData(this);
        }

        public virtual bool EditLoad(LEditTextFile file)
        {
            Reset();

	        file.SkipLine(_Key_ConvexDataHead);
            mFlags = file.LoadValueLine<uint>(_Key_ConvexFlag);
            int vertnum = file.LoadValueLine<int>(_Key_VertexNum);
            for (int i = 0; i < vertnum; i++)
			{
			    Vector3 vec = file.LoadVector3Line(_Key_Vertex);
                mLstVertices.Add(vec);
			}
	        int facenum = file.LoadValueLine<int>(_Key_FaceNum);
            for (int i = 0; i < facenum; i++)
			{
                CovFace face = new CovFace();
                face.EditLoad(file);
                mLstCovFace.Add(face);
			}
            //mAABBDirty = true;

	        return true;
        }

        public virtual bool EditSave(LEditTextFile file)
        {
            file.SaveStrLine(_Key_ConvexDataHead, "");
            file.SaveValueLine<uint>(_Key_ConvexFlag, mFlags);
            int vertnum = GetVertexNum();
            file.SaveValueLine<int>(_Key_VertexNum, vertnum);
            for (int i = 0; i < vertnum; i++)
            {
                file.SaveVector3Line(_Key_Vertex, mLstVertices[i]);
            }
            int facenum = GetFaceNum();
            file.SaveValueLine<int>(_Key_FaceNum, facenum);
            for (int i = 0; i < facenum; i++)
            {
                mLstCovFace[i].EditSave(file);
            }
            return true;
        }
        public virtual bool Load(LBinaryFile fs, uint version)
        {
            Reset();

            mFlags = fs.Reader.ReadUInt32();
            int vertnum = fs.Reader.ReadInt32();
            for (int i = 0; i < vertnum; i++)
            {
                Vector3 vec = SLBinary.LoadVector3(fs);
                mLstVertices.Add(vec);
            }
            int facenum = fs.Reader.ReadInt32();
            for (int i = 0; i < facenum; i++)
            {
                CovFace face = new CovFace();
                face.Load(fs, version);
                mLstCovFace.Add(face);
            }
            //mAABBDirty = true;

            return true;
        }

        public virtual bool Save(LBinaryFile fs)
        {
            fs.Writer.Write(mFlags);
            int vertnum = GetVertexNum();
            fs.Writer.Write(vertnum);
            for (int i = 0; i < vertnum; i++)
            {
                SLBinary.SaveVector3(fs, mLstVertices[i]);
            }
            int facenum = GetFaceNum();
            fs.Writer.Write(facenum);
            for (int i = 0; i < facenum; i++)
            {
                mLstCovFace[i].Save(fs);
            }
            return true;
        }

        public void DebugRender(bool debug = true, string Text = "")
        {
            if(!debug)
            {
                ClearDebugRender();
                return;
            }

            if(null == _DebugRender)
            {
                _DebugRender = new UEDebugMeshRender();
            }

            List<Vector3> vlist = new List<Vector3>();
            List<int> ilist = new List<int>();
            List<Color> clist = new List<Color>();
            int iindex = 0;
            for (int i = 0; i < GetFaceNum(); ++i)
            {
                CovFace cf = GetFace(i);
                Vector3 v0 = (GetVertex(cf.GetVID(0)));

                for (int j = 1; j < cf.GetVNum() - 1; ++j)
                {
                    vlist.Add(v0);
                    ilist.Add(iindex++);
                    Vector3 v1 = GetVertex(cf.GetVID(j + 0));
                    vlist.Add(v1);
                    ilist.Add(iindex++);
                    Vector3 v2 = GetVertex(cf.GetVID(j + 1));
                    vlist.Add(v2);
                    ilist.Add(iindex++);
                    Vector3 normal = Vector3.Cross(v0 - v1, v1 - v2);
                    normal.Normalize();

                    float fdot = Vector3.Dot(normal, (Vector3.left - Vector3.down));
                    float fweight = Mathf.Max(0, fdot) * .8f;
                    Color facecolor = Color.red * (.2f + fweight);
                    clist.Add(facecolor);
                    clist.Add(facecolor);
                    clist.Add(facecolor);
                }
            }

            int index = 0;
            foreach (Vector3 v in vlist)
            {
                _DebugRender.SetPoint(index++, v);
            }
            _DebugRender.CreateLineRender();
            //_DebugRender.AABB = GetAABB();
            _DebugRender.Create(vlist, ilist, clist);

            //if(!string.IsNullOrEmpty(Text))
            //{
            //    _DebugRender.CreateText(Text);
            //}
        }

        public bool CreateConvexHullData(Vector3[] vertBuf, CovFace faceTop, CovFace faceBottom)
        {
            if (vertBuf == null || vertBuf.Length <= 0)
                return false;

            int vertCount = vertBuf.Length;

            Reset();
            for (int i = 0; i < vertCount; ++i)
                AddVertex(vertBuf[i]);

            Vector3 vNormalTop = faceTop.Normal;
            Vector3 vNormalBTM = faceBottom.Normal;

            int idx = 0;
            int nHalfVert = vertCount / 2;
            int idxInverse = (nHalfVert - 1) * 2;
            int p0, p1, p2, p3;
            int pi0, pi2, pi3;
            for (int i = 0; i < nHalfVert; ++i)
            {
                /*
                a side face:
                    p0      p2 
                    +-------+
                    |       |
                    |       |
                    |		|
                    +-------+
                    p1      p3
                */
                p0 = idx;
                p1 = idx + 1;
                p2 = idx + 2;
                p3 = idx + 3;
                if (p2 >= vertCount)
                    p2 -= vertCount;
                if (p3 >= vertCount)
                    p3 -= vertCount;

                CovFace face = new CovFace();
                face.Set(vertBuf[p0], vertBuf[p1], vertBuf[p2]);
                Vector3 vNormal = face.Normal;

                face.AddElement(p0);
                face.AddElement(p2);
               
                face.AddElement(p3);

               
                face.AddElement(p1);

                AddFace(face);

                faceBottom.AddElement(p1);

                pi0 = idxInverse;
                //pi1 = idxInverse + 1;
                pi2 = idxInverse + 2;
                pi3 = idxInverse + 3;
                if (pi2 >= vertCount)
                    pi2 -= vertCount;
                if (pi3 >= vertCount)
                    pi3 -= vertCount;

                faceTop.AddElement(pi0);

                idx += 2;
                idxInverse -= 2;
            }

            AddFace(faceTop);
            AddFace(faceBottom);

            return true;
        }

        // Generate convex hull from a triangle directly
        public bool Import(Vector3[] triangle, float fThickness = 0.01f)
        {
            if (triangle == null)
                return false;

            fThickness = Mathf.Abs(fThickness);
            if (0.0001 > fThickness)
                return false;

            Vector3 e01 = triangle[1] - triangle[0];
            //Vector3 e12 = triangle[2] - triangle[1];
            Vector3 e20 = triangle[0] - triangle[2];

            Vector3 vNTop = Vector3.Cross(e01, e20);
            float fDNTop = MathUtil.Normalize(ref vNTop);

            // Collinear test
            if (fDNTop < 1e-5)
                return false;

            Reset();

            // Compute all 6 triangle of convex hull;
            Vector3[] v = new Vector3[6];
            float fHalfThickness = 0.5f * fThickness;
            v[0] = triangle[0] + fHalfThickness * vNTop;
            v[1] = triangle[0] - fHalfThickness * vNTop;
            v[2] = triangle[1] + fHalfThickness * vNTop;
            v[3] = triangle[1] - fHalfThickness * vNTop;
            v[4] = triangle[2] + fHalfThickness * vNTop;
            v[5] = triangle[2] - fHalfThickness * vNTop;

            float fTopD = Vector3.Dot(vNTop, v[0]);
            CovFace faceTop = new CovFace();
            faceTop.Normal = vNTop;
            faceTop.Dist = fTopD;

            float fBtmD = Vector3.Dot(vNTop * -1.0f, v[3]);
            CovFace faceBottom = new CovFace();
            faceBottom.Normal = vNTop * -1.0f;
            faceBottom.Dist = fBtmD;

            return CreateConvexHullData(v, faceTop, faceBottom);
        }

        public bool Import(CYLINDER cylinder, int nbFaceInSide)
        {
            if (nbFaceInSide < 3)
                return false;

            float fHalfRad = Mathf.PI / nbFaceInSide;
            float fRad = fHalfRad * 2;
            float fNewR = cylinder.Radius / Mathf.Cos(fHalfRad);

            Matrix4x4 mat = Matrix4x4.identity;
            mat.SetRow(0, cylinder.AxisX);
            mat.SetRow(1, cylinder.AxisY);
            mat.SetRow(2, cylinder.AxisZ);
            mat.SetRow(3, cylinder.Center);

            Vector3 vHalfHigh = cylinder.AxisY * cylinder.HalfLen;

            int nAllCount = nbFaceInSide * 2;
            Vector3[] vertBuf = new Vector3[nAllCount];

            Vector3 p = Vector3.zero;
            int idx = 0;
            float fTheta = 0;
            for (int i = 0; i < nbFaceInSide; ++i)
            {
                p.Set(0, 0, 0);
                p.z -= fNewR * Mathf.Cos(fTheta);
                p.x += fNewR * Mathf.Sin(fTheta);
                p = mat.MultiplyPoint3x4(p);
                vertBuf[idx] = p + vHalfHigh;
                vertBuf[idx + 1] = p - vHalfHigh;
                idx += 2;
                fTheta += fRad;
            }

            Vector3 vTopCenter = cylinder.Center + vHalfHigh;
            float fTopD = Vector3.Dot(cylinder.AxisY, vTopCenter);
            CovFace faceTop = new CovFace();
            faceTop.Normal = cylinder.AxisY;
            faceTop.Dist = fTopD;

            Vector3 vBtmCenter = cylinder.Center - vHalfHigh;
            float fBtmD = Vector3.Dot(cylinder.AxisY * -1, vBtmCenter);
            CovFace faceBottom = new CovFace();
            faceBottom.Normal = cylinder.AxisY * -1;
            faceBottom.Dist = fBtmD;

            return CreateConvexHullData(vertBuf, faceTop, faceBottom);
        }

        // 对ConvexData进行坐标变换！变换矩阵为mtxTrans
        // 注：不能处理任意变换，只能是带有相同scale值的刚性变换
        public virtual void Transform(Matrix4x4 mtxTrans)
        {
            for (int i = 0; i < mLstVertices.Count; i++)
            {
                mLstVertices[i] = mtxTrans.MultiplyPoint3x4(mLstVertices[i]);
            }

            // 变换面片
            for (int i = 0; i < mLstCovFace.Count; i++)
            {
                mLstCovFace[i].Transform(mtxTrans);
            }

            // after transformation, we should rebuild the convex hull's aabb.
            mAABBDirty = true;
        }

        public virtual void MirrorTransform(MIRROR_TYPE mirror)
        {
            int i;
            Vector3 tmpvert = Vector3.zero;
            switch (mirror)
            {
                case MIRROR_TYPE.MIRROR_AXIS_X:
                    for (i = 0; i < mLstVertices.Count; i++)
                    {
                        tmpvert = mLstVertices[i];
                        tmpvert.x = -tmpvert.x;
                        mLstVertices[i] = tmpvert;
                    }
                    break;
                case MIRROR_TYPE.MIRROR_AXIS_Y:
                    for (i = 0; i < mLstVertices.Count; i++)
                    {
                        tmpvert = mLstVertices[i];
                        tmpvert.y = -tmpvert.y;
                        mLstVertices[i] = tmpvert;
                    }
                    break;
                case MIRROR_TYPE.MIRROR_AXIS_Z:
                    for (i = 0; i < mLstVertices.Count; i++)
                    {
                        tmpvert = mLstVertices[i];
                        tmpvert.z = -tmpvert.z;
                        mLstVertices[i] = tmpvert;
                    }
                    break;
                case MIRROR_TYPE.MIRROR_AXIS_XY:
                    for (i = 0; i < mLstVertices.Count; i++)
                    {
                        tmpvert = mLstVertices[i];
                        float tmp = tmpvert.x;
                        tmpvert.x = tmpvert.y;
                        tmpvert.y = tmp;
                        mLstVertices[i] = tmpvert;
                    }
                    break;
                case MIRROR_TYPE.MIRROR_AXIS_YZ:
                    for (i = 0; i < mLstVertices.Count; i++)
                    {
                        tmpvert = mLstVertices[i];
                        float tmp = tmpvert.y;
                        tmpvert.y = tmpvert.z;
                        tmpvert.z = tmp;
                        mLstVertices[i] = tmpvert;
                    }
                    break;
                case MIRROR_TYPE.MIRROR_AXIS_XZ:
                    for (i = 0; i < mLstVertices.Count; i++)
                    {
                        tmpvert = mLstVertices[i];
                        float tmp = tmpvert.x;
                        tmpvert.x = tmpvert.z;
                        tmpvert.z = tmp;
                        mLstVertices[i] = tmpvert;
                    }
                    break;
                default:
                    break;
            }

            for (i = 0; i < mLstCovFace.Count; i++)
            {
                mLstCovFace[i].MirrorTransform(mirror);
            }

            mAABBDirty = true;
        }

        public void Reset()
        {
            mLstVertices.Clear();
            mLstCovFace.Clear();
            mAABBDirty = true;

            ClearDebugRender();
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

        public int GetVertexNum()
        {
            return mLstVertices.Count;
        }

        public int GetFaceNum()
        {
            return mLstCovFace.Count;
        }

        public bool IsEmpty()
        {
            return (GetFaceNum() == 0) || (GetVertexNum() < 2);
        }

        public Vector3 GetVertex(int vid)
        {
            return mLstVertices[vid];
        }

        public CovFace GetFace(int fid)
        {
            return mLstCovFace[fid];
        }

        // build the aabb at runtime.
        public bool GetAABB(out Bounds aabb)
        {
            if (GetVertexNum() < 2)
            {
                aabb = new Bounds();
                return false;
            }

            if (mAABBDirty)
                BuildAABB();
            aabb = mAABB;

            return true;
        }

        // get the aabb we precomputed.
        public Bounds GetAABB()
        {
            if (mAABBDirty)
                BuildAABB();
            return mAABB;
        }

        public void AddVertex(Vector3 v)
        {
            mLstVertices.Add(v);
            mAABBDirty = true;
        }

        public void AddFace(CovFace face)
        {
            CovFace covface = new CovFace(face);
            face.CHData = this;
            mLstCovFace.Add(covface);
        }

        public void SetBadFitFlag(bool badfit = true)
        {
            if (badfit)
                mFlags |= CHFLAG_BAD_DETOUR_FIT;
            else
                mFlags &= ~(CHFLAG_BAD_DETOUR_FIT);
        }

        public bool IsBadFit()
        {
            return (mFlags & CHFLAG_BAD_DETOUR_FIT) > 0;
        }

        //is convexhull validate? 
        public bool IsValidte()
        {
            for (int i = 0; i < mLstVertices.Count; i++)
            {
                if (Mathf.Abs(mLstVertices[i].x) < MathUtil.FLOAT_EPSILON ||
                    Mathf.Abs(mLstVertices[i].y) < MathUtil.FLOAT_EPSILON ||
                    Mathf.Abs(mLstVertices[i].z) < MathUtil.FLOAT_EPSILON)
                    return false;
                if (mLstVertices[i].x > VERT_VALUE_MAX || mLstVertices[i].x < -VERT_VALUE_MAX ||
                    mLstVertices[i].y > VERT_VALUE_MAX || mLstVertices[i].y < -VERT_VALUE_MAX ||
                    mLstVertices[i].z > VERT_VALUE_MAX || mLstVertices[i].z < -VERT_VALUE_MAX)
                    return false;
            }

            for (int i = 0; i < mLstCovFace.Count; i++)
            {
                float fSqrlen = mLstCovFace[i].Normal.sqrMagnitude;
                if (fSqrlen < 1e-4f)
                    return false;
            }

            return true;
        }

        public CovFace GetNeighborFace(CovFace face, int vID1, int vID2)
        {
            for (int i = 0; i < GetFaceNum(); i++)
            {
                CovFace cface = GetFace(i);
                if (cface == face)
                    continue;
                int j;
                for (j = 0; j < cface.GetVNum() - 1; j++)
                {
                    if ((cface.GetVID(j) == vID1 && cface.GetVID(j + 1) == vID2)
                        || (cface.GetVID(j) == vID2 && cface.GetVID(j + 1) == vID1))
                    {
                        return cface;
                    }
                }

                if ((cface.GetVID(j) == vID1 && cface.GetVID(0) == vID2)
                    || (cface.GetVID(j) == vID2 && cface.GetVID(0) == vID1))
                {
                    return cface;
                }
            }
            return null;
        }

        public Vector3 GetSupportPlaneNormal(int vID1, int vID2)
        {
            Vector3 normal = Vector3.zero;
            float maxY = 0;
            for (int i = 0; i < GetFaceNum(); i++)
            {
                CovFace cface = GetFace(i);
                int j;
                for (j = 0; j < cface.GetVNum() - 1; j++)
                {
                    if ((cface.GetVID(j) == vID1 && cface.GetVID(j + 1) == vID2)
                        || (cface.GetVID(j) == vID2 && cface.GetVID(j + 1) == vID1))
                    {
                        if(cface.Normal.y > maxY)
                        {
                            maxY = cface.Normal.y;
                            normal = cface.Normal;
                        }
                        break;
                    }
                }

                if ((cface.GetVID(j) == vID1 && cface.GetVID(0) == vID2)
                    || (cface.GetVID(j) == vID2 && cface.GetVID(0) == vID1))
                {
                    if (cface.Normal.y > maxY)
                    {
                        maxY = cface.Normal.y;
                        normal = cface.Normal;
                    }
                }
            }
            return normal;
        }

        public Vector3 GetSupportPlaneNormal(int vID1)
        {
            Vector3 normal = Vector3.zero;
            float maxY = 0;
            for (int i = 0; i < GetFaceNum(); i++)
            {
                CovFace cface = GetFace(i);
                for (int j = 0; j < cface.GetVNum(); j++)
                {
                    if (cface.GetVID(j) == vID1)
                    {
                        if (cface.Normal.y > maxY)
                        {
                            maxY = cface.Normal.y;
                            normal = cface.Normal;
                        }
                        break;
                    }
                }
            }
            return normal;
        }

        protected void BuildAABB()
        {
            mAABBDirty = false;
            mAABB.Build(mLstVertices.ToArray());
            mAABB.Expand(1E-4f);
        }

        public int BruteForceSearch(Vector3 _dir) 
		{
			//brute force
			//get the support point from the orignal margin
			float max = Vector3.Dot(mLstVertices[0], _dir);
			int maxIndex=0;

			for(int i = 1; i < mLstVertices.Count; ++i)
			{
				float dist = Vector3.Dot(mLstVertices[i], _dir);
				if(dist > max)
				{
					max = dist;
					maxIndex = i;
				}
			}

			return maxIndex;
		}

        //This function is used in epa
		//dir is in the shape space
        public Vector3 supportSweepLocal(Vector3 dir, ref int index)
		{
            index = BruteForceSearch(dir);
            return mLstVertices[index];
		}

        public void Export(CDBrush cdBrush)
        {
            if (cdBrush == null)
            {
                return;
            }
            cdBrush.Release();
            for (int i = 0; i < GetFaceNum(); i++)
            {
                CDSide side = new CDSide();
                HalfSpace HS = new HalfSpace();
                HS.Normal = GetFace(i).Normal;
                HS.Dist = GetFace(i).Dist;

                side.Init(HS, false);
                cdBrush.LstSides.Add(side);
                cdBrush.cd = this;
            }

            Bounds tmpAABB;
            GetAABB(out tmpAABB);
            if (tmpAABB != null)
            {
                cdBrush.BoundAABB = tmpAABB;
            }
            cdBrush.Flags = Flags;
        }

    }

