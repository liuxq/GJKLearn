
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections;

 

public class SLBinary
{
    public static void SaveColor(LBinaryFile fs, Color col)
    {
        if (fs == null || fs.Writer == null)
            return;

        fs.Writer.Write(col.r);
        fs.Writer.Write(col.g);
        fs.Writer.Write(col.b);
        fs.Writer.Write(col.a);
    }

    public static Color LoadColor(LBinaryFile fs)
    {
        UnityEngine.Color col = Color.white;
        if (fs == null || fs.Reader == null)
            return col;

        col.r = fs.Reader.ReadSingle();
        col.g = fs.Reader.ReadSingle();
        col.b = fs.Reader.ReadSingle();
        col.a = fs.Reader.ReadSingle();

        return col;
    }

    public static void SaveVector3(LBinaryFile fs, Vector3 vec)
    {
        if (fs == null || fs.Writer == null)
            return;

        fs.Writer.Write(vec.x);
        fs.Writer.Write(vec.y);
        fs.Writer.Write(vec.z);
    }

    public static Vector3 LoadVector3(LBinaryFile fs)
    {
        Vector3 vec = Vector3.zero;
        if (fs == null || fs.Reader == null)
            return vec;

        vec.x = fs.Reader.ReadSingle();
        vec.y = fs.Reader.ReadSingle();
        vec.z = fs.Reader.ReadSingle();
        return vec;
    }

    public static void SaveString(LBinaryFile fs, string str)
    {
        if (fs == null || fs.Writer == null)
            return;
            
        if(string.IsNullOrEmpty(str))
        {
            str = "";
        }

        byte[] buf = fs.CurEncoding.GetBytes(str);
        fs.Writer.Write(buf.Length);
        if(buf.Length > 0)
        {
            fs.Writer.Write(buf);
        }
    }

    public static string LoadString(LBinaryFile fs)
    {
        string str = "";
        if (fs == null || fs.Reader == null)
            return str;

        int len = fs.Reader.ReadInt32();
        if (len > 0)
        {
            byte[] buf = fs.Reader.ReadBytes(len);
            str = fs.CurEncoding.GetString(buf);
        }

        return str;
    }

    public static void SaveBool(LBinaryFile fs, bool val)
    {
        if (fs == null || fs.Writer == null)
            return;

        fs.Writer.Write(val ? 1 : 0);
    }

    public static bool LoadBool(LBinaryFile fs)
    {
        bool val = false;
        if (fs == null || fs.Reader == null)
            return val;

        val = (fs.Reader.ReadInt32() != 0);

        return val;
    }

    public static void SaveAABB(LBinaryFile fs, Bounds aabb)
    {
        if (fs == null || fs.Writer == null)
            return;

        fs.Writer.Write(aabb.center.x);
        fs.Writer.Write(aabb.center.y);
        fs.Writer.Write(aabb.center.z);
        fs.Writer.Write(aabb.extents.x);
        fs.Writer.Write(aabb.extents.y);
        fs.Writer.Write(aabb.extents.z);
    }

    public static Bounds LoadAABB(LBinaryFile fs)
    {
        Bounds aabb = new Bounds();
        if (fs == null || fs.Reader == null)
            return aabb;

        Vector3 center = Vector3.zero;
        Vector3 extents = Vector3.zero;

        center.x = fs.Reader.ReadSingle();
        center.y = fs.Reader.ReadSingle();
        center.z = fs.Reader.ReadSingle();
        extents.x = fs.Reader.ReadSingle();
        extents.y = fs.Reader.ReadSingle();
        extents.z = fs.Reader.ReadSingle();
        aabb.center = center;
        aabb.extents = extents;
        return aabb;
    }

    public static void SaveBounds(LBinaryFile fs, Bounds bounds)
    {
        if (fs == null || fs.Writer == null)
            return;

        fs.Writer.Write(bounds.center.x);
        fs.Writer.Write(bounds.center.y);
        fs.Writer.Write(bounds.center.z);
        fs.Writer.Write(bounds.extents.x);
        fs.Writer.Write(bounds.extents.y);
        fs.Writer.Write(bounds.extents.z);
    }

    public static Bounds LoadBounds(LBinaryFile fs)
    {
        Bounds bounds = new Bounds();
        if (fs == null || fs.Reader == null)
            return bounds;

        Vector3 center = Vector3.zero;
        Vector3 extents = Vector3.zero;

        center.x = fs.Reader.ReadSingle();
        center.y = fs.Reader.ReadSingle();
        center.z = fs.Reader.ReadSingle();
        extents.x = fs.Reader.ReadSingle();
        extents.y = fs.Reader.ReadSingle();
        extents.z = fs.Reader.ReadSingle();
        bounds.center = center;
        bounds.extents = extents;

        return bounds;
    }

    public static void SaveQuaternion(LBinaryFile fs, Quaternion rot)
    {
        if (fs == null || fs.Writer == null)
            return;

        fs.Writer.Write(rot.x);
        fs.Writer.Write(rot.y);
        fs.Writer.Write(rot.z);
        fs.Writer.Write(rot.w);
    }

    public static Quaternion LoadQuaternion(LBinaryFile fs)
    {
        Quaternion rot = Quaternion.identity;
        if (fs == null || fs.Reader == null)
            return rot;

        rot.x = fs.Reader.ReadSingle();
        rot.y = fs.Reader.ReadSingle();
        rot.z = fs.Reader.ReadSingle();
        rot.w = fs.Reader.ReadSingle();
        return rot;
    }
}



