/*
 * CREATED:     2014-12-31 14:43:43
 * PURPOSE:     Best fit algorithm
 * AUTHOR:      Wangrui
 */

// A code snippet to compute the best fit AAB, OBB, plane, capsule and sphere
// Quaternions are assumed a float X,Y,Z,W
// Matrices are assumed 4x4 D3DX style format passed as a float pointer
// The orientation of a capsule is assumed that height is along the Y axis, the same format as the PhysX SDK uses
// The best fit plane routine is derived from code previously published by David Eberly on his Magic Software site.
// The best fit OBB is computed by first approximating the best fit plane, and then brute force rotating the points
// around a single axis to derive the closest fit.  If you set 'bruteforce' to false, it will just use the orientation
// derived from the best fit plane, which is close enough in most cases, but not all.
// Each routine allows you to pass the point stride between position elements in your input vertex stream.
// These routines should all be thread safe as they make no use of any global variables.

/*!
**
** Copyright (c) 2009 by John W. Ratcliff mailto:jratcliffscarab@gmail.com
**
** The MIT license:
**
** Permission is hereby granted, FREE of charge, to any person obtaining a copy
** of this software and associated documentation files (the "Software"), to deal
** in the Software without restriction, including without limitation the rights
** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
** copies of the Software, and to permit persons to whom the Software is furnished
** to do so, subject to the following conditions:
**
** The above copyright notice and this permission notice shall be included in all
** copies or substantial portions of the Software.

** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
** IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
** FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
** AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
** WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
** CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

using UnityEngine;
using System;


    class BestFit
    {
        private const float FM_PI = 3.1415926535897932384626433832795028841971693993751f;
        private const float FM_DEG_TO_RAD = ((2.0f * FM_PI) / 360.0f);
        private const float FM_RAD_TO_DEG = (360.0f / (2.0f * FM_PI));


        private static void fm_identity(ref Matrix4x4 matrix) // set 4x4 matrix to identity.
        {
            matrix = Matrix4x4.identity;
        }

        private static void fm_matrixMultiply(Matrix4x4 a, Matrix4x4 b, ref Matrix4x4 ret)
        {
            ret = a * b;
        }

        private static void fm_matrixToQuat(Matrix4x4 matrix, ref Quaternion quat) // convert the 3x3 portion of a 4x4 matrix into a quaterion as x,y,z,w
        {
            Vector4 vy = matrix.GetColumn(1);
            Vector4 vz = matrix.GetColumn(2);
            quat = Quaternion.LookRotation(new Vector3(vz.x, vz.y, vz.z), new Vector3(vy.x, vy.y, vy.z));
        }

        private static void fm_getTranslation(Matrix4x4 matrix, ref Vector3 t)
        {
	        t[0] = matrix[3*4+0];
	        t[1] = matrix[3*4+1];
	        t[2] = matrix[3*4+2];
        }

        private static void fm_rotate(Matrix4x4 matrix, Vector3 v, ref Vector3 t) // rotate and translate this point
        {
            float tx = (matrix[0 * 4 + 0] * v[0]) + (matrix[1 * 4 + 0] * v[1]) + (matrix[2 * 4 + 0] * v[2]);
            float ty = (matrix[0 * 4 + 1] * v[0]) + (matrix[1 * 4 + 1] * v[1]) + (matrix[2 * 4 + 1] * v[2]);
            float tz = (matrix[0 * 4 + 2] * v[0]) + (matrix[1 * 4 + 2] * v[1]) + (matrix[2 * 4 + 2] * v[2]);
            t[0] = tx;
            t[1] = ty;
            t[2] = tz;
            //if (matrix != null)
            //{
            //    float tx = (matrix[0*4+0] * v[0]) +  (matrix[1*4+0] * v[1]) + (matrix[2*4+0] * v[2]);
            //    float ty = (matrix[0*4+1] * v[0]) +  (matrix[1*4+1] * v[1]) + (matrix[2*4+1] * v[2]);
            //    float tz = (matrix[0*4+2] * v[0]) +  (matrix[1*4+2] * v[1]) + (matrix[2*4+2] * v[2]);
            //    t[0] = tx;
            //    t[1] = ty;
            //    t[2] = tz;
            //}
            //else
            //{
            //    t[0] = v[0];
            //    t[1] = v[1];
            //    t[2] = v[2];
            //}
        }

        private static void fm_inverseRT(Matrix4x4 matrix, Vector3 pos, ref Vector3 t) // inverse rotate translate the point.
        {
	        float _x = pos[0] - matrix[3*4+0];
	        float _y = pos[1] - matrix[3*4+1];
	        float _z = pos[2] - matrix[3*4+2];

	        // Multiply inverse-translated source vector by inverted rotation transform
	        t[0] = (matrix[0*4+0] * _x) + (matrix[0*4+1] * _y) + (matrix[0*4+2] * _z);
	        t[1] = (matrix[1*4+0] * _x) + (matrix[1*4+1] * _y) + (matrix[1*4+2] * _z);
	        t[2] = (matrix[2*4+0] * _x) + (matrix[2*4+1] * _y) + (matrix[2*4+2] * _z);
        }

        private static void fm_setTranslation(Vector3 translation, ref Matrix4x4 matrix)
        {
            matrix[12] = translation[0];
            matrix[13] = translation[1];
            matrix[14] = translation[2];
        }

        private static void fm_transform(Matrix4x4 matrix, Vector3 v, ref Vector3 t) // rotate and translate this point
        {
            //if (matrix != null)
            {
                float tx = (matrix[0*4+0] * v[0]) +  (matrix[1*4+0] * v[1]) + (matrix[2*4+0] * v[2]) + matrix[3*4+0];
                float ty = (matrix[0*4+1] * v[0]) +  (matrix[1*4+1] * v[1]) + (matrix[2*4+1] * v[2]) + matrix[3*4+1];
                float tz = (matrix[0*4+2] * v[0]) +  (matrix[1*4+2] * v[1]) + (matrix[2*4+2] * v[2]) + matrix[3*4+2];
                t[0] = tx;
                t[1] = ty;
                t[2] = tz;
            }
            //else
            //{
            //    t[0] = v[0];
            //    t[1] = v[1];
            //    t[2] = v[2];
            //}
        }

        private static void fm_quatToMatrix(Quaternion quat, ref Matrix4x4 matrix) // convert quaterinion rotation to matrix, zeros out the translation component.
        {
            matrix.SetTRS(Vector3.zero, quat, new Vector3(1, 1, 1));
        }

        private static void fm_cross(Vector3 a, Vector3 b, ref Vector3 cross)
        {
            cross = Vector3.Cross(a, b);
        }

        private static float fm_dot(Vector3 p1, Vector3 p2)
        {
            return Vector3.Dot(p1, p2);
        }

        // Reference, from Stan Melax in Game Gems I
        //  Quaternion q;
        //  vector3 c = CrossProduct(v0,v1);
        //  float   d = DotProduct(v0,v1);
        //  float   s = (float)sqrt((1+d)*2);
        //  q.x = c.x / s;
        //  q.y = c.y / s;
        //  q.z = c.z / s;
        //  q.w = s /2.0f;
        //  return q;
        private static void fm_rotationArc(Vector3 v0, Vector3 v1, ref Quaternion quat)
        {
            Vector3 cross = Vector3.zero;
            fm_cross(v0, v1, ref cross);
            float d = fm_dot(v0, v1);
            float s = Mathf.Sqrt((1.0f + d) * 2.0f);
            float recip = 1.0f / s;
            quat[0] = cross[0] * recip;
            quat[1] = cross[1] * recip;
            quat[2] = cross[2] * recip;
            quat[3] = s * 0.5f;
        }

        private static void fm_planeToMatrix(Vector4 plane, ref Matrix4x4 matrix) // convert a plane equation to a 4x4 rotation matrix
        {
            Vector3 vec = Vector3.up;
            Quaternion quat = Quaternion.identity;
            Vector3 planevec = new Vector3(plane[0], plane[1], plane[2]);
            fm_rotationArc(vec, planevec, ref quat);
            fm_quatToMatrix(quat, ref matrix);
            Vector3 origin = new Vector3(0.0f, -plane[3], 0.0f);
            Vector3 center = Vector3.zero;
            fm_transform(matrix, origin, ref center);
            fm_setTranslation(center, ref matrix);
        }

        private static void fm_planeToQuat(Vector4 plane, ref Quaternion quat, ref Vector3 pos) // convert a plane equation to a quaternion and translation
        {
            Vector3 vec = Vector3.up;
            Matrix4x4 matrix = Matrix4x4.identity;
            fm_rotationArc(vec, plane, ref quat);
            fm_quatToMatrix(quat, ref matrix);
            Vector3 origin = new Vector3(0.0f, plane[3], 0.0f);
            fm_transform(matrix, origin, ref pos);
        }

        private static void fm_eulerToQuat(float roll, float pitch, float yaw, ref Quaternion quat) // convert euler angles to quaternion.
        {
	        roll  *= 0.5f;
	        pitch *= 0.5f;
	        yaw   *= 0.5f;

	        float cr = Mathf.Cos(roll);
	        float cp = Mathf.Cos(pitch);
	        float cy = Mathf.Cos(yaw);

	        float sr = Mathf.Sin(roll);
	        float sp = Mathf.Sin(pitch);
	        float sy = Mathf.Sin(yaw);

	        float cpcy = cp * cy;
	        float spsy = sp * sy;
	        float spcy = sp * cy;
	        float cpsy = cp * sy;

	        quat[0]   = ( sr * cpcy - cr * spsy);
	        quat[1]   = ( cr * spcy + sr * cpsy);
	        quat[2]   = ( cr * cpsy - sr * spcy);
	        quat[3]   = cr * cpcy + sr * spsy;
        }

        private static void  fm_eulerToQuat(Vector3 euler, ref Quaternion quat) // convert euler angles to quaternion.
        {
            fm_eulerToQuat(euler[0], euler[1], euler[2], ref quat);
        }


        private static void fm_eulerMatrix(float ax, float ay, float az, ref Matrix4x4 matrix) // convert euler (in radians) to a dest 4x4 matrix (translation set to zero)
        {
            Quaternion quat = Quaternion.identity;
            fm_eulerToQuat(ax, ay, az, ref quat);
            fm_quatToMatrix(quat, ref matrix);
        }

        private class Eigen
        {
            public float[,] Elements 
            {
                get { return mElement; }
            }

            public void DecrSortEigenStuff()
            {
                Tridiagonal(); //diagonalize the matrix.
                QLAlgorithm(); //
                DecreasingSort();
                GuaranteeRotation();
            }

            void Tridiagonal()
            {
                float fM00 = mElement[0,0];
                float fM01 = mElement[0,1];
                float fM02 = mElement[0,2];
                float fM11 = mElement[1,1];
                float fM12 = mElement[1,2];
                float fM22 = mElement[2,2];

                mDiag[0] = fM00;
                mSubd[2] = 0.0f;
                if (fM02 != 0.0f)
                {
                    float length = Mathf.Sqrt(fM01*fM01 + fM02*fM02);
                    float invLength = 1.0f / length;
                    fM01 *= invLength;
                    fM02 *= invLength;
                    float fQ = 2.0f*fM01*fM12 + fM02*(fM22-fM11);
                    mDiag[1] = fM11+fM02*fQ;
                    mDiag[2] = fM22-fM02*fQ;
                    mSubd[0] = length;
                    mSubd[1] = fM12-fM01*fQ;
                    mElement[0,0] = (float)1.0;
                    mElement[0,1] = (float)0.0;
                    mElement[0,2] = (float)0.0;
                    mElement[1,0] = (float)0.0;
                    mElement[1,1] = fM01;
                    mElement[1,2] = fM02;
                    mElement[2,0] = (float)0.0;
                    mElement[2,1] = fM02;
                    mElement[2,2] = -fM01;
                    mIsRotation = false;
                }
                else
                {
                    mDiag[1] = fM11;
                    mDiag[2] = fM22;
                    mSubd[0] = fM01;
                    mSubd[1] = fM12;
                    mElement[0,0] = (float)1.0;
                    mElement[0,1] = (float)0.0;
                    mElement[0,2] = (float)0.0;
                    mElement[1,0] = (float)0.0;
                    mElement[1,1] = (float)1.0;
                    mElement[1,2] = (float)0.0;
                    mElement[2,0] = (float)0.0;
                    mElement[2,1] = (float)0.0;
                    mElement[2,2] = (float)1.0;
                    mIsRotation = true;
                }
            }

            bool QLAlgorithm()
            {
                const int iMaxIter = 32;
                for (int i0 = 0; i0 < 3; i0++)
                {
                    int i1;
                    for (i1 = 0; i1 < iMaxIter; i1++)
                    {
                        int i2;
                        for (i2 = i0; i2 <= (3-2); i2++)
                        {
                            float fTmp = Mathf.Abs(mDiag[i2]) + Mathf.Abs(mDiag[i2+1]);
                            if (Mathf.Abs(mSubd[i2]) + fTmp == fTmp)
                                break;
                        }
                        if (i2 == i0)
                        {
                            break;
                        }

                        float fG = (mDiag[i0+1] - mDiag[i0]) / ((2.0f) * mSubd[i0]);
                        float fR = Mathf.Sqrt(fG*fG + 1.0f);
                        if (fG < 0.0f)
                        {
                            fG = mDiag[i2] - mDiag[i0] + mSubd[i0]/(fG-fR);
                        }
                        else
                        {
                            fG = mDiag[i2] - mDiag[i0] + mSubd[i0]/(fG+fR);
                        }

                        float fSin = 1.0f;
                        float fCos = 1.0f; 
                        float fP = 0.0f;

                        for (int i3 = i2-1; i3 >= i0; i3--)
                        {
                            float fF = fSin*mSubd[i3];
                            float fB = fCos*mSubd[i3];

                            if (Mathf.Abs(fF) >= Mathf.Abs(fG))
                            {
                                fCos = fG/fF;
                                fR = Mathf.Sqrt(fCos*fCos + 1.0f);
                                mSubd[i3+1] = fF*fR;
                                fSin = 1.0f/fR;
                                fCos *= fSin;
                            }
                            else
                            {
                                fSin = fF/fG;
                                fR = Mathf.Sqrt(fSin*fSin+(float)1.0);
                                mSubd[i3+1] = fG*fR;
                                fCos = 1.0f/fR;
                                fSin *= fCos;
                            }

                            fG = mDiag[i3+1]-fP;
                            fR = (mDiag[i3]-fG)*fSin+((float)2.0)*fB*fCos;
                            fP = fSin*fR;
                            mDiag[i3+1] = fG+fP;
                            fG = fCos*fR-fB;
                            for (int i4 = 0; i4 < 3; i4++)
                            {
                                fF = mElement[i4,i3+1];
                                mElement[i4,i3+1] = fSin*mElement[i4,i3]+fCos*fF;
                                mElement[i4,i3] = fCos*mElement[i4,i3]-fSin*fF;
                            }
                        }
                        mDiag[i0] -= fP;
                        mSubd[i0] = fG;
                        mSubd[i2] = 0.0f;
                    }
                    if (i1 == iMaxIter)
                    {
                        return false;
                    }
                }

                return true;
            }

            void DecreasingSort()
            {
                //sort eigenvalues in decreasing order, e[0] >= ... >= e[iSize-1]
                for (int i0 = 0, i1; i0 <= 3-2; i0++)
                {
                    // locate maximum eigenvalue
                    i1 = i0;
                    float fMax = mDiag[i1];
                    int i2;
                    for (i2 = i0+1; i2 < 3; i2++)
                    {
                        if (mDiag[i2] > fMax)
                        {
                            i1 = i2;
                            fMax = mDiag[i1];
                        }
                    }

                    if (i1 != i0)
                    {
                        // swap eigenvalues
                        mDiag[i1] = mDiag[i0];
                        mDiag[i0] = fMax;
                        // swap eigenvectors
                        for (i2 = 0; i2 < 3; i2++)
                        {
                            float fTmp = mElement[i2,i0];
                            mElement[i2,i0] = mElement[i2,i1];
                            mElement[i2,i1] = fTmp;
                            mIsRotation = !mIsRotation;
                        }
                    }
                }
            }


            void GuaranteeRotation()
            {
                if (!mIsRotation)
                {
                    // change sign on the first column
                    for (int iRow = 0; iRow <3; iRow++)
                    {
                        mElement[iRow,0] = -mElement[iRow,0];
                    }
                }
            }

            private float[,] mElement = new float[3, 3];
            private float[] mDiag = new float[3];
            private float[] mSubd = new float[3];
            private bool mIsRotation;
        };


        public static bool ComputeBestFitPlane(Vector3[] points, float[] weights, ref Vector4 plane)
        {
            if (points == null || points.Length <= 0)
                return false;

            if (weights != null && weights.Length != points.Length)
                return false;

            Vector3 origin = Vector3.zero;
            float wtotal = 0;
            for (int i = 0; i < points.Length; i++)
            {
                float w = (weights != null) ? weights[i] : 1.0f;
                origin += w * points[i];
                wtotal += w;
            }
            float recip = 1.0f / wtotal; // reciprocol of total weighting
            origin = recip * origin;

            float sumXX = 0.0f;
            float sumXY = 0.0f;
            float sumXZ = 0.0f;
            float sumYY = 0.0f;
            float sumYZ = 0.0f;
            float sumZZ = 0.0f;

            for (int i = 0; i < points.Length; i++)
            {
                float w = (weights != null) ? weights[i] : 1.0f;
                origin += w * points[i];

                Vector3 diff = w * (points[i] - origin);
                
                sumXX += diff[0] * diff[0]; // sume of the squares of the differences.
                sumXY += diff[0] * diff[1]; // sume of the squares of the differences.
                sumXZ += diff[0] * diff[2]; // sume of the squares of the differences.

                sumYY += diff[1] * diff[1];
                sumYZ += diff[1] * diff[2];
                sumZZ += diff[2] * diff[2];
            }

            sumXX *= recip;
            sumXY *= recip;
            sumXZ *= recip;
            sumYY *= recip;
            sumYZ *= recip;
            sumZZ *= recip;

            // setup the eigensolver
            Eigen ES = new Eigen();

            ES.Elements[0,0] = sumXX;
            ES.Elements[0,1] = sumXY;
            ES.Elements[0,2] = sumXZ;

            ES.Elements[1,0] = sumXY;
            ES.Elements[1,1] = sumYY;
            ES.Elements[1,2] = sumYZ;

            ES.Elements[2,0] = sumXZ;
            ES.Elements[2,1] = sumYZ;
            ES.Elements[2,2] = sumZZ;

            // compute eigenstuff, smallest eigenvalue is in last position
            ES.DecrSortEigenStuff();

            Vector3 normal = new Vector3(ES.Elements[0,2], ES.Elements[1,2], ES.Elements[2,2]);

            // the minimum energy
            plane[0] = normal[0];
            plane[1] = normal[1];
            plane[2] = normal[2];
            plane[3] = 0.0f - fm_dot(normal, origin);

            return true;
        }

        /*

        // computes the OBB for this set of points relative to this transform matrix.
        void computeOBB(size_t vcount,const float *points,size_t pstride,float *sides,float *matrix)
        {
          const char *src = (const char *) points;

          float bmin[3] = { 1e9, 1e9, 1e9 };
          float bmax[3] = { -1e9, -1e9, -1e9 };

          for (size_t i=0; i<vcount; i++)
          {
            const float *p = (const float *) src;
            float t[3];

            fm_inverseRT(matrix, p, t ); // inverse rotate translate

            if ( t[0] < bmin[0] ) bmin[0] = t[0];
            if ( t[1] < bmin[1] ) bmin[1] = t[1];
            if ( t[2] < bmin[2] ) bmin[2] = t[2];

            if ( t[0] > bmax[0] ) bmax[0] = t[0];
            if ( t[1] > bmax[1] ) bmax[1] = t[1];
            if ( t[2] > bmax[2] ) bmax[2] = t[2];

            src+=pstride;
          }

          float center[3];

          sides[0] = bmax[0]-bmin[0];
          sides[1] = bmax[1]-bmin[1];
          sides[2] = bmax[2]-bmin[2];

          center[0] = sides[0]*0.5f+bmin[0];
          center[1] = sides[1]*0.5f+bmin[1];
          center[2] = sides[2]*0.5f+bmin[2];

          float ocenter[3];

          fm_rotate(matrix,center,ocenter);

          matrix[12]+=ocenter[0];
          matrix[13]+=ocenter[1];
          matrix[14]+=ocenter[2];

        }



        void computeBestFitOBB(size_t vcount,const float *points,size_t pstride,float *sides,float *matrix,bool bruteForce)
        {
          fm_identity(matrix);
          float bmin[3];
          float bmax[3];
          computeBestFitAABB(vcount,points,pstride,bmin,bmax);

          float avolume = (bmax[0]-bmin[0])*(bmax[1]-bmin[1])*(bmax[2]-bmin[2]);

          float plane[4];
          computeBestFitPlane(vcount,points,pstride,0,0,plane);
          fm_planeToMatrix(plane,matrix);
          computeOBB( vcount, points, pstride, sides, matrix );

          float refmatrix[16];
          memcpy(refmatrix,matrix,16*sizeof(float));

          float volume = sides[0]*sides[1]*sides[2];
          if ( bruteForce )
          {
            for (float a=10; a<180; a+=10)
            {
              float quat[4];
              fm_eulerToQuat(0,a*FM_DEG_TO_RAD,0,quat);
              float temp[16];
              float pmatrix[16];
              fm_quatToMatrix(quat,temp);
              fm_matrixMultiply(temp,refmatrix,pmatrix);
              float psides[3];
              computeOBB( vcount, points, pstride, psides, pmatrix );
              float v = psides[0]*psides[1]*psides[2];
              if ( v < volume )
              {
                volume = v;
                memcpy(matrix,pmatrix,sizeof(float)*16);
                sides[0] = psides[0];
                sides[1] = psides[1];
                sides[2] = psides[2];
              }
            }
          }
          if ( avolume < volume )
          {
            fm_identity(matrix);
            matrix[12] = (bmin[0]+bmax[0])*0.5f;
            matrix[13] = (bmin[1]+bmax[1])*0.5f;
            matrix[14] = (bmin[2]+bmax[2])*0.5f;
            sides[0] = bmax[0]-bmin[0];
            sides[1] = bmax[1]-bmin[1];
            sides[2] = bmax[2]-bmin[2];
          }
        }

        void computeBestFitOBB(size_t vcount,const float *points,size_t pstride,float *sides,float *pos,float *quat,bool bruteForce)
        {
          float matrix[16];
          computeBestFitOBB(vcount,points,pstride,sides,matrix,bruteForce);
          fm_getTranslation(matrix,pos);
          fm_matrixToQuat(matrix,quat);
        }

        void computeBestFitABB(size_t vcount,const float *points,size_t pstride,float *sides,float *pos)
        {
	        float bmin[3];
	        float bmax[3];

          bmin[0] = points[0];
          bmin[1] = points[1];
          bmin[2] = points[2];

          bmax[0] = points[0];
          bmax[1] = points[1];
          bmax[2] = points[2];

	        const char *cp = (const char *) points;
	        for (size_t i=0; i<vcount; i++)
	        {
		        const float *p = (const float *) cp;

		        if ( p[0] < bmin[0] ) bmin[0] = p[0];
		        if ( p[1] < bmin[1] ) bmin[1] = p[1];
		        if ( p[2] < bmin[2] ) bmin[2] = p[2];

            if ( p[0] > bmax[0] ) bmax[0] = p[0];
            if ( p[1] > bmax[1] ) bmax[1] = p[1];
            if ( p[2] > bmax[2] ) bmax[2] = p[2];

            cp+=pstride;
	        }


	        sides[0] = bmax[0] - bmin[0];
	        sides[1] = bmax[1] - bmin[1];
	        sides[2] = bmax[2] - bmin[2];

	        pos[0] = bmin[0]+sides[0]*0.5f;
	        pos[1] = bmin[1]+sides[1]*0.5f;
	        pos[2] = bmin[2]+sides[2]*0.5f;

        }


        float  computeBestFitSphere(size_t vcount,const float *points,size_t pstride,float *center)
        {
          float sides[3];
          float omatrix[16];
          computeBestFitOBB(vcount,points,pstride,sides,omatrix,true);
          center[0] = omatrix[12];
          center[1] = omatrix[13];
          center[2] = omatrix[14];
          float radius = sqrt( sides[0]*sides[0] + sides[1]*sides[1] + sides[2]*sides[2] );
          return radius*0.5f;
        }

        void computeBestFitCapsule(size_t vcount,const float *points,size_t pstride,float &radius,float &height,float matrix[16],bool bruteForce)
        {
          float sides[3];
          float omatrix[16];
          computeBestFitOBB(vcount,points,pstride,sides,omatrix,bruteForce);

          int axis = 0;
          if ( sides[0] > sides[1] && sides[0] > sides[2] )
            axis = 0;
          else if ( sides[1] > sides[0] && sides[1] > sides[2] )
            axis = 1;
          else
            axis = 2;

          float localTransform[16];

          float maxDist = 0;
          float maxLen = 0;

          switch ( axis )
          {
            case 0:
              {
                fm_eulerMatrix(0,0,FM_PI/2,localTransform);
                fm_matrixMultiply(localTransform,omatrix,matrix);

                const unsigned char *scan = (const unsigned char *)points;
                for (size_t i=0; i<vcount; i++)
                {
                  const float *p = (const float *)scan;
                  float t[3];
                  fm_inverseRT(omatrix,p,t);
                  float dist = t[1]*t[1]+t[2]*t[2];
                  if ( dist > maxDist )
                  {
                    maxDist = dist;
                  }
                  float l = (float) Mathf.Abs(t[0]);
                  if ( l > maxLen )
                  {
                    maxLen = l;
                  }
                  scan+=pstride;
                }
              }
              height = sides[0];
              break;
            case 1:
              {
                fm_eulerMatrix(0,FM_PI/2,0,localTransform);
                fm_matrixMultiply(localTransform,omatrix,matrix);

                const unsigned char *scan = (const unsigned char *)points;
                for (size_t i=0; i<vcount; i++)
                {
                  const float *p = (const float *)scan;
                  float t[3];
                  fm_inverseRT(omatrix,p,t);
                  float dist = t[0]*t[0]+t[2]*t[2];
                  if ( dist > maxDist )
                  {
                    maxDist = dist;
                  }
                  float l = (float) Mathf.Abs(t[1]);
                  if ( l > maxLen )
                  {
                    maxLen = l;
                  }
                  scan+=pstride;
                }
              }
              height = sides[1];
              break;
            case 2:
              {
                fm_eulerMatrix(FM_PI/2,0,0,localTransform);
                fm_matrixMultiply(localTransform,omatrix,matrix);

                const unsigned char *scan = (const unsigned char *)points;
                for (size_t i=0; i<vcount; i++)
                {
                  const float *p = (const float *)scan;
                  float t[3];
                  fm_inverseRT(omatrix,p,t);
                  float dist = t[0]*t[0]+t[1]*t[1];
                  if ( dist > maxDist )
                  {
                    maxDist = dist;
                  }
                  float l = (float) Mathf.Abs(t[2]);
                  if ( l > maxLen )
                  {
                    maxLen = l;
                  }
                  scan+=pstride;
                }
              }
              height = sides[2];
              break;
          }
          radius = (float)sqrt(maxDist);
          height = (maxLen*2)-(radius*2);
        }

        float computeBestFitAABB(size_t vcount,const float *points,size_t pstride,float *bmin,float *bmax) // returns the diagonal distance
        {

          const unsigned char *source = (const unsigned char *) points;

	        bmin[0] = points[0];
	        bmin[1] = points[1];
	        bmin[2] = points[2];

	        bmax[0] = points[0];
	        bmax[1] = points[1];
	        bmax[2] = points[2];


          for (size_t i=1; i<vcount; i++)
          {
  	        source+=pstride;
  	        const float *p = (const float *) source;

  	        if ( p[0] < bmin[0] ) bmin[0] = p[0];
  	        if ( p[1] < bmin[1] ) bmin[1] = p[1];
  	        if ( p[2] < bmin[2] ) bmin[2] = p[2];

		        if ( p[0] > bmax[0] ) bmax[0] = p[0];
		        if ( p[1] > bmax[1] ) bmax[1] = p[1];
		        if ( p[2] > bmax[2] ) bmax[2] = p[2];

          }

          float dx = bmax[0] - bmin[0];
          float dy = bmax[1] - bmin[1];
          float dz = bmax[2] - bmin[2];

	        return (float) sqrt( dx*dx + dy*dy + dz*dz );

        }
        
         
        */


    }; // end of namespace

