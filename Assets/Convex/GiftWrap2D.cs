
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

    public class GiftWrap2D : GiftWrap
    {
        public GiftWrap2D()
        {
            mLstVertex.Clear();
        }

        //获得凸包上的顶点，顶点的顺序为向-Y方向看是逆时针
        public override List<int> GetCHVertecies()
        {
            if (mLstPlanes.Count == 1)
                return mLstPlanes[0];

            return null;
        }

        public void SetVertexes(List<Vector3> vertexes)
        {
            base.SetVertexes(vertexes, false);

            //将顶点数据拷贝到内部
            mLstVertex = new List<Vector3>();
            mLstVertex.AddRange(vertexes);
            //置顶点的Y坐标为0.0f;
            for (int i = 0; i < mLstVertex.Count; i++)
            {
                Vector3 vec = mLstVertex[i];
                vec.y = 0.0f;
                mLstVertex[i] = vec;
            }
        }

        public override void ComputeConvexHull()
        {
            //设置HalfSpace的共面距离阈值	
            float hsDistThresh = 1e-1f;

            //保证没有异常的求出凸包！
            do
            {
                hsDistThresh *= 0.1f;							//放大阈值10倍
                HalfSpace.SetDistThresh(hsDistThresh);	//调整阈值

                //先调用父类函数从而完成清空操作
                base.ComputeConvexHull();
                ResetSameVertices();

                //寻找第一条边
                Edge edge = SearchFirstEdge(Vector3.up);
                mFirstEdge = edge;

                //由于都是共面因此，不需要作压栈等处理了！

                //处理该边
                DealEdge(edge);

            } while (mExceptionOccur && hsDistThresh > 5e-7);

            if (hsDistThresh < 5e-7f)
            {
                //说明已经超出了阈值范围，如果还有异常发生
                //则说明确实该模型的凸壳计算存在异常！
                mExceptionOccur = true;
            }

            //计算完后，m_Planes中保存了2D凸包的顶点连接信息

            //清空边栈
            mEdgeStack.Clear();

            HalfSpace.SetDistThresh();  //恢复到缺省的阈值
        }

        //设置顶点信息，X,Z形式的数据
        public void SetVertexes(List<float> lstX, List<float> lstZ)
        {
            base.SetVertexes(null);
            mLstVertex = new List<Vector3>();
            for (int i = 0; i < lstX.Count; i++)
            {
                mLstVertex.Add(new Vector3(lstX[i], 0.0f, lstZ[i]));
            }
        }
    }
