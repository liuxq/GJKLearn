
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

    public struct Matrix3x3
    {
        public float m00;
        public float m01;
        public float m02;
        public float m10;
        public float m11;
        public float m12;
        public float m20;
        public float m21;
        public float m22;

        public Matrix3x3(Matrix3x3 mat)
        {
            m00 = mat[0, 0];
            m01 = mat[0, 1];
            m02 = mat[0, 2];
            m10 = mat[1, 0];
            m11 = mat[1, 1];
            m12 = mat[1, 2];
            m20 = mat[2, 0];
            m21 = mat[2, 1];
            m22 = mat[2, 2];
        }

        public Matrix3x3(float _m00, float _m01, float _m02,
			       float _m10, float _m11, float _m12,
			       float _m20, float _m21, float _m22)
	    {
		    m00 = _m00;
		    m01 = _m01;
		    m02 = _m02;
		    m10 = _m10;
		    m11 = _m11;
		    m12 = _m12;
		    m20 = _m20;
		    m21 = _m21;
		    m22 = _m22;
	    }

        public float this[int index]
        {
            get 
            { 
                switch (index)
                {
                    case 0: return m00;
                    case 1: return m01;
                    case 2: return m02;
                    case 3: return m10;
                    case 4: return m11;
                    case 5: return m12;
                    case 6: return m20;
                    case 7: return m21;
                    case 8: return m22;
                    default: return 0.0f;
                }
            }
            set 
            {
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m01 = value; break;
                    case 2: m02 = value; break;
                    case 3: m10 = value; break;
                    case 4: m11 = value; break;
                    case 5: m12 = value; break;
                    case 6: m20 = value; break;
                    case 7: m21 = value; break;
                    case 8: m22 = value; break;
                    default: break;
                }
            }
        }

        public float this[int row, int col]
        {
            get { return this[row * 3 + col]; }
            set { this[row * 3 + col] = value; }
        }

        public static readonly Matrix3x3 identity = new Matrix3x3(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
        public static readonly Matrix3x3 zero = new Matrix3x3(0.0f, 0.0f, 0.0f, 0.0f, .0f, 0.0f, 0.0f, 0.0f, 0.0f);

        public void Clear()
        {
            for (int i = 0; i < 9; i++)
            {
                this[i] = 0.0f;
            }
        }

        public void Identity()
        {
            Clear();
            this[0, 0] = 1.0f;
            this[1, 1] = 1.0f;
            this[1, 1] = 1.0f;
        }

        public void Transpose()
        {
            float t;
            t = this[0,1]; this[0,1] = this[1,0]; this[1,0] = t;
            t = this[0,2]; this[0,2] = this[2,0]; this[2,0] = t;
            t = this[1,2]; this[1,2] = this[2,1]; this[2,1] = t;
        }

        public static Matrix3x3 operator *(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            Matrix3x3 mat = new Matrix3x3();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        mat[i, j] += lhs[i, k] * rhs[k, j];
                    }
                }
            }
            return mat;
        }

        public static Vector3 operator *(Matrix3x3 lhs, Vector3 v)
        {
            return new Vector3(v.x * lhs[0,0] + v.y * lhs[1,0] + v.z * lhs[2,0],
                              v.x * lhs[0,1] + v.y * lhs[1,1] + v.z * lhs[2,1],
                              v.x * lhs[0,2] + v.y * lhs[1,2] + v.z * lhs[2,2]);
        }

        public static Vector3 operator *(Vector3 v, Matrix3x3 rhs)
        {
            return new Vector3(v.x * rhs[0,0] + v.y * rhs[1,2] + v.z * rhs[2,0],
                              v.x * rhs[0,1] + v.y * rhs[1,1] + v.z * rhs[2,1],
                              v.x * rhs[0,2] + v.y * rhs[1,2] + v.z * rhs[2,2]);             
        }

        public static bool operator ==(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            for (int i = 0; i < 9; i++)
            {
                if (lhs[i] != rhs[i])
                    return false;
            }
            return true;
        }

        public static bool operator !=(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object other)
        {
            return base.Equals(other);
        }

        public Vector3 GetRow(int row)
        {
            return new Vector3(this[row, 1], this[row, 2], this[row, 3]);
        }

        public Vector3 GetColumn(int col)
        {
            return new Vector3(this[0, col], this[1, col], this[2, col]);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Matrix  
    {
	    // 其矩阵数据缓冲区为一维double列表
	    // 存储顺序为行优先，即逐行存储，同一行的元素在位置上邻近
        public int RowNum { get; set; }
        public int ColNum { get; set; }
        public List<double> LstData { get; private set; }
         
	    public Matrix()
        {
            RowNum = 1;
            ColNum = 1;
            Init(RowNum, ColNum);
        }

	    public Matrix(int row, int col)					        // 指定行列构造函数
        {
            RowNum = row;
            ColNum = col;
            Init(RowNum, ColNum);
        }

	    public Matrix(int row, int col, List<double> lstData)	// 指定数据构造函数
        {
            RowNum = row;
            ColNum = col;
            SetData(LstData);
        }

	    public Matrix(Matrix other)					            // 拷贝构造函数
        {
            RowNum = other.RowNum;
            ColNum = other.ColNum;
            SetData(other.LstData);
        }

	    public bool Init(int row, int col)
        {
            if (LstData != null)
	        {
		        LstData.Clear();
	        }

            RowNum = row;
            ColNum = col;
            int size = RowNum * ColNum;
            if (size < 0)
                return false;

            LstData = new List<double>(size);
            for (int i = 0; i < size; ++i)
            {
                LstData.Add(.0f);
            }
            return true;
        }

        public bool SetData(List<double> data)
        {
            if (data == null || data.Count == 0)
            {
                if (LstData != null)
                {
                    LstData.Clear();
                    LstData = null;
                }
            }

            if (LstData == null)
            {
                LstData = new List<double>(data.Count);
                for (int i = 0; i < data.Count; ++i)
                {
                    LstData.Add(.0f);
                }
            }

            for (int i = 0; i < data.Count; i++)
            {
                LstData[i] = data[i];
            }

            return true;
        }
	
	    public bool MakeUnitMatrix(int size)			    // 将方阵初始化为单位矩阵
        {
	        if (!Init(size, size))
		        return false;

            for (int i = 0; i < size; i++)
			{
			    SetElement(i, i, 1);
			}

	        return true;
        }

	    public bool	SetElement(int row, int col, double value)	// 设置指定元素的值
        {
	        if (col < 0 || col >= ColNum || row < 0 || row >= RowNum)
		        return false;						    // array bounds error
	        if (LstData == null)
		        return false;				            // bad pointer error	
	        LstData[col + row * ColNum] = value;

            return true;
        }

	    public double GetElement(int row, int col)			    // 获取指定元素的值
        {
	        if (col < 0 || col >= ColNum || row < 0 || row >= RowNum)
		        return 0;
	        if (LstData == null)
		        return 0;
            return LstData[col + row * ColNum];
        }

	    public int GetRowVector(int row, List<double> lstVec)	// 获取矩阵的指定行矩阵
        {
	        if (lstVec == null)
		        return 0;

            lstVec.Clear();
	        for (int j = 0; j < ColNum; ++j)
		        lstVec[j] = GetElement(row, j);

	        return ColNum;
        }
	    public int GetColVector(int col, List<double> lstVec)	// 获取矩阵的指定列矩阵
        {
	        if (lstVec == null)
		        return 0;

            lstVec.Clear();
	        for (int j = 0; j < ColNum; ++j)
		        lstVec[j] = GetElement(j, col);

	        return ColNum;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(!base.Equals(obj))
            {
                return false;
            }

            Matrix omatrix = obj as Matrix;
            
            if (this.ColNum != omatrix.ColNum || this.RowNum != omatrix.RowNum)
                return false;

            for (int i = 0; i < this.RowNum; ++i)
            {
                for (int j = 0; j < this.ColNum; ++j)
                {
                    if (this.GetElement(i, j) != omatrix.GetElement(i, j))
                        return false;
                }
            }

            return true;
        }

        //public static bool operator ==(Matrix lhs, Matrix rhs)
        //{
        //    if (lhs.ColNum != rhs.ColNum || lhs.RowNum != rhs.RowNum)
        //        return false;            	

        //    for (int i = 0; i < lhs.RowNum; ++i)
        //    {
        //        for (int j = 0; j < lhs.ColNum; ++j)
        //        {
        //            if (lhs.GetElement(i, j) != rhs.GetElement(i, j))
        //                return false;
        //        }
        //    }

        //    return true;
        //}

        //public static bool operator !=(Matrix lhs, Matrix rhs)
        //{
        //    return !(lhs == rhs);
        //}

        public static Matrix operator +(Matrix lhs, Matrix rhs)
        {
            if (lhs.ColNum != rhs.ColNum || lhs.RowNum != rhs.RowNum)
                return null;

	        // 构造结果矩阵
	        Matrix result = new Matrix(lhs.RowNum, lhs.ColNum);
	        // 矩阵加法
            for (int i = 0; i < result.RowNum; ++i)
	        {
		        for (int j = 0 ; j <  result.ColNum; ++j)
			        result.SetElement(i, j, lhs.GetElement(i, j) + rhs.GetElement(i, j)) ;
	        }

	        return result ;
        }

        public static Matrix operator -(Matrix lhs, Matrix rhs)
        {
            if (lhs.ColNum != rhs.ColNum || lhs.RowNum != rhs.RowNum)
                return null;

            // 构造结果矩阵
            Matrix result = new Matrix(lhs.RowNum, lhs.ColNum);
            // 矩阵加法
            for (int i = 0; i < result.RowNum; ++i)
            {
                for (int j = 0; j < result.ColNum; ++j)
                    result.SetElement(i, j, lhs.GetElement(i, j) - rhs.GetElement(i, j));
            }

            return result;
        }

        public static Matrix operator *(Matrix lhs, double val)
        {
	        // 构造目标矩阵
            Matrix result = new Matrix(lhs);		// copy ourselves
	        // 进行数乘
	        for (int i = 0 ; i < lhs.RowNum ; ++i)
	        {
                for (int j = 0; j < lhs.ColNum; ++j)
                {
                    result.SetElement(i, j, result.GetElement(i, j) * val);
                }
	        }

	        return result ;
        }

        public static Matrix operator *(double val, Matrix rhs)
        {
            // 构造目标矩阵
            Matrix result = new Matrix(rhs);		// copy ourselves
            // 进行数乘
            for (int i = 0; i < rhs.RowNum; ++i)
            {
                for (int j = 0; j < rhs.ColNum; ++j)
                {
                    result.SetElement(i, j, result.GetElement(i, j) * val);
                }
            }

            return result;
        }

        //////////////////////////////////////////////////////////////////////
        // 复矩阵的乘法
        //
        // 参数：
        // 1. Matrix AR - 左边复矩阵的实部矩阵
        // 2. Matrix AI - 左边复矩阵的虚部矩阵
        // 3. Matrix BR - 右边复矩阵的实部矩阵
        // 4. Matrix BI - 右边复矩阵的虚部矩阵
        // 5. out Matrix CR - 乘积复矩阵的实部矩阵
        // 6. out Matrix CI - 乘积复矩阵的虚部矩阵
        //
        // 返回值：bool型，复矩阵乘法是否成功
        //////////////////////////////////////////////////////////////////////
	    public bool CMul(Matrix AR, Matrix AI, Matrix BR, Matrix BI, out Matrix CR, out Matrix CI)
        {
	        // 首先检查行列数是否符合要求
            if (AR.ColNum != AI.ColNum || AR.RowNum != AI.RowNum ||
                BR.ColNum != BI.ColNum || BR.RowNum != BI.RowNum ||
                AR.ColNum != BR.RowNum)
            {
                CR = null;
                CI = null;
                return false;
            }

	        // 构造乘积矩阵实部矩阵和虚部矩阵
            CR = new Matrix(AR.RowNum, BR.ColNum);
            CI = new Matrix(AR.RowNum, BR.ColNum);
	        // 复矩阵相乘
            for (int i = 0; i < AR.RowNum; ++i)
	        {
                for (int j = 0; j < BR.ColNum; ++j)
		        {
			        double vr = 0;
			        double vi = 0;
                    for (int k = 0; k < AR.ColNum; ++k)
			        {
                        double p = AR.GetElement(i, k) * BR.GetElement(k, j);
                        double q = AI.GetElement(i, k) * BI.GetElement(k, j);
                        double s = (AR.GetElement(i, k) + AI.GetElement(i, k)) * (BR.GetElement(k, j) + BI.GetElement(k, j));
                        vr += p - q;
                        vi += s - p - q;
			        }
                    CR.SetElement(i, j, vr);
                    CI.SetElement(i, j, vi);
                }
	        }

	        return true;
        }

	    // 矩阵的转置
	    public Matrix Transpose()
        {
	        // 构造目标矩阵
	        Matrix trans = new Matrix(ColNum, RowNum);

	        // 转置各元素
	        for (int i = 0 ; i < RowNum ; ++i)
	        {
		        for (int j = 0 ; j < ColNum ; ++j)
			        trans.SetElement(j, i, GetElement(i, j)) ;
	        }

	        return trans;
        }
    };




