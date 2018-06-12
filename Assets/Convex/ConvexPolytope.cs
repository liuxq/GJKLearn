
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class VertexInfo
    {
        public bool Invalid;			//是否为有效顶点，无效顶点不应出现在多面体的任何一个面片之中
        public byte Degree;				//顶点的度数，这里标志有多少个面片过此顶点，或与该顶点相邻接！
    }

    public class ConvexPolytope
    {
        public static void AddDifferentV(List<Vector3> lstVertices, Vector3 v)
        {
            Vector3 diff;
            for (int i = 0; i < lstVertices.Count; i++)
            {
                diff = lstVertices[i] - v;
                if (diff.sqrMagnitude < 1e-6f)			//如果两者相等说明有和v重复的点！因此不作任何操作！
                    return;
            }
            lstVertices.Add(v);
        }
        
        public static int FindInArray(int id, List<int> lstArr)
        {
            if (lstArr == null)
                return -1;

            for (int i = 0; i < lstArr.Count; i++)
                if (lstArr[i] == id)
                    return i;

            return -1;
        }

        public const float MAX_ERROR = 1.0e8f;

        public Vector3 Centroid { get; set; }			//待计算的整个点集的质心
        public bool ExceptionOccur { get; set; }        //简化出现异常，最终不能简化成凸4面体！
        public float ErrorBase { get; set; }
        public int OriginPatchNum { get; set; }         //原始面片数量
        public int MinPatchNum { get; set; } 			//简化到头时，所乘面片的数量！实际证明该数量不一定为4，例如一个Cube，其各个面都无法简化了，因此，其面片数量为6
        public int CurVNum { get; set; }                //当前的定点数
        public List<Vector3> LstVertecies { get; set; }         //顶点列表，只记录纯几何信息，从而方便绘制
        public List<VertexInfo> LstVertexInfo { get; set; }     //顶点的相关信息列表，应和顶点列表保持一致
        public List<Patch> LstPatches { get; set; }             //所有的面片列表，采用链表结构存储，便于删除操作！

        public static float HULL2D_HALF_THICKNESS = 0.05f;	//2D Convex Hull生成ConvexPolytope时的一半厚度    
        public static void SetHull2DThickness(float fThickness)
        {
            HULL2D_HALF_THICKNESS = fThickness * 0.5f;
        }

        protected List<ushort> mLstIndices;				//绘制时采用三角形方法，用以三角形的顶点id!
        protected List<ushort> mLstLEPIndices;			//绘制当前最小误差面片的三角形顶点id
        protected List<ushort> mLstLEPNIndices;			//绘制当前最小误差面片的邻域面片三角形顶点id
        protected int mCurVNum;							//当前有效的顶点数，由于每次在删除一个面时，会增加顶点，该变量记录了当前层次时的顶点数量
        protected List<Vector3> mLstVerticesForRender;	//经过变换后得到的顶点的坐标，直接用于绘制	
        protected Patch mCurLeastErrorPatch;			//记录当前删除误差最小的面片
        protected List<float> mLstRemovedError;			//删除误差数组，记录下来每一次的删除误差

        //删除面片的邻域备份结构
        private struct NeighborBak
        {
            public Patch PatchCur;				//当前邻域在LstPatches中的引用
            public Patch PatchBak;				//保存邻域指针内容的一个备份
            public NeighborBak(Patch cur, Patch bak)
            {
                PatchCur = cur;
                PatchBak = bak;
            }
        };

        private struct RemoveOperatorRecord				//一次删除操作所需记录的信息
        {
            public Patch Removed;			            //被删除的面片指针
            public List<NeighborBak> LstNeighborBak;    //其邻域信息
            public int VNumAdded;                       //增加的顶点数量
        };

        private List<RemoveOperatorRecord> mLstRemoveOperatorRecords = new List<RemoveOperatorRecord>();	//记录依次删除操作的数组，从而可以完成撤销，反复等操作
        private int mCurOperator;           	                        //当前操作所在位置

        public ConvexPolytope()
        {
            mLstIndices = new List<ushort>();					    //初始化为一个大值
            mLstLEPIndices = new List<ushort>();					//最小误差面片的三角形个数*3
            mLstLEPNIndices = new List<ushort>();					//最小误差面片的邻域面片三角形个数*3

            mLstVerticesForRender = new List<Vector3>();		    //初始化渲染用到的顶点
            OriginPatchNum = 0;
            mCurOperator = -1;

            mLstRemovedError = null;
            ErrorBase = 1.0f;					//给一个缺省值1.0f，即绝对误差
            MinPatchNum = 4;
            ExceptionOccur = false;
            Centroid = Vector3.zero;
            HalfSpace.SetDistThresh();  //将Halfspace恢复到缺省的阈值
        }

        public AABB GetAABB()
        {
            AABB aabb = new AABB();
            for (int i = 0; i < LstVertecies.Count; i++)
            {
                //只考虑度数必须不小于3的顶点
                if (LstVertexInfo[i].Degree < (byte)3)
                    continue;

                aabb.AddVertex(LstVertecies[i]);
            }
            aabb.CompleteCenterExts();

            return aabb;
        }

        public Patch IsVertexOnPatch(Vector3 v, float fInflateRadius)
        {
            HalfSpace hs = new HalfSpace();
            Patch cur = null;
            for (int i = 0; i < LstPatches.Count; i++)
            {
                cur = LstPatches[i];
                hs.Normal = cur.Normal;
                hs.Dist = cur.Dist + fInflateRadius;
                if (hs.OnPlane(v))
                    return cur;
            }
            return null;
        }

        public bool IsVertexInside(Vector3 v, float fInflateRadius)
        {
            HalfSpace hs = new HalfSpace();
            Patch cur = null;
            for (int i = 0; i < LstPatches.Count; i++)
            {
                cur = LstPatches[i];
                hs.Normal = cur.Normal;
                hs.Dist = cur.Dist + fInflateRadius;
                if (hs.Inside(v))
                    return false;
            }
            return true;
        }

        public bool IsVertexOutside(Vector3 v, float fInflateRadius = 0.0f)
        {
            HalfSpace hs = new HalfSpace();
            Patch cur = null;
            for (int i = 0; i < LstPatches.Count; i++)
            {
                cur = LstPatches[i];
                hs.Normal = cur.Normal;
                hs.Dist = cur.Dist + fInflateRadius;
                if (hs.Outside(v))
                    return true;
            }
            return false;
        }

        public bool Init(GiftWrap gw)
        {
            if (gw.ExceptionOccur())
                return false;

            Reset();							//重置为初始状态

            Centroid = gw.Centroid;				//质心置为初始化gw的质心

            //分情况考虑构造CConvexPolytope
            if (gw.HullType == GiftWrap.HULL_TYPE.HULL_3D)	//3D Hull的情况	
            {
                int[] map = new int[gw.LstVertex.Count];		//构造一个映射表
                int count = 0;

                //添加有效顶点&构造映射表
                int i;
                for (i = 0; i < gw.LstVertex.Count; i++)
                {
                    map[i] = -1;
                    if (gw.LstExtremeVertex[i])
                    {
                        //当前点有效，用其初始化v，并将其插入到顶点列表
                        Vector3 v = gw.LstVertex[i];
                        LstVertecies.Add(v);
                        LstVertexInfo.Add(new VertexInfo());
                        map[i] = count++;
                    }
                }

                //构造面片
                //添加三角形面片
                for (i = 0; i < gw.LstFaces.Count; i++)
                {
                    Face f = gw.LstFaces[i];
                    if (!f.InPolygon)		//不属于任何多边形，添加
                    {
                        Vector3 v1 = gw.LstVertex[f.v1];
                        Vector3 v2 = gw.LstVertex[f.v2];
                        Vector3 v3 = gw.LstVertex[f.v3];
                        Patch pat = new Patch(this);
                        pat.Set(v1, v2, v3);			//几何信息

                        //依次向Neighbors中添加元素
                        VPNeighbor vpn = new VPNeighbor();
                        List<VPNeighbor> lstNeighbor = pat.LstNeighbors;

                        vpn.Vid = map[f.v1];				//这里必须作一个映射
                        lstNeighbor.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = map[f.v2];
                        lstNeighbor.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = map[f.v3];
                        lstNeighbor.Add(vpn);

                        //各顶点度数增1
                        LstVertexInfo[map[f.v1]].Degree++;
                        LstVertexInfo[map[f.v2]].Degree++;
                        LstVertexInfo[map[f.v3]].Degree++;

                        //添加到链表
                        LstPatches.Add(pat);
                    }
                }

                //添加多边形面片
                for (i = 0; i < gw.LstPlanes.Count; i++)
                {
                    List<int> planes = gw.LstPlanes[i];

                    //取前三个点构造平面几何信息
                    Vector3 v1 = gw.LstVertex[planes[0]];
                    Vector3 v2 = gw.LstVertex[planes[1]];
                    Vector3 v3 = gw.LstVertex[planes[2]];
                    Patch pat = new Patch(this);
                    pat.Set(v1, v2, v3);			//几何信息			

                    //依次向Neighbors中添加元素
                    VPNeighbor vpn = new VPNeighbor();
                    List<VPNeighbor> lstNeighbors = pat.LstNeighbors;

                    for (int j = 0; j < planes.Count; j++)
                    {
                        vpn = new VPNeighbor();
                        vpn.Vid = map[planes[j]];			//这里必须作一个映射
                        lstNeighbors.Add(vpn);

                        LstVertexInfo[vpn.Vid].Degree++;		//顶点度数增1
                    }

                    //添加到链表
                    LstPatches.Add(pat);
                }
            }
            else
            {

                //说明是2D Hull的情况
                List<int> lstCHVs = gw.GetCHVertecies();
                if (lstCHVs == null)
                    return false;
                if (lstCHVs.Count < 3)		//至少是一个三角形
                    return false;

                //顶点信息
                VertexInfo vInfo = new VertexInfo();
                vInfo.Degree = 3;			//直棱拄所有顶点的面度数均为3

                HalfSpace planeOut = new HalfSpace();
                HalfSpace planeIn = new HalfSpace();

                //取前三个点构造平面几何信息
                Vector3 v1 = gw.LstVertex[lstCHVs[0]];
                Vector3 v2 = gw.LstVertex[lstCHVs[1]];
                Vector3 v3 = gw.LstVertex[lstCHVs[2]];

                //构造两个平面PlaneOut和PlaneIn，分别表示顶面和底面
                planeOut.Set(v1, v2, v3);

                planeIn.Normal = -planeOut.Normal;
                planeIn.Dist = -planeOut.Dist;

                planeIn.Translate(HULL2D_HALF_THICKNESS);
                planeOut.Translate(HULL2D_HALF_THICKNESS);

                Vector3 vOutNormal = planeOut.Normal;
                Vector3 vInNormal = planeIn.Normal;

                //分别求出PlaneOut,PlaneIn上的两点
                Vector3 vOut = v1 + HULL2D_HALF_THICKNESS * vOutNormal;
                Vector3 vIn = v1 + HULL2D_HALF_THICKNESS * vInNormal;

                //构造顶点及顶点信息
                int i;
                for (i = 0; i < lstCHVs.Count; i++)
                {
                    //同时添加底面和顶面的一个顶点
                    Vector3 vec1 = gw.LstVertex[lstCHVs[i]];
                    Vector3 vec2 = vec1;
                    if (i < 3)
                    {
                        vec1 += HULL2D_HALF_THICKNESS * vOutNormal;
                        vec2 += HULL2D_HALF_THICKNESS * vInNormal;
                    }
                    else
                    {
                        Vector3 vDiff = vec1 - vOut;
                        vec1 -= Vector3.Dot(vDiff, vOutNormal) * vOutNormal;

                        vDiff = vec2 - vIn;
                        vec2 -= Vector3.Dot(vDiff, vInNormal) * vInNormal;
                    }

                    LstVertecies.Add(vec1);
                    LstVertecies.Add(vec2);

                    //相应的，添加两个顶点信息
                    LstVertexInfo.Add(vInfo);
                    LstVertexInfo.Add(vInfo);
                }

                //开始构造平面面片

                //向外的面
                Patch pat = new Patch(this);
                pat.Normal = vOutNormal;			//几何信息
                pat.Dist = planeOut.Dist;

                //依次向Neighbors中添加元素
                VPNeighbor vpn = new VPNeighbor();
                List<VPNeighbor> lstNeighbors1 = pat.LstNeighbors;
                for (i = 0; i < lstCHVs.Count; i++)
                {
                    vpn = new VPNeighbor();
                    vpn.Vid = 2 * i;
                    lstNeighbors1.Add(vpn);
                }
                //添加到链表
                LstPatches.Add(pat);

                //向内的面

                pat = new Patch(this);
                pat.Normal = vInNormal;			//几何信息
                pat.Dist = planeIn.Dist;

                //依次向Neighbors中添加元素
                List<VPNeighbor> lstNeighbors2 = pat.LstNeighbors;
                //顶面按照逆序添加
                for (i = lstCHVs.Count - 1; i >= 0; i--)
                {
                    vpn = new VPNeighbor();
                    vpn.Vid = 2 * i + 1;
                    lstNeighbors2.Add(vpn);
                }
                //添加到链表
                LstPatches.Add(pat);

                //开始添加各个侧面
                for (i = 0; i < lstCHVs.Count; i++)
                {
                    pat = new Patch(this);
                    List<VPNeighbor> lstNeighbors = pat.LstNeighbors;

                    //每个侧面都是一个矩形
                    if (i < lstCHVs.Count - 1)
                    {
                        v1 = LstVertecies[2 * i + 2];
                        v2 = LstVertecies[2 * i];
                        v3 = LstVertecies[2 * i + 1];
                        pat.Set(v1, v2, v3);

                        vpn = new VPNeighbor();
                        vpn.Vid = 2 * i;
                        lstNeighbors.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = 2 * i + 1;
                        lstNeighbors.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = 2 * i + 3;
                        lstNeighbors.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = 2 * i + 2;
                        lstNeighbors.Add(vpn);

                    }
                    else
                    {
                        //最后一个矩形的情况比较特殊
                        v1 = LstVertecies[0];
                        v2 = LstVertecies[2 * i];
                        v3 = LstVertecies[2 * i + 1];
                        pat.Set(v1, v2, v3);

                        vpn = new VPNeighbor();
                        vpn.Vid = 2 * i;
                        lstNeighbors.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = 2 * i + 1;
                        lstNeighbors.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = 1;
                        lstNeighbors.Add(vpn);

                        vpn = new VPNeighbor();
                        vpn.Vid = 0;
                        lstNeighbors.Add(vpn);
                    }
                    LstPatches.Add(pat);
                }
            }


            OriginPatchNum = LstPatches.Count;
            CurVNum = LstVertecies.Count;

            //初始化删除误差
            if (mLstRemovedError == null)
                mLstRemovedError = new List<float>(OriginPatchNum + 1);
            mLstRemovedError.Clear();
            for (int i = 0; i < OriginPatchNum + 1; i++)
                mLstRemovedError.Add(-1.0f);			//0,1,2,3都置为无效值

            ExceptionOccur = false;

            //计算每个patch的邻域patch
            ComputePatchNeighbors();

            //HalfSpace::SetDistThresh(1e-3);  //恢复到缺省的阈值

            //寻找最小删除误差对应的面片
            SearchLeastErrorPatch();

            //开始简化，简化到头
            ReduceAll();

            return true;
        }

        public bool Init(GiftWrap gw, float error)
        {
            //重载一个Init的方法，避免顺序耦合
            ErrorBase = error;
            return Init(gw);
        }

        public void Goto(float fError = 0.1f)     //直接跳到误差不大于fError指定的简化层次，缺省为10%
        {
            if (fError < 0) return;
            int LeftPatchesNum = GetLPNByError(fError);
            Goto(LeftPatchesNum);
        }

        public bool Goto(int LeftPatchesNum)
        {
            if (LeftPatchesNum < MinPatchNum || LeftPatchesNum > OriginPatchNum)
                return false;
            int i;
            int RemovedPatchesNum = OriginPatchNum - LeftPatchesNum;
            int UnRemoveTimes = mCurOperator + 1 - RemovedPatchesNum;
            if (UnRemoveTimes > 0)
            {
                for (i = 0; i < UnRemoveTimes; i++)
                    UndoRemove();
            }
            else
            {
                for (i = 0; i < -UnRemoveTimes; i++)
                    RedoRemove();
            }
            return true;
        }

        public void UndoRemove()				//撤销操作，向前，即恢复到后面的操作
        {
            if (mCurOperator == -1)			//已经回溯到最初始的状态了，不能继续回溯了
                return;

            RemoveOperatorRecord ror = mLstRemoveOperatorRecords[mCurOperator];

            //插入当前操作记录中删除的面片
            LstPatches.Add(ror.Removed);
            ror.Removed.IncreVDegree();			//顶点度数都增1

            //修改当前顶点数量
            CurVNum -= ror.VNumAdded;

            Patch cur = null;
            Patch bak = null;
            Patch patchTemp = new Patch(this);
            int nNBNum = ror.LstNeighborBak.Count;
            for (int i = 0; i < nNBNum; i++)
            {
                cur = ror.LstNeighborBak[i].PatchCur;
                bak = ror.LstNeighborBak[i].PatchBak;
                patchTemp.Set(cur);
                cur.Set(bak);
                bak.Set(patchTemp);
            }

            mCurOperator--;						//前移一步

            mCurLeastErrorPatch = ror.Removed;		//修改当前误差最小面片
        }

        public void RedoRemove()				//撤销操作，向后，即回溯到以前的操作
        {
            int RemOprCount = mLstRemoveOperatorRecords.Count;
            if (mCurOperator == RemOprCount - 1)	//已经是最后一个操作了，不能再往前恢复了
                return;

            RemoveOperatorRecord ror = mLstRemoveOperatorRecords[mCurOperator + 1];

            //再一次删除当前操作记录中删除的面片，因为前面的恢复过程已经将其插入了
            //注意：该面片应该是被插入在整个listpatch的最后！因为前面的恢复操作，将其插入到尾部了。
            //这样，就可以避免再作一次遍历寻找！
            Patch ptail = LstPatches[LstPatches.Count - 1];
            LstPatches.RemoveAt(LstPatches.Count - 1);
            ror.Removed.DecreVDegree();			//顶点度数都减1

            //修改当前顶点数量
            CurVNum += ror.VNumAdded;

            Patch cur = null;
            Patch bak = null;
            Patch patchTemp = new Patch(this);
            int nNBNum = ror.LstNeighborBak.Count;
            for (int i = 0; i < nNBNum; i++)
            {
                cur = ror.LstNeighborBak[i].PatchCur;
                bak = ror.LstNeighborBak[i].PatchBak;
                patchTemp.Set(cur);
                cur.Set(bak);
                bak.Set(patchTemp);
            }

            mCurOperator++;		//前移一步

            if (mCurOperator == RemOprCount - 1)		//后移一步后变成了最后一个操作
                SearchLeastErrorPatch();		//此时要重新计算最小误差面片
            else
                mCurLeastErrorPatch = mLstRemoveOperatorRecords[mCurOperator + 1].Removed;		//修改当前误差最小面片
        }

        public void ReduceAll()				//删除所有能删的面片，简化到头！
        {
            int curPatchNum = OriginPatchNum;
            do
            {
                mLstRemovedError[curPatchNum] = GetCurLeastError() / ErrorBase;
                curPatchNum--;
            } while (RemoveLeastErrorPatch());

            MinPatchNum = ++curPatchNum;

            if (curPatchNum < 4)		//不可能比4还少！
                ExceptionOccur = true;
        }

        public bool RedoAll()					//恢复到头？
        {
            return (mCurOperator == mLstRemoveOperatorRecords.Count - 1);
        }

        public bool UndoAll()					//撤销到头？
        {
            if (mCurOperator == -1)		//说明撤销到头了
                return true;
            else
                return false;
        }

        //添加一个顶点
        public void AddV(Vector3 v, VertexInfo vInfo)
        {
            LstVertecies.Add(v);
            LstVertexInfo.Add(vInfo);
        }

        //导处数据到ConvexData中
        public void ExportCHData(ConvexData chData)
        {
            List<int> lstMap = new List<int>();		//一个由顶点在本类中id到CConvexHullData中id的一个映射表；

            //遍历每一个Patch
            for (int i = 0; i < LstPatches.Count; i++)
            {
                Patch pat = LstPatches[i];
                Vector3 n = pat.Normal;
                CovFace face = new CovFace();
                face.Normal = n;
                face.Dist = pat.Dist;

                HalfSpace hs = new HalfSpace();
                for (int j = 0; j < pat.GetVNum(); j++)
                {
                    int vid = pat.GetVID(j);

                    //构造垂直于该边法向朝外的HalfSpace
                    Vector3 v1 = Vector3.zero;
                    Vector3 v2 = Vector3.zero;
                    pat.GetEdge(j, ref v1, ref v2);
                    hs.Set(v1, v2, v2 + n);
                    int ExistID = FindInArray(vid, lstMap);
                    if (ExistID == -1)
                    {
                        //说明是新的顶点

                        //插入到CConvexHullData的Vertices中
                        chData.AddVertex(v1);
                        int newID = chData.GetVertexNum() - 1;	//在pCHData中的id
                        face.AddElement(newID, hs);

                        lstMap.Add(vid);
                    }
                    else
                    {
                        //说明是已经存在的顶点
                        face.AddElement(ExistID, hs);
                    }
                }

                //插入该面片
                chData.AddFace(face);

            }

            // 计算额外边界半空间
            chData.ComputeFaceExtraHS();

        }

        //计算凸多面体与平面组相交所得到的交点投影到XOZ平面构成的2D凸包
        //要求，平面法向不能垂直于Y轴，即平面不能与Y平行，一般的，我们的求交平面都垂直于Y轴
        //交点少于2个，否则返回false；
        //传出参数：gw2d表明了由这些交点构成的2D凸包投影到了XOZ平面
        //通过参数b2ParallelPlanes，表明计算是否为求解两平行平面截得的部分的2D凸包
        bool IntersectPlanesProj2XOZ(List<HalfSpace> lstPlanes, GiftWrap2D gw2d, bool b2ParallelPlanes = false)
        {
            Vector3 n1 = Vector3.zero;
            Vector3 n2 = Vector3.zero;
            if (b2ParallelPlanes)
            {
                //两平行面片凸包
                if (lstPlanes.Count != 2) return false;
                n1 = lstPlanes[0].Normal;
                n2 = lstPlanes[1].Normal;
                if (!(n1 == n2 || n1 == -n2))
                    return false;
            }

            if (lstPlanes.Count < 1) return false;

            int i, j, k;
            //判断Planes的法向
            for (i = 0; i < lstPlanes.Count; i++)
            {
                //平面与Y轴的夹角至少为30 degree
                //该阈值可调
                if (Mathf.Abs(lstPlanes[i].Normal.y) < 0.5f)
                {
                    return false;
                }
            }

            //开始求交运算
            List<Vector3> lstVertices = new List<Vector3>();		//存放交点的顶点集
            //对每一个面片遍历
            for (i = 0; i < LstPatches.Count; i++)
            {
                Patch curpatch = LstPatches[i];
                Vector3 v1 = Vector3.zero;
                Vector3 v2 = Vector3.zero;
                //逐边进行考虑
                for (k = 0; k < curpatch.GetVNum(); k++)
                {
                    curpatch.GetEdge(k, ref v1, ref v2);
                    for (j = 0; j < lstPlanes.Count; j++)
                    {
                        Vector3 intersect = Vector3.zero;
                        int res = lstPlanes[j].IntersectLineSeg(v1, v2, ref intersect);
                        switch (res)
                        {
                            case 0:
                                break;
                            case 1:
                                AddDifferentV(lstVertices, v1);
                                break;
                            case 2:
                                AddDifferentV(lstVertices, v2);
                                break;
                            case 3:
                                AddDifferentV(lstVertices, v1);
                                AddDifferentV(lstVertices, v2);
                                break;
                            case 4:
                                AddDifferentV(lstVertices, intersect);
                                break;
                            default:
                                break;
                        }
                    }
                }

            }

            if (b2ParallelPlanes)
            {
                //还需添加本凸多面体在两平面间的顶点
                if (n1 == -n2)
                    lstPlanes[1].Inverse();	//对P2转向，使得P1,P2法向相同

                //遍历所有有效顶点
                for (i = 0; i < CurVNum; i++)
                    if (LstVertexInfo[i].Degree >= 3) 		//有效顶点
                        if ((lstPlanes[0].Outside(LstVertecies[i]) && lstPlanes[1].Inside(LstVertecies[i])) ||
                           (lstPlanes[0].Inside(LstVertecies[i]) && lstPlanes[1].Outside(LstVertecies[i])))
                            lstVertices.Add(LstVertecies[i]);
            }

            //求交完毕，开始构造2D凸包
            int vNum = lstVertices.Count;
            if (vNum < 3) return false;

            List<Vector3> lstVs = new List<Vector3>(vNum);
            for (i = 0; i < vNum; i++)
                lstVs[i] = lstVertices[i];

            gw2d.SetVertexes(lstVs);
            gw2d.ComputeConvexHull();

            return true;
        }

        public float GetCurLeastError()
        {
            if (mCurLeastErrorPatch != null)
                return mCurLeastErrorPatch.RemovedError;
            return -1.0f;		//否则，一个无效值
        }

        protected Patch SearchLeastErrorPatch()
        {
            int PatchNum = LstPatches.Count;
            if (PatchNum <= 6)
            {
                //如果多面体的面数小于等于6，则不在进行简化了！
                mCurLeastErrorPatch = null;
                return null;
            }


            //查找误差最小的patch
            Patch curPatch = null;
            Patch leastErrorPatch = null;
            float leastError = MAX_ERROR;			//初始化为一个大值
            for (int i = 0; i < PatchNum; i++)
            {
                curPatch = LstPatches[i];
                float error = curPatch.RemovedError;
                if (error != -1.0f && error < leastError)
                {
                    leastError = error;
                    leastErrorPatch = curPatch;
                }
            }

            if (leastError < 0.2)			//该误差应该为大于等于0
                mCurLeastErrorPatch = leastErrorPatch;
            else	//说明没有最小误差，不能再删除了
                mCurLeastErrorPatch = null;

            return mCurLeastErrorPatch;
        }

        protected bool RemovePatch(Patch pat)		//删除一个面片
        {
            RemoveOperatorRecord ror = new RemoveOperatorRecord();
            ror.Removed = pat;
            ror.LstNeighborBak = new List<NeighborBak>();

            //将邻域面片复制并保存好
            Patch bakNP = null;
            Patch curNP = null;
            List<VPNeighbor> lstNeighbors = pat.LstNeighbors;
            int nbsize = lstNeighbors.Count;
            int i;
            for (i = 0; i < nbsize; i++)
            {
                curNP = lstNeighbors[i].NeighborPatch;
                bakNP = new Patch(curNP);				//复制给patchNeighborBak

                //由于要考虑撤销和恢复操作，因此这里似乎不能直接添加到最后！
                //而应改为：将mCurOperator游标其后的所有删除操作删除
                //然后再添加到最后的位置！
                ror.LstNeighborBak.Add(new NeighborBak(curNP, bakNP));
            }

            if (pat.Removed())		//删除成功
            {

                //移除当前面片
                int pos = LstPatches.IndexOf(pat);
                if (pos != -1)
                    LstPatches.RemoveAt(pos);

                //记录下来增加的顶点数
                ror.VNumAdded = LstVertecies.Count - CurVNum;
                CurVNum = LstVertecies.Count;

                //备份操作！
                mLstRemoveOperatorRecords.Add(ror);
                mCurOperator++;
                return true;
            }

            return false;
        }

        protected bool RemoveLeastErrorPatch()
        {
            //寻找最小删除误差对应的面片
            SearchLeastErrorPatch();
            if (mCurLeastErrorPatch == null)
                return false;

            return RemovePatch(mCurLeastErrorPatch);
        }

        protected int GetLPNByError(float error)
        {
            int i = OriginPatchNum;
            //改为遍历查找  //二分查找
            while (mLstRemovedError[i] < error && i > MinPatchNum)
                i--;

            return i;
        }

        protected void ComputePatchNeighbors()						//计算各个面片的领域面片，私有，应该在Init()内最后调用
        {
            for (int i = 0; i < LstPatches.Count; i++)
            {
                for (int j = 0; j < LstPatches.Count; j++)
                {
                    if (i != j)
                        LstPatches[i].Neighbor(LstPatches[j]);
                }
                //在这里添加完当前Patch的所有邻域面片后，
                //就可以计算其删除误差了
                LstPatches[i].UpdateRemovedError();
            }
        }

        protected void Reset()
        {
            LstVertecies = new List<Vector3>();
            LstVertexInfo = new List<VertexInfo>();

            //清空ListPatch
            if (LstPatches == null)
                LstPatches = new List<Patch>();

            LstPatches.Clear();

            //清空RemoveOperatorRecords
            mCurOperator = -1;
            mLstRemoveOperatorRecords.Clear(); 
        }
    }
