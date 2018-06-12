
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


 

    /////////////////////////////////////////////////////////////
    // 从m种不同的选择中选出n种作为一个组合
    // 本类列出了每一种可能的选法
    // 即一个C(n,m)问题
    /////////////////////////////////////////////////////////////

    class CombGenerator  
    {
        public bool Solvable;   //是否有解？当n>m以及n,m为非正时，置为false
        public bool Over;       //是否已经全部罗列出所有的组合？

        private List<int> mLstData;
        private int mM;
        private int mN;

        public CombGenerator(int nn, int mm)
        {
            Set(nn, mm);
        }

        //////////////////////////////////////////////////////////////////////
        // 将当前算出的组合传给pData，同时计算下一个组合
        //////////////////////////////////////////////////////////////////////
        public void GetNextComb(List<int> lstData)				//将当前算出的组合传给pData，同时计算下一个组合
        {
            lstData.Clear();
            for (int i = 0; i < mLstData.Count; i++)
            {
                lstData.Add(mLstData[i]);
            }
            
            //传出当前的组合
            if (Over) 
                return;

            int carryPos = mN - 1;		//进位的位置！
            //计算下一组合
            for (int i = mN - 1; i >= 0; i--)
            {
                if (mLstData[i] == mM - mN + i)			//当前位置已经达到最大值，如果增1，需要进位！因此记录进位位置为上一位置
                    carryPos = i - 1;
                else
                    break;
            }

            if (carryPos == -1)
            {
                Over = true;		//所有的组合都已产生完毕！
                return;				//如果进位的位置是-1，则说明已经计算出所有的组合，并且不能再进位了，因此直接返回！
            }

            mLstData[carryPos]++;					//进位处增1

            int value = mLstData[carryPos] + 1;
            for (int i = carryPos + 1; i < mN; i++)				//进位处后面的位置重新置新值，如进位处为k,则后面依次为k+1,k+2,...
            {
                mLstData[i] = value;
                value++;
            }
        }

	    public void Set(int nn, int mm)
        {
            mN = nn;
            mM = mm;

            if (mN <= 0 || mM <= 0 || mN > mM)
            {
                Solvable = false;
                if (mLstData != null)
                    mLstData.Clear();
                return;
            }

            mLstData = new List<int>(mN);

            for (int i = 0; i < mN; i++)		//初始化，并得到了第一个组合 0,1,2,...,n-1
                mLstData.Add(i);

            Over = false;
            Solvable = true;
        }
    }

