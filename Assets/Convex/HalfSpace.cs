
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 

    ////////////////////////////////////////////////////////////////////////////
    // 
    // The equation of a 3D plane is "mNormal * X = mD"
    // This is a little different from "Ax+By+Cz+D=0" as "mD == -D".
    // 
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class HalfSpace
    {
        // Distance transhold to decide if a posion is inside, outside or on the edge of a half space.
        private static float mDistTreash = MathUtil.FLOAT_EPSILON;

        public Vector3 mNormal = Vector3.up;
        public float mD = 0.0f;

        public Vector3 Normal
        {
            get { return mNormal; }
            set { mNormal = value; }
        }

        public float Dist
        {
            get { return mD; }
            set { mD = value; }
        }

        public HalfSpace()
        {

        }

        public HalfSpace(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Set(v1, v2, v3);
        }

        public HalfSpace(HalfSpace hs)
        {
            mNormal = hs.Normal;
            mD = hs.Dist;
        }

        public static void SetDistThresh(float treash = MathUtil.FLOAT_EPSILON)
        {
            mDistTreash = treash;
        }

        public void Set(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            mNormal = Vector3.Cross(v2 - v1, v3 - v1);
            mNormal.Normalize();
            SetNV(mNormal, v1);
        }

        // Set a plane using a normal and position
        public void SetNV(Vector3 normal, Vector3 v)
        {
            mNormal = normal;
            mNormal.Normalize();
            mD = Vector3.Dot(mNormal, v);
        }

        // Transform the space that define the half space.
        public virtual void Transform(Matrix4x4 mtxTrans)
        {
            //从mtxTrans分解出Scale,Rotate,Translate分量,unity用的是右乘矩阵所以要横取呢就转一下//

            Matrix4x4 dxmatrix = mtxTrans.transpose;
            Vector3 trans = new Vector3(dxmatrix.GetRow(3).x, dxmatrix.GetRow(3).y, dxmatrix.GetRow(3).z);
            Vector3 scalevec = new Vector3(dxmatrix.GetColumn(0).x, dxmatrix.GetColumn(0).y, dxmatrix.GetColumn(0).z);
            float scale = scalevec.magnitude;

            Matrix3x3 mtx3rotate = new Matrix3x3(dxmatrix.m00 / scale, dxmatrix.m01 / scale, dxmatrix.m02 / scale, dxmatrix.m10 / scale, dxmatrix.m11 / scale, dxmatrix.m12 / scale, dxmatrix.m20 / scale, dxmatrix.m21 / scale, dxmatrix.m22 / scale);
            
            //对于平面方程N.X=D的变换(s,R,t)如下：
            // Nomral transform: N'=NR
            mNormal = mtx3rotate * mNormal;
            mNormal.Normalize();
            // Distance transform D'=s*D+N'.t
            mD = scale * mD + Vector3.Dot(mNormal, trans);
        }

        public virtual void MirrorTransform(ConvexData.MIRROR_TYPE mirror)
        {
            float tmp;
            switch (mirror)
            {
                case ConvexData.MIRROR_TYPE.MIRROR_AXIS_X:
                    {
                        mNormal.x = -mNormal.x;
                    }
                    break;
                case ConvexData.MIRROR_TYPE.MIRROR_AXIS_Y:
                    {
                        mNormal.y = -mNormal.y;
                    }
                    break;
                case ConvexData.MIRROR_TYPE.MIRROR_AXIS_Z:
                    {
                        mNormal.z = -mNormal.z;
                    }
                    break;
                case ConvexData.MIRROR_TYPE.MIRROR_AXIS_XY:
                    {
                        tmp = mNormal.x;
                        mNormal.x = mNormal.y;
                        mNormal.y = tmp;
                    }
                    break;
                case ConvexData.MIRROR_TYPE.MIRROR_AXIS_YZ:
                    {
                        tmp = mNormal.y;
                        mNormal.y = mNormal.z;
                        mNormal.z = tmp;
                    }
                    break;
                case ConvexData.MIRROR_TYPE.MIRROR_AXIS_XZ:
                    {
                        tmp = mNormal.x;
                        mNormal.x = mNormal.z;
                        mNormal.z = tmp;
                    }
                    break;
                default:
                    break;
            }
        }

        public void Translate(float delta)
        {
            mD += delta;
        }

        // inverse the front-back of the half space
        public void Inverse()
        {
            mD = -mD;
            mNormal = -mNormal;
        }

        // Distance to this plane
        public float Dist2Plane(Vector3 pos)
        {
            float d = Vector3.Dot(pos, mNormal);
            return (d - mD);
        }

        // The definaton of "inside/outside and on" the plane.
        //				   /\
        //		          /||\
        //				   ||
        //				   || normal    (Outside)
        //------------------------------------------Plane
        //								(Inside)
        //

        public bool Inside(Vector3 pos)
        {
            return Dist2Plane(pos) < -mDistTreash;
        }

        public bool Outside(Vector3 pos)
        {
            return Dist2Plane(pos) > mDistTreash;
        }

        public bool OnPlane(Vector3 pos)
        {
            return Mathf.Abs(Dist2Plane(pos)) <= mDistTreash;
        }

        public Vector3 GetPorjectPos(Vector3 pos)
        {
            return pos - Dist2Plane(pos) * mNormal;
        }


        ///////////////////////////////////////////////////
        // If this plane intersect with a segement.
        // Return Value:
        // 0: Not intersect
        // 1: V1 is on the plane
        // 2: V2 is on the plane
        // 3: V1 and V2 are both on the plane
        // 4: intersect on some position
        ///////////////////////////////////////////////////
        public int IntersectLineSeg(Vector3 v1, Vector3 v2, ref Vector3 inter)
        {
            inter = Vector3.zero;
            bool v1OnPlane = OnPlane(v1);
            bool v2OnPlane = OnPlane(v2);
            if (v1OnPlane && v2OnPlane)
                return 3;
            else if (v1OnPlane)
                return 1;
            else if (v2OnPlane)
                return 2;

            if (Outside(v1) == Outside(v2))
                return 0;

            // calculate the intersect point
            Vector3 vd = v2 - v1;
            float t = mD - Vector3.Dot(mNormal, v1);
            t /= Vector3.Dot(mNormal, vd);
            inter = v1 + t * vd;
            return 4;
        }

        // compute the best-fit plane via an array of vertices, return true if all vertices's 
        // distances to plane is less than MaxDistError.
        public static bool BestFitPlane(Vector3[] verts, ref HalfSpace plane, float maxDistErr)
        {
            Vector4 p = Vector4.zero;
            BestFit.ComputeBestFitPlane(verts, null, ref p);
            Vector3 normal = new Vector3(p[0], p[1], p[2]);
            plane.Normal = normal;
            plane.Dist = -p[3];

            // now check the distance error...
            for (int i = 0; i < verts.Length; i++)
            {
                if (plane.Dist2Plane(verts[i]) > maxDistErr)
                    return false;
            }

            return true;
        }
    }

    public class VPNeighbor
    {
        public int Vid;					        //顶点在polytope的全局id
        public Patch NeighborPatch;		        //对应于 该顶点与下一个顶点构成的边的邻接平面片
    };

    public class Patch : HalfSpace
    {
        public float mRemovedError;				    //移除误差：如果将该面片从多面体中移除，所带来的误差！
        List<VPNeighbor> mLstNeighbors;		        //顶点及相邻的面片
        ConvexPolytope mConvexPolytope;			    //该面片所属的多面体的指针；

        public List<VPNeighbor> LstNeighbors
        {
            get { return mLstNeighbors; }
            private set { mLstNeighbors = value; }
        }

        public float RemovedError
        {
            get { return mRemovedError; }
            set { mRemovedError = value; }
        }

        public Patch(ConvexPolytope cp)
        {
            mLstNeighbors = new List<VPNeighbor>();
            mConvexPolytope = cp;
        }

        public Patch(Patch pat)
        {
            mLstNeighbors = pat.LstNeighbors;
            mRemovedError = pat.RemovedError;
            mConvexPolytope = pat.mConvexPolytope;
        }

        public void Set(Patch pat)
        {
            Normal = pat.Normal;
            Dist = pat.Dist;
            mRemovedError = pat.mRemovedError;
            mConvexPolytope = pat.mConvexPolytope;

            if (pat.LstNeighbors == null)
            {
                mLstNeighbors = null;
                return;
            }

            if (mLstNeighbors == null)
                mLstNeighbors = new List<VPNeighbor>();

            mLstNeighbors.Clear();
            for (int i = 0; i < pat.LstNeighbors.Count; i++)
            {
                mLstNeighbors.Add(pat.LstNeighbors[i]);
            }
        }

        public void DecreVDegree()
        {
            //int vid;
            for (int i = 0; i < mLstNeighbors.Count; i++)
            {
                //vid = mLstNeighbors[i].Vid;
                mConvexPolytope.LstVertexInfo[i].Degree--;
            }
        }

        public void IncreVDegree()
        {
            //int vid;
            for (int i = 0; i < mLstNeighbors.Count; i++)
            {
                //vid = mLstNeighbors[i].Vid;
                mConvexPolytope.LstVertexInfo[i].Degree++;
            }
        }

        public bool InNeighbors(Patch pat)
        {
            for (int i = 0; i < mLstNeighbors.Count; i++)
            {
                if (pat == mLstNeighbors[i].NeighborPatch)
                    return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////
        // 本类最核心的方法： 将本面片从凸多面体中删除！
        // 涉及到了很多相关的操作，包括：
        // 计算新的顶点、新顶点的添加、本面片的删除、及邻域面片的更新等操作
        //////////////////////////////////////////////////////////////////////
        public bool Removed()
        {
            int npCount = mLstNeighbors.Count;
            if (npCount < 3)
                return false;

            //一些重要的数据结构
            List<Vector3> lstVerteciesAdded = new List<Vector3>();			//删除后将产生的新的顶点集
            List<byte> lstVDegree = new List<byte>();						//新产生顶点的度数，即邻接平面数。与上动态数组一一对应

            //哪些邻域平面相交于相同的顶点
            //对应于上面的要添加的顶点
            //如对于添加的顶点v,其在VerteciesAdded中的id为2
            //则PatchesIntersectV[2]对应了一个动态数组，其元素
            //即为相交于点v的所有patch领域面片的id
            //＝＝＝＝＝＝＝注意：＝＝＝＝＝＝
            //AArray<AArray<int,int>,AArray<int,int>>在编译时会报错！
            //说明模板类的使用暂时不支持嵌套定义。因此，这里采用了指针！
            List<List<int>> PatchesIntersectV = new List<List<int>>();

            //邻域面片包含了哪些新加入的顶点
            List<List<int>> VerticesInPatch = new List<List<int>>(npCount);
            int i;
            for (i = 0; i < npCount; i++)
            {
                //初始化VerticesInPatch的每一个元素
                VerticesInPatch.Add(new List<int>());
            }

            /////////////////////////////////////////////////////////////////////
            // 求解新交点部分！
            /////////////////////////////////////////////////////////////////////

            List<int> lstN = new List<int>();		    //存储选出的三个相邻面片的id
            CombGenerator cg = new CombGenerator(3, npCount);
            Vector3 v = Vector3.zero;
            bool valid;

            //主循环
            while (!cg.Over)
            {
                //产生一个组合
                cg.GetNextComb(lstN);
                if (Processed(lstN, PatchesIntersectV))			//已经处理过，直接跳过，进入下一轮循环！
                    continue;

                if (Solve3NPIntersection(lstN, ref v))				//有唯一解
                {
                    valid = true;

                    //如果有解，先代入到当前平面的方程
                    //注意：考察当前面片时，则应该是不在当前面片的内部！
                    //在该平面上或其外部都可以
                    if (this.Inside(v))
                        continue;			//直接进入下一轮while循环
                    List<int> lstPatchesPassV = new List<int>();		//定义一个过v的所有面片的动态数组

                    //依次代入其他的邻域面片方程检验
                    for (i = 0; i < npCount; i++)
                    {
                        if (i != lstN[0] && i != lstN[1] && i != lstN[2])
                        {
                            //不能在这些面片的外部
                            if (mLstNeighbors[i].NeighborPatch.Outside(v))
                            {
                                valid = false;
                                break;		//在某个平面外部，跳出for循环
                            }
                            //在平面上的情况
                            if (mLstNeighbors[i].NeighborPatch.OnPlane(v))
                            {
                                lstPatchesPassV.Add(i);			//出现共面的情况！
                            }
                        }
                    }

                    if (valid)	//有效的交点
                    {
                        lstVerteciesAdded.Add(v);		//添加到新有效顶点列表

                        //添加lstN[0,1,2]到相交于该顶点的邻域平面列表
                        lstPatchesPassV.Add(lstN[0]);
                        lstPatchesPassV.Add(lstN[1]);
                        lstPatchesPassV.Add(lstN[2]);

                        //modified by wf, 04-10-09
                        //为保证前后计算的一致性而作的代码修改：
                        int ExistPIV;
                        if ((ExistPIV = HasPIntersectVExist(lstPatchesPassV, PatchesIntersectV)) == -1)
                        {
                            //不存在不一致的情况

                            //添加上述列表到PatchesIntersectV中
                            PatchesIntersectV.Add(lstPatchesPassV);

                            //将顶点度数记录下来
                            lstVDegree.Add((byte)lstPatchesPassV.Count);

                            //将该顶点在VerteciesAdded中的id添加到
                            //相交于该顶点的各个面片对应的VerticesInPatch中
                            int vid = lstVerteciesAdded.Count - 1;
                            int npid;
                            for (i = 0; i < lstPatchesPassV.Count; i++)
                            {
                                npid = lstPatchesPassV[i];
                                VerticesInPatch[npid].Add(vid);
                            }
                        }
                        else
                        {
                            //出现了不一致的情况，此时按照最后最多面片相交的情况处理！
                            lstVerteciesAdded.RemoveAt(lstVerteciesAdded.Count - 1);		//删除该点

                            List<int> lstExistPIV = PatchesIntersectV[ExistPIV];
                            PatchesIntersectV[ExistPIV] = lstPatchesPassV;
                            lstVDegree[ExistPIV] = (byte)lstPatchesPassV.Count;
                            //将该顶点在VerteciesAdded中的id添加到
                            //相交于该顶点的各个面片对应的VerticesInPatch中
                            //int vid=VerteciesAdded.GetSize()-1;
                            int npid;
                            for (i = 0; i < lstPatchesPassV.Count; i++)
                            {
                                npid = lstPatchesPassV[i];
                                if (!InArray(npid, lstExistPIV))			//如果该npid没有出现过，则添加vid
                                    VerticesInPatch[npid].Add(ExistPIV);
                            }

                            lstExistPIV.Clear();		//释放内存
                        }

                    }
                    else
                    {
                        lstPatchesPassV.Clear();        //当前顶点无效，回收前面分配的内存
                    };
                }//无解的情况暂不考虑
            }

            //如果没有合法的新交点，则这里应该返回false了
            //正常而言，应该不太回出现这种情况，因为在计算最小误差时
            //就应该排除掉了无解的情况，因此还有一个最小误差求解和本方法结果一致性的问题
            if (lstVerteciesAdded.Count == 0)
            {
                // error occured!
                mRemovedError = -1.0f;
                mConvexPolytope.ExceptionOccur = true;
                return false;
            }

            /////////////////////////////////////////////////////////////////////
            // 向Polytope中插入新交点部分！
            /////////////////////////////////////////////////////////////////////
            //记录插入前Polytope已有的顶点数目，新交点插入后的全局id则依次为：g_vid,g_vid+1,g_vid+2...
            //因此可供以后更新面片的邻域顶点列表使用
            int g_vid = mConvexPolytope.LstVertecies.Count;
            for (i = 0; i < lstVerteciesAdded.Count; i++)
            {
                VertexInfo vInfo = new VertexInfo();
                vInfo.Degree = lstVDegree[i];
                mConvexPolytope.AddV(lstVerteciesAdded[i], vInfo);
            }


            //更新之前应先将以前的邻域面片备份，这一步可以考虑放在Polytope调用Removed()函数
            //前进行！

            /////////////////////////////////////////////////////////////////////
            // 更新邻域面片的邻域列表(包括顶点和相邻面片)
            /////////////////////////////////////////////////////////////////////
            Patch curNP;			//当前的邻域面片
            int g_vid1, g_vid2;		//当前的邻域面片对应的顶点，及邻边上的另一个顶点（都是全局id）
            /////////////////////////////////////////////////////////////////////
            // ＝＝＝＝＝＝＝注意在两相邻面片中，边的方向正好相反＝＝＝＝＝
            //
            //				 当前面片P
            //             \  ---.   /
            //	对应(Pnk) v1\________/ v2 对应邻域面片(Pn2)
            //              /        \
            //             / <-----   \
            //				  邻域面片(Pn1)
            //
            /////////////////////////////////////////////////////////////////////
            int next, pre;
            List<int> lstVSorted = new List<int>();				//按照顶点连接顺序连接好的数组
            List<int> lstVertices = null;
            int ivid, lastvid;
            for (i = 0; i < npCount; i++)					//外层循环，对每一个邻域面片分别考虑！循环体内是一个邻域面片的处理
            {
                lstVSorted.Clear();				//清空上次循环中的顶点排序列表

                next = (i + 1 < npCount) ? i + 1 : 0;			//下一个neighbor的索引，由于是用数组表示环状结构，因此要作一个运算
                pre = (i - 1 < 0) ? npCount - 1 : i - 1;			//上一个neighbor的索引

                curNP = mLstNeighbors[i].NeighborPatch;
                g_vid1 = mLstNeighbors[i].Vid;
                g_vid2 = mLstNeighbors[next].Vid;
                lstVertices = VerticesInPatch[i];

                /////////////////////////////////////////////////////////////////////
                // 开始顶点连接
                // 注意下面的顶点连接算法似乎没有必然得出正确结果的保障
                // 因此，这一段也应作为测试和调试的重点！
                /////////////////////////////////////////////////////////////////////

                //先查找与vid2邻接的顶点
                int j;
                for (j = 0; j < lstVertices.Count; j++)
                {
                    ivid = lstVertices[j];
                    if (InArray(i, PatchesIntersectV[ivid]) &&		//vid1对应的邻域面片(Pn1)过vid
                       InArray(next, PatchesIntersectV[ivid]))	//vid2对应的邻域面片(Pn2)过vid
                    {
                        //说明vid与v2相邻接
                        lstVSorted.Add(ivid);		//插入数组中
                        break;					//找到了，跳出
                    }
                }
                //确保找到！
                if (lstVSorted.Count != 1)
                {
                    // error occured!
                    mRemovedError = -1.0f;
                    mConvexPolytope.ExceptionOccur = true;
                    return false;
                }

                //然后，依次寻找并连接相邻的两个顶点！		
                //lastvid标志最后一个已被连接的顶点
                //定义llvid则标志倒数第二个被连接的顶点，引入改变量是为了避免重复连接！
                int llvid;
                int counter = 1;			//引入counter防止死循环
                while (counter < lstVertices.Count)
                {
                    counter++;

                    lastvid = lstVSorted[lstVSorted.Count - 1];
                    if (lstVSorted.Count < 2)
                        llvid = -1;		//一个无效值，因为此时还没有倒数第二个
                    else
                        llvid = lstVSorted[lstVSorted.Count - 2];

                    for (j = 0; j < lstVertices.Count; j++)
                    {
                        ivid = lstVertices[j];
                        //这个地方应该增加判断：即不能重复连接！
                        if ((ivid != lastvid) && (ivid != llvid) && IsVAdjacent(PatchesIntersectV[lastvid], PatchesIntersectV[ivid]))	//相邻
                        {
                            lstVSorted.Add(ivid);
                            break;
                        }
                    }
                }

                if (counter != lstVSorted.Count)
                {
                    // error occured!
                    mRemovedError = -1.0f;
                    mConvexPolytope.ExceptionOccur = true;
                    return false;
                }

                if (lstVSorted.Count != 1)
                {
                    // error occured!
                }

                lastvid = lstVSorted[lstVSorted.Count - 1];

                //确保最后一个顶点lastvid应该是和v1邻接的

                if (!InArray(pre, PatchesIntersectV[lastvid]) || !InArray(i, PatchesIntersectV[lastvid]))
                {
                    // error occured!
                    mRemovedError = -1.0f;
                    mConvexPolytope.ExceptionOccur = true;
                    return false;
                }

                /////////////////////////////////////////////////////////////////////
                // 确定顶点连接关系后，则可以开始邻域面片的顶点插入操作了
                /////////////////////////////////////////////////////////////////////
                //判断v1和v2是否应该从当前邻域面片的Neighbor中删除！
                //判断方法：以v2为例，如果过v2的面片仅有P,Pn1,Pn2，则v2可以删除了，这可以简化为看其度数是否为3或>3
                List<VPNeighbor> lstNeighbors = curNP.LstNeighbors;
                //寻找v2在当前邻域面片中的位置
                for (j = 0; j < lstNeighbors.Count; j++)
                {
                    int tmp = lstNeighbors[j].Vid;
                    if (g_vid2 == tmp)
                        break;
                }
                //循环结束，j保存了当前的v2位置
                int v2pos = j;

                if (v2pos >= lstNeighbors.Count)       //确保一定找到
                {
                    // error occured!
                    mRemovedError = -1.0f;
                    mConvexPolytope.ExceptionOccur = true;
                    return false;
                }

                if (g_vid1 != curNP.GetNextV(v2pos))       //确保一定找到
                {
                    // error occured!
                    mRemovedError = -1.0f;
                    mConvexPolytope.ExceptionOccur = true;
                    return false;
                }

                bool bv2Last = (v2pos == lstNeighbors.Count - 1);	//v2是否是Neighbor中最后一个元素？

                //先处理v1，因为对其的操作比较简单！直接删除或保留即可！
                //判断v1是否应该从当前邻域面片的Neighbor中删除
                VertexInfo v1Info = mConvexPolytope.LstVertexInfo[g_vid1];
                if (v1Info.Degree == 3)
                {
                    //删除该邻域，v1在当前patch中的索引为pCurNP.GetNext(j)
                    lstNeighbors.RemoveAt(curNP.GetNext(v2pos));
                    if (bv2Last)		//说明v1是第一个元素，因此，删除v1后，应将v2pos自减
                        v2pos--;
                }
                int InsertPos;				//新邻域添加的位置

                //处理v2是否删除或保留！
                VertexInfo v2Info = mConvexPolytope.LstVertexInfo[g_vid2];
                if (v2Info.Degree == 3)
                {
                    //则应删除v2
                    lstNeighbors.RemoveAt(v2pos);
                    InsertPos = v2pos;						//v2已被删除，直接从v2pos插入
                }
                else
                {
                    //不删除v2，但必须修改其pNeighborPatch
                    lstNeighbors[v2pos].NeighborPatch = mLstNeighbors[next].NeighborPatch;	//修改其对应NeighborPatch为Pn2!
                    InsertPos = curNP.GetNext(v2pos);		//插入点在v2pos后面第一个元素起
                }

                //构造新要添加的邻域
                List<VPNeighbor> lstNeighborsToAdd = new List<VPNeighbor>();		//新要添加的邻域
                VPNeighbor vpn = new VPNeighbor();
                for (j = 0; j < lstVSorted.Count; j++)
                {
                    vpn.Vid = lstVSorted[j] + g_vid;						//注意，这里要用全局id，因此应加上g_vid
                    if (j == lstVSorted.Count - 1)
                    {
                        //最后一个邻域节点要特殊处理
                        //其邻域面片应等于本面片的上一个邻域面片
                        vpn.NeighborPatch = mLstNeighbors[pre].NeighborPatch;
                    }
                    else
                    {
                        //要用一个查找算法！找出对应于该点的邻域面片！
                        for (int k = 0; k < npCount; k++)
                        {
                            if (k != i)		//不是当前正在考察的面片
                            {
                                List<int> lstVertecies = VerticesInPatch[k];
                                if (InArray(lstVSorted[j], lstVertecies) && InArray(lstVSorted[j + 1], lstVertecies))
                                {
                                    //由VSorted[j,j+1]构成的边出现在pVertecies中
                                    //说明第k个邻域面片对应了顶点VSorted[j]
                                    vpn.NeighborPatch = mLstNeighbors[k].NeighborPatch;
                                }

                            }
                        }
                    }

                    if (vpn.NeighborPatch == null)       //确保一定找到
                    {
                        // error occured!
                        mRemovedError = -1.0f;
                        mConvexPolytope.ExceptionOccur = true;
                        return false;
                    }

                    lstNeighborsToAdd.Add(vpn);
                }

                //插入新的邻域
                lstNeighbors.InsertRange(InsertPos, lstNeighborsToAdd);

                //当前邻域面片pCurNP的新的邻域构造OK后，需要对其进行更新删除误差操作！
                curNP.UpdateRemovedError();

            } // end of for-loop

            //循环遍历当前面片的每一个顶点，并将其度数自减1
            int curvid;
            for (i = 0; i < npCount; i++)
            {
                curvid = mLstNeighbors[i].Vid;
                VertexInfo curVInfo = mConvexPolytope.LstVertexInfo[curvid];
                curVInfo.Degree--;
            }

            return true;		//删除成功
        }

        public void UpdateRemovedError()				//更新该平面面片的删除误差！
        {
            mRemovedError = -1.0f;		//每次更新时，首先初始化为一个负值！

            //计算所有邻域的可能交点，找出其中距离该平面最远的点，其距离即为该平面的删除误差
            int npCount = mLstNeighbors.Count;
            if (npCount < 3)
                return;			//邻域面片数小于3，出现了异常！

            // 防止出现有效交点但拓扑错误的情况
            bool[] lstNPValid = new bool[npCount];		//记录每一个邻域面片是否有效，即是否经过一个有效交点！
            List<int> lstN = new List<int>();                         //存储选出的三个相邻面片的id
            Vector3 v = Vector3.zero;
            bool valid = false;

            CombGenerator cg = new CombGenerator(3, npCount);
            if (!cg.Solvable)
            {
                return;
            }

            while (!cg.Over)
            {
                //产生一个组合
                cg.GetNextComb(lstN);
                //求解
                if (Solve3NPIntersection(lstN, ref v))
                {
                    valid = true;
                    //如果有解，先代入到当前平面的方程
                    //注意：考察当前面片时，则应该是不在当前面片的内部！
                    if (Inside(v))
                        continue;			//直接进入下一轮while循环

                    //继续代入其他的邻域面片方程检验
                    for (int i = 0; i < npCount; i++)
                    {
                        if (i != lstN[0] && i != lstN[1] && i != lstN[2])
                        {
                            //不能在这些面片的外部
                            if (LstNeighbors[i].NeighborPatch.Outside(v))
                            {
                                valid = false;
                                break;		//在某个平面外部，跳出for循环
                            }
                        }
                    }

                    if (valid)
                    {
                        //说明该点确实是一个有效的交点，可以计算误差
                        float error = OnPlane(v) ? 0.0f : Dist2Plane(v);		//如果顶点满足OnPlane()，则置距离为0.0f
                        if (error > mRemovedError)
                            mRemovedError = error;

                        //将产生该交点的三个面片置为有效
                        lstNPValid[lstN[0]] = true;
                        lstNPValid[lstN[1]] = true;
                        lstNPValid[lstN[2]] = true;
                    }
                }
                else
                {
                    //无解的情况暂不考虑
                }
            }

            //是否所有的邻域面片都有效？
            bool allvalid = true;
            for (int i = 0; i < lstNPValid.Length; i++)
            {
                if (!lstNPValid[i])
                {
                    allvalid = false;
                    break;
                }
            }

            if (allvalid)
            {
                mRemovedError = -1.0f;		//还能找到无效的面片
            }
        }

        public void Neighbor(Patch pat)			//判断pPatch是否是当前patch的邻接patch，如果是，插入到相应的位置
        {
            int start, end;
            for (int i = 0; i < mLstNeighbors.Count; i++)
            {
                start = mLstNeighbors[i].Vid;
                end = (i == mLstNeighbors.Count - 1) ? mLstNeighbors[0].Vid : mLstNeighbors[i + 1].Vid;
                if (pat.VInPatch(start) && pat.VInPatch(end))
                    mLstNeighbors[i].NeighborPatch = pat;
            }
        }

        public int GetVNum()
        {
            return mLstNeighbors.Count;
        }

        public Vector3 GetVertex(int vid)
        {
            if(vid < 0 || vid >= mLstNeighbors.Count)
            {
                return Vector3.zero;
            }

            int nvid = mLstNeighbors[vid].Vid;
            if (nvid < 0 || nvid >= mConvexPolytope.LstVertecies.Count)
            {
                return Vector3.zero;
            }

            return mConvexPolytope.LstVertecies[nvid];
        }

        public void GetEdge(int id, ref Vector3 v1, ref Vector3 v2)
        {
            v1 = mConvexPolytope.LstVertecies[mLstNeighbors[id].Vid];
            v2 = mConvexPolytope.LstVertecies[GetNextV(id)];
        }

        public int GetVID(int idx)
        {
            if (idx >= 0 && idx < mLstNeighbors.Count)
            {
                return mLstNeighbors[idx].Vid;
            }
            return -1;
        }


        //////////////////////////////////////////////////////////////
        // 为保证计算交点的一致性而添加的函数
        // 对于以前计算出的每一个相交于一点的平面片集，查找是否有
        // 可以包含在当前待插入的交于一点的平面片集PIntersectV
        // 如果有，则返回其在PatchesIntersectV的索引id
        // 否则，返回-1
        //////////////////////////////////////////////////////////////
        protected int HasPIntersectVExist(List<int> lstPIntersectV, List<List<int>> patchIntersectV)
        {
            if (lstPIntersectV.Count == 3)
                return -1;			//只有三个元素，因此不存在包含关系，直接返回－1

            List<int> lstPIV;
            int pid;
            bool bIn;
            for (int i = 0; i < patchIntersectV.Count; i++)
            {
                bIn = true;
                lstPIV = patchIntersectV[i];
                for (int j = 0; j < lstPIV.Count; j++)
                {
                    pid = lstPIV[j];
                    if (!InArray(pid, lstPIntersectV))
                    {
                        bIn = false;
                        break;
                    }
                }
                if (bIn)
                    return i;
            }

            return -1;
        }

        //////////////////////////////////////////////////////////////
        // 判断新加入的两个顶点是否邻接
        // 其方法为，判断这两个顶点对应的面片集合中，是否有两个完全相同！
        // 供Removed()进行调用
        //////////////////////////////////////////////////////////////
        protected bool IsVAdjacent(List<int> lst1, List<int> lst2)
        {
            int count = 0;
            for (int i = 0; i < lst1.Count; i++)
            {
                if (InArray(lst1[i], lst2))
                {
                    count++;
                    if (count == 2) return true;
                }
            }
            return false;
        }

        protected bool InArray(int pid, List<int> lst)		        //pid是否出现在动态数组pArr中
        {
            for (int i = 0; i < lst.Count; i++)
                if (pid == lst[i])
                    return true;
            return false;
        }

        protected bool VInPatch(int vid)								//判断顶点vid是否为当前面片的一个顶点
        {
            for (int i = 0; i < mLstNeighbors.Count; i++)
            {
                if (vid == mLstNeighbors[i].Vid)
                    return true;
            }
            return false;
        }

        protected bool Processed(List<int> lstN, List<List<int>> coSolutionList)
        {
            bool[] lstInArrat = new bool[3];
            List<int> lstArr = null;

            for (int i = 0; i < coSolutionList.Count; i++)
            {
                lstArr = coSolutionList[i];
                if (lstArr.Count == 3)			//标准情况，三个面有一个解，直接跳过
                    continue;

                lstInArrat[0] = false;				//初始化
                lstInArrat[1] = false;				//初始化
                lstInArrat[2] = false;				//初始化

                //多于三个面有同解的情况
                for (int j = 0; j < lstArr.Count; j++)
                {
                    if (lstN[0] == lstArr[j]) lstInArrat[0] = true;
                    if (lstN[1] == lstArr[j]) lstInArrat[1] = true;
                    if (lstN[2] == lstArr[j]) lstInArrat[2] = true;
                }

                if (lstInArrat[0] && lstInArrat[1] && lstInArrat[2])
                    return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////
        // 求解第n[0,1,2]个邻域面片的交点，结果存入vIntersection
        // 无解及其他情况返回false!
        //////////////////////////////////////////////////////////////////////
        protected bool Solve3NPIntersection(List<int> lstN, ref Vector3 intersection)
        {
            int npCount = mLstNeighbors.Count;

            //输入无效
            if (lstN == null || lstN.Count < 3 ||
               lstN[0] < 0 || lstN[0] >= npCount ||
               lstN[1] < 0 || lstN[1] >= npCount ||
               lstN[2] < 0 || lstN[2] >= npCount)
                return false;

            //构造线性方程组
            Matrix mtxCoef = new Matrix(3, 3);		// 系数矩阵
            Matrix mtxConst = new Matrix(3, 1);		// 常数矩阵
            Matrix mtxResult;		                // 结果(3,1)

            for (int i = 0; i < 3; i++)
            {
                Patch pat = mLstNeighbors[lstN[i]].NeighborPatch;

                Vector3 v = pat.Normal;
                mtxCoef.SetElement(i, 0, v.x);
                mtxCoef.SetElement(i, 1, v.y);
                mtxCoef.SetElement(i, 2, v.z);
                mtxConst.SetElement(i, 0, pat.Dist);
            }

            //用高斯全选主元法求解
            LEquations le = new LEquations(mtxCoef, mtxConst);
            if (!le.GetRootsetGauss(out mtxResult))
                return false;

            intersection.x = (float)mtxResult.GetElement(0, 0);
            intersection.y = (float)mtxResult.GetElement(1, 0);
            intersection.z = (float)mtxResult.GetElement(2, 0);

            return true;
        }

        //返回第i个邻域顶点的下一个顶点的全局id，从而与当前邻域点构成一个邻边！
        protected int GetNextV(int i)
        {
            return ((i < mLstNeighbors.Count - 1) ? mLstNeighbors[i + 1].Vid : mLstNeighbors[0].Vid);
        }

        protected int GetNext(int i)
        {
            return ((i < mLstNeighbors.Count - 1) ? i + 1 : 0);
        }

        protected int GetPre(int i)
        {
            return (i < 1 ? mLstNeighbors.Count - 1 : i - 1);
        }
    }
