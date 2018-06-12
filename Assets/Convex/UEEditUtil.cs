using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System;
using System.Reflection;

public class UEEditUtil
{
    public const float CLOSE_VERTEX_DISTANCE_THRESH = .0001f;
    public const int MAX_FACE_IN_HULL = 200;

    public static bool MakeHull(Mesh mesh, Transform tr, string path, ref ConvexData data)
    {
        GiftWrap gw = new GiftWrap();
        gw.Reset();

        List<Vector3> vecVertex = new List<Vector3>();
        Vector3 vmin = Vector3.zero;
        Vector3 vmax = Vector3.zero;

        Bounds bound = mesh.bounds;
        vmin = bound.min;
        vmax = bound.max;

        Vector3[] verts = mesh.vertices;
        for (int j = 0; j < mesh.vertexCount; ++j)
        {
            Vector3 wp = verts[j];
            if(null != tr)
            {
                wp = tr.TransformPoint(wp);
            }
            bool bnear = false;
            for (int k = 0; k < (int)vecVertex.Count; k++)
            {
                if ((wp - vecVertex[k]).magnitude < CLOSE_VERTEX_DISTANCE_THRESH)
                {
                    bnear = true;
                    break;
                }
            }
            if (!bnear)
            {
                vecVertex.Add(wp);
            }
        }

        int numallvert = vecVertex.Count;
        gw.SetVertexes(vecVertex);
        gw.ComputeConvexHull();

        path += "/" + System.DateTime.Now.ToString("dd-MM-yy-HH-mm-ss") + "_vts.txt";
        if (gw.ExceptionOccur())
        {
            gw.SaveVerticesToFile(path);
            gw.Reset();
            return false;
        }

        ConvexPolytope cp = new ConvexPolytope();
        cp.Init(gw, (vmax - vmin).magnitude);

        if (cp.ExceptionOccur && cp.MinPatchNum > 20)
        {
            gw.SaveVerticesToFile(path);
            return false;
        }

        int patchnum = Mathf.Min(cp.OriginPatchNum, Mathf.Max(10, cp.MinPatchNum));
        if (patchnum > MAX_FACE_IN_HULL)
        {
            gw.Reset();
            return false;
        }
        else
        {
            cp.Goto(Mathf.Min(patchnum, MAX_FACE_IN_HULL));
        }

        gw.Reset();

        data.Reset();
        cp.ExportCHData(data);

        return true;
    }

}
