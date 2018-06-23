/*
 * CREATED:     2015-1-29 16:42:42
 * PURPOSE:     all kinds of collision
 * AUTHOR:      Wangrui
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UEEngine;

public class TRACE_PURPOSE
{
    public const uint PURPOSE_PICK = 0;
    public const uint PURPOSE_MOVE = 1;
    public const uint ONLY_TERRAIN = 2;
}

public class HIT_ENVTYPE
{
    public const uint HIT_NULL = 0;
    public const uint HIT_HMAP = 1;
    public const uint HIT_BRUSH = 2;
}

public class CKTRACE
{
    public const uint CKTRACE_TERRAIN = 0x0001;
    public const uint CKTRACE_SCENEOBJ = 0x0002;
    public const uint CKTRACE_SCENE = 0x0003;
    public const uint CKTRACE_NPC = 0x0004;
    public const uint CKTRACE_PLAYER = 0x0008;
    public const uint CKTRACE_CHARACTEROBJECT = 0x0010;
}

// Move collision detection resoult
public class MoveCDR
{
    public Vector3 Center;      // Capsule center
    public float HalfLen;
    public float Radius;
    public Vector3 VelDirH;     // wish horizontal dir
    public Vector3 ClipVel;     // velolcity after clipping
    public Vector3 ActualVel;   // absolute velocity, output for forcast
    public Vector3 TPNormal;    // trace plane normal
    public float MoveDist;      // move distance
    public float Speed;         // move speed
    public float TimeSec;       // move time in second
    public float Gravity;       // gravity acceleration
    public float SlopeThresh;   // Slop thread to stay
    public float StepHeight;    // move step height
    public bool Blocked;        // if this move is blocked
    public bool OnSurface;      // if player is on surface
    public bool CanStay;        // if player can stay
    public long PlayerID;       // for player collision

    public MoveCDR()
    {
        Gravity = UECollision.GRAVITY;
        SlopeThresh = UECollision.STAY_SLOP_MIN_Y;
        StepHeight = UECollision.MOVE_STEP_HEIGHT;
    }

    public void ResetInfo(bool clearspeed)
    {
        if (clearspeed)
            ClearSpeed();

        TPNormal.Set(0.0f, 0.0f, 0.0f);
        Blocked = false;
        OnSurface = false;
        CanStay = false;
    }

    public void ClearSpeed()
    {
        ClipVel.Set(0.0f, 0.0f, 0.0f);
        ActualVel.Set(0.0f, 0.0f, 0.0f);
    }
}

public class ObjectMoveCD
{
    public Vector3 Pos;
    public Vector3 Dir;
    public float Velocity;
    public float TimeSec;
    public bool TraceGnd;
    public Vector3 Normal;	    //meaningful when trace ground
}

public class EnvTraceInfo
{
    public Vector3 Start;       //brush start
    public float HalfLen;
    public float Radius;
    public Vector3 Delta;
    public Vector3 TerStart;    //terrain start
    public uint CheckFlag;      //check brush flag
    public float Fraction;
    public Vector3 HitNormal;
    public bool StartSolid;     //start in solid
    public uint ClsFlag;        //collision flag
    public uint HitEnv;         //hit enviroment type
    public long PlayerID;
};

public class MoveTraceInfo
{
    public Vector3 Start;
    public float HalfLen;
    public float Radius;
    public Vector3 Velocity;
    public float TimeSec;
    public float Slope;
    public Vector3 TPNormal;
    public Vector3 WishDir;
    public float WishSpd;
    public Vector3 AbsVelocity;
    public float MaxFallSpd;
    public float Accel;
    public Vector3 End;
    public float Gravity;
    public float StepHeight;
    public long PlayerID;
};

public class GroundTraceInfo
{
    public Vector3 Start;
    public float HalfLen;
    public float Radius;
    public float DeltaY;      //down (-y)
    public Vector3 End;
    public Vector3 HitNormal;
    public bool Support;      //false if ground missed
};

public class UECollision
{
    public const float GRAVITY = 9.81f;
    public const float TRACE_STEP_HEIGHT = 3.0f;
    public const float MOVE_STEP_HEIGHT = 0.3f;
    public const float STAY_SLOP_MIN_Y = 0.707f;
    public const float AIR_MAX_SPEED = 4.0f;
    public const float MOVE_ACCELERATION = 6.0f;
    public const float MAX_FALL_SPEED = -12.0f;
    public const float CLIMB_SPEED_RATIO = 0.8f / AIR_MAX_SPEED;
    public const float MAX_JUMP_COUNT = 1;
    public const float STAND_JUMP_SPEED = 1.0f;
    public const float JUMP_HEIGHT = 1.2f;

    public static readonly float DTP_LIMIT = Mathf.Cos(UEMathUtil.DegreeToRadian(105));

    public const float DIST_EPSILON = 1e-4f;
    public const float SQR_DIST_EPSILON = 1e-8f;
    public const float VELOCITY_EPSILON = 1e-4f;
    public const float NORMAL_EPSILON = 1e-2f;
    public const float FRACTION_EPSILON = 1e-3f;
    public const int MAX_TRY_MOVE = 4;
    public const float CAMERA_SIZE = .2f;

    public static bool MoveLog = false;

    private static UERayTrace mRayTraceInfo = new UERayTrace();
    private static EnvTraceInfo mEnvTraceInfo = new EnvTraceInfo();
    private static GroundTraceInfo mGndTraceInfo = new GroundTraceInfo();
    private static BrushTraceInfo mBrushTraceInfo = new BrushTraceInfo();
    private static MoveTraceInfo mMoveTraceInfo = new MoveTraceInfo();
    private static CapsuleTraceBrushInfo mCapsuleTraceBrushInfo = new CapsuleTraceBrushInfo();

    private static Vector3[] mFaceVertLst = null;
    private static List<int> mFaceIdxLst = new List<int>();
    private static Vector3[] mFaceVert = new Vector3[3];
    private static Vector3 mCamExt = new Vector3(CAMERA_SIZE, CAMERA_SIZE, CAMERA_SIZE);
    private static Vector3[] mSlideMoveNormals = new Vector3[MAX_TRY_MOVE + 1];

    public static float GetJumpStartSpeed()
    {
        return Mathf.Sqrt(2.0f * GRAVITY * JUMP_HEIGHT);
    }

    private static bool RetrieveSupportPlane(GroundTraceInfo gtrc)
    {
        mEnvTraceInfo.HalfLen = gtrc.HalfLen;
        mEnvTraceInfo.Radius = gtrc.Radius;
        mEnvTraceInfo.Start = gtrc.Start;
        mEnvTraceInfo.Delta.Set(0.0f, -(gtrc.DeltaY), 0.0f);
        mEnvTraceInfo.TerStart = gtrc.Start;
        mEnvTraceInfo.TerStart.y -= mEnvTraceInfo.Radius + mEnvTraceInfo.HalfLen; //foot position
        mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;

        bool collide = CollideWithEnv(mEnvTraceInfo, true);
        //if (MoveLog)
        //{
        //    UELogMan.LogMsg("RetrieveSupportPlane " + " collide: " + collide +
        //        " start:" + mEnvTraceInfo.Start.ToString("G") +
        //        " Delta:" + mEnvTraceInfo.Delta.ToString("G") +
        //        " Fraction:" + mEnvTraceInfo.Fraction.ToString("G"));
        //}

        if (mEnvTraceInfo.StartSolid)
            return false;

        gtrc.Support = false;
        if (collide)
        {
            gtrc.Support = true;
            gtrc.HitNormal = mEnvTraceInfo.HitNormal;
            if (mEnvTraceInfo.Fraction < 0)
                gtrc.End = mEnvTraceInfo.Start - mEnvTraceInfo.HitNormal * mEnvTraceInfo.Fraction;
            else
                gtrc.End = mEnvTraceInfo.Start + mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;

        }
        return true;
    }

    private static void Accelerate(Vector3 wishdir, float wishspeed, float accel, float t, ref Vector3 vel)
    {
        float addspeed = 0.0f;
        float accelspeed = 0.0f;
        float currentspeed = 0.0f;

        // See if we are changing direction a bit
		//currentspeed = Mathf.Lerp(Vector3.Dot(vel, wishdir), vel.magnitude, 0.3f);
        currentspeed = UEMathUtil.Normalize(ref vel);
        wishspeed *= Mathf.Clamp01(Vector3.Dot(vel, wishdir)) * 0.8f + 0.2f;

        // Reduce wishspeed by the amount of veer.
        addspeed = wishspeed - currentspeed;

        // If not going to add any speed, done.
        if (addspeed <= 0)
        {
            vel = wishdir * wishspeed;
            return;
        }

        // Determine amount of acceleration.
        accelspeed = accel * t;

        if (accelspeed > addspeed)
            accelspeed = addspeed;

        // Adjust velocity.
        vel = wishdir * (currentspeed + accelspeed);
    }

    private static void ClipVelocity(Vector3 velin, Vector3 normal, float bounce, float wishspeedh, ref Vector3 velout)
    {
        float DTP_EPSILON = 0.001f;
        float backoff = 0.0f;
        UEMathUtil.Clamp(ref bounce, 1.0f, 1.5f);
        Vector3 indir = velin;

        float inspd = UEMathUtil.Normalize(ref indir);
        float dtp = Vector3.Dot(indir, normal);
        if (dtp > -DTP_EPSILON && dtp < DTP_EPSILON)
        {
            velout = velin;
            return;
        }

        velout = velin - normal * dtp * inspd;

        Vector3 ori = velout.normalized;
        float xzLengh = Mathf.Sqrt(ori.x * ori.x + ori.z * ori.z);
        if (xzLengh < 0.001f)
            xzLengh = 0.001f;
        velout = ori * inspd / xzLengh;

        backoff = Mathf.Abs(dtp) * (bounce - 1.0f) * inspd;
        UEMathUtil.ClampFloor(ref backoff, DTP_EPSILON);
        velout += backoff * normal;

    }

    private static void TrySlideMove(MoveTraceInfo mv, bool error = false)
    {
        Vector3 orivel = Vector3.zero;
        Vector3 curvel = Vector3.zero;
        Vector3 newvel = Vector3.zero;
        Vector3 oripos = Vector3.zero;
        Vector3 delta = Vector3.zero;
        float timeleft = 0.0f;
        float allfraction = 0.0f;
        int numplanes = 0;

        mEnvTraceInfo.HalfLen = mv.HalfLen;
        mEnvTraceInfo.Radius = mv.Radius;
        orivel = mv.Velocity;
        curvel = mv.Velocity;
        oripos = mv.Start;

        timeleft = mv.TimeSec;

        if (MoveLog)
        {
            Vector3 origveln = orivel;
            origveln.Normalize();
            UELogMan.LogMsg("TrySlideMove " +
                " OriVel:" + orivel.ToString("G") +
                " OriVelDir:" + origveln.ToString("G"));
        }

        for (int tryidx = 0; tryidx < MAX_TRY_MOVE; ++tryidx)
        {
            mv.End = mv.Start;
            delta = mv.Velocity * timeleft;

            if (MoveLog)
            {
                UELogMan.LogMsg("Try " + tryidx.ToString() +
                    " start:" + mv.Start.ToString("G") +
                    " delta:" + delta.ToString("G"));
            }

            if (delta.sqrMagnitude < SQR_DIST_EPSILON)
            {
                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " small delta break");
                }

                break;
            }

            mEnvTraceInfo.Start = mv.Start;
            mEnvTraceInfo.Delta = delta;
            mEnvTraceInfo.TerStart = mv.Start;
            mEnvTraceInfo.TerStart.y -= mv.Radius + mv.HalfLen; //foot
            mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;

            bool collide = CollideWithEnv(mEnvTraceInfo);
            if (mEnvTraceInfo.StartSolid)
            {
                // If we started in a solid object, or in solid spac the whole way, zero out our velocity 
                mv.End = oripos;
                mv.Velocity = Vector3.zero;
                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " start solid return");
                }
                return;
            }

		    allfraction += mEnvTraceInfo.Fraction;
		    timeleft -= timeleft * mEnvTraceInfo.Fraction;
            if(mEnvTraceInfo.Fraction < 0)
                mv.Start -= mEnvTraceInfo.HitNormal * mEnvTraceInfo.Fraction;
            else
                mv.Start += mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;
		    mv.End = mv.Start;

            if (MoveLog)
            {
                Vector3 delpos = mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;
                Vector3 deldir = delpos;
                float deldis = UEMathUtil.Normalize(ref deldir);
                UELogMan.LogMsg("Try " + tryidx.ToString() + " result" +
                    " end:" + mv.End.ToString("G") +
                    " fraction:" + mEnvTraceInfo.Fraction.ToString("G") +
                    " deldis:" + deldis.ToString("G") +
                    " delpos:" + delpos.ToString("G") +
                    " deldir:" + deldir.ToString("G"));

                UELogMan.LogMsg("Normal:" + mEnvTraceInfo.HitNormal.ToString("G"));
            }

            if (mEnvTraceInfo.Fraction > FRACTION_EPSILON)
            {
                //moved some portion of the total distance
                newvel = mv.Velocity;
                numplanes = 0;
            }

            if (mEnvTraceInfo.Fraction > 1.0f - FRACTION_EPSILON)
            {
                // covered the entire distance, done and return
                break;
            }

            mSlideMoveNormals[numplanes] = mEnvTraceInfo.HitNormal;
            ++numplanes;

            if (numplanes == 1 && mv.TPNormal.y < mv.Slope)
            {
                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " into 1plane branch");
                }

                // reflect player velocity on the first try
                if (mSlideMoveNormals[0].y > mv.Slope)
                {
                    //floor or slope
                    ClipVelocity(curvel, mSlideMoveNormals[0], 1.01f, mv.WishSpd, ref newvel);
                }
                else
                {
                    //steep slope, reduce the horizontal speed to avoid a high-speed rush on the slop 
                    Vector3 clipped = curvel;
                    clipped.x *= CLIMB_SPEED_RATIO;
                    clipped.z *= CLIMB_SPEED_RATIO;
                    ClipVelocity(clipped, mSlideMoveNormals[0], 1.01f, mv.WishSpd, ref newvel);
                }

                mv.Velocity = newvel;
                curvel = newvel;
            }
            else
            {
                // modify cur_velocity so it parallels all of the clip planes
                int i = 0;
                for (i = 0; i < numplanes; ++i)
                {
                    if (mSlideMoveNormals[i].y < mv.Slope && mSlideMoveNormals[i].y > -mv.Slope)
                    {
                        mSlideMoveNormals[i].y = 0;
                        mSlideMoveNormals[i].Normalize();
                    }
                    ClipVelocity(curvel, mSlideMoveNormals[i], 1.01f, mv.WishSpd, ref mv.Velocity);
                    int j = 0;
                    for (j = 0; j < numplanes; ++j)
                    {
                        if (j != i)
                        {
                            // Are we now moving against this plane?
                            if (Vector3.Dot(mSlideMoveNormals[j], mv.Velocity) < 0)
                                break;	// not ok
                        }
                    }
                    if (j == numplanes)  // Didn't need clip, so we're ok
                        break;
                }

                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " into other branch" +
                        " curvel:" + curvel.ToString("G") +
                        " newvel:" + mv.Velocity.ToString("G"));
                }

                // Did we go all the way through plane set
                if (i != numplanes)
                {
                    // go along this plane
                    // velocity is set in clipping call, no need to set again.
                    curvel = mv.Velocity;
                }
                else
                {	// go along the crease
                    if (numplanes != 2)
                    {
                        //对于复杂的场景，crease有三个或三个以上的plane, 清除velocity,
                        //会造成不能移动???
                        mv.Velocity = Vector3.zero;

                        if (MoveLog)
                        {
                            UELogMan.LogMsg("Try " + tryidx.ToString() + " break try at numplanes!=2");
                        }

                        break;
                    }

                    //only two planes, calc a new speed direction
                    Vector3 dir = Vector3.Cross(mSlideMoveNormals[0], mSlideMoveNormals[1]);
                    //map the orig velocity to the new direction
                    float dtp = Vector3.Dot(dir, mv.Velocity);
                    mv.Velocity = dir * dtp;

                    if (MoveLog)
                    {
                        UELogMan.LogMsg("Try " + tryidx.ToString() + " when numplanes==2" +
                            " newvel:" + mv.Velocity.ToString("G"));
                    }
                }

                //
                // if original velocity is against the original velocity, stop dead
                // to avoid tiny occilations in sloping corners
                //
                Vector3 nrlmvVel = mv.Velocity;
                nrlmvVel.Normalize();
                Vector3 nrloriVel = orivel;
                nrloriVel.Normalize();
                float nrldtp = Vector3.Dot(nrlmvVel, nrloriVel);
                if (nrldtp <= DTP_LIMIT)
                {
                    mv.Velocity = Vector3.zero;

                    if (MoveLog)
                    {
                        UELogMan.LogMsg("Try " + tryidx.ToString() + " break at dtp<limit");
                    }

                    break;
                }
            }
        }

        if (allfraction <= FRACTION_EPSILON)
        {
            mv.Velocity = Vector3.zero;

            if (MoveLog)
            {
                UELogMan.LogMsg("clear vel when all fraction==0");
            }
        }
    }

    //true:成功，false： 失败，需要stepupmove继续尝试
    private static bool PreTrySlideMove(MoveTraceInfo mv)
    {
        Vector3 orivel = Vector3.zero;
        Vector3 curvel = Vector3.zero;
        Vector3 newvel = Vector3.zero;
        Vector3 oripos = Vector3.zero;
        Vector3 delta = Vector3.zero;
        float timeleft = 0.0f;
        float allfraction = 0.0f;
        int numplanes = 0;

        mEnvTraceInfo.HalfLen = mv.HalfLen;
        mEnvTraceInfo.Radius = mv.Radius;
        orivel = mv.Velocity;
        curvel = mv.Velocity;
        oripos = mv.Start;

        timeleft = mv.TimeSec;

        if (MoveLog)
        {
            Vector3 origveln = orivel;
            origveln.Normalize();
            UELogMan.LogMsg("TrySlideMove " +
                " OriVel:" + orivel.ToString("G") +
                " OriVelDir:" + origveln.ToString("G"));
        }

        for (int tryidx = 0; tryidx < MAX_TRY_MOVE; ++tryidx)
        {
            mv.End = mv.Start;
            delta = mv.Velocity * timeleft;

            if (MoveLog)
            {
                UELogMan.LogMsg("Try " + tryidx.ToString() +
                    " start:" + mv.Start.ToString("G") +
                    " delta:" + delta.ToString("G"));
            }

            if (delta.sqrMagnitude < SQR_DIST_EPSILON)
            {
                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " small delta break");
                }

                break;
            }

            mEnvTraceInfo.Start = mv.Start;
            mEnvTraceInfo.Delta = delta;
            mEnvTraceInfo.TerStart = mv.Start;
            mEnvTraceInfo.TerStart.y -= mv.Radius + mv.HalfLen; //foot
            mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;

            bool collide = CollideWithEnv(mEnvTraceInfo);
            if (mEnvTraceInfo.StartSolid)
            {
                // If we started in a solid object, or in solid spac the whole way, zero out our velocity 
                mv.End = oripos;
                mv.Velocity = Vector3.zero;
                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " start solid return");
                }
                return true;
            }

            allfraction += mEnvTraceInfo.Fraction;
            timeleft -= timeleft * mEnvTraceInfo.Fraction;
            if (mEnvTraceInfo.Fraction < 0)
                mv.Start -= mEnvTraceInfo.HitNormal * mEnvTraceInfo.Fraction;
            else
                mv.Start += mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;
            mv.End = mv.Start;

            if (MoveLog)
            {
                Vector3 delpos = mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;
                Vector3 deldir = delpos;
                float deldis = UEMathUtil.Normalize(ref deldir);
                UELogMan.LogMsg("Try " + tryidx.ToString() + " result" +
                    " end:" + mv.End.ToString("G") +
                    " fraction:" + mEnvTraceInfo.Fraction.ToString("G") +
                    " deldis:" + deldis.ToString("G") +
                    " delpos:" + delpos.ToString("G") +
                    " deldir:" + deldir.ToString("G"));

                UELogMan.LogMsg("Normal:" + mEnvTraceInfo.HitNormal.ToString("G"));
            }

            if (mEnvTraceInfo.Fraction > FRACTION_EPSILON)
            {
                //moved some portion of the total distance
                newvel = mv.Velocity;
                numplanes = 0;
            }

            if (!collide)
            {
                // covered the entire distance, done and return
                break;
            }

            mSlideMoveNormals[numplanes] = mEnvTraceInfo.HitNormal;
            ++numplanes;

            // modify cur_velocity so it parallels all of the clip planes
            int i = 0;
            for (i = 0; i < numplanes; ++i)
            {
                if (mSlideMoveNormals[i].y < mv.Slope)//碰到过不去的地方
                {
                    mv.TimeSec = timeleft;
                    return false;
                }

                ClipVelocity(curvel, mSlideMoveNormals[i], 1.01f, mv.WishSpd, ref mv.Velocity);
                int j = 0;
                for (j = 0; j < numplanes; ++j)
                {
                    if (j != i)
                    {
                        // Are we now moving against this plane?
                        if (Vector3.Dot(mSlideMoveNormals[j], mv.Velocity) < 0)
                            break;	// not ok
                    }
                }
                if (j == numplanes)  // Didn't need clip, so we're ok
                    break;
            }

            if (MoveLog)
            {
                UELogMan.LogMsg("Try " + tryidx.ToString() + " into other branch" +
                    " curvel:" + curvel.ToString("G") +
                    " newvel:" + mv.Velocity.ToString("G"));
            }

            // Did we go all the way through plane set
            if (i != numplanes)
            {
                // go along this plane
                // velocity is set in clipping call, no need to set again.
                curvel = mv.Velocity;
            }
            else
            {	// go along the crease
                if (numplanes != 2)
                {
                    //对于复杂的场景，crease有三个或三个以上的plane, 清除velocity,
                    //会造成不能移动???
                    mv.Velocity = Vector3.zero;

                    if (MoveLog)
                    {
                        UELogMan.LogMsg("Try " + tryidx.ToString() + " break try at numplanes!=2");
                    }

                    break;
                }

                //only two planes, calc a new speed direction
                Vector3 dir = Vector3.Cross(mSlideMoveNormals[0], mSlideMoveNormals[1]);
                //map the orig velocity to the new direction
                float dtp = Vector3.Dot(dir, mv.Velocity);
                mv.Velocity = dir * dtp;

                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " when numplanes==2" +
                        " newvel:" + mv.Velocity.ToString("G"));
                }
            }

            //
            // if original velocity is against the original velocity, stop dead
            // to avoid tiny occilations in sloping corners
            //
            Vector3 nrlmvVel = mv.Velocity;
            nrlmvVel.Normalize();
            Vector3 nrloriVel = orivel;
            nrloriVel.Normalize();
            float nrldtp = Vector3.Dot(nrlmvVel, nrloriVel);
            if (nrldtp <= DTP_LIMIT)
            {
                mv.Velocity = Vector3.zero;

                if (MoveLog)
                {
                    UELogMan.LogMsg("Try " + tryidx.ToString() + " break at dtp<limit");
                }

                break;
            }
        }

        if (allfraction <= FRACTION_EPSILON)
        {
            mv.Velocity = Vector3.zero;

            if (MoveLog)
            {
                UELogMan.LogMsg("clear vel when all fraction==0");
            }
        }

        return true;
    }

    private static void StepUpMove(MoveTraceInfo mv)
    {
        // step up, move fowrad, and trace down
        Vector3 origin = mv.Start;
        Vector3 originvel = mv.Velocity;

        mEnvTraceInfo.HalfLen = mv.HalfLen;
        mEnvTraceInfo.Radius = mv.Radius;
        mEnvTraceInfo.Start = mv.Start;
        mEnvTraceInfo.TerStart = mEnvTraceInfo.Start;
        mEnvTraceInfo.TerStart.y -= mEnvTraceInfo.HalfLen + mEnvTraceInfo.Radius;

        //up frist
        mEnvTraceInfo.Delta = Vector3.up * mv.StepHeight;
        mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;
        mEnvTraceInfo.PlayerID = mv.PlayerID;

        CollideWithEnv(mEnvTraceInfo);

        if (mEnvTraceInfo.StartSolid)
        {
            mv.End = origin;

            if (MoveLog)
            {
                UELogMan.LogMsg("StepUpMove up start solid");
            }
            return;
        }

        Vector3 stepupdist = Vector3.zero;

        //if (mEnvTraceInfo.Fraction < 0.8 && mEnvTraceInfo.HitNormal.y < 0 && mEnvTraceInfo.HitNormal.y > -0.707)
        //{
        //    collideHead = true;
        //    //碰头了，而且碰的是立面墙，此时改变方向沿着墙继续上探
        //    if (mEnvTraceInfo.Fraction < 0)
        //        stepupdist += -mEnvTraceInfo.HitNormal * mEnvTraceInfo.Fraction;
        //    else
        //        stepupdist += mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;

        //    Vector3 delta = (Vector3.up - mEnvTraceInfo.HitNormal.y * mEnvTraceInfo.HitNormal).normalized * mv.StepHeight * (1 - mEnvTraceInfo.Fraction);

        //    mEnvTraceInfo.Start = mv.Start + stepupdist;
        //    mEnvTraceInfo.Delta = delta;
        //    mEnvTraceInfo.TerStart = mEnvTraceInfo.Start;
        //    mEnvTraceInfo.TerStart.y -= mEnvTraceInfo.Radius + mEnvTraceInfo.HalfLen;
        //    mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;
        //    CollideWithEnv(mEnvTraceInfo);
        //    if (mEnvTraceInfo.StartSolid)
        //    {
        //        mv.End = origin;

        //        if (MoveLog)
        //        {
        //            UELogMan.LogMsg("StepUpMove up start solid");
        //        }
        //        return;
        //    }
        //}

        if (mEnvTraceInfo.Fraction < 0)
            stepupdist += - mEnvTraceInfo.HitNormal * mEnvTraceInfo.Fraction;
        else
            stepupdist += mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;

        Vector3 beforeslidestart = mv.Start + stepupdist;
        mv.Start = beforeslidestart;


        //move forward
        if(!PreTrySlideMove(mv))
        {
            mv.Start = origin;
            mv.Velocity = originvel;
            TrySlideMove(mv, true);
        }
        else
        {
            if (MoveLog)
            {
                UELogMan.LogMsg("StepUpMove after forward move:" +
                    " End:" + mv.End.ToString("G") +
                    " Dist:" + (mv.End - beforeslidestart).magnitude.ToString("G") +
                    " Vel:" + mv.Velocity.ToString("G"));
            }

            //trace down
            float dist = (mv.End - beforeslidestart).magnitude;
            mEnvTraceInfo.Start = mv.End;
            mEnvTraceInfo.Delta = new Vector3(0, -dist - stepupdist.magnitude, 0);
            mEnvTraceInfo.TerStart = mEnvTraceInfo.Start;
            mEnvTraceInfo.TerStart.y -= mEnvTraceInfo.Radius + mEnvTraceInfo.HalfLen;
            mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;

            bool collide = CollideWithEnv(mEnvTraceInfo, true);
            if (mEnvTraceInfo.StartSolid)
            {
                mv.End = origin;

                UELogMan.LogMsg("trace down move: StartSolid " +
                    " origin:" + origin.ToString("G") +
                    " Start:" + mEnvTraceInfo.Start.ToString("G"));
                if (MoveLog)
                {
                    UELogMan.LogMsg("StepUpMove down start solid");
                }

                return;
            }

            if (mEnvTraceInfo.Fraction < 0)
                mv.End += -mEnvTraceInfo.HitNormal * mEnvTraceInfo.Fraction;
            else
                mv.End += mEnvTraceInfo.Delta * mEnvTraceInfo.Fraction;

            //mStepOnPos = mv.End;
            if (collide)
            {
                if (mEnvTraceInfo.HitNormal.y < mv.Slope)
                {
                    mv.Start = origin;
                    mv.Velocity = originvel;
                    TrySlideMove(mv, true);
                    //mv.End = origin;
                }
            }
        }
    }

    private static void WalkMove(MoveTraceInfo mv)
    {
        Vector3 wishdir = mv.WishDir;
        wishdir.y = 0.0f;
        float wishspeed = mv.WishSpd;

        // Set pmove velocity
        mv.Velocity.y = 0.0f;
        Accelerate(wishdir, wishspeed, mv.Accel, mv.TimeSec, ref mv.Velocity);
        mv.Velocity.y = 0.0f;
        mv.AbsVelocity = mv.Velocity;
        if (mv.Velocity.sqrMagnitude < VELOCITY_EPSILON)
        {
            mv.Velocity = Vector3.zero;
            mv.End = mv.Start;
            if (MoveLog)
            {
                UELogMan.LogMsg("WalkMove too small vel");
            }
            return;
        }

        if (usePreSlide)
        {
            if(!PreTrySlideMove(mv))
            {
                StepUpMove(mv);
            }
        }
        else
        {
            StepUpMove(mv);
        }
        
    }

    public static bool usePreSlide = false;

    private static void JumpFallMove(MoveTraceInfo mv)
    {
        Vector3 wishdir;
        float wishspeed;

        wishdir = mv.WishDir;
        wishspeed = mv.WishSpd;

        //AirAccelerate(wishdir, wishspeed, mv.Accel, mv.TimeSec, ref mv.Velocity);
        mv.AbsVelocity = mv.Velocity;

        TrySlideMove(mv);
    }

    public static void FullGroundMove(MoveTraceInfo mv)
    {
        // print ground move info
        string strinfo = null;
        if (MoveLog)
        {
            strinfo = string.Format("mv.Start = new Vector3({0}f, {1}f, {2}f);\nmv.WishDir = new Vector3({3}f, {4}f, {5}f);\nmv.TimeSec = {6}f;\nmv.Velocity = new Vector3({7}f, {8}f, {9}f);\nmv.TPNormal = new Vector3({10}f, {11}f, {12}f);\n",
            mv.Start.x.ToString("G"),
            mv.Start.y.ToString("G"),
            mv.Start.z.ToString("G"),
            mv.WishDir.x.ToString("G"),
            mv.WishDir.y.ToString("G"),
            mv.WishDir.z.ToString("G"),
            mv.TimeSec.ToString("G"),
            mv.Velocity.x.ToString("G"),
            mv.Velocity.y.ToString("G"),
            mv.Velocity.z.ToString("G"),
            mv.TPNormal.x.ToString("G"),
            mv.TPNormal.y.ToString("G"),
            mv.TPNormal.z.ToString("G"));
            UELogMan.LogMsg(strinfo);
        }


        Vector3 origin = mv.Start;
        Vector3 originvel = mv.Velocity;

        if (mv.TPNormal.y > mv.Slope)
        {
            mv.Velocity.y = 0.0f;
        }
        else
        {
            mv.Velocity.y -= mv.Gravity * mv.TimeSec * 0.5f;
        }

        UEMathUtil.ClampFloor(ref mv.Velocity.y, mv.MaxFallSpd);

        if (MoveLog)
        {
            UELogMan.LogMsg("FullGroundMove: " +
                " start: " + mv.Start.ToString("G") +
                " orivel: " + originvel.ToString("G") +
                " usevel: " + mv.Velocity.ToString("G"));
        }

        if (mv.TPNormal.y > mv.Slope)
        {
            if (MoveLog)
            {
                UELogMan.LogMsg("Do WalkMove --------------------------------------------");
            }

            WalkMove(mv);
        }
        else
        {
            if (MoveLog)
            {
                UELogMan.LogMsg("Do JumpFallMove --------------------------------------------");
            }

            JumpFallMove(mv);
        }

        //UELogMan.LogMsg("Vel: " + mv.Velocity.ToString() + " vel.mag:" +mv.Velocity.magnitude.ToString());


        if (originvel.y > 1.0f)
        {
            //jump 
            mv.TPNormal = Vector3.zero;
        }
        else
        {
            mGndTraceInfo.Start = mv.End;
            mGndTraceInfo.HalfLen = mv.HalfLen;
            mGndTraceInfo.Radius = mv.Radius;
            mGndTraceInfo.DeltaY = 0.08f;

            if (!RetrieveSupportPlane(mGndTraceInfo))
            {
                mv.End = origin;
                mv.Velocity = originvel;

                if (mv.TPNormal.y < mv.Slope)
                {
                    mv.TPNormal = Vector3.up;
                }

                if (MoveLog)
                {
                    UELogMan.LogMsg("RetrieveSupportPlane Fail");
                }

                return;
            }

            if (mGndTraceInfo.Support)
            {
                mv.End = mGndTraceInfo.End;
                mv.TPNormal = mGndTraceInfo.HitNormal;

                if (MoveLog)
                {
                    UELogMan.LogMsg("RetrieveSupportPlane Can Support");
                }
            }
            else
            {
                mv.TPNormal = Vector3.zero;

                if (MoveLog)
                {
                    UELogMan.LogMsg("RetrieveSupportPlane Cannot Support");
                }
            }
        }

        if (mv.TPNormal.y > mv.Slope)
        {
            mv.Velocity.y = 0.0f;
        }
        else
        {
            mv.Velocity.y -= mv.Gravity * mv.TimeSec * 0.5f;
        }

        if (MoveLog)
        {
            strinfo = string.Format("mv.End = {0}", mv.End.ToString("G"));
            UELogMan.LogMsg(strinfo);
        }


    }

    public static void OnGroundMove(MoveCDR cdr)
    {
        string loginfo = "";
        if (MoveLog)
        {
            loginfo = "=================== ground move ====================";
            UELogMan.LogMsg(loginfo);
            loginfo = string.Format("in:[{0}, {1}, {2}] xoz:[{3}, {4}] vel:[{5}, {6}, {7}] t:{8} tpn:[{9}, {10}, {11}]",
                cdr.Center.x, cdr.Center.y, cdr.Center.z,
                cdr.VelDirH.x, cdr.VelDirH.z,
                cdr.ClipVel.x, cdr.ClipVel.y, cdr.ClipVel.z,
                cdr.TimeSec, cdr.TPNormal.x, cdr.TPNormal.y, cdr.TPNormal.z);
            UELogMan.LogMsg(loginfo);
        }

        mMoveTraceInfo.Start = cdr.Center;
        mMoveTraceInfo.HalfLen = cdr.HalfLen;
        mMoveTraceInfo.Radius = cdr.Radius;
        mMoveTraceInfo.TPNormal = cdr.TPNormal;
        mMoveTraceInfo.Slope = cdr.SlopeThresh;
        mMoveTraceInfo.TimeSec = cdr.TimeSec;
        mMoveTraceInfo.WishDir = cdr.VelDirH;
        mMoveTraceInfo.WishSpd = cdr.Speed;
        mMoveTraceInfo.Accel = UECollision.MOVE_ACCELERATION;
        mMoveTraceInfo.Velocity = cdr.ClipVel; // the last move speed
        mMoveTraceInfo.MaxFallSpd = UECollision.MAX_FALL_SPEED;
        mMoveTraceInfo.Gravity = cdr.Gravity;
        mMoveTraceInfo.StepHeight = cdr.StepHeight;
        mMoveTraceInfo.PlayerID = cdr.PlayerID;

        FullGroundMove(mMoveTraceInfo);

        cdr.ClipVel = mMoveTraceInfo.Velocity;
        cdr.MoveDist = UEMathUtil.Magnitude(mMoveTraceInfo.End - cdr.Center);
        cdr.Blocked = (cdr.MoveDist < DIST_EPSILON);
        cdr.Center = mMoveTraceInfo.End;
        cdr.TPNormal = mMoveTraceInfo.TPNormal;
        cdr.ActualVel = mMoveTraceInfo.AbsVelocity;
        cdr.CanStay = cdr.TPNormal.y >= cdr.SlopeThresh;
        cdr.OnSurface = cdr.TPNormal.sqrMagnitude > DIST_EPSILON;

        if (cdr.Blocked)
        {
            cdr.ClipVel.Set(0.0f, 0.0f, 0.0f);
        }

        if (MoveLog)
        {
            loginfo = string.Format("out:[{0}, {1}, {2}] tpn:[{3}, {4}, {5}] vel:[{6}, {7}, {8}] move:{9} block:{10} stay:{11} surface:{12}",
                cdr.Center.x, cdr.Center.y, cdr.Center.z,
                cdr.TPNormal.x, cdr.TPNormal.y, cdr.TPNormal.z,
                cdr.ClipVel.x, cdr.ClipVel.y, cdr.ClipVel.z,
                cdr.MoveDist, cdr.Blocked, cdr.CanStay, cdr.OnSurface);
            UELogMan.LogMsg(loginfo);
        }
    }

    //public static void HostGroundMove(HostMoveCD move)
    //{
    //    mMoveTraceInfo.Start = move.Center;
    //    mMoveTraceInfo.Extent = move.Extents;
    //    mMoveTraceInfo.TPNormal = move.TPNormal;
    //    mMoveTraceInfo.Slope = move.SlopeThresh;
    //    mMoveTraceInfo.TimeSec = move.TimeSec;
    //    mMoveTraceInfo.WishDir = move.VelDirH;
    //    mMoveTraceInfo.WishSpd = move.Speed;
    //    mMoveTraceInfo.Accel = 10.0f;           // speed acceration
    //    mMoveTraceInfo.Velocity = move.ClipVel; // the last move speed
    //    mMoveTraceInfo.MaxFallSpd = -50.0f;
    //    mMoveTraceInfo.Gravity = move.Gravity;
    //    mMoveTraceInfo.StepHeight = move.StepHeight;

    //    FullGroundMove(mMoveTraceInfo);

    //    move.ClipVel = mMoveTraceInfo.Velocity;
    //    move.MoveDist = UEMathUtil.Magnitude(mMoveTraceInfo.End - move.Center);
    //    move.Blocked = (move.MoveDist < DIST_EPSILON);
    //    move.Center = mMoveTraceInfo.End;
    //    move.TPNormal = mMoveTraceInfo.TPNormal;
    //    move.ActualVel = mMoveTraceInfo.AbsVelocity;
    //    move.CanStay = move.TPNormal.y >= move.SlopeThresh;
    //    move.OnSurface = move.TPNormal.sqrMagnitude > DIST_EPSILON;

    //    if (move.Blocked)
    //    {
    //        move.ClipVel = Vector3.zero;
    //    }
    //}

    public static float VertRayTraceEnvHeight(Vector3 start)
    {
        start.y += TRACE_STEP_HEIGHT;
        Vector3 pos, normal;
        if (VertRayTrace(start, 1000.0f, out pos, out normal))
        {
            return pos.y;
        }

        return start.y - TRACE_STEP_HEIGHT;
    }

    public static void ObjectGroundMove(ObjectMoveCD move)
    {
        float dist = move.TimeSec * move.Velocity;
        Vector3 delta = dist * move.Dir;
        Vector3 oldPos = move.Pos;
        oldPos.y += MOVE_STEP_HEIGHT;
        move.Pos = delta + oldPos;
        move.Normal = Vector3.up;

        if (move.TraceGnd)
        {
            Vector3 traceStart = move.Pos;
            traceStart.y += TRACE_STEP_HEIGHT;
            Vector3 hitPos, hitNormal;
            if (VertRayTrace(traceStart, 1000.0f, out hitPos, out hitNormal))
            {
                // find gournd and can stay
                move.Pos = hitPos;
                move.Pos.y += MOVE_STEP_HEIGHT;
                move.Normal = hitNormal;
            }
        }

        move.Pos.y -= MOVE_STEP_HEIGHT;
    }

    public static bool RayTrace(Vector3 start, Vector3 dir, float dist, out UEEngine.UETraceRt traceRt, uint trace = TRACE_PURPOSE.PURPOSE_PICK)
    {
        traceRt = mRayTraceInfo.traceRt;

        UEWorld curWorld = UEGlobal.GetCurWorld();
        if (curWorld == null)
            return false;

        mRayTraceInfo.ray.origin = start;
        mRayTraceInfo.ray.direction = dir;
        mRayTraceInfo.traceDist = dist;
        mRayTraceInfo.traceRt.Reset();

        if (curWorld.RayTrace(mRayTraceInfo, trace))
        {
            return true;
        }

        return false;
    }

    public static bool RayTraceTerrain(Vector3 start, Vector3 dir, float dist, out UEEngine.UETraceRt traceRt)
    {
        traceRt = mRayTraceInfo.traceRt;

        UEWorld curWorld = UEGlobal.GetCurWorld();
        if (curWorld == null)
            return false;

        mRayTraceInfo.ray.origin = start;
        mRayTraceInfo.ray.direction = dir;
        mRayTraceInfo.traceDist = dist;
        mRayTraceInfo.traceRt.Reset();

        if (curWorld.RayTrace(mRayTraceInfo, TRACE_PURPOSE.ONLY_TERRAIN))
        {
            return true;
        }

        return false;
    }

    public static bool RayTrace(Vector3 start, Vector3 dir, float dist, out Vector3 hitPos, out Vector3 hitNormal)
    {
        UEEngine.UETraceRt traceRt;

        bool ret = RayTrace(start, dir, dist, out traceRt);

        hitPos = traceRt.hitPos;
        hitNormal = traceRt.hitNormal;

        return ret;
    }

    // enviroment ray trace, for move collision
    public static bool RayTraceEnv(Vector3 start, Vector3 dir, float dist, out Vector3 hitPos, out Vector3 hitNormal, out float hitDist)
    {
        UEWorld curWorld = UEGlobal.GetCurWorld();
        hitPos = Vector3.zero;
        hitNormal = Vector3.up;
        hitDist = 0.0f;
        if (curWorld == null)
            return false;

        mRayTraceInfo.ray.origin = start;
        mRayTraceInfo.ray.direction = dir;
        mRayTraceInfo.traceDist = dist;
        mRayTraceInfo.traceRt.Reset();

        if (curWorld.RayTrace(mRayTraceInfo, TRACE_PURPOSE.PURPOSE_MOVE))
        {
            hitPos = mRayTraceInfo.traceRt.hitPos;
            hitNormal = mRayTraceInfo.traceRt.hitNormal;
            hitDist = mRayTraceInfo.traceRt.hitDist;
            return true;
        }

        return false;
    }

    // Vertically ray trace enviroment
    public static bool VertRayTrace(Vector3 start, float dist, out Vector3 hitPos, out Vector3 hitNormal)
    {
        hitPos = Vector3.zero;
        hitNormal = Vector3.up;
        UEWorld curWorld = UEGlobal.GetCurWorld();
        if (curWorld == null)
            return false;

        mRayTraceInfo.ray.origin = start;
        mRayTraceInfo.ray.direction = Vector3.down;
        mRayTraceInfo.traceDist = dist;
        mRayTraceInfo.traceRt.Reset();

        if (curWorld.RayTrace(mRayTraceInfo, TRACE_PURPOSE.PURPOSE_MOVE))
        {
            hitPos = mRayTraceInfo.traceRt.hitPos;
            hitNormal = mRayTraceInfo.traceRt.hitNormal;
            return true;
        }

        return false;
    }

    public static bool CollideWithHMap(Vector3 start, Vector3 delta, out float fraction, out Vector3 hitnormal, out bool startsolid)
    {
        UEWorld curworld = UEGlobal.GetCurWorld();
        UECollideHMap collidehmap = (curworld != null) ? curworld.GetCollideHMap() : null;
        if (collidehmap == null || !collidehmap.IsLoaded())
        {
            startsolid = false;
            fraction = 0.0f;
            hitnormal = Vector3.zero;
            return false;
        }

        startsolid = false;
        float h1 = collidehmap.GetPosHeight(start, out hitnormal);
        if (h1 > start.y + DIST_EPSILON)
        {
            startsolid = true;
            fraction = 0.0f;
            return true;
        }

        float fGridSizeInv = collidehmap.GridSizeInv;
        int iMaxWid = collidehmap.NumTotalGridCol;
        int iMaxHei = collidehmap.NumTotalGridRow;

        int nWid, nHei;
        float fMag = (delta.x > 0) ? (delta.x) : (-delta.x);
        nWid = Mathf.CeilToInt(fMag * fGridSizeInv) + 1;
        if (nWid > iMaxWid)
            nWid = iMaxWid;
        nWid = Mathf.Max(3, nWid);

        fMag = (delta.z > 0) ? (delta.z) : (-delta.z);
        nHei = Mathf.CeilToInt(fMag * fGridSizeInv) + 1;
        if (nHei > iMaxHei)
            nHei = iMaxHei;
        nHei = Mathf.Max(3, nHei);

        int nTriangles = nWid * nHei * 2;
        //need clear
        if (null == mFaceVertLst)
        {
            mFaceVertLst = new Vector3[iMaxWid * iMaxHei];
        }

        if (!collidehmap.GetFacesOfArea(start + delta * 0.5f, nWid, nHei, ref mFaceVertLst, ref mFaceIdxLst))
        {
            startsolid = false;
            fraction = 0.0f;
            hitnormal = Vector3.zero;
            return false;
        }

        //Vector3[] pVerts = new Vector3[(nWid + 1) * (nHei + 1)];
        //int[] pIndices = new int[nTriangles * 3];
        //if (!collidehmap.GetFacesOfArea(start + delta * 0.5f, nWid, nHei, ref pVerts, ref pIndices))
        //{
        //    startsolid = false;
        //    fraction = 0.0f;
        //    hitnormal = Vector3.zero;
        //    return false;
        //}

        int i;

        fraction = 100.0f;
        float tmpFraction = fraction;

        for (i = 0; i < nTriangles; i++)
        {
            mFaceVert[0] = mFaceVertLst[mFaceIdxLst[i * 3]];
            mFaceVert[1] = mFaceVertLst[mFaceIdxLst[i * 3 + 1]];
            mFaceVert[2] = mFaceVertLst[mFaceIdxLst[i * 3 + 2]];

            Vector3 vPt = Vector3.zero;
            if (UECollisionUtil.RayToTriangle(start, delta, mFaceVert[0], mFaceVert[1], mFaceVert[2], out vPt, true, out tmpFraction) &&
                (tmpFraction <= 1.0f) && (tmpFraction < fraction))
            {
                //get the triangle normal
                Vector3 vEdge1 = (mFaceVert[1]) - (mFaceVert[0]);
                Vector3 vEdge2 = (mFaceVert[2]) - (mFaceVert[0]);
                hitnormal = Vector3.Cross(vEdge1, vEdge2);
                hitnormal.Normalize();

                Vector3 vDir = delta.normalized;
                if (Vector3.Dot(hitnormal, vDir) > 0.01f)
                {
                    continue;
                }

                fraction = Mathf.Max(0.0f, tmpFraction);
            }
        }

        return (fraction <= 1.0f);
    }

    public static bool CollideWithEnv(EnvTraceInfo info, bool SupportPlane = false)
    {
        bool collide = false;
        info.Fraction = 100.0f;
        info.StartSolid = false;
        info.ClsFlag = 0;
        info.HitEnv = HIT_ENVTYPE.HIT_NULL;

        // check terrain
        float fraction;
        Vector3 ternormal;
        bool startsolid = false;
        bool collideret = CollideWithHMap(info.TerStart, info.Delta, out fraction, out ternormal, out startsolid);

        if (collideret && (fraction < info.Fraction))
        {
            collide = true;
            info.Fraction = fraction;
            info.HitNormal = ternormal;
            info.StartSolid = startsolid;
            info.HitEnv = HIT_ENVTYPE.HIT_HMAP;
        }

        mCapsuleTraceBrushInfo.Init(new CAPSULE(info.Start, info.HalfLen, info.Radius), info.Delta, info.CheckFlag);
        mCapsuleTraceBrushInfo.PlayerID = info.PlayerID;
        if (CapsuleCollideWithBrush(mCapsuleTraceBrushInfo) && mCapsuleTraceBrushInfo.Fraction < info.Fraction)
        {
            collide = true;
            info.Fraction = mCapsuleTraceBrushInfo.Fraction;
            info.StartSolid = mCapsuleTraceBrushInfo.StartSolid;
            info.HitEnv = HIT_ENVTYPE.HIT_BRUSH;
            info.ClsFlag = mCapsuleTraceBrushInfo.HitFlags;

            if (!info.StartSolid)
            {
                if (SupportPlane && mCapsuleTraceBrushInfo.HitObject != null && mCapsuleTraceBrushInfo.HitObject.cd != null)
                {
                    CollidePoints cp = mCapsuleTraceBrushInfo.HitPoints;
                    if (cp.size == 3)//和面最近，直接用陷入法线
                    {
                        info.HitNormal = mCapsuleTraceBrushInfo.Normal;
                    }
                    if (cp.size == 2)//和线最近
                    {
                        info.HitNormal = mCapsuleTraceBrushInfo.HitObject.cd.GetSupportPlaneNormal(cp.a, cp.b);
                    }
                    if (cp.size == 1)//和面最近，直接用陷入法线
                    {
                        info.HitNormal = mCapsuleTraceBrushInfo.HitObject.cd.GetSupportPlaneNormal(cp.a);
                    }
                }
                else
                {
                    info.HitNormal = mCapsuleTraceBrushInfo.Normal;
                }
            }
        }
        

        if (info.Fraction > 1.0f) info.Fraction = 1.0f;
        //UEMathUtil.Clamp(ref info.Fraction, 0.0f, 1.0f);

        return collide;
    }

    public static bool AABBCollideWithBrush(BrushTraceInfo info)
    {
        if (null == info || null == UEEngineRoot.Scene)
            return false;

        bool ret = false;
        // trace static collision first
        UECollisioinMan manager = UEEngineRoot.Scene.CollisioinMan;
        if (null != manager && manager.BrushTrace(info))
        {
            ret = true;
        }

        // trace moving collision later
        UEWorld world = UEGlobal.GetCurWorld();
        if (world != null && world.WorldLoaded && world.BrushTrace(info))
        {
            ret = true;
        }

        return ret;
    }

    public static bool CapsuleCollideWithBrush(CapsuleTraceBrushInfo info)
    {
        if (null == info || null == UEEngineRoot.Scene)
            return false;

        bool ret = false;
        UECollisioinMan manager = UEEngineRoot.Scene.CollisioinMan;
        if (null != manager && manager.CapsuleTraceBrush(info))
        {
            ret = true;
        }

        // trace moving collision later
        UEWorld world = UEGlobal.GetCurWorld();
        if (world != null && world.WorldLoaded && world.BrushTraceCapsule(info))
        {
            ret = true;
        }

        return ret;
    }

    public static bool AABBTrace(Vector3 center, Vector3 ext, Vector3 delta, ref Vector3 end, ref float fraction)
    {
        mEnvTraceInfo.Start = center;
        mEnvTraceInfo.TerStart = center;
        float radius = Mathf.Max(ext.x, ext.z);
        mGndTraceInfo.HalfLen = ext.y - radius;
        if (mGndTraceInfo.HalfLen < 0) mGndTraceInfo.HalfLen = 0;
        mEnvTraceInfo.TerStart.y -= ext.y;
        mEnvTraceInfo.Delta = delta;
        mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_MOVETRACE;
        mEnvTraceInfo.PlayerID = -1;

        bool collide = CollideWithEnv(mEnvTraceInfo);

        if (mEnvTraceInfo.StartSolid)
        {
            collide = true;
            mEnvTraceInfo.Fraction = 0.0f;
        }

        fraction = mEnvTraceInfo.Fraction;
        end = center + delta * fraction;

        return collide;
    }

    public static bool AnimCameraTrace(Vector3 start, Vector3 delta, ref Vector3 end)
    {
        Vector3 pdelta = new Vector3(0, -.9f, 0);
        Vector3 pstart = start;
        pstart.y += .9f;
        mEnvTraceInfo.Start = pstart;
        mEnvTraceInfo.TerStart = pstart;
        mEnvTraceInfo.HalfLen = 0;
        mEnvTraceInfo.Radius = CAMERA_SIZE;
        mEnvTraceInfo.TerStart.y -= mCamExt.y;
        mEnvTraceInfo.Delta = pdelta;
        mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_CAMTRACE;
        mEnvTraceInfo.PlayerID = -1;

        bool collide = CollideWithEnv(mEnvTraceInfo);
        pstart += pdelta * mEnvTraceInfo.Fraction;

        pdelta = start + delta - pstart;

        mEnvTraceInfo.Start = pstart;
        mEnvTraceInfo.TerStart = pstart;
        mEnvTraceInfo.HalfLen = 0;
        mEnvTraceInfo.Radius = CAMERA_SIZE;
        mEnvTraceInfo.TerStart.y -= mCamExt.y;
        mEnvTraceInfo.Delta = pdelta;
        mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_CAMTRACE;

        collide = CollideWithEnv(mEnvTraceInfo);
        end = pstart + pdelta * mEnvTraceInfo.Fraction;
        return collide;
    }

    public static bool CameraTrace(Vector3 start, Vector3 delta, ref Vector3 end, ref float fraction)
    {
        mEnvTraceInfo.Start = start;
        mEnvTraceInfo.TerStart = start;
        mEnvTraceInfo.HalfLen = 0;
        mEnvTraceInfo.Radius = CAMERA_SIZE;
        mEnvTraceInfo.TerStart.y -= mCamExt.y;
        mEnvTraceInfo.Delta = delta;
        mEnvTraceInfo.CheckFlag = ConvexData.CHFLAG_SKIP_CAMTRACE;
        mEnvTraceInfo.PlayerID = -1;

        bool collide = CollideWithEnv(mEnvTraceInfo);

        if (mEnvTraceInfo.StartSolid)
        {
            collide = true;
            mEnvTraceInfo.Fraction = 0.0f;
            //UELogMan.LogMsg("CameraTrace: " + start + " delta: " + delta + " newpos: " + end + " frac: " + fraction+" start solid");
        }

        if (!collide || mEnvTraceInfo.HitEnv != HIT_ENVTYPE.HIT_HMAP)
        {
            fraction = mEnvTraceInfo.Fraction;
            end = start + delta * fraction;
            return collide;
        }

        // collide with hmap, if against the plane, move camera back a distance according to normal
        Vector3 tracedir = delta;
        if (Vector3.Dot(mEnvTraceInfo.HitNormal, tracedir) < 0.0f)
        {
            fraction = mEnvTraceInfo.Fraction;
            float deltadist = UEMathUtil.Normalize(ref tracedir);
            float dotabs = Mathf.Abs(Vector3.Dot(mEnvTraceInfo.HitNormal, tracedir));
            UEMathUtil.ClampFloor(ref dotabs, 0.10f);
            float backdist = CAMERA_SIZE / dotabs;
            fraction -= backdist / deltadist;
            UEMathUtil.ClampFloor(ref fraction, 0.0f);
            end = start + delta * fraction;
        }

        // protect the camera pos not under the ground
        UEWorld curworld = UEGlobal.GetCurWorld();
        UECollideHMap collidehmap = (curworld != null) ? curworld.GetCollideHMap() : null;
        if (collidehmap != null && collidehmap.IsLoaded())
        {
            Vector3 normal;
            float gndheight = collidehmap.GetPosHeight(end, out normal);
            UEMathUtil.ClampFloor(ref end.y, gndheight + 0.2f);
        }

        return collide;
    }

    private static bool GetHMapInfo(Vector3 pos, ref Vector3 surface, ref Vector3 normal)
    {
        UECollideHMap collidehmap = (UEGlobal.GetCurWorld() != null) ? UEGlobal.GetCurWorld().GetCollideHMap() : null;
        if (collidehmap == null)
            return false;

        surface = pos;
        surface.y = collidehmap.GetPosHeight(pos, out normal);

        return true;
    }

    public static bool VertAABBTrace(Vector3 center, Vector3 ext, ref Vector3 hitpos, ref Vector3 normal)
    {
        Vector3 terpos = Vector3.zero;
        Vector3 ternormal = Vector3.zero;
        GetHMapInfo(center, ref terpos, ref normal);

        mGndTraceInfo.Start = center;
        float radius = Mathf.Max(ext.x, ext.z);
        mGndTraceInfo.HalfLen = ext.y - radius;
        if (mGndTraceInfo.HalfLen < 0) mGndTraceInfo.HalfLen = 0;
        mGndTraceInfo.Radius = radius;
        mGndTraceInfo.DeltaY = Mathf.Min(center.y - ext.y - terpos.y + 0.5f, 1000.0f);
        if (!RetrieveSupportPlane(mGndTraceInfo) || (!mGndTraceInfo.Support))
        {
            hitpos = terpos;
            hitpos.y += (ext.y + DIST_EPSILON);
            normal = ternormal;
            return false;
        }

        hitpos = mGndTraceInfo.End;
        normal = mGndTraceInfo.HitNormal;

        return true;
    }
}
