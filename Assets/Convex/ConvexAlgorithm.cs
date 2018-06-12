

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 

    public class Face
    {
        public int v1;
        public int v2;
        public int v3;
        public bool InPolygon;  // this angle may be only created from a polygon for render
        
        public Face()
        {
        }

        public Face(int p1, int p2, int p3, bool inP = false)
        {
            v1 = p1;
            v2 = p2;
            v3 = p3;
            InPolygon = inP;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }

            Face oface = obj as Face;
            if(null == oface)
            {
                return false;
            }

            return (this.v1 == oface.v1 || this.v1 == oface.v2 || this.v1 == oface.v3)
                && (this.v2 == oface.v1 || this.v2 == oface.v2 || this.v2 == oface.v3)
                && (this.v3 == oface.v1 || this.v3 == oface.v2 || this.v3 == oface.v3);
        }

        //public static bool operator ==(Face lhs, Face rhs)
        //{
        //    //if (lhs.Equals(null) && rhs.Equals(null))
        //    //    return true;
        //    //else if (lhs.Equals(null) || rhs.Equals(null))
        //    //    return false;

        //    return (lhs.v1 == rhs.v1 || lhs.v1 == rhs.v2 || lhs.v1 == rhs.v3)
        //        && (lhs.v2 == rhs.v1 || lhs.v2 == rhs.v2 || lhs.v2 == rhs.v3)
        //        && (lhs.v3 == rhs.v1 || lhs.v3 == rhs.v2 || lhs.v3 == rhs.v3);
        //}

        //public static bool operator !=(Face lhs, Face rhs)
        //{
        //    return !(lhs == rhs);
        //}
    }

    public class Edge
    {
        public int start;
        public int end;

        public Edge()
        {

        }

        public Edge(int v1, int v2)
        {
            Set(v1, v2);
        }

        public void Set(int v1, int v2)
        {
            start = v1;
            end = v2;
        }

        public bool InFace(Face f)
        {
            return (start == f.v1 || start == f.v2 || start == f.v3)
                && (end == f.v1 || end == f.v2 || end == f.v3);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }

            Edge oedge = obj as Edge;
            if (null == oedge)
            {
                return false;
            }

            return (this.start == oedge.start && this.end == oedge.end)
                    || (this.start == oedge.end && this.end == oedge.start);
            
        }

        //public static bool operator ==(Edge lhs, Edge rhs)
        //{
        //    //if (lhs.Equals(null) && rhs.Equals(null))
        //    //    return true;
        //    //else if (lhs.Equals(null) || rhs.Equals(null))
        //    //    return false;

        //    return (lhs.start == rhs.start && lhs.end == rhs.end)
        //        || (lhs.start == rhs.end && lhs.end == rhs.start);
        //}

        //public static bool operator !=(Edge lhs, Edge rhs)
        //{
        //    return !(lhs == rhs);
        //}
    }

    public abstract class ConvexAlgorithm
    {
        public Vector3 Centroid 
        {
            get { return mCentroid; }
            set { mCentroid = value; }
        }

        public List<Vector3> LstVertex
        {
            get { return mLstVertex; }
            protected set { mLstVertex = value; }
        }

        public List<bool> LstExtremeVertex
        {
            get { return mLstExtremeVertex; }
            protected set { mLstExtremeVertex = value; }
        }

        public List<Face> LstFaces
        {
            get { return mLstFaces; }
            protected set { mLstFaces = value; }
        }

        public List<List<int>> LstPlanes
        {
            get { return mLstPlanes; }
            protected set { mLstPlanes = value; }
        }

        protected Vector3 mCentroid;	    //待计算的整个点集的质心
	    protected List<Face> mLstFaces = new List<Face>();              //根据顶点计算出的凸壳，用面片表示
        protected List<Vector3> mLstVertex = new List<Vector3>();       //顶点集合，以数组方式组织,注意该变量仅为该类和接口调用者的一个引用，并不负责new和delete!
        protected List<bool> mLstVInvalid = new List<bool>();                              //对应于每一个顶点，标志该点是否处于共面共线的情况而被视为无效
        protected List<bool> mLstExtremeVertex = new List<bool>();                         //对应于每一个顶点，标志该点是否为Extreme Vertex!凸壳上的点
        protected List<List<int>> mLstPlanes = new List<List<int>>();   //对应于所有多点(>=3)共面的情况，这些平面集将被添加至此列表中

        public ConvexAlgorithm()
        {
            Reset();
        }

        public virtual void ComputeConvexHull()
        {
            mLstFaces.Clear();
        }

        public bool IsEdgeInCH(Edge edge)
        {
            if (edge == null)
                return false;

            for (int i = 0; i < mLstFaces.Count; i++)
            {
                if (edge.InFace(mLstFaces[i]))
                    return true;
            }
            
            // 然后，再需检查是否该面已经处于共面的平面中了
            for (int i = 0; i < mLstPlanes.Count; i++)
            {
                if (IsVInVSets(edge.start, mLstPlanes[i]) && IsVInVSets(edge.end, mLstPlanes[i]))
                    return true;
            }

            return false;
        }

	    public virtual void Reset()
        {
            mLstFaces.Clear();
            mLstVInvalid.Clear();
            mLstExtremeVertex.Clear();

            for (int i = 0; i < mLstPlanes.Count; i++)
            {
                mLstPlanes[i].Clear();
            }
            mLstPlanes.Clear();
        }

	    public bool IsVInVSets(int vid, List<int> verts)
        {
            return verts.Contains(vid);
        }

	    public bool IsFaceInCH(Face face)
        {
            if (face == null)
                return false;

            for (int i = 0; i < mLstPlanes.Count; i++)
            {
                if (IsVInVSets(face.v1, mLstPlanes[i]) && IsVInVSets(face.v2, mLstPlanes[i]) && IsVInVSets(face.v3, mLstPlanes[i]))
                    return true;
            }

            return false;
        }
	    
        public virtual void SetVertexes(List<Vector3> vertexes, bool trans = false)
        {
            mLstVertex = vertexes;
            mCentroid = Vector3.zero;
            if (trans)
            {
                for (int i = 0; i < mLstVertex.Count; i++)
                {
                    mCentroid += mLstVertex[i];
                }

                if (mLstVertex.Count > 0)
                {
                    mCentroid /= (float)(mLstVertex.Count);
                }
               
                // Translate vertex according to centroid
                for (int i = 0; i < mLstVertex.Count; i++)
                    mLstVertex[i] -= mCentroid;
            }

            mLstVInvalid.Clear();
            mLstExtremeVertex.Clear();
            for (int i = 0; i < mLstVertex.Count; i++)
            {
                mLstVInvalid.Add(false);
                mLstExtremeVertex.Add(false);
            }
        }
        
	    List<Face> GetCHFaces() 
        { 
            return mLstFaces;
        }    
    }

    public class EdgeStack
    {
        private Stack<Edge> mEdgeStack = new Stack<Edge>();
        public int Count 
        {
            get { return mEdgeStack.Count; }
        }

        public EdgeStack()
        {
        }

        public void Clear()
        {
            mEdgeStack.Clear();
        }

        public bool IsEmpty()
        {
            return mEdgeStack.Count == 0;
        }

        public void Push(Edge edge)
        {
            if(null == edge)
            {
                return;
            }

            mEdgeStack.Push(edge);
        }

        public Edge Pop()
        {
            return mEdgeStack.Pop();
        }

        //选择性压栈！如果edge已经在栈中，则不但不压，反而弹出！
        public bool CheckPush(Edge edge)
        {
            bool find = false;
            if (mEdgeStack.Count > 0)
            {
                // If stack contains edge, remove it
                Stack<Edge> stkTmp = new Stack<Edge>();
                while (mEdgeStack.Count > 0)
	            {
                    Edge e = mEdgeStack.Pop();
                    if (e != null && e.Equals(edge))
                    {
                        // find the edge in stack, remove it
                        find = true;
                        break;
                    }
                    else
                    {
                        stkTmp.Push(e);
                    }
	            }

                while (stkTmp.Count > 0)
                {
                    mEdgeStack.Push(stkTmp.Pop());
                }
            }

            if (find)
                return false;

            mEdgeStack.Push(edge);

            return true;           
        }
    }


