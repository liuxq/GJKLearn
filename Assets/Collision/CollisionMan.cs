
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CollisionMan : MonoBehaviour
{
    static public CollisionMan Instance = null;

    public ConvexCollection _ConvexCollection;

    public List<Transform> DynamicColliders = new List<Transform>();

    protected CollisionTree _Tree;

    public void Start()
    {
        _Tree = new CollisionTree();
        Instance = this;
        ResetCollisionMap();
    }

    public void UnLoad()
    {
        _Tree.Release();
    }

    public bool CapsuleTraceBrush(CapsuleTraceBrushInfo info)
    {
        bool ret = _Tree.CapsuleTraceBrush(info);
        return ret;
    }

    public bool DynamicCapsuleTraceCapsules(CapsuleTraceBrushInfo info)
    {
        bool ret = false;
        foreach(Transform tf in DynamicColliders)
        {
            CAPSULE b = new CAPSULE(tf.transform.position, 0.5f, 0.5f, Quaternion.identity);
            float fFraction = 100.0f;
            Vector3 normal = Vector3.up;
            bool bStartSolid = false;
            if (GJKRaycast.GjkLocalRayCast_CapsuleCapsule(info.Start, b, info.Delta, ref fFraction, ref normal, ref bStartSolid) && fFraction < info.Fraction)
            {
                info.StartSolid = bStartSolid;
                info.Fraction = fFraction;
                info.Normal = normal;
                ret = true;
            }
        }
        return ret;
    }

    public bool PointInBrush(Vector3 p, float offset)
    {
        return _Tree.PointInBrush(p, offset);
    }

    public void ResetCollisionMap()
    {
        List<CDBrush> list = new List<CDBrush>();
        if (_ConvexCollection != null)
        {
            foreach (ConvexData cd in _ConvexCollection.ConvexDatas)
            {
                cd.GetAABB();
                CDBrush pCDBrush = new CDBrush();
                cd.Export(pCDBrush);
                list.Add(pCDBrush);
            }

        }

        if (list.Count != 0)
            _Tree.Build(list);
    }
}

    
