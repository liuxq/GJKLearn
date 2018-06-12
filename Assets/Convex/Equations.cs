

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 


    class LEquations
    {
        public Matrix MtxCoef { get; set; }
        public Matrix MtxConst { get; set; }

        public LEquations()				    // 默认构造函数
        {

        }
        // 指定系数和常数构造函数
        public LEquations(Matrix mtxCoef, Matrix mtxConst)
        {
            Init(mtxCoef, mtxConst);
        }

        public bool Init(Matrix mtxCoef, Matrix mtxConst)
        {
            if (mtxCoef.RowNum != mtxConst.RowNum)
                return false;

            MtxCoef = mtxCoef;
            MtxConst = mtxConst;

            return true;
        }


        public int GetNumEquations()	    // 获取方程个数
        {
            return (MtxCoef != null) ? MtxCoef.RowNum : 0;
        }

        public int GetNumUnknowns()		// 获取未知数个数
        {
            return (MtxCoef != null) ? MtxCoef.ColNum : 0;
        }

        //////////////////////////////////////////////////////////////////////
        // 全选主元高斯消去法
        //
        // 参数：
        // 1. out Matrix result - 返回方程组的解
        //
        // 返回值：bool 型，方程组求解是否成功
        //////////////////////////////////////////////////////////////////////
        public bool GetRootsetGauss(out Matrix result)
        {
            int l, k, i, j, p, q;
            int nIs = 0;
            double d, t;

            // 方程组的属性，将常数矩阵赋给解矩阵
            result = new Matrix(MtxConst);
            List<double> lstDataCoef = MtxCoef.LstData;
            List<double> lstDataConst = result.LstData;
            int n = GetNumUnknowns();
            // 临时缓冲区，存放列数
            List<int> lstNJs = new List<int>(n);
            for (int m = 0; m < n; ++m )
            {
                lstNJs.Add(0);
            }

            // 消元
            l = 1;
            for (k = 0; k <= n - 2; k++)
            {
                d = 0.0;
                for (i = k; i <= n - 1; i++)
                {
                    for (j = k; j <= n - 1; j++)
                    {
                        t = Math.Abs(lstDataCoef[i * n + j]);
                        if (t > d)
                        {
                            d = t;
                            lstNJs[k] = j;
                            nIs = i;
                        }
                    }
                }

                if (d == 0.0)
                    l = 0;
                else
                {
                    if (lstNJs[k] != k)
                    {
                        for (i = 0; i <= n - 1; i++)
                        {
                            p = i * n + k;
                            q = i * n + lstNJs[k];
                            t = lstDataCoef[p];
                            lstDataCoef[p] = lstDataCoef[q];
                            lstDataCoef[q] = t;
                        }
                    }

                    if (nIs != k)
                    {
                        for (j = k; j <= n - 1; j++)
                        {
                            p = k * n + j;
                            q = nIs * n + j;
                            t = lstDataCoef[p];
                            lstDataCoef[p] = lstDataCoef[q];
                            lstDataCoef[q] = t;
                        }

                        t = lstDataConst[k];
                        lstDataConst[k] = lstDataConst[nIs];
                        lstDataConst[nIs] = t;
                    }
                }

                // 求解失败
                if (l == 0)
                {
                    lstNJs.Clear();
                    return false;
                }

                d = lstDataCoef[k * n + k];
                for (j = k + 1; j <= n - 1; j++)
                {
                    p = k * n + j;
                    lstDataCoef[p] = lstDataCoef[p] / d;
                }

                lstDataConst[k] = lstDataConst[k] / d;
                for (i = k + 1; i <= n - 1; i++)
                {
                    for (j = k + 1; j <= n - 1; j++)
                    {
                        p = i * n + j;
                        lstDataCoef[p] = lstDataCoef[p] - lstDataCoef[i * n + k] * lstDataCoef[k * n + j];
                    }

                    lstDataConst[i] = lstDataConst[i] - lstDataCoef[i * n + k] * lstDataConst[k];
                }
            }

            // 求解失败
            d = lstDataCoef[(n - 1) * n + n - 1];
            if (d == 0.0)
            {
                lstNJs.Clear();
                return false;
            }

            // 求解
            lstDataConst[n - 1] = lstDataConst[n - 1] / d;
            for (i = n - 2; i >= 0; i--)
            {
                t = 0.0;
                for (j = i + 1; j <= n - 1; j++)
                    t = t + lstDataCoef[i * n + j] * lstDataConst[j];
                lstDataConst[i] = lstDataConst[i] - t;
            }

            // 调整解的位置
            lstNJs[n - 1] = n - 1;
            for (k = n - 1; k >= 0; k--)
            {
                if (lstNJs[k] != k)
                {
                    t = lstDataConst[k];
                    lstDataConst[k] = lstDataConst[lstNJs[k]];
                    lstDataConst[lstNJs[k]] = t;
                }
            }

            lstNJs.Clear();
            return true;
        }
    }
