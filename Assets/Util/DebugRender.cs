
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class UEDebugRender
{
    public List<Vector3> PosList = new List<Vector3>();
    public LineRenderer LineRender;
    public List<int> IndexList = new List<int>();
    public Color Color = Color.green;
    public float Width = .02f;

    public void ChangeColor(Color color)
    {
        if (null != LineRender)
        {
            LineRender.startColor = color;
            LineRender.endColor = color;
            //LineRender.SetColors(color, color);
        }
    }

    public LineRenderer CreateLineRender()
    {
        //if (null == LineRender)
        //{
        //    GameObject obj = new GameObject("tlinerender");
        //    LineRender = obj.GetComponent<LineRenderer>();
        //    if (null == LineRender)
        //        LineRender = obj.AddComponent<LineRenderer>();

        //    //Shader shader = UEMacroConf.GetShader(GetShaderName());
        //    //if(null == shader)
        //    //{
        //    //    shader = Shader.Find("Diffuse");
        //    //}
        //    Shader shader = Shader.Find(GetShaderName());

        //    LineRender.sharedMaterial = new Material(shader);
        //    Color startcol = null != Param ? Param.Color : Color.green;
        //    Color endcol = null != Param ? Param.Color : Color.green;
        //    Color gratcol = null != Param ? Param.Color : Color.green;
        //    LineRender.startColor = startcol;
        //    LineRender.endColor = endcol;

        //    float startwid = null != Param ? Param.Width : .02f;
        //    float endwid = null != Param ? Param.Width : .02f;
        //    LineRender.startWidth = startwid;
        //    LineRender.endWidth = endwid;
        //    //if(null != Param)
        //    //    LineRender.SetColors(Param.Color, Param.Color);
        //    //else
        //    //    LineRender.SetColors(Color.green, Color.green);
        //    //if(null != Param)
        //    //    LineRender.SetWidth(Param.Width, Param.Width);
        //    //else
        //    //    LineRender.SetWidth(.02f, .02f);
        //}
        //else
        //{

        //}

        return LineRender;
    }

    public virtual void Destroy()
    {
        if (null != LineRender)
        {
            UnityEngine.Object.Destroy(LineRender.gameObject);
            LineRender = null;
        }
    }

    public void SetPoint(int index, Vector3 pos)
    {
        if (index >= PosList.Count)
        {
            PosList.Add(pos);
        }
        else if (index >= 0)
        {
            PosList[index] = pos;
        }
    }

    public virtual void Update()
    {
        if (null != LineRender)
        {

            LineRender.positionCount = PosList.Count;
            LineRender.SetPositions(PosList.ToArray());
            //for (int i = 0; i < PosList.Count; ++i)
            //{

            //}
        }
    }

}

public class UEDebugAABB : UEDebugRender
{
    private Bounds mDebugAABB;

    public void BeginDebug(Bounds aabb)
    {
        mDebugAABB = aabb;
        SetPointinfo();
        CreateLineRender();
    }

    public void SetAABB(Bounds aabb)
    {
        if (mDebugAABB.Equals(aabb) || aabb.extents.Equals(Vector3.zero))
        {
            return;
        }

        mDebugAABB = aabb;
        SetPointinfo();
    }

    public override void Update()
    {
        SetPointinfo();
        base.Update();
    }

    private void SetPointinfo()
    {
        PosList.Clear();
        Vector3 dir = mDebugAABB.extents;
        if (dir.Equals(Vector3.zero))
        {
            return;
        }
        float dirx = mDebugAABB.extents.x * 2;
        float diry = mDebugAABB.extents.y * 2;
        float dirz = mDebugAABB.extents.z * 2;
        Vector3 center = mDebugAABB.center;
        Vector3 pos1 = center - dir;
        Vector3 pos2 = center - dir + dirx * Vector3.right;
        Vector3 pos3 = center - dir + diry * Vector3.up;
        Vector3 pos4 = center - dir + dirz * Vector3.forward;
        Vector3 pos5 = center + dir;
        Vector3 pos6 = center + dir - dirx * Vector3.right;
        Vector3 pos7 = center + dir - diry * Vector3.up;
        Vector3 pos8 = center + dir - dirz * Vector3.forward;

        PosList.Add(pos1);
        PosList.Add(pos2);
        PosList.Add(pos7);
        PosList.Add(pos4);
        PosList.Add(pos1);

        PosList.Add(pos1);
        PosList.Add(pos3);
        PosList.Add(pos2);
        PosList.Add(pos8);

        PosList.Add(pos7);
        PosList.Add(pos5);
        PosList.Add(pos4);
        PosList.Add(pos6);

        PosList.Add(pos5);
        PosList.Add(pos6);
        PosList.Add(pos3);
        PosList.Add(pos8);
        PosList.Add(pos5);
    }
}

public class UEDebugMeshRender : UEDebugRender
{
    public const string MatPath = "Assets&Resources&Effect&DebugMat&convexmat.mat";
    public GameObject ParentObj;
    public GameObject _MeshObj;
    public Bounds AABB { get; set; }
    public Material Mat;

    public void AddCollider()
    {
        if (null != _MeshObj)
        {
            MeshCollider mc = _MeshObj.GetComponent<MeshCollider>();
            if (null == mc)
            {
                _MeshObj.AddComponent<MeshCollider>();
            }
        }
    }

    public void Create(List<Vector3> vlist, List<int> ilist, List<Color> clist = null)
    {
        CreateMesh(vlist, ilist, clist);
    }

    public void CreateMesh(List<Vector3> vlist, List<int> ilist, List<Color> clist)
    {
        if (null != _MeshObj)
        {
            Destroy();
        }

        if (vlist == null || ilist == null)
        {
            return;
        }

        MeshFilter mf = null;
        if (null == _MeshObj)
        {
            _MeshObj = new GameObject("debugconvexdata");

            mf = _MeshObj.AddComponent<MeshFilter>();
            MeshRenderer mr = _MeshObj.AddComponent<MeshRenderer>();

            if (null == Mat)
            {
                Mat = new Material(Shader.Find("Diffuse"));//Shader.Find("Terrain/CollideMesh")
            }
            mr.sharedMaterial = Mat; 
        }
        else
        {
            mf = _MeshObj.GetComponent<MeshFilter>();
        }

        Mesh _Mesh = new Mesh();
        _Mesh.vertices = vlist.ToArray();
        _Mesh.triangles = ilist.ToArray();
        //if(null != clist)
        //    _Mesh.colors = clist.ToArray();
        List<Vector2> uv = new List<Vector2>();
        foreach (Vector3 p in vlist)
        {
            uv.Add(Vector2.zero);
        }
        _Mesh.uv = uv.ToArray();
        _Mesh.RecalculateNormals();

        mf.mesh = _Mesh;
    }

    public override void Destroy()
    {
        base.Destroy();

        if (null != _MeshObj)
        {
            MeshFilter mf = _MeshObj.GetComponent<MeshFilter>();
            if (null != mf)
            {

                UnityEngine.Object.DestroyImmediate(mf.sharedMesh);
            }

            UnityEngine.Object.DestroyImmediate(_MeshObj);
            _MeshObj = null;
        }
    }
}


