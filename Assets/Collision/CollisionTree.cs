using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CollisioinTreeNode
{
    public Bounds AABB;
    public List<CDBrush> LstCDBrush { get; set; }
    public CollisioinTreeNode[] Children { get; set; }
    public bool Used { get; set; }

    public CollisioinTreeNode()
    {
        AABB = new Bounds();
        AABB.Clear();
        LstCDBrush = new List<CDBrush>();
        Children = new CollisioinTreeNode[8];
    }

    public bool IsLeaf()
    {
        return Children[0] == null;
    }

    public void Release()
    {
        AABB.Clear();
        LstCDBrush.Clear();

        for (int i = 0; i < 8; ++i)
        {
            if(Children[i] == null)
            {
                continue;
            }
            Children[i].Release();
        }
    }
}

public class CollisionTree
{
    private CollisioinTreeNode _Root;
    private int _MinBrushInNode;
    private float _MinNodeSize;

    public Bounds AABB
    {
        get { return _Root.AABB; }
    }

    public CollisionTree()
    {
        _Root = new CollisioinTreeNode();
    }

    public CollisionTree(List<CDBrush> list)
    {
        Build(list);
    }

    public void AddCDBrush(List<CDBrush> brushs)
    {
        int all = brushs.Count;
        Bounds aabb = _Root.AABB;
        for (int i = 0; i < all; ++i)
        {
            aabb.Merge(brushs[i].BoundAABB);
            _Root.LstCDBrush.Add(brushs[i]);
        }

        _Root.AABB = aabb;
        Split(_Root);
    }

    public void RemoveCDBrush(List<CDBrush> brushs)
    {
        if(null == brushs || brushs.Count == 0)
        {
            return;
        }

        int all = brushs.Count;
            
        for (int i = 0; i < all; ++i)
        {
            Remove(_Root, brushs[i]);
        }

        Split(_Root);
    }

    public void Build(List<CDBrush> brushs,int minbrushinnode = 16,float minnodesize = 16)
    {
        Release();

        _MinBrushInNode = minbrushinnode;
        _MinNodeSize = minnodesize;
        _Root = new CollisioinTreeNode();

        Bounds aabb = _Root.AABB;

        int all = brushs.Count;
        for(int i = 0; i < all; ++i)
        {
            aabb.Merge(brushs[i].BoundAABB);
            _Root.LstCDBrush.Add(brushs[i]);
        }

        Split(_Root);
    }

    public bool CapsuleTraceBrush(CapsuleTraceBrushInfo pInfo)
    {
        return _CapsuleTraceBrush(_Root, pInfo);
    }
        
    public bool PointInBrush(Vector3 p, float offset)
    {
        return _PointInBrush(_Root, p, offset);
    }

    public bool _PointInBrush(CollisioinTreeNode pNode, Vector3 p, float offset)
    {
        if (null == pNode)
        {
            return false;
        }

        if(!pNode.AABB.IsPointIn(p,offset))
            return false;

        if (pNode.IsLeaf())
        {
            int i;
            for (i = 0; i < (int)pNode.LstCDBrush.Count; ++i)
            {
                CDBrush pBrush = pNode.LstCDBrush[i];

                if (pBrush.PointInBrush(p, offset))
                    return true;
            }
        }
        else
        {
            for (int j = 0; j < 8; j++)
            {
                if (_PointInBrush(pNode.Children[j], p, offset))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Release()
    {
        _Root.Release();
    }

    private void Remove(CollisioinTreeNode pNode,CDBrush brush)
    {
        for (int i = 0; i < 8; ++i)
        {
            if(pNode.Children.Length > i)
                Split(pNode.Children[i]);
        }

        for(int i = 0; i < pNode.LstCDBrush.Count; ++i)
        {
            if(pNode.LstCDBrush[i] == brush)
            {
                pNode.LstCDBrush.RemoveAt(i);
                break;
            }
        }
    }

    private void Split(CollisioinTreeNode pNode)
    {
        int nBrushes = pNode.LstCDBrush.Count;
	    if (nBrushes < _MinBrushInNode)
		    return;
	    if (pNode.AABB.extents.x * 2 < _MinNodeSize + 0.1f)
		    return;

	    Vector3 child_ext = pNode.AABB.extents * 0.5f;
	    float[] candidatex = new float[]{pNode.AABB.center.x - child_ext.x, pNode.AABB.center.x + child_ext.x};
	    float[] candidatey = new float[]{pNode.AABB.center.y - child_ext.y, pNode.AABB.center.y + child_ext.y};
	    float[] candidatez = new float[]{pNode.AABB.center.z - child_ext.z, pNode.AABB.center.z + child_ext.z};

	    for (int i = 0; i < 8; ++i)
	    {
		    pNode.Children[i] = new CollisioinTreeNode();
		    pNode.Children[i].AABB.center = new Vector3(candidatex[i&1], candidatey[(i&2)>>1], candidatez[(i&4)>>2]);
		    pNode.Children[i].AABB.extents = child_ext;

		    //divide brushes into child node
		    for (int j = 0; j < nBrushes; ++j)
		    {
                if (BoundsExtansions.AABBAABBOverlap(pNode.LstCDBrush[j].BoundAABB, pNode.Children[i].AABB))
                    pNode.Children[i].LstCDBrush.Add(pNode.LstCDBrush[j]);
		    }

            Split(pNode.Children[i]);
	    }

	    //remove brushes of parent node
        pNode.LstCDBrush.Clear();
    }

    private bool _CapsuleTraceBrush(CollisioinTreeNode pNode, CapsuleTraceBrushInfo pInfo)
    {
        if (null == pNode || null == pInfo)
        {
            return false;
        }

        if (!BoundsExtansions.AABBAABBOverlap(pNode.AABB, pInfo.Bound))
        {
            return false;
        }

	    bool bCollide = false;
        bool bStartSolid = false;	
	
        CDBrush HitObject = null;
        CollidePoints HitPoints = new CollidePoints();
        Vector3 normal = Vector3.zero;
	    float fFraction = 100.0f;

	    if (pNode.IsLeaf())
	    {
		    int i;
		    for (i = 0; i < (int)pNode.LstCDBrush.Count; ++i)
		    {
                CDBrush pBrush = pNode.LstCDBrush[i];

			    if (pBrush.CapsuleTraceBrush(pInfo) && (pInfo.Fraction < fFraction)) 
			    {
				    //update the saving info
				    bStartSolid = pInfo.StartSolid;
				    fFraction = pInfo.Fraction;
                    HitObject = pInfo.HitObject;
                    HitPoints = pInfo.HitPoints;
                    normal = pInfo.Normal;
				    bCollide = true;
	
                    //if (pInfo.Fraction == 0.0f)
                    //{
                    //    break;
                    //}
			    }
		    }
	    }
	    else
	    {
		    for (int j = 0; j < 8; j++)
		    {
                if (_CapsuleTraceBrush(pNode.Children[j], pInfo) && pInfo.Fraction < fFraction)
			    {
				    bStartSolid = pInfo.StartSolid;
				    fFraction = pInfo.Fraction;
                    HitObject = pInfo.HitObject;
                    HitPoints = pInfo.HitPoints;
                    normal = pInfo.Normal;
				    bCollide = true;
                    //if (pInfo.Fraction == 0.0f)
                    //{
                    //    break;
                    //}
			    }
		    }
	    }

	    if (bCollide)
	    {
		    //set back
		    pInfo.StartSolid = bStartSolid;
		    pInfo.Fraction = fFraction;
            pInfo.HitObject = HitObject;
            pInfo.HitPoints = HitPoints;
            pInfo.Normal = normal;
	    }
	    return bCollide;	
    }
}

    
