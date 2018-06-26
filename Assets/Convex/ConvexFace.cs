
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 

[Serializable]
public class CovFace : HalfSpace  
{
    public const string _Key_FaceNormal = "FaceNormal";
    public const string _Key_FaceDist = "FaceDist";
    public const string _Key_FaceEleNum = "FaceEleNum";
    public const string _Key_FaceEleVid = "FaceEleVid";
    public const string _Key_FaceEleNormal = "FaceEleNormal";
    public const string _Key_FaceEleDist = "FaceEleDist";
		
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

    public List<int> mLstVIDs = new List<int>();                     //按顺序记录顶点的索引id；

    public CovFace()
    {

    }

	public CovFace(CovFace face)
    {
        Set(face);
    }

    public void Set(CovFace face)
    {
        Normal = face.Normal;
        Dist = face.Dist;
           
        for (int i = 0; i < face.GetVNum(); i++)
        {
            int vid = face.GetVID(i);
            AddElement(vid);
        }
    }

	public void SetHS(HalfSpace hs)
	{
		Normal = hs.Normal;
		Dist = hs.Dist;
	}

    // 对面片进行变换！变换矩阵为mtxTrans
    public override void Transform(Matrix4x4 mtxTrans)
    {
        base.Transform(mtxTrans);
    }

    // 镜像变换，镜像轴参见CConvexHullData::MIRROR_TYPE
    public override void MirrorTransform(ConvexData.MIRROR_TYPE mirror)
    {
        base.MirrorTransform(mirror);

        int nAllVTX = mLstVIDs.Count;
        int nHalfVTX = nAllVTX / 2;
        for (int i = 0; i < nHalfVTX; ++i)
        {
            int tmp = mLstVIDs[i];
            mLstVIDs[i] = mLstVIDs[nAllVTX - 1 - i];
            mLstVIDs[nAllVTX - 1 - i] = tmp;
        }
    }

    //重置，清空顶点
    public void Reset() 
    { 
        mLstVIDs.Clear(); 
    }

    public int GetEdgeNum()
    { 
        return mLstVIDs.Count;
    }

    public int GetVNum()
    { 
        return mLstVIDs.Count;
    }

    public int GetVID(int i)
    { 
        return mLstVIDs[i];
    }

    public void AddElement(int vid)
    { 
        mLstVIDs.Add(vid);
    }

    public bool EditLoad(LEditTextFile file)
    {
        Normal = file.LoadVector3Line(_Key_FaceNormal);
        Dist = file.LoadValueLine<float>(_Key_FaceDist);
        int elenum = file.LoadValueLine<int>(_Key_FaceEleNum);
        for (int i = 0; i < elenum; i++)
        {
            HalfSpace hs = new HalfSpace();
            int vid = file.LoadValueLine<int>(_Key_FaceEleVid);
            hs.Normal = file.LoadVector3Line(_Key_FaceEleNormal);
            hs.Dist = file.LoadValueLine<float>(_Key_FaceEleDist);
            AddElement(vid);
        } 
        return true;
    }


    public bool EditSave(LEditTextFile file)
    {
        file.SaveVector3Line(_Key_FaceNormal, Normal);
        file.SaveValueLine<float>(_Key_FaceDist, Dist);
        int elenum = GetEdgeNum();
        file.SaveValueLine<int>(_Key_FaceEleNum, elenum);
        for (int i = 0; i < elenum; i++)
        {
            file.SaveValueLine<int>(_Key_FaceEleVid, mLstVIDs[i]);
            file.SaveVector3Line(_Key_FaceEleNormal, Vector3.up);//废弃
            file.SaveValueLine<float>(_Key_FaceEleDist, 0);//废弃
        }
        return true;
    }
    public virtual bool Load(LBinaryFile fs, uint version)
    {
        Normal = SLBinary.LoadVector3(fs);
        Dist = fs.Reader.ReadSingle();
        int elenum = fs.Reader.ReadInt32();
        for (int i = 0; i < elenum; i++)
        {
            int vid = fs.Reader.ReadInt32();
            AddElement(vid);
        }
        return true;
    }

    public virtual bool Save(LBinaryFile fs)
    {
        SLBinary.SaveVector3(fs, Normal);
        fs.Writer.Write(Dist);
        int elenum = GetEdgeNum();
        fs.Writer.Write(elenum);
        for (int i = 0; i < elenum; i++)
        {
            fs.Writer.Write(mLstVIDs[i]);
        }
        return true;
    }        
}
