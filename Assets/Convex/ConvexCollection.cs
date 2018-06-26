using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConvexCollection : ScriptableObject
{
    public List<ConvexData> ConvexDatas = new List<ConvexData>();

    public void DebugRender(bool flag)
    {
        foreach(ConvexData cd in ConvexDatas)
        {
            cd.DebugRender(flag);
        }
    }
}
