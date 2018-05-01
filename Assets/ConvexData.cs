using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class ConvexData
{
    public List<Vector3> Verts = new List<Vector3>();
        
    public Vector3 GetCenter()
    {
        Vector3 sum = Vector3.zero;
        foreach(Vector3 v in Verts)
        {
            sum += v;
        }
        return sum / Verts.Count;
    }

    public int BruteForceSearch(Vector3 _dir) 
	{
		//brute force
		//get the support point from the orignal margin
		float max = Vector3.Dot(Verts[0], _dir);
		int maxIndex=0;

		for(int i = 1; i < Verts.Count; ++i)
		{
            Vector3 vertex = Verts[i];
			float dist = Vector3.Dot(vertex, _dir);
			if(dist > max)
			{
				max = dist;
				maxIndex = i;
			}
		}

		return maxIndex;
	}

    //This function is used in epa
	//dir is in the shape space
    public Vector3 supportSweepLocal(Vector3 dir)
	{
		int maxIndex = BruteForceSearch(dir);
        return Verts[maxIndex];
	}

    public float getSweepMargin()
    {
        return 0;
    }

}

