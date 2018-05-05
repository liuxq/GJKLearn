/*
 * CREATED:     2018-5-5
 * PURPOSE:     GJK Raycast
 * AUTHOR:      Lxq
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UEEngine
{
    struct BoolV4
    {
        public bool x;
        public bool y;
        public bool z;
        public bool w;

        public bool BAllEq(bool v)
        {
            return x == v && y == v && z == v && w == v;
        }
    }

    struct Index
    {
        public int x;
        public int y;
        public int z;

        public void Reset()
        {
            x = 0;
            y = 1;
            z = 2;
        }
    }

    public class GJKRaycast
    {
        static Vector3 closestPtPointSegment(Vector3[] Q, ref int size)
        {
            Vector3 a = Q[0];
            Vector3 b = Q[1];

            //Test degenerated case
            Vector3 ab = (b - a);
            float denom = Vector3.Dot(ab, ab);
            Vector3 ap = -a;
            float nom = Vector3.Dot(ap, ab);
            bool con = FEps >= denom;
            //TODO - can we get rid of this branch? The problem is size, which isn't a vector!
            if (con)
            {
                size = 1;
                return Q[0];
            }

            /*  int count = BAllEq(con, true);
                size = 2 - count;*/

            float tValue = Mathf.Clamp01(nom / denom);
            return ab * tValue + a;
        }

        static Vector3 closestPtPointTriangleBaryCentric(Vector3 a, Vector3 b, Vector3 c, ref Index indices, ref int size)
        {
            size = 3;
            float eps = FEps;

            Vector3 ab = (b - a);
            Vector3 ac = (c - a);

            Vector3 n = Vector3.Cross(ab, ac);

            Vector3 bCrossC = Vector3.Cross(b, c);
            Vector3 cCrossA = Vector3.Cross(c, a);
            Vector3 aCrossB = Vector3.Cross(a, b);

            float va = Vector3.Dot(n, bCrossC);//edge region of BC, signed area rbc, u = S(rbc)/S(abc) for a
            float vb = Vector3.Dot(n, cCrossA);//edge region of AC, signed area rac, v = S(rca)/S(abc) for b
            float vc = Vector3.Dot(n, aCrossB);//edge region of AB, signed area rab, w = S(rab)/S(abc) for c

            bool isFacePoints = (va >= 0) && (vb >= 0) && (vc >= 0);

            //face region
            if (isFacePoints)
            {
                float nn = Vector3.Dot(n, n);
                float t = Vector3.Dot(n, a) / nn;
                return n * t;
            }

            Vector3 ap = -a;
            Vector3 bp = -b;
            Vector3 cp = -c;

            float d1 = Vector3.Dot(ab, ap); //  snom
            float d2 = Vector3.Dot(ac, ap); //  tnom
            float d3 = Vector3.Dot(ab, bp); // -sdenom
            float d4 = Vector3.Dot(ac, bp); //  unom = d4 - d3
            float d5 = Vector3.Dot(ab, cp); //  udenom = d5 - d6
            float d6 = Vector3.Dot(ac, cp); // -tdenom

            float unom = d4 - d3;
            float udenom = d5 - d6;

            size = 2;
            //check if p in edge region of AB
            bool con30 = (0 >= vc);
            bool con31 = (d1 >= 0);
            bool con32 = (0 >= d3);
            bool con3 = con30 && con31 && con32;//edge AB region
            if (con3)
            {
                float toRecipAB = d1 - d3;
                float recipAB = (Mathf.Abs(toRecipAB) > eps) ? 1 / toRecipAB : 0;
                float t = d1 * recipAB;
                return ab * t + a;
            }

            //check if p in edge region of BC
            bool con40 = (0 >= va);
            bool con41 = (d4 >= d3);
            bool con42 = (d5 >= d6);
            bool con4 = con40 && con41 && con42; //edge BC region
            if (con4)
            {
                Vector3 bc = c - b;
                float toRecipBC = unom + udenom;
                float recipBC = (Mathf.Abs(toRecipBC) > eps) ? (1 / toRecipBC) : 0;
                float t = unom * recipBC;
                indices.x = indices.y;
                indices.y = indices.z;
                return bc * t + b;
            }

            //check if p in edge region of AC
            bool con50 = (0 >= vb);
            bool con51 = (d2 >= 0);
            bool con52 = (0 >= d6);

            bool con5 = con50 && con51 && con52;//edge AC region
            if (con5)
            {
                float toRecipAC = d2 - d6;
                float recipAC = (Mathf.Abs(toRecipAC) > eps) ? (1 / toRecipAC) : 0;
                float t = d2 * recipAC;
                indices.y = indices.z;
                return ac * t + a;
            }

            size = 1;
            //check if p in vertex region outside a
            bool con00 = (0 >= d1); // snom <= 0
            bool con01 = (0 >= d2); // tnom <= 0
            bool con0 = con00 && con01; // vertex region a
            if (con0)
            {
                return a;
            }

            //check if p in vertex region outside b
            bool con10 = (d3 >= 0);
            bool con11 = (d3 >= d4);
            bool con1 = con10 && con11; // vertex region b
            if (con1)
            {
                indices.x = indices.y;
                return b;
            }

            //p is in vertex region outside c
            indices.x = indices.z;
            return c;

        }

        static Vector3 closestPtPointTriangle(Vector3[] Q, ref int size)
        {
            size = 3;

            float eps = FEps;
            Vector3 a = Q[0];
            Vector3 b = Q[1];
            Vector3 c = Q[2];
            Vector3 ab = (b - a);
            Vector3 ac = (c - a);
            Vector3 signArea = Vector3.Cross(ab, ac);//0.5*(abXac)
            float area = Vector3.Dot(signArea, signArea);
            if (eps >= area)
            {
                //degenerate
                size = 2;
                return closestPtPointSegment(Q, ref size);
            }

            int _size = 0;

            Index indices = new Index(); indices.Reset();

            Vector3 closest = closestPtPointTriangleBaryCentric(a, b, c, ref indices, ref _size);

            if (_size != 3)
            {
                Vector3 q0 = Q[indices.x]; Vector3 q1 = Q[indices.y];
                Q[0] = q0; Q[1] = q1;

                size = _size;
            }

            return closest;
        }

        static Vector3 getClosestPtPointTriangle(Vector3[] Q, BoolV4 bIsOutside4, ref Index indices, ref int size)
        {
            float bestSqDist = float.MaxValue;

            Index _indices = new Index(); _indices.Reset();

            Vector3 result = Vector3.zero;

            if (bIsOutside4.x)
            {
                //use the original indices, size, v and w
                result = closestPtPointTriangleBaryCentric(Q[0], Q[1], Q[2], ref indices, ref size);
                bestSqDist = Vector3.Dot(result, result);
            }

            if (bIsOutside4.y)
            {

                int _size = 3;
                _indices.x = 0; _indices.y = 2; _indices.z = 3;
                Vector3 q = closestPtPointTriangleBaryCentric(Q[0], Q[2], Q[3], ref _indices, ref _size);

                float sqDist = Vector3.Dot(q, q);
                bool con = bestSqDist > sqDist;
                if (con)
                {
                    result = q;
                    bestSqDist = sqDist;

                    indices.x = _indices.x;
                    indices.y = _indices.y;
                    indices.z = _indices.z;

                    size = _size;
                }
            }

            if (bIsOutside4.z)
            {
                int _size = 3;

                _indices.x = 0; _indices.y = 3; _indices.z = 1;

                Vector3 q = closestPtPointTriangleBaryCentric(Q[0], Q[3], Q[1], ref _indices, ref _size);
                float sqDist = Vector3.Dot(q, q);
                bool con = bestSqDist > sqDist;
                if (con)
                {
                    result = q;
                    bestSqDist = sqDist;

                    indices.x = _indices.x;
                    indices.y = _indices.y;
                    indices.z = _indices.z;

                    size = _size;
                }

            }

            if (bIsOutside4.w)
            {
                int _size = 3;
                _indices.x = 1; _indices.y = 3; _indices.z = 2;
                Vector3 q = closestPtPointTriangleBaryCentric(Q[1], Q[3], Q[2], ref _indices, ref _size);

                float sqDist = Vector3.Dot(q, q);
                bool con = bestSqDist > sqDist;

                if (con)
                {
                    result = q;
                    bestSqDist = sqDist;

                    indices.x = _indices.x;
                    indices.y = _indices.y;
                    indices.z = _indices.z;

                    size = _size;
                }
            }

            return result;
        }

        static BoolV4 PointOutsideOfPlane4(Vector3 _a, Vector3 _b, Vector3 _c, Vector3 _d)
        {
            // this is not 0 because of the following scenario:
            // All the points lie on the same plane and the plane goes through the origin (0,0,0).
            // On the Wii U, the math below has the problem that when point A gets projected on the
            // plane cumputed by A, B, C, the distance to the plane might not be 0 for the mentioned
            // scenario but a small positive or negative value. This can lead to the wrong boolean
            // results. Using a small negative value as threshold is more conservative but safer.
            float zero = -1e-6f;

            Vector3 ab = (_b - _a);
            Vector3 ac = (_c - _a);
            Vector3 ad = (_d - _a);
            Vector3 bd = (_d - _b);
            Vector3 bc = (_c - _b);

            Vector3 v0 = Vector3.Cross(ab, ac);
            Vector3 v1 = Vector3.Cross(ac, ad);
            Vector3 v2 = Vector3.Cross(ad, ab);
            Vector3 v3 = Vector3.Cross(bd, bc);

            float signa0 = Vector3.Dot(v0, _a);
            float signa1 = Vector3.Dot(v1, _a);
            float signa2 = Vector3.Dot(v2, _a);
            float signd3 = Vector3.Dot(v3, _a);

            float signd0 = Vector3.Dot(v0, _d);
            float signd1 = Vector3.Dot(v1, _b);
            float signd2 = Vector3.Dot(v2, _c);
            float signa3 = Vector3.Dot(v3, _b);

            BoolV4 ret;
            ret.x = signa0 * signd0 >= zero;
            ret.y = signa1 * signd1 >= zero;
            ret.z = signa2 * signd2 >= zero;
            ret.w = signa3 * signd3 >= zero;

            return ret;//same side, outside of the plane
        }

        static Vector3 closestPtPointTetrahedron(Vector3[] Q, ref int size)
        {
            float eps = (1e-4f);
            Vector3 a = Q[0];
            Vector3 b = Q[1];
            Vector3 c = Q[2];
            Vector3 d = Q[3];

            //degenerated
            Vector3 ab = (b - a);
            Vector3 ac = (c - a);
            Vector3 n = Vector3.Normalize(Vector3.Cross(ab, ac));
            float signDist = Vector3.Dot(n, (d - a));
            if (eps > Mathf.Abs(signDist))
            {
                size = 3;
                return closestPtPointTriangle(Q, ref  size);
            }

            BoolV4 bIsOutside4 = PointOutsideOfPlane4(a, b, c, d);

            if (bIsOutside4.BAllEq(false))
            {
                //All inside
                return Vector3.zero;
            }

            Index indices = new Index(); indices.Reset();
            Vector3 closest = getClosestPtPointTriangle(Q, bIsOutside4, ref indices, ref  size);

            Vector3 q0 = Q[indices.x]; Vector3 q1 = Q[indices.y]; Vector3 q2 = Q[indices.z];
            Q[0] = q0; Q[1] = q1; Q[2] = q2;

            return closest;
        }

        static Vector3 GJKCPairDoSimplex(Vector3[] Q, Vector3 support, ref int size)
        {
            //calculate a closest from origin to the simplex
            switch (size)
            {
                case 1:
                    {
                        return support;
                    }
                case 2:
                    {
                        return closestPtPointSegment(Q, ref size);
                    }
                case 3:
                    {
                        return closestPtPointTriangle(Q, ref size);
                    }
                case 4:
                    return closestPtPointTetrahedron(Q, ref size);
                //default:
                //PX_ASSERT(0);
            }
            return support;
        }

        static float FEps = 0.0001f;

        static Vector3[] Q = new Vector3[4]; //simplex set

        static public bool _gjkLocalRayCast(CAPSULE a, ConvexData b, Vector3 r, ref float lambda, ref Vector3 normal, ref bool StartSolid)
        {
            bool _StartSolid = true;
            float inflation = a.Radius;
            float maxDist = float.MaxValue;

            float _lambda = 0;

            r = -r;
            Vector3 x = r * _lambda;
            int size = 1;

            Vector3 dir = a.Center - b.GetAABB().Center;
            Vector3 _initialSearchDir = (Vector3.Dot(dir, dir) > FEps) ? dir : Vector3.right;
            Vector3 initialSearchDir = Vector3.Normalize(_initialSearchDir);

            Vector3 initialSupportA = a.supportSweepLocal(-initialSearchDir);
            Vector3 initialSupportB = b.supportSweepLocal(initialSearchDir);

            Q[0] = initialSupportA - initialSupportB; Q[1] = Vector3.zero; Q[2] = Vector3.zero; Q[3] = Vector3.zero; //simplex set

            Vector3 closest = Q[0];
            Vector3 supportA = initialSupportA;
            Vector3 supportB = initialSupportB;
            Vector3 support = Q[0];

            //float minMargin = Mathf.Min(a.getSweepMargin(), b.getSweepMargin());
            float eps1 = 0;//minMargin * 0.1f;
            float inflationPlusEps = eps1 + inflation;
            float eps2 = eps1 * eps1;

            float inflation2 = inflationPlusEps * inflationPlusEps;

            float sDist = Vector3.Dot(closest, closest);
            float minDist = sDist;

            bool bNotTerminated = sDist > eps2;
            bool bCon = true;

            Vector3 nor = closest;
            Vector3 prevClosest = closest;

            while (bNotTerminated == true)
            {
                minDist = sDist;
                prevClosest = closest;

                Vector3 vNorm = -Vector3.Normalize(closest);

                supportA = a.supportSweepLocal(vNorm);
                supportB = x + b.supportSweepLocal(-vNorm);

                //calculate the support point
                support = supportA - supportB;
                Vector3 w = -support;
                float vw = Vector3.Dot(vNorm, w) - inflationPlusEps;
                float vr = Vector3.Dot(vNorm, r);
                if (vw > 0)
                {
                    if (vr >= 0)
                    {
                        return false;
                    }
                    else
                    {
                        _StartSolid = false;
                        float _oldLambda = _lambda;
                        _lambda = _lambda - vw / vr;
                        if (_lambda > _oldLambda)
                        {
                            if (_lambda > 1)
                            {
                                return false;
                            }
                            Vector3 bPreCenter = x;
                            x = r * _lambda;

                            Vector3 offSet = x - bPreCenter;

                            Q[0] -= offSet;
                            Q[1] -= offSet;
                            Q[2] -= offSet;

                            supportB += offSet;
                            support = supportA - supportB;
                            minDist = maxDist;
                            nor = closest;
                            //size=0;
                        }
                    }
                }

                //ASSERT(size < 4); lxq test
                Q[size++] = support;

                //calculate the closest point between two convex hull
                closest = GJKCPairDoSimplex(Q, support, ref size);
                sDist = Vector3.Dot(closest, closest);

                bCon = minDist > sDist;
                bNotTerminated = (sDist > inflation2) && bCon;
            }

            //bool aQuadratic = a.isMarginEqRadius();
            //ML:if the Minkowski sum of two objects are too close to the original(eps2 > sDist), we can't take v because we will lose lots of precision. Therefore, we will take
            //previous configuration's normal which should give us a reasonable approximation. This effectively means that, when we do a sweep with inflation, we always keep v because
            //the shapes converge separated. If we do a sweep without inflation, we will usually use the previous configuration's normal.
            nor = ((sDist > eps2) && bCon) ? closest : nor;
            nor = Vector3.Normalize(nor);
            normal = nor;
            //lambda = (_lambda > 0) ? _lambda - 0.01f : _lambda;
            lambda = _lambda;

            float offset = 0.001f / Vector3.Dot(nor, r);
            lambda -= offset;
            if (lambda < 0) lambda = 0;

            StartSolid = false;
            if(_StartSolid)
            {
                GJKType ret = gjkLocalPenetration(a, b, ref normal, ref lambda);
                if (ret == GJKType.EPA_CONTACT)
                    StartSolid = true;
                else
                {
                    //UEEditTextFile file = new UEEditTextFile(System.Text.Encoding.Unicode);
                    //if (!file.OpenWrite("convex.txt", OPEN_MODE.OPEN_WRITE_CREATE))
                    //{
                    //    file.Close();
                    //}
                    //a.EditSave(file);
                    //b.EditSave(file);
                    //file.Close();
                    lambda -= 0.001f;
                    if (lambda > 0) lambda = 0;
                }
            }

            return true;
        }

        public enum GJKType
        {
            GJK_CONTACT,
            GJK_NON_INTERSECT,
            GJK_DEGENERATE,
            EPA_CONTACT,
        }

        static float GJK_RELATIVE_EPSILON = 0.0004f;//square of 2%.
        //ML: if we are using gjk local which means one of the object will be sphere/capsule, in that case, if we define takeCoreShape is true, we just need to return the closest point as the sphere center or a point in the capsule segment. This will increase the stability
        //for the manifold recycling code
        public static GJKType gjkLocalPenetration(CAPSULE a, ConvexData b, ref Vector3 normal, ref float penetrationDepth)
        {
            float marginA = a.getMargin();
            float marginB = 0;//b.getMargin();

            //float minMargin = 0;// Mathf.Min(a.getMinMargin(), b.getMinMargin());
            //float _eps2 = (minMargin * (0.1f));
            //ML: eps2 is the threshold that uses to detemine whether two (shrunk) shapes overlap. We calculate eps2 based on 10% of the minimum margin of two shapes
            float eps2 = 0;// (_eps2 * _eps2);
            //ML: epsRel2 is the square of 1.5%. This is used to scale the the sqaure distance of a closet point to origin to detemine whether two shrunk shapes overlap in the margin, but
            //they don't overlap.
            float epsRel2 = GJK_RELATIVE_EPSILON;

            float sumOrignalMargin = marginA + marginB;

            float sDist = float.MaxValue;
            float minDist = sDist;

            bool bNotTerminated = true;
            bool bCon = true;
            Vector3 closest;

            Vector3 supportA = Vector3.zero, supportB = Vector3.zero, support = Vector3.zero;

            int size = 0;

            Vector3 _initialSearchDir = a.Center - b.GetAABB().Center;
            closest = Vector3.Dot(_initialSearchDir, _initialSearchDir) > 0 ? _initialSearchDir : Vector3.right;

            Vector3 prevClosest = closest;

            // ML : termination condition
            //(1)two (shrunk)shapes overlap. GJK will terminate based on sq(v) < eps2 and indicate that two shapes are overlapping.
            //(2)two (shrunk + margin)shapes separate. If sq(vw) > sqMargin * sq(v), which means the original objects do not intesect, GJK terminate with GJK_NON_INTERSECT. 
            //(3)two (shrunk) shapes don't overlap. However, they interect within margin distance. if sq(v)- vw < epsRel2*sq(v), this means the shrunk shapes interect in the margin, 
            //   GJK terminate with GJK_CONTACT.
            while (bNotTerminated)
            {
                //minDist, tempClosA, tempClosB are used to store the previous iteration's closest points(in A and B space) and the square distance from the closest point
                //to origin in Mincowski space
                minDist = sDist;
                prevClosest = closest;

                supportA = a.supportSweepLocal(-closest);
                supportB = b.supportSweepLocal(closest);

                //calculate the support point
                support = supportA - supportB;
                Q[size] = support;

                //ML: because we shrink the shapes by plane shifting(box and convexhull), the distance from the "shrunk" vertices to the original vertices may be larger than contact distance. 
                //therefore, we need to take the largest of these 2 values into account so that we don't incorrectly declare shapes to be disjoint. If we don't do this, there is
                //an inherent inconsistency between fallback SAT tests and GJK tests that may result in popping due to SAT discovering deep penetrations that were not detected by
                //GJK operating on a shrunk shape.
                float sqMargin = (sumOrignalMargin * sumOrignalMargin);

                float vw = Vector3.Dot(closest, support);

                bool con = (vw > 0) && (vw * vw > sDist * sqMargin);//this is the non intersect condition
                bool conGrtr = (epsRel2 * sDist) >= (sDist - vw);//this is the margin intersect condition
                bool conOrconGrtr = con || conGrtr;

                if (conOrconGrtr)
                {
                    if (!con) //must be true otherwise we wouldn't be in here...
                    {
                        float dist = Mathf.Sqrt(sDist);
                        //PX_ASSERT(FAllGrtr(dist, FEps));
                        Vector3 n = closest / dist;//normalise
                        normal = n;
                        penetrationDepth = dist - sumOrignalMargin;

                        return GJKType.GJK_CONTACT;

                    }
                    else
                    {
                        return GJKType.GJK_NON_INTERSECT;
                    }
                }

                size++;
                //PX_ASSERT(size <= 4);

                //calculate the closest point between two convex hull
                closest = GJKCPairDoSimplex(Q, support, ref size);

                sDist = Vector3.Dot(closest, closest);

                bCon = minDist > sDist;
                bNotTerminated = sDist > eps2 && bCon;
            }

            if (!bCon)
            {
                sDist = minDist;

                float sqExpandedMargin = sumOrignalMargin * sumOrignalMargin;
                //Reset back to older closest point
                closest = prevClosest;//V3Sub(closA, closB);
                sDist = minDist;

                float dist = Mathf.Sqrt(sDist);
                //PX_ASSERT(dist > FEps);
                Vector3 n = closest / dist;//normalise

                penetrationDepth = dist - sumOrignalMargin;

                normal = n;
                if (sqExpandedMargin >= sDist)
                {
                    return GJKType.GJK_CONTACT;
                }

                //此时明明没有碰撞，但是误差导致的碰撞，向法线方向移动0.001f
                penetrationDepth = -0.001f;
                return GJKType.GJK_DEGENERATE;

            }
            else
            {
                //this two shapes are deeply intersected with each other, we need to use EPA algorithm to calculate MTD
                return GJKType.EPA_CONTACT;
            }
        }
    }
}

