
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 

    [Serializable]
    public class CovFace : HalfSpace  
    {
        [NonSerialized]
        private ConvexData mCHData;

        public ConvexData CHData 
        {
            get
            {
                return mCHData;
            }
            set
            {
                mCHData = value;
            }
        }

	    private static float ANGLE_ACUTE_THRESH = 0.0f; //两边夹锐角的阈值
        
        public List<int> mLstVIDs = new List<int>();                     //按顺序记录顶点的索引id；
        public List<HalfSpace> mLstEdgeHSs = new List<HalfSpace>();      //按顺序（同上）记录一条边和该面的法向决定的halfspace
        public List<HalfSpace> mLstExtraHSs = new List<HalfSpace>();     //处理尖锐夹角情况时附加的一组Halfspace

        public CovFace()
        {

        }

	    public CovFace(CovFace face)
        {
            Set(face);
        }
        	
	    public static void SetAngleAcuteThresh(float AAThresh)		
	    {
		    ANGLE_ACUTE_THRESH=AAThresh;
	    }

        public void Set(CovFace face)
        {
            Normal = face.Normal;
            Dist = face.Dist;

            SetVIDNum(face.GetVNum());
            SetEdgeHSNum(face.GetVNum());
            for (int i = 0; i < face.GetVNum(); i++)
            {
                int vid = face.GetVID(i);
                //HalfSpace hs = face.GetEdgeHalfSpace(i);
                AddElement(vid, face.GetEdgeHalfSpace(i));
            }

            SetExtraHSNum(face.GetExtraHSNum());
            for (int i = 0; i < face.GetExtraHSNum(); i++)
            {
                AddExtraHS(face.GetExtraHalfSpace(i));
            }
        }

	    public void SetHS(HalfSpace hs)
	    {
		    Normal = hs.Normal;
		    Dist = hs.Dist;
	    }

        // 返回点vPos到mLstEdgeHSs和mLstExtraHSs中最短距离的Halfspace
        public HalfSpace GetNearestHS(Vector3 pos)
        {
            float mindist = 1000.0f;
            float dist;
            int iHS = -1;
            bool bHSInEdge = true;

            for (int i = 0; i < mLstEdgeHSs.Count; i++)
            {
                dist = Mathf.Abs(mLstEdgeHSs[i].Dist2Plane(pos));
                if (dist < mindist)
                {
                    mindist = dist;
                    iHS = i;
                }
            }

            for (int i = 0; i < mLstExtraHSs.Count; i++)
            {
                dist = Mathf.Abs(mLstExtraHSs[i].Dist2Plane(pos));
                if (dist < mindist)
                {
                    mindist = dist;
                    iHS = i;
                    bHSInEdge = false;
                }
            }

            HalfSpace hs = null;
            if (iHS >= 0)
            {
                hs = (bHSInEdge) ? mLstEdgeHSs[iHS] : mLstExtraHSs[iHS];
            }

            return hs;
        }

        // 计算额外的边界半空间
        public void ComputeExtraHS()
        {
            if (mLstEdgeHSs.Count == 0) return;

            Vector3 curN = Vector3.zero;
            Vector3 NextN = Vector3.zero;
            int vi;		//顶点索引
            for (int i = 0; i < mLstEdgeHSs.Count; i++)
            {
                curN = mLstEdgeHSs[i].Normal;
                if (i == mLstEdgeHSs.Count - 1)
                {
                    NextN = mLstEdgeHSs[0].Normal;
                    vi = 0;
                }
                else
                {
                    NextN = mLstEdgeHSs[i + 1].Normal;
                    vi = i + 1;
                }

                if (Vector3.Dot(curN, NextN) < ANGLE_ACUTE_THRESH)
                    AddExtraHS(vi);
            }
        }

        // 对面片进行变换！变换矩阵为mtxTrans
        public override void Transform(Matrix4x4 mtxTrans)
        {
            base.Transform(mtxTrans);

            for (int i = 0; i < mLstEdgeHSs.Count; i++)
            {
                mLstEdgeHSs[i].Transform(mtxTrans);
            }

            for (int i = 0; i < mLstExtraHSs.Count; i++)
            {
                mLstExtraHSs[i].Transform(mtxTrans);
            }
        }

        // 镜像变换，镜像轴参见CConvexHullData::MIRROR_TYPE
        public override void MirrorTransform(ConvexData.MIRROR_TYPE mirror)
        {
            base.MirrorTransform(mirror);

            int nAllEdges = mLstEdgeHSs.Count;
            for (int i = 0; i < nAllEdges; ++i)
                mLstEdgeHSs[i].MirrorTransform(mirror);

            int nAllExtraHSs = mLstExtraHSs.Count;
            for (int i = 0; i < nAllExtraHSs; ++i)
                mLstExtraHSs[i].MirrorTransform(mirror);

            int nAllVTX = mLstVIDs.Count;
            int nHalfVTX = nAllVTX / 2;
            for (int i = 0; i < nHalfVTX; ++i)
            {
                int tmp = mLstVIDs[i];
                mLstVIDs[i] = mLstVIDs[nAllVTX - 1 - i];
                mLstVIDs[nAllVTX - 1 - i] = tmp;
            }
        }

        //重置，清空顶点和边平面数组
        public void Reset() 
        { 
            mLstVIDs.Clear(); 
            mLstEdgeHSs.Clear(); 
            mLstExtraHSs.Clear(); 
        }

        public int GetEdgeNum()
        { 
            return mLstVIDs.Count;
        }

        public int GetVNum()
        { 
            return mLstVIDs.Count;
        }

        public int GetExtraHSNum()
        { 
            return mLstExtraHSs.Count; 
        }

        public HalfSpace GetExtraHalfSpace(int id)
        { 
            return mLstExtraHSs[id];
        }

        public HalfSpace GetEdgeHalfSpace(int eid) 
        { 
            return mLstEdgeHSs[eid];
        }

        public int GetVID(int i)
        { 
            return mLstVIDs[i];
        }

        public void AddExtraHS(HalfSpace hs) 
        { 
            mLstExtraHSs.Add(hs); 
        }

        //添加一个元素，必须同时添加一个vid和该vid与下一个id构成的边所对应的HalfSpace
        public void AddElement(int vid, HalfSpace hs)
        { 
            mLstVIDs.Add(vid);
            mLstEdgeHSs.Add(hs);
        }

        public virtual bool Load(UEBinaryFile fs, uint version)
        {
            Normal = EditSLBinary.LoadVector3(fs);
            Dist = fs.Reader.ReadSingle();
            int elenum = fs.Reader.ReadInt32();
            for (int i = 0; i < elenum; i++)
            {
                HalfSpace hs = new HalfSpace();
                int vid = fs.Reader.ReadInt32();
                hs.Normal = EditSLBinary.LoadVector3(fs);
                hs.Dist = fs.Reader.ReadSingle();
                AddElement(vid, hs);
            }
            return true;
        }

        public virtual bool Save(UEBinaryFile fs)
        {
            EditSLBinary.SaveVector3(fs, Normal);
            fs.Writer.Write(Dist);
            int elenum = GetEdgeNum();
            fs.Writer.Write(elenum);
            for (int i = 0; i < elenum; i++)
            {
                fs.Writer.Write(mLstVIDs[i]);
                EditSLBinary.SaveVector3(fs, mLstEdgeHSs[i].Normal);
                fs.Writer.Write(mLstEdgeHSs[i].Dist);
            }
            return true;
        }

        public void SetVIDNum(int num)
        {
            mLstVIDs = new List<int>();
            //for (int i = 0; i < num; i++)
            //    mLstVIDs.Add(0);
        }

        public void SetEdgeHSNum(int num)
        {
            mLstEdgeHSs = new List<HalfSpace>(num);
            //for (int i = 0; i < num; i++)
            //    mLstEdgeHSs.Add(new HalfSpace());
        }
        
        public void SetExtraHSNum(int num) 
        { 
            mLstExtraHSs = new List<HalfSpace>(num);
            //for (int i = 0; i < num; i++)
            //    mLstExtraHSs.Add(new HalfSpace());
        }

        protected void AddExtraHS(int i)			// 对第i个顶点添加一个额外面片
        {
            Vector3 curVert = CHData.GetVertex(GetVID(i));
            int prei = (i == 0) ? GetVNum() - 1 : i - 1;

            // 求解方程！

            //构造3*3线性方程组
            Matrix mtxCoef = new Matrix(3, 3);		// 系数矩阵
            Matrix mtxConst = new Matrix(3, 1);		// 常数矩阵
            Matrix mtxResult = null;	            // 结果		

            mtxCoef.SetElement(0, 0, mLstEdgeHSs[prei].Normal.x);
            mtxCoef.SetElement(0, 1, mLstEdgeHSs[prei].Normal.y);
            mtxCoef.SetElement(0, 2, mLstEdgeHSs[prei].Normal.z);

            mtxCoef.SetElement(1, 0, mLstEdgeHSs[i].Normal.x);
            mtxCoef.SetElement(1, 1, mLstEdgeHSs[i].Normal.y);
            mtxCoef.SetElement(1, 2, mLstEdgeHSs[i].Normal.z);

            mtxCoef.SetElement(2, 0, Normal.x);
            mtxCoef.SetElement(2, 1, Normal.y);
            mtxCoef.SetElement(2, 2, Normal.z);

            //初始化常数矩阵
            mtxConst.SetElement(0, 0, mLstEdgeHSs[prei].Dist + 1.0f);
            mtxConst.SetElement(1, 0, mLstEdgeHSs[i].Dist + 1.0f);
            mtxConst.SetElement(2, 0, Dist);

            LEquations le = new LEquations(mtxCoef, mtxConst);
            if (!le.GetRootsetGauss(out mtxResult))
            {
                return;
            }

            Vector3 v = new Vector3((float)mtxResult.GetElement(0, 0), (float)mtxResult.GetElement(1, 0), (float)mtxResult.GetElement(2, 0));
            v -= curVert;
            HalfSpace hs = new HalfSpace();
            hs.SetNV(v, curVert);
            AddExtraHS(hs);
        }
    }
