/*
 * CREATED:     2015-1-5 18:46:35
 * PURPOSE:     Basic geometry for 3D
 * AUTHOR:      Wangrui
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;



///////////////////////////////////////////////////////////////////////////
//	
//	struct CAPSULE
//	
///////////////////////////////////////////////////////////////////////////

//	Capsule
public struct CAPSULE
{
    public Vector3 Center;
    public float HalfLen;
    public float Radius;

    public CAPSULE(Vector3 center, float halfLen, float radius)
    {
        Center = center;
        HalfLen = halfLen;
        Radius = radius;
    }

	public CAPSULE(CAPSULE src)
    {
        Center = src.Center;
        HalfLen = src.HalfLen;
        Radius = src.Radius;
    }

    public float getSweepMargin()
    {
        return 0;
    }

    public bool isMarginEqRadius()
    {
        return true;
    }

    public float getMargin()
    {
        return Radius;
    }

        //This function is used in epa
        //dir is in the shape space
        public Vector3 supportSweepLocal(Vector3 dir)
        {
            Vector3 p0 = Center + HalfLen * Vector3.up;
            Vector3 p1 = Center + HalfLen * Vector3.down;
            float dist0 = Vector3.Dot(p0, dir);
            float dist1 = Vector3.Dot(p1, dir);
            return (dist0 > dist1)? p0 : p1;
        }

	//	Check whether a point is in this capsule
	public bool IsPointIn(Vector3 pos)
    {
        Vector3 delta = pos - Center;
            
        if (float.Equals(HalfLen, 0.0f))
        {
            //	The capped cylinder Is a sphere
            return (delta.sqrMagnitude <= Radius * Radius);
        }

        if (delta.x > Radius || delta.x < -Radius)  // Quick check
            return false;

        if (delta.z > Radius || delta.z < -Radius)  // Quick check
            return false;

        if (delta.x * delta.x + delta.z * delta.z > Radius * Radius)
            return false;

        if (delta.y >= -HalfLen && delta.y <= HalfLen)  // Quick check
            return true;

        if (delta.y > 0.0f)
        {
            pos.y -= HalfLen;
            delta = pos - Center;
        }
        else
        {
            pos.y += HalfLen;
            delta = pos - Center;
        }

        if (delta.sqrMagnitude <= Radius * Radius)
            return true;

        return false;
    }
}
