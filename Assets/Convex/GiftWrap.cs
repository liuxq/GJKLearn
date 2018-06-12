
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /*
        When we start the GiftWrap algorithm, we should make sure that the 
    vertices passed are unique from each other. This is important since many 
    3D content authoring tools will create and export almost uniform-position
    vertices.
        This function will process the vertices passed in and clean up those
    overlapped vertices...
    */

    public class GiftWrap : ConvexAlgorithm
    {
        private const float CH_FLT_EPSILON = 2.0e-7F;			// 绝对值最小的浮点数
        private const float CH_VALID_TOLERANCE = 0.03f;         // 
        private const float CH_IDENTICAL_POS_ERROR = 0.01f;      

        public enum HULL_TYPE
        {
            HULL_2D = 0,
            HULL_3D,
        }

        public HULL_TYPE HullType { get; set; }     //计算出的凸包的类型。二维或三维
        public float HSDistThresh { get; set; }     //计算凸包的初始Halfspace距离阈值

        protected EdgeStack mEdgeStack = new EdgeStack();   //边的堆栈
        protected Edge mFirstEdge;                             //找到的第一条边	
        protected bool mExceptionOccur;                           //计算过程中出现异常，计算结果可能有错误
        protected float mIdenticalPosErr;                         //共点误差

        public GiftWrap()
        {
            HSDistThresh = 0.01f;
            HullType = HULL_TYPE.HULL_3D;
            Reset();
        }

        public GiftWrap(List<Vector3> lstVert)
        {
            HSDistThresh = 0.01f;
            HullType = HULL_TYPE.HULL_3D;
            Reset();
            SetVertexes(lstVert);
        }

        //应用于2D凸包的求解
        public virtual List<int> GetCHVertecies()
        {
            if (LstPlanes.Count == 1)
                return LstPlanes[0]; 
            return null;
        }

        public override void Reset()
        {
            base.Reset();
            mEdgeStack.Clear();
            mExceptionOccur = false;
        }

        public void Start()
        {
		    //先调用父类函数从而完成清空操作
	        ComputeConvexHull();
	        Edge edge = SearchFirstEdge(Vector3.up);
	        mEdgeStack.Push(edge);
	        mEdgeStack.Push(edge);			//注意，这里应该push两次！从而保证在while条件中的弹出后，仍有一个边被保存在栈中！
            mFirstEdge = edge;
        }

        public void GoOneStep()
        {
            Edge edge = new Edge();
            edge = mEdgeStack.Pop();
            DealEdge(edge);
        }

        public bool IsOver()
        {
            return mEdgeStack.Count == 0;
        }

        public bool ExceptionOccur()
        {
            return mExceptionOccur || !ValidConvexHull();
        }

        // 计算生成的凸包是否合法
        public bool ValidConvexHull() 
        {
            // 检查共面的顶点是否会出现顶点到平面的距离超出阈值的情况
            HalfSpace hs = new HalfSpace();
            for (int i = 0; i < mLstPlanes.Count; i++)
            {
                if (mLstPlanes[i].Count < 3)
                    continue;

                hs.Set(mLstVertex[mLstPlanes[i][0]], mLstVertex[mLstPlanes[i][1]], mLstVertex[mLstPlanes[i][2]]);

                for (int j = 3; j < mLstPlanes[i].Count; j++)
                {
                    if (Mathf.Abs(hs.Dist2Plane(mLstVertex[mLstPlanes[i][j]])) > CH_VALID_TOLERANCE)
                        return false;
                }
            }

            return true;
        }

        public bool SaveVerticesToFile(string filepath)
        {
            //string dir = System.IO.Path.GetDirectoryName(filepath);
            //if(!System.IO.Directory.Exists(dir))
            //{
            //    System.IO.Directory.CreateDirectory(dir);
            //}

            //UEEditTextFile file = new UEEditTextFile();
            //if (!file.Open(filepath, OPEN_MODE.OPEN_WRITE))
            //    return false;

            //file.SaveNeatLine(mLstVertex.Count.ToString());
            //for (int i = 0; i < mLstVertex.Count; i++)
            //{
            //    file.SaveNeatLine(string.Format("{0} {1} {2}", mLstVertex[i].x, mLstVertex[i].y, mLstVertex[i].z));
            //}

            //file.Close();
            
            return true;
        }

        public override void ComputeConvexHull()
        {
            for (mIdenticalPosErr = CH_IDENTICAL_POS_ERROR; mIdenticalPosErr >= 1e-5f; mIdenticalPosErr *= 0.1f)
            {
                InternalComputeConvexHull();
                if (!ExceptionOccur())
                    break;
            }
        }

        protected Edge SearchFirstEdge(Vector3 refNormal)
        {
	        Edge edge = new Edge();
	        // find lowest vertexes
	        List<int> lstLowestVertexes = new List<int>();
	        float yMin = mLstVertex[0].y;
	        lstLowestVertexes.Add(0);
            int i = 0;
	        for (i = 1; i < mLstVertex.Count; i++)
	        {
		        if (yMin - mLstVertex[i].y > mIdenticalPosErr)	// smaller than yMin
		        {
			        yMin = mLstVertex[i].y;
			        lstLowestVertexes.Clear();
			        lstLowestVertexes.Add(i);
		        }
		        else if(Mathf.Abs(yMin-mLstVertex[i].y) < mIdenticalPosErr)
		        {
			        // vertex in the same plane
			        lstLowestVertexes.Add(i);
		        }
	        }

	        if (lstLowestVertexes.Count == 1)   // only one vertex on bottom
	        {
		        edge.start = lstLowestVertexes[0];
                // search second vertex on XOY plane
		        for (i = 0; i == edge.start; i++);
		
		        // 利用vRefNormal构造半平面...
		        HalfSpace hs = new HalfSpace(mLstVertex[edge.start], mLstVertex[i], mLstVertex[edge.start] + refNormal);
		        List<int> lstCoPlanarVertexes = new List<int>();
		        lstCoPlanarVertexes.Add(i);			    //add first vertex		
		        for (i++; i < mLstVertex.Count; i++)
                {
			        if (i != edge.start && !hs.Inside(mLstVertex[i]))
			        {
                        if (hs.OnPlane(mLstVertex[i]))
                        {
					        lstCoPlanarVertexes.Add(i);		//add current vetex
                        }
				        else
				        {
					        // this vetex is not inside, use this vetex as a new one to reconstruct the helf space
                            hs.Set(mLstVertex[edge.start], mLstVertex[i], mLstVertex[edge.start] + refNormal);
                            lstCoPlanarVertexes.Clear();
                            lstCoPlanarVertexes.Add(i);
				        }
                    }
                }
		
		        if (lstCoPlanarVertexes.Count == 1)
		        {
			        // only one vetex, let it be edge's end
			        edge.end = lstCoPlanarVertexes[0];
			        return edge;
		        }
		        else
		        {
			        // more than one conplane vetex
			        Vector3 vd3 = hs.Normal;

			        // contruct initial half space
			        hs.Set(mLstVertex[edge.start], mLstVertex[lstCoPlanarVertexes[0]], mLstVertex[edge.start] + vd3);
                    List<int> lstCoLinearVertexes = new List<int>();
			        lstCoLinearVertexes.Add(lstCoPlanarVertexes[0]);
			        for (i = 1; i < lstCoPlanarVertexes.Count; i++)
                    {
				        if (!hs.Inside(mLstVertex[lstCoPlanarVertexes[i]]))
				        {
                            if (hs.OnPlane(mLstVertex[lstCoPlanarVertexes[i]]))
                            {
						        lstCoLinearVertexes.Add(lstCoPlanarVertexes[i]);
                            }
					        else
					        {
						        // rerconstruct halfspace
						        hs.Set(mLstVertex[edge.start], mLstVertex[lstCoPlanarVertexes[i]], mLstVertex[edge.start] + vd3);
						        lstCoLinearVertexes.Clear();
						        lstCoLinearVertexes.Add(lstCoPlanarVertexes[i]);
					        }
                        }
                    }

			        if (lstCoLinearVertexes.Count == 1)
			        {
				        // only one vetex, let it be edge's end
				        edge.end = lstCoLinearVertexes[0];
				        return edge;
			        }
			        else
			        {
				        // sort all colinear vertex
                        SortByDist(edge.start, ref lstCoLinearVertexes);

				        // the farest veterx is the end, set other vertexes to invalid
                        edge.end = lstCoLinearVertexes[lstCoLinearVertexes.Count-1];
                        for (i = 0; i < lstCoLinearVertexes.Count-1; i++)
                        {
                            mLstVInvalid[lstCoLinearVertexes[i]] = true;
                        }				        
				        return edge;
			        }
		        }
	        }
	        else            // more than one vetexes on bottom
	        {
		        // just consider to find the edge on XOZ plane
		        if (lstLowestVertexes.Count == 2)
		        {
                    // only two vertex, just return the result edge
			        edge.start = lstLowestVertexes[0];
			        edge.end = lstLowestVertexes[1];
			        return edge;
		        }

		        //find the minimum vetex acording to z value
		        List<int> lstZMinVertexes = new List<int>();
		        float zmin = mLstVertex[lstLowestVertexes[0]].z;
		        lstZMinVertexes.Add(lstLowestVertexes[0]);
		        for (i = 1; i < lstLowestVertexes.Count; i++)
		        {
			        if (zmin - mLstVertex[lstLowestVertexes[i]].z > mIdenticalPosErr)
			        {
				        zmin = mLstVertex[lstLowestVertexes[i]].z;
				        lstZMinVertexes.Clear();
				        lstZMinVertexes.Add(lstLowestVertexes[i]);
			        }
			        else if(Mathf.Abs(zmin - mLstVertex[lstLowestVertexes[i]].z) < mIdenticalPosErr)
			        {
				        // coplane
				        lstZMinVertexes.Add(lstLowestVertexes[i]);
			        }
		        }
		
		        if (lstZMinVertexes.Count == 1) //only one veterx found
		        {
			        //set the vertex to be start
			        edge.start = lstZMinVertexes[0];
			        List<int> lstCoLinearVertexes = new List<int>();
			
			        //find the other vertex in LowestVertexes
                    for (i = 0; lstLowestVertexes[i] == edge.start; i++);

			        lstCoLinearVertexes.Add(lstLowestVertexes[i]);
			        Vector3 vd3 = new Vector3(0.0f, -1.0f, 0.0f);
			        HalfSpace hs = new HalfSpace(mLstVertex[edge.start], mLstVertex[lstLowestVertexes[i]], mLstVertex[edge.start] + vd3);
			        for (i++; i < lstLowestVertexes.Count; i++)
                    {
				        if (lstLowestVertexes[i] != edge.start && !hs.Inside(mLstVertex[lstLowestVertexes[i]]))
				        {
                            if (hs.OnPlane(mLstVertex[lstLowestVertexes[i]]))
                            {
						        lstCoLinearVertexes.Add(lstLowestVertexes[i]);
                            }
					        else
					        {
                                //reonstruct halfspace
						        hs.Set(mLstVertex[edge.start], mLstVertex[lstLowestVertexes[i]], mLstVertex[edge.start] + vd3);
						        //清空，并重新计算共线点集
						        lstCoLinearVertexes.Clear();
						        lstCoLinearVertexes.Add(lstLowestVertexes[i]);
					        }
                        }
                    }
			
			        if (lstCoLinearVertexes.Count == 1)
			        {
				        //only one colinear vertex
				        edge.end = lstCoLinearVertexes[0];
				        return edge;
			        }
			        else
			        {
                        // sort all colinear vertex
                        SortByDist(edge.start, ref lstCoLinearVertexes);

                        // the farest veterx is the end, set other vertexes to invalid
                        edge.end = lstCoLinearVertexes[lstCoLinearVertexes.Count - 1];
                        for (i = 0; i < lstCoLinearVertexes.Count - 1; i++)
                        {
                            mLstVInvalid[lstCoLinearVertexes[i]] = true;
                        }				        
				        return edge;
			        }			
		        }
                else    //more than one veterx found
		        {
			        //find the veterxs with min and max X value
                    int xminID = lstZMinVertexes[0];
                    int xmaxID = lstZMinVertexes[0];
			        float XMin = mLstVertex[lstZMinVertexes[0]].x;
			        float XMax = mLstVertex[lstZMinVertexes[0]].x;
			        for (i = 1; i < lstZMinVertexes.Count; i++)
			        {
				        if (XMin - mLstVertex[lstZMinVertexes[i]].x > mIdenticalPosErr)
                        {
					        XMin = mLstVertex[lstZMinVertexes[i]].x;
					        xminID = lstZMinVertexes[i];
				        }
				        if(mLstVertex[lstZMinVertexes[i]].x - XMax > mIdenticalPosErr)
                        {
					        XMax = mLstVertex[lstZMinVertexes[i]].x;
					        xmaxID = lstZMinVertexes[i];
				        }
			        }

			        // set all vertex to invalid
                    for (i = 0; i < lstZMinVertexes.Count; i++)
                    {
                        mLstVInvalid[lstZMinVertexes[i]] = true;
                    }

                    // set edge end vertex to valid
                    mLstVInvalid[xmaxID] = false;
                    mLstVInvalid[xminID] = false;

			        mLstExtremeVertex[xmaxID] = true;
                    mLstExtremeVertex[xminID] = true;

			        edge.start = xminID;
			        edge.end = xmaxID;
			        return edge;
		        }
            }

            //return null;
        }

        //判断边edge和点v之间的关系
        //返回值情况如下：
        //0:v在edge外！
        //1:v在直线edge上，但在线段e左侧，即e.start外
        //2:v在直线edge上，但在线段e右侧，即e.end外
        //3:v在线段edge内
        //-1:v和e.start重合
        //-2:v和e.end重合
        protected int EVRelation(Edge edge, int v)
        {
            if (v == edge.start) 
                return -1;
            if (v == edge.end) 
                return -2;

            if (!IsTriVsCoLinear(edge.start, edge.end, v)) 
                return 0;

            Vector3 de = mLstVertex[edge.end] - mLstVertex[edge.start]; // direction
            Vector3 vd1 = mLstVertex[v] - mLstVertex[edge.start];
            Vector3 vd2 = mLstVertex[v] - mLstVertex[edge.end];

            if (Vector3.Dot(vd1, vd2) < 0.0f) 
                return 3;

            if (Vector3.Dot(vd1, de) > 0.0f) 
                return 2;

            return 1;
        }

	    protected bool IsTriVsCoLinear(int v1, int v2, int v3)
        {
            Vector3 vd1 = mLstVertex[v2] - mLstVertex[v1];
            Vector3 vd2 = mLstVertex[v3] - mLstVertex[v1];
            vd1.Normalize();
            vd2.Normalize();

            if (1.0f - Mathf.Abs(Vector3.Dot(vd1, vd2)) < 0.00006f)
            {
                return true;
            }
            
            return false;
        }

	    protected void SortByDist(int v,ref List<int> lst)
        {
            if (lst == null || lst.Count < 2) 
                return;

            // calc distance first
            float[] dists = new float[lst.Count];
            for (int i = 0; i < lst.Count; i++)
            {
                dists[i] = (mLstVertex[lst[i]] - mLstVertex[v]).sqrMagnitude;
            }

            int minDistID;
            int tmp;
            //sort by dist
            for (int i = 0; i < lst.Count; i++)
            {
                minDistID = i;
                for (int j = i + 1; j < lst.Count; j++)
                {
                    if (dists[j] < dists[minDistID])
                    {
                        minDistID = j;
                    }
                }
                
                tmp = lst[i];
                lst[i] = lst[minDistID];
                lst[minDistID] = tmp;

                //同时将距离重置
                dists[minDistID] = dists[i];
            }
        }

        /////////////////////////////////////////////////////////
        // 该函数用来处理对于边edge，找到的目标点是多点共面的情况
        // 注意，参数中的CoPlanarVertexes所含的点数应>1
        /////////////////////////////////////////////////////////
	    protected void DealCoPlanarV(Edge edge, List<int> lstCoPlanarVertexes)
        {
	        //如果出现该边的两点无效的情况，直接返回
	        if (mLstVInvalid[edge.start]||mLstVInvalid[edge.end])
	        {
		        //异常退出时，一定要释放内存
		        lstCoPlanarVertexes.Clear();
		        return;
	        }

	        //构造初始的halfspace
	        //注意构造的方法：该halfspace的分割面过e.end和CoPlanarVertexes[0]，同时平行于法向
	        HalfSpace hs = new HalfSpace();	
	        List<int> lstExtremeVertexes = new List<int>();				//平面上所有点的凸壳点集，用来存储结果
            List<int> lstCoLinearVertexes = new List<int>();			//共线的情况，用一个点集表示

	        //将edge.start和edge.end也存入CoPlanarVertexes中
	        lstCoPlanarVertexes.Add(edge.start);
	        lstCoPlanarVertexes.Add(edge.end);
	
	        //每条边edge已经是延伸到最大的情况，因此，不用考虑还能将其延伸的情况
	        int vidNotCL = lstCoPlanarVertexes[0];		
	        //由于上面的条件，CoPlanarVertexes[0]一定不会和edge共线的
	        //利用vidNotCL，首先求出平面的法向
	        Vector3 normal = Vector3.Cross(mLstVertex[edge.end]-mLstVertex[edge.start], mLstVertex[vidNotCL]-mLstVertex[edge.start]);
	        normal.Normalize();

	        Edge curE = new Edge(edge.end,vidNotCL);						//当前的边

	        int vSize = lstCoPlanarVertexes.Count;

	        //main loop
	        while (curE.start != edge.start)				//当最后找到edge.start这一点时，表明找到了所有点处在凸壳上的点
	        {
		        //初始化hs
		        hs.Set(mLstVertex[curE.start], mLstVertex[curE.end], mLstVertex[curE.start] + normal);
		
		        lstCoLinearVertexes.Clear();
		        lstCoLinearVertexes.Add(curE.end);		//当前点添加至共线点集

		        //寻找满足当前curE的目标点
                for (int idx = 0; idx < lstCoPlanarVertexes.Count; idx++)
		        {
			        int id = lstCoPlanarVertexes[idx];
			        if (id != curE.start && id != curE.end && !mLstVInvalid[id] && !hs.Inside(mLstVertex[id]))
			        {
				        //判断是否共面，实际上为是否共线
				        if (hs.OnPlane(mLstVertex[lstCoPlanarVertexes[idx]]))
				        {
					        lstCoLinearVertexes.Add(lstCoPlanarVertexes[idx]);
				        }
				        else
				        {
					        //更改当前边
					        curE.end=lstCoPlanarVertexes[idx];
					        //重新计算halfspace
					        hs.Set(mLstVertex[curE.start], mLstVertex[curE.end], mLstVertex[curE.start]+normal);

					        //清空，并重新计算共线点集
					        lstCoLinearVertexes.Clear();
					        lstCoLinearVertexes.Add(lstCoPlanarVertexes[idx]);
				        }
			        }
		        }

		        //循环完成，此时应该已经找到一个点或一组点集
		        if (lstCoLinearVertexes.Count == 1)
		        {
			        //一个点的情况
			        //添加至凸壳边界点集
			        lstExtremeVertexes.Add(lstCoLinearVertexes[0]);
			        //找到的点成为当前边的起点
			        curE.start = lstCoLinearVertexes[0];

			        //找一个点作为curE.end
			        int j;
			        for (j = 0; j < lstCoPlanarVertexes.Count && lstCoPlanarVertexes[j] == curE.start; j++);	//不能等于start!
			        curE.end = lstCoPlanarVertexes[j];

			        vSize--;
			        if (vSize < 0)
			        {
				        mExceptionOccur = true;		//出现异常了，退出！				
				        //异常退出时，一定要释放内存
                        lstCoPlanarVertexes.Clear();
				        return;
			        }
		        }
		        else
		        {
			        //多个点集的情况
			        //按点到curE.start欧氏距离的远近排序，从近到远
			        SortByDist(curE.start,ref lstCoLinearVertexes);
			        //重置起点为最远点
			        curE.start = lstCoLinearVertexes[lstCoLinearVertexes.Count-1];
			        //插入最远点！
			        lstExtremeVertexes.Add(curE.start);

			        //找一个点作为curE.end
                    int j;
			        for (j = 0; j < lstCoPlanarVertexes.Count && (lstCoPlanarVertexes[j]==curE.start || mLstVInvalid[lstCoPlanarVertexes[j]]); j++);	//不能等于start!
			        curE.end = lstCoPlanarVertexes[j];			

			        vSize--;
			        if (vSize < 0)
                    {
				        mExceptionOccur = true;		//出现异常了，退出！				
				        //异常退出时，一定要释放内存
                        lstCoPlanarVertexes.Clear();
				        return;
			        }

		        }

	        }   // end of main loop

	        //注意，经过前面的循环ExtremeVertexes的最后一个元素应该就是e.start
	        if (lstExtremeVertexes.Count <= 1)
	        {
				mExceptionOccur = true;		//出现异常了，退出！				
				//异常退出时，一定要释放内存
                lstCoPlanarVertexes.Clear();
				return;
	        }

	        //此时，已经得到了一组按顺序的顶点，可以向边堆栈和面列表中添加了
	        Edge ep = new Edge(lstExtremeVertexes[0], edge.end);
	        SelectivePushStack(ep);	
	        mLstFaces.Add(new Face(edge.start, edge.end, lstExtremeVertexes[0], true)); //该面将被剖分

            int i;
	        for (i = 0; i < lstExtremeVertexes.Count-2; i++)
	        {
                ep = new Edge(lstExtremeVertexes[i + 1], lstExtremeVertexes[i]);
		        //添加边到堆栈，一定要注意顺序！
		        //ep.Set(lstExtremeVertexes[i+1],lstExtremeVertexes[i]);		//应该反向添加
		        SelectivePushStack(ep);
                mLstFaces.Add(new Face(edge.start, lstExtremeVertexes[i], lstExtremeVertexes[i + 1], true));		
	        }

	        //最后一条边
	        //注意：对该边的处理应该直接使用m_EdgeStack.CheckPush
	        //因为，此前已经添加了包含该边的三角面，所以
	        //如果使用SelectivePushStack，则会认为该边已经被
	        //添加到三角形列表中，从而漏掉了这条边！
	        //这样会导致最终凸壳缺少了一个面！
            ep = new Edge(lstExtremeVertexes[i + 1], lstExtremeVertexes[i]);
	        //ep.Set(lstExtremeVertexes[i+1],lstExtremeVertexes[i]);
	        mEdgeStack.CheckPush(ep);

	        //将所有共面点都置为无效
            for (i = 0; i < lstCoPlanarVertexes.Count; i++)
            {
                mLstVInvalid[lstCoPlanarVertexes[i]] = true;
            }

	        //重新整理CoPlanarVertexes，使其仅包括边界点
	        lstCoPlanarVertexes.Clear();	
	        //将e.end插入到ExtremeVertexes中
	        lstExtremeVertexes.Add(edge.end);
	        //然后将所有边界点置为有效
	        for (i = 0; i < lstExtremeVertexes.Count; i++)
	        {
		        mLstVInvalid[lstExtremeVertexes[i]] = false;
		        mLstExtremeVertex[lstExtremeVertexes[i]] = true;
		        lstCoPlanarVertexes.Add(lstExtremeVertexes[i]);
	        }
	
	        //此时再将CoPlanarVertexes插入到m_Planes中
	        //将共面的所有点构成的平面添加至m_Planes
            mLstPlanes.Add(lstCoPlanarVertexes);
        }

	    protected bool DealEdge(Edge edge)
        {
	        int i;
	        for (i = 0; i == edge.start || i == edge.end || IsFaceInCH(new Face(edge.start,edge.end,i)) || mLstVInvalid[i];i++);

	        //构造e和该点组成的半空间，此时的hs必定有效，因为，保证了不共线条件
	        HalfSpace hs = new HalfSpace(mLstVertex[edge.start], mLstVertex[edge.end], mLstVertex[i]);
            List<int> lstCoPlanarVertexes = new List<int>();    //构造一个所有共面点的列表
	        lstCoPlanarVertexes.Add(i);			//添加当前第一个元素
	        //开始主循环！注意循环开始条件！从下一个元素开始！
	        for (i++; i <mLstVertex.Count; i++)
	        {
		        //不是e端点，没有考察过，且不在半空间内部！
		        if (i != edge.start && i != edge.end && !mLstVInvalid[i] && !IsFaceInCH(new Face(edge.start, edge.end, i)) && !hs.Inside(mLstVertex[i]))	
		        {
			        if (hs.OnPlane(mLstVertex[i]))
			        {
				        //在半空间的边界上，则添加到共面点列表
				        lstCoPlanarVertexes.Add(i);			//添加当前点
				    }
			        else
			        {
				        //说明该点不在内部，则该点为新的目标点，并重构半空间
				        hs.Set(mLstVertex[edge.start], mLstVertex[edge.end], mLstVertex[i]);

				        //同时清空共面列表，并将该点添加至共面列表
				        lstCoPlanarVertexes.Clear();
				        lstCoPlanarVertexes.Add(i);			//添加当前点

				        //注意，根据凸壳的性质，这里不需从头开始遍历
			        }
		        }
	        }
	
	        //经过上面的循环后，CoPlanarVertexes中就存储了由当前边e找到的一个凸面上的所有点

	        //在这里判断是否是2D的凸包
	        HullType = HULL_TYPE.HULL_2D;
	        for (i = 0; i < mLstVertex.Count; i++)			//遍历所有顶点
	        {
		        if (i != edge.start && i != edge.end && !mLstVInvalid[i])	//非e的端点，而且有效
		        {
			        if (!IsVInVSets(i, lstCoPlanarVertexes))	// 如果当前顶点不再公面集中！
			        {
				        // 增加一个判断，从而使得结果更严密
				        HalfSpace hstmp = new HalfSpace(mLstVertex[edge.start], mLstVertex[edge.end], mLstVertex[lstCoPlanarVertexes[0]]);
				        if (hstmp.OnPlane(mLstVertex[i]))
                        {
					        lstCoPlanarVertexes.Add(i);
                        }
				        else
				        {
					        HullType = HULL_TYPE.HULL_3D;
					        break;
				        }
			        }
		        }
	        }

	        //下面开始进行处理，这里将原本在ComputeConvexHull的主循环中的处理放在这里！
	        if (lstCoPlanarVertexes.Count == 1)		
	        {
		        //最简单的情况：没有共面点		
		        int v3 = lstCoPlanarVertexes[0];
		        //将另两条边压栈
		        Edge e1 = new Edge(edge.start, v3);
                Edge e2 = new Edge(v3, edge.end);
		        SelectivePushStack(e1);
		        SelectivePushStack(e2);

		        //构造三角面片，并插入队列中
		        mLstFaces.Add(new Face(edge.start, edge.end, v3));
		        //同时，该三角面片的三个顶点为凸壳的ExtremeVertex!
		        mLstExtremeVertex[edge.start] = true;
		        mLstExtremeVertex[edge.end] = true;
		        mLstExtremeVertex[v3] = true;

                if (HullType == HULL_TYPE.HULL_2D)			// 凸包为一个三角形的情况
                {
                    lstCoPlanarVertexes.Add(edge.start);
                    lstCoPlanarVertexes.Add(edge.end);
                    mLstPlanes.Add(lstCoPlanarVertexes);
                }
                else
                {
                    lstCoPlanarVertexes.Clear();				//没有多点共面
                }

	        }
	        else		//多点共面的情况！
	        {
		        //在由e.start,e.end和CoPlanarVertexes[0]组成的平面上找凸壳
		        DealCoPlanarV(edge,lstCoPlanarVertexes);
	        }

	        return true;
        }

	    protected void SelectivePushStack(Edge edge)
        {
            if (mEdgeStack.CheckPush(edge))
            {
                //压栈成功，表明压栈前栈中没有e存在
                //此时应判断该边是否已经被处理过
                if (IsEdgeInCH(edge))
                {
                    //正常情况下，本算法不会出现一条边连续3次
                    //被访问到！因此，这里出现了异常！
                    mExceptionOccur = true;		//说明异常发生！
                    mEdgeStack.Pop();		    //new interface!
                }
            }
        }

	    protected void ResetSameVertices()
        {
            Reset();

	        //初始化凸壳边界点集状态
	        mLstVInvalid = new List<bool>();
	        mLstExtremeVertex = new List<bool>();

	        for (int i = 0; i < mLstVertex.Count; i++)
	        {
		        mLstVInvalid.Add(false);
                mLstExtremeVertex.Add(false);
	        }
        }

	    protected bool ValidateCHPlane(HalfSpace hs)
        {
            for (int i = 0; i < mLstVertex.Count; i++)
            {
                if (!hs.Inside(mLstVertex[i]) && !hs.OnPlane(mLstVertex[i]))
                    return false;
            }
            return true;
        }

        protected void InternalComputeConvexHull()
        {
            HalfSpace hsFitPlane = new HalfSpace();
            Vector3 vFitNormal = Vector3.zero;
            bool bFit = HalfSpace.BestFitPlane(LstVertex.ToArray(), ref hsFitPlane, mIdenticalPosErr);
            if (bFit)
            {
                // tune the vertices's position to make the convex-hull generation more accurately...
                //for (int i = 0; i < LstVertex.Count; i++)
                //{
                //    LstVertex[i] = hsFitPlane.GetPorjectPos(LstVertex[i]);
                //}
            }

            vFitNormal = hsFitPlane.Normal;
            if (vFitNormal == Vector3.zero)
                vFitNormal.Set(0.0f, 1.0f, 0.0f);

            //////////////////////////////////////////////////////////////////////////
            // 由于求解凸包的过程在一定程度上依赖于HalfSpace类的距离阈值，因此
            // 这里将设法找到一个可以得到正确结果的距离阈值。
            // 一个重要的问题是能否求解出凸包与该阈值并非是单调的关系，即并非是阈值越大或越小
            // 就一定可以得到结果。实际上，阈值的一个微小的扰动，将可能导致异常出现或求出凸包
            // 但目前还没有找出两者之间的必然关系和规律。
            // 因此，目前的阈值设定，采用了宽度优先的尝试法。简单说明如下：
            // 首先尝试一个基本值hsDistThreshBase，然后将阈值设为hsDistThreshBase×0.1，
            // 直到阈值<CH_FLT_EPSILON。
            // 如果上述所有的阈值仍不能满足要求，则对hsDistThreshBase-0.1*hsDistThreshBase，
            // 然后继续上面的循环！
            // 可考虑另外的方法，如随机数的方法
            //////////////////////////////////////////////////////////////////////////	

            float hsDistThreshBase = HSDistThresh * 10.0f;	//每次递减循环的基数，先放大10倍
            float hsDistThreshBaseStep = HSDistThresh;		//递减步长
            float hsDistThresh;			//当前的阈值

            //保证没有异常的求出凸包！
            //双重循环
            do
            {
                hsDistThresh = hsDistThreshBase;

                do
                {
                    HullType = HULL_TYPE.HULL_3D;						//重新计算时，初始化为3D凸包

                    hsDistThresh *= 0.1f;								//缩小阈值10倍

                    HalfSpace.SetDistThresh(hsDistThresh);	//调整阈值

                    //先调用父类函数从而完成清空操作
                    base.ComputeConvexHull();
                    ResetSameVertices();

                    Edge e = SearchFirstEdge(vFitNormal);

                    if (!ValidateCHPlane(new HalfSpace(LstVertex[e.start], LstVertex[e.end], LstVertex[e.start] + vFitNormal)))
                    {
                        Vector3[] vAxis = new Vector3[3] { Vector3.right, Vector3.up, Vector3.forward };
                        for (int i = 0; i < 3; i++)
                        {
                            if (Mathf.Abs(Vector3.Dot(vFitNormal, vAxis[i])) > 0.9f)
                                continue;

                            Vector3 vN = Vector3.Cross(vFitNormal, vAxis[i]);
                            Edge e1 = SearchFirstEdge(vN);
                            if (ValidateCHPlane(new HalfSpace(LstVertex[e1.start], LstVertex[e1.end], LstVertex[e1.start] + vN)))
                            {
                                e = e1;
                                break;
                            }
                        }
                    }

                    mFirstEdge = e;
                    mEdgeStack.Push(e);
                    //e = new Edge(e.start,e.end);
                    mEdgeStack.Push(new Edge(e.start, e.end));			//注意，这里应该push两次！从而保证在while条件中的弹出后，仍有一个边被保存在栈中！

                    while (!mEdgeStack.IsEmpty() && !mExceptionOccur && HullType == HULL_TYPE.HULL_3D)
                    {
                        Edge se = mEdgeStack.Pop();
                        DealEdge(se);
                    }

                } while (ExceptionOccur() && hsDistThresh > CH_FLT_EPSILON);

                hsDistThreshBase -= hsDistThreshBaseStep;		//外层循环再进行一个扰动

            } while (ExceptionOccur() && hsDistThreshBase > 0.0f);

            if (hsDistThreshBase <= 0.0f)
            {
                //说明已经超出了阈值范围，如果还有异常发生
                //则说明确实该模型的凸壳计算存在异常！
                Debug.LogError("gift wrap ExceptionOccur: hsDistThreshBase <= 0.0f");
                mExceptionOccur = true;
            }

            //hsDistThresh*=0.01f;	
            //CHalfSpace::SetDistThresh(hsDistThresh);  //恢复到缺省的阈值

            HalfSpace.SetDistThresh();  //恢复到缺省的阈值
        }
    }
