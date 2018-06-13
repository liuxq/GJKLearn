
using UnityEngine;
using System;


    public static class UEMathUtil
    {
        public const float INVALID_POSVALUE = 9000000.0f;
        public const float FLOAT_EPSILON = 0.00001f;
        public const float DIR_COMPRESS_INV_INTER = 65536.0f / 360.0f;
        public const float DIR_COMPRESS_INTER = 360.0f / 65536.0f;

        public enum BoundOriginType
        {
            BOT_CENTER = 0,
            BOT_BOTTOM_CENTER = 1,
            BOT_LEFT_BOTTOM = 2,
        }

        public const int MASK_PIXEL_ARR_LEN_DEFAULT = 32 * 32;
        public static Color[] maskPixels = new Color[MASK_PIXEL_ARR_LEN_DEFAULT];

        public static void CalMaskTex(Texture2D tex, Color colMask, Color colNoMask, float angle)
        {
            if (tex.width * tex.height > maskPixels.Length)
                maskPixels = new Color[tex.width * tex.height];

            Array.Clear(maskPixels, 0, maskPixels.Length);

            int w = tex.width;
            int h = tex.height;
            int w_2 = tex.width / 2;
            int h_2 = tex.height / 2;
            angle = Mathf.Clamp(angle, 0, 360);
            if (FloatEqual(angle, 0))
            {
                for (int i = 0; i < w * h; ++i)
                    maskPixels[i] = colNoMask;
            }
            else if (FloatEqual(angle, 360))
            {
                for (int i = 0; i < w * h; ++i)
                    maskPixels[i] = colMask;
            }
            else
            {
                int s_x = 0;
                int s_y = 0;
                int e_x = 0;
                int e_y = 0;
                Color col_1 = Color.black; //right-top
                Color col_2 = Color.black; //right-bottom
                Color col_3 = Color.black; //left-bottom
                Color col_4 = Color.black; //left-top

                float cos = 0.0f;
                Vector3 s_v = Vector3.zero;
                Vector3 e_v = Vector3.zero;

                if (angle < 90)
                {
                    s_x = w_2;
                    s_y = 0;
                    e_x = w;
                    e_y = h_2;

                    cos = Mathf.Cos(DegreeToRadian(angle));
                    s_v.y = 0 - h_2;
                    s_v.Normalize();

                    col_2 = colNoMask;
                    col_3 = colNoMask;
                    col_4 = colNoMask;
                }
                else if (angle < 180)
                {
                    s_x = w_2;
                    s_y = h_2;
                    e_x = w;
                    e_y = h;

                    cos = Mathf.Cos(DegreeToRadian(angle - 90));
                    s_v.x = w - w_2;
                    s_v.Normalize();

                    col_1 = colMask;
                    col_3 = colNoMask;
                    col_4 = colNoMask;
                }
                else if (angle < 270)
                {
                    s_x = 0;
                    s_y = h_2;
                    e_x = w_2;
                    e_y = h;

                    cos = Mathf.Cos(DegreeToRadian(angle - 180));
                    s_v.y = h - h_2;
                    s_v.Normalize();

                    col_1 = colMask;
                    col_2 = colMask;
                    col_4 = colNoMask;
                }
                else if (angle < 360)
                {
                    s_x = 0;
                    s_y = 0;
                    e_x = w_2;
                    e_y = h_2;

                    cos = Mathf.Cos(DegreeToRadian(angle - 270));
                    s_v.x = 0 - w_2;
                    s_v.Normalize();

                    col_1 = colMask;
                    col_2 = colMask;
                    col_3 = colMask;
                }

                for (int i = 0; i < h_2; ++i)
                {
                    for (int j = w_2; j < w; ++j)
                        maskPixels[i * w + j] = col_1;

                    for (int j = 0; j < w_2; ++j)
                        maskPixels[i * w + j] = col_4;
                }
                for (int i = h_2; i < h; ++i)
                {
                    for (int j = w_2; j < w; ++j)
                        maskPixels[i * w + j] = col_2;

                    for (int j = 0; j < w_2; ++j)
                        maskPixels[i * w + j] = col_3;
                }

                for (int i = s_y; i < e_y; ++i)
                {
                    for (int j = s_x; j < e_x; ++j)
                    {
                        e_v.y = i - h_2;
                        e_v.x = j - w_2;
                        e_v.Normalize();

                        if (Vector3.Dot(e_v, s_v) > cos)
                            maskPixels[i * w + j] = colMask;
                        else
                            maskPixels[i * w + j] = colNoMask;
                    }
                }

                for (int i = 0; i < h_2; ++i)
                {
                    for (int j = 0; j < w; ++j)
                    {
                        Color col = maskPixels[i * w + j];
                        maskPixels[i * w + j] = maskPixels[(h - 1 - i) * w + j];
                        maskPixels[(h - 1 - i) * w + j] = col;
                    }
                }
            }

            tex.SetPixels(maskPixels);
            tex.Apply();
        }

        public static int ConvertToInt32(float num)
        {
            return (int)num * 10000;
        }

        public static float ConvertToFloat(int num)
        {
            return num * .0001f;
        }

        public static Vector3 ToVector3(string str)
        {
            return ToVector3(str, ',');
        }

        public static Vector3 ToVector3(string str, char separator)
        {
            string[] vals = str.Split(separator);

            Vector3 vec;
            vec.x = Convert.ToSingle(vals[0]);
            vec.y = Convert.ToSingle(vals[1]);
            vec.z = Convert.ToSingle(vals[2]);

            return vec;
        }

        public static string Vector3ToString(Vector3 vec)
        {
            return string.Format("{0},{1},{2}", vec.x, vec.y, vec.z);
        }

        public static Vector2 ToVector2(string str)
        {
            return ToVector2(str, ',');
        }

        public static Vector2 ToVector2(string str, char separator)
        {

            string[] vals = str.Split(separator);

            Vector2 vec;
            vec.x = Convert.ToSingle(vals[0]);
            vec.y = Convert.ToSingle(vals[1]);

            return vec;
        }

        public static string Vector2ToString(Vector2 vec)
        {
            return string.Format("{0},{1}", vec.x, vec.y);
        }

        public static Quaternion ToQuaternion(string str)
        {

            string[] vals = str.Split(',');

            Quaternion quat = new Quaternion();
            quat.x = Convert.ToSingle(vals[0]);
            quat.y = Convert.ToSingle(vals[1]);
            quat.z = Convert.ToSingle(vals[2]);
            quat.z = Convert.ToSingle(vals[3]);
            return quat;
        }

        public static string QuatToString(Quaternion quat)
        {
            return string.Format("{0},{1},{2},{3}", quat.x, quat.y, quat.z, quat.w); ;
        }

        public static bool FloatLargerEqual(float f0,float f1)
        {
            if (f0 > f1)
            {
                return true;
            }

            float diss = Mathf.Abs(f0 - f1);
            return FloatEqual(f0, f1, 0.1f);
        }

        public static bool FloatEqual(float f0, float f1)
        {
            return Mathf.Abs(f0 - f1) <= .01;//Mathf.Approximately(f0, f1);
        }

        public static bool FloatEqual(float f0, float f1, float epsilon)
        {
            return Mathf.Abs(f0 - f1) < Mathf.Abs(epsilon);
        }

        public static float DegreeToRadian(float degree)
        {
            return degree * Mathf.Deg2Rad;
        }

        public static float RadianToDegree(float radian)
        {
            return radian * Mathf.Rad2Deg;
        }

        public static int FloatToFix16(float x)
        {
            return (int)(x * 65536.0f + 0.5f);
        }

        public static float Fix16ToFloat(int x)
        {
            float tmp = x;
            return tmp / 65536.0f;
        }

        public static short FloatToFix8(float x)
        {
            return (short)(x * 256.0f + 0.5f);
        }

        public static float Fix8ToFloat(short x)
        {
            float tmp = x;
            return tmp / 256.0f;
        }

        // Clip value
        public static void ClampFloor<T>(ref T val, T min) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
                val = min;
        }

        public static void ClampRoof<T>(ref T val, T max) where T : IComparable<T>
        {
            if (val.CompareTo(max) > 0)
                val = max;
        }

        public static void Clamp<T>(ref T val, T min, T max) where T : IComparable
        {
            if (val.CompareTo(min) < 0)
                val = min;
            else if (val.CompareTo(max) > 0)
                val = max;
        }

        public static void Clamp(ref float val, float min, float max)
        {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
        }

        private const float _invInter = 128.0f / Mathf.PI;
        private readonly static float[] _cos = new float[256]
	    {
		    1.000000f, 0.999699f, 0.998795f, 0.997290f,
		    0.995185f, 0.992480f, 0.989177f, 0.985278f,
		    0.980785f, 0.975702f, 0.970031f, 0.963776f,
		    0.956940f, 0.949528f, 0.941544f, 0.932993f,
		    0.923880f, 0.914210f, 0.903989f, 0.893224f,
		    0.881921f, 0.870087f, 0.857729f, 0.844854f,
		    0.831470f, 0.817585f, 0.803208f, 0.788346f,
		    0.773010f, 0.757209f, 0.740951f, 0.724247f,
		    0.707107f, 0.689541f, 0.671559f, 0.653173f,
		    0.634393f, 0.615232f, 0.595699f, 0.575808f,
		    0.555570f, 0.534998f, 0.514103f, 0.492898f,
		    0.471397f, 0.449611f, 0.427555f, 0.405241f,
		    0.382683f, 0.359895f, 0.336890f, 0.313682f,
		    0.290285f, 0.266713f, 0.242980f, 0.219101f,
		    0.195090f, 0.170962f, 0.146730f, 0.122411f,
		    0.098017f, 0.073564f, 0.049068f, 0.024541f,
		    -0.000000f, -0.024541f, -0.049068f, -0.073565f,
		    -0.098017f, -0.122411f, -0.146731f, -0.170962f,
		    -0.195090f, -0.219101f, -0.242980f, -0.266713f,
		    -0.290285f, -0.313682f, -0.336890f, -0.359895f,
		    -0.382684f, -0.405241f, -0.427555f, -0.449611f,
		    -0.471397f, -0.492898f, -0.514103f, -0.534998f,
		    -0.555570f, -0.575808f, -0.595699f, -0.615232f,
		    -0.634393f, -0.653173f, -0.671559f, -0.689541f,
		    -0.707107f, -0.724247f, -0.740951f, -0.757209f,
		    -0.773010f, -0.788346f, -0.803208f, -0.817585f,
		    -0.831470f, -0.844854f, -0.857729f, -0.870087f,
		    -0.881921f, -0.893224f, -0.903989f, -0.914210f,
		    -0.923880f, -0.932993f, -0.941544f, -0.949528f,
		    -0.956940f, -0.963776f, -0.970031f, -0.975702f,
		    -0.980785f, -0.985278f, -0.989177f, -0.992480f,
		    -0.995185f, -0.997290f, -0.998795f, -0.999699f,
		    -1.000000f, -0.999699f, -0.998795f, -0.997290f,
		    -0.995185f, -0.992480f, -0.989177f, -0.985278f,
		    -0.980785f, -0.975702f, -0.970031f, -0.963776f,
		    -0.956940f, -0.949528f, -0.941544f, -0.932993f,
		    -0.923880f, -0.914210f, -0.903989f, -0.893224f,
		    -0.881921f, -0.870087f, -0.857729f, -0.844854f,
		    -0.831470f, -0.817585f, -0.803208f, -0.788346f,
		    -0.773010f, -0.757209f, -0.740951f, -0.724247f,
		    -0.707107f, -0.689541f, -0.671559f, -0.653173f,
		    -0.634393f, -0.615232f, -0.595699f, -0.575808f,
		    -0.555570f, -0.534997f, -0.514103f, -0.492898f,
		    -0.471397f, -0.449611f, -0.427555f, -0.405241f,
		    -0.382683f, -0.359895f, -0.336890f, -0.313682f,
		    -0.290285f, -0.266713f, -0.242980f, -0.219101f,
		    -0.195090f, -0.170962f, -0.146730f, -0.122411f,
		    -0.098017f, -0.073564f, -0.049067f, -0.024541f,
		    0.000000f, 0.024541f, 0.049068f, 0.073565f,
		    0.098017f, 0.122411f, 0.146730f, 0.170962f,
		    0.195090f, 0.219101f, 0.242980f, 0.266713f,
		    0.290285f, 0.313682f, 0.336890f, 0.359895f,
		    0.382684f, 0.405242f, 0.427555f, 0.449612f,
		    0.471397f, 0.492898f, 0.514103f, 0.534998f,
		    0.555570f, 0.575808f, 0.595699f, 0.615232f,
		    0.634393f, 0.653173f, 0.671559f, 0.689541f,
		    0.707107f, 0.724247f, 0.740951f, 0.757209f,
		    0.773011f, 0.788347f, 0.803208f, 0.817585f,
		    0.831470f, 0.844854f, 0.857729f, 0.870087f,
		    0.881921f, 0.893224f, 0.903989f, 0.914210f,
		    0.923880f, 0.932993f, 0.941544f, 0.949528f,
		    0.956940f, 0.963776f, 0.970031f, 0.975702f,
		    0.980785f, 0.985278f, 0.989177f, 0.992480f,
		    0.995185f, 0.997290f, 0.998795f, 0.999699f,
	    };
        private readonly static float[] _sin = new float[256]
	    {
		    0.000000f, 0.024541f, 0.049068f, 0.073565f,
		    0.098017f, 0.122411f, 0.146730f, 0.170962f,
		    0.195090f, 0.219101f, 0.242980f, 0.266713f,
		    0.290285f, 0.313682f, 0.336890f, 0.359895f,
		    0.382683f, 0.405241f, 0.427555f, 0.449611f,
		    0.471397f, 0.492898f, 0.514103f, 0.534998f,
		    0.555570f, 0.575808f, 0.595699f, 0.615232f,
		    0.634393f, 0.653173f, 0.671559f, 0.689541f,
		    0.707107f, 0.724247f, 0.740951f, 0.757209f,
		    0.773010f, 0.788346f, 0.803208f, 0.817585f,
		    0.831470f, 0.844854f, 0.857729f, 0.870087f,
		    0.881921f, 0.893224f, 0.903989f, 0.914210f,
		    0.923880f, 0.932993f, 0.941544f, 0.949528f,
		    0.956940f, 0.963776f, 0.970031f, 0.975702f,
		    0.980785f, 0.985278f, 0.989177f, 0.992480f,
		    0.995185f, 0.997290f, 0.998795f, 0.999699f,
		    1.000000f, 0.999699f, 0.998795f, 0.997290f,
		    0.995185f, 0.992480f, 0.989176f, 0.985278f,
		    0.980785f, 0.975702f, 0.970031f, 0.963776f,
		    0.956940f, 0.949528f, 0.941544f, 0.932993f,
		    0.923880f, 0.914210f, 0.903989f, 0.893224f,
		    0.881921f, 0.870087f, 0.857729f, 0.844854f,
		    0.831470f, 0.817585f, 0.803207f, 0.788346f,
		    0.773010f, 0.757209f, 0.740951f, 0.724247f,
		    0.707107f, 0.689540f, 0.671559f, 0.653173f,
		    0.634393f, 0.615231f, 0.595699f, 0.575808f,
		    0.555570f, 0.534997f, 0.514103f, 0.492898f,
		    0.471397f, 0.449611f, 0.427555f, 0.405241f,
		    0.382683f, 0.359895f, 0.336890f, 0.313682f,
		    0.290285f, 0.266713f, 0.242980f, 0.219101f,
		    0.195090f, 0.170962f, 0.146730f, 0.122411f,
		    0.098017f, 0.073564f, 0.049067f, 0.024541f,
		    -0.000000f, -0.024541f, -0.049068f, -0.073565f,
		    -0.098017f, -0.122411f, -0.146731f, -0.170962f,
		    -0.195090f, -0.219101f, -0.242980f, -0.266713f,
		    -0.290285f, -0.313682f, -0.336890f, -0.359895f,
		    -0.382683f, -0.405241f, -0.427555f, -0.449612f,
		    -0.471397f, -0.492898f, -0.514103f, -0.534998f,
		    -0.555570f, -0.575808f, -0.595699f, -0.615232f,
		    -0.634393f, -0.653173f, -0.671559f, -0.689541f,
		    -0.707107f, -0.724247f, -0.740951f, -0.757209f,
		    -0.773010f, -0.788346f, -0.803208f, -0.817585f,
		    -0.831470f, -0.844854f, -0.857729f, -0.870087f,
		    -0.881921f, -0.893224f, -0.903989f, -0.914210f,
		    -0.923880f, -0.932993f, -0.941544f, -0.949528f,
		    -0.956940f, -0.963776f, -0.970031f, -0.975702f,
		    -0.980785f, -0.985278f, -0.989177f, -0.992480f,
		    -0.995185f, -0.997290f, -0.998795f, -0.999699f,
		    -1.000000f, -0.999699f, -0.998795f, -0.997290f,
		    -0.995185f, -0.992479f, -0.989177f, -0.985278f,
		    -0.980785f, -0.975702f, -0.970031f, -0.963776f,
		    -0.956940f, -0.949528f, -0.941544f, -0.932993f,
		    -0.923879f, -0.914210f, -0.903989f, -0.893224f,
		    -0.881921f, -0.870087f, -0.857729f, -0.844853f,
		    -0.831469f, -0.817585f, -0.803208f, -0.788346f,
		    -0.773010f, -0.757209f, -0.740951f, -0.724247f,
		    -0.707107f, -0.689541f, -0.671559f, -0.653173f,
		    -0.634393f, -0.615231f, -0.595699f, -0.575808f,
		    -0.555570f, -0.534998f, -0.514103f, -0.492898f,
		    -0.471397f, -0.449611f, -0.427555f, -0.405241f,
		    -0.382683f, -0.359895f, -0.336890f, -0.313682f,
		    -0.290284f, -0.266712f, -0.242980f, -0.219101f,
		    -0.195090f, -0.170962f, -0.146730f, -0.122410f,
		    -0.098017f, -0.073565f, -0.049068f, -0.024541f,
	    };

        // Dir compress
        public static byte CompressDirH(float x, float z)
        {
            if (Mathf.Abs(x) < FLOAT_EPSILON)
            {
                return (z > 0.0f) ? (byte)64 : (byte)192;
            }

            float val = Mathf.Atan2(z, x) * _invInter;
            if (val < 0.0f)
                val += 256.0f;

            return (byte)val;
        }

        public static Vector3 DecompressDirH(byte byDir)
        {
            return new Vector3(_cos[byDir], 0.0f, _sin[byDir]);
        }

        // Dir compress 2
        public static ushort CompressDirH2(float x, float z)
        {
            if (Mathf.Abs(x) < UEMathUtil.FLOAT_EPSILON)
                return (z > 0.0f) ? (ushort)16384 : (ushort)49152;

            float deg = RadianToDegree(Mathf.Atan2(z, x));
            return (ushort)(deg * DIR_COMPRESS_INV_INTER);
        }

        public static Vector3 DecompressDirH2(ushort sDir)
        {
            float rad = DegreeToRadian(sDir * DIR_COMPRESS_INTER);
            return new Vector3(Mathf.Cos(rad), 0.0f, Mathf.Sin(rad));
        }

        public static float Vector3ToDir(Vector3 dir)
        {
            float at = Mathf.Atan2(dir.z, dir.x);
            float angle = at * Mathf.Rad2Deg;

            return angle;
        }

        public static Vector3 DirToVector3(float angle)
        {
            //angle *= 256f / 360f;
            Vector3 dir = Vector3.zero;
            double x = Mathf.Cos(angle * Mathf.Deg2Rad);
            dir.x = (float)Math.Round(x, 6);
            double y = Mathf.Sin(angle * Mathf.Deg2Rad);
            dir.z = (float)Math.Round(y, 6);
            return dir;
        }

        public static ushort CompressDir(Vector3 dir)
        {
            byte b1, b2;
            if (1.0f - Mathf.Abs(dir.y) < FLOAT_EPSILON)
            {
                b1 = 0;
                b2 = dir.y < 0.0f ? (byte)128 : (byte)0;
                return (ushort)(b2 << 8 | b1);
            }

            Vector3 vh = new Vector3(dir.x, 0.0f, dir.z);
            vh.Normalize();

            if (Mathf.Abs(dir.x) < FLOAT_EPSILON)
            {
                b1 = (dir.z > 0.0f) ? (byte)64 : (byte)192;
            }
            else
            {
                float val1 = Mathf.Atan2(dir.z, dir.x) * _invInter;
                if (val1 < 0.0f)
                    val1 += 256.0f;

                b1 = (byte)(val1 * _invInter);
            }

            float val2 = Mathf.Acos(dir.y) * _invInter;
            if (val2 < 0.0f)
                val2 += 256.0f;

            b2 = (byte)(val2 * _invInter);

            return (ushort)(b2 << 8 | b1);
        }

        public static Vector3 DecompressDir(ushort sdir)
        {
            byte b1 = (byte)(sdir & 0xff);
            byte b2 = (byte)(sdir >> 8);
            return new Vector3(_cos[b1] * _sin[b2], _cos[b2], _sin[b1] * _sin[b2]);
        }

        public static float Magnitude(Vector3 vec)
        {
            return vec.magnitude;
        }

        public static float MagnitudeH(Vector3 vec)
        {
            return Mathf.Sqrt(vec.x * vec.x + vec.z * vec.z);
        }

        public static float SqrMagnitude(Vector3 vec)
        {
            return vec.sqrMagnitude;
        }

        public static float SqrMagnitudeH(Vector3 vec)
        {
            return (vec.x * vec.x + vec.z * vec.z);
        }

        public static float Normalize(ref Vector3 vec)
        {
            float magnitude = vec.magnitude;
            if (magnitude > 0.0f)
            {
                vec.x /= magnitude;
                vec.y /= magnitude;
                vec.z /= magnitude;
            }
            return magnitude;
        }

        public static void Snap(ref Vector3 vec)
        {
            for (int i = 0; i < 3; i++)
            {
                if (vec[i] > 1.0f - 1e-5f)
                {
                    vec = Vector3.zero;
                    vec[i] = 1.0f;
                    break;
                }
                else if (vec[i] < -1.0f + 1e-5f)
                {
                    vec = Vector3.zero;
                    vec[i] = -1.0f;
                    break;
                }
            }
        }

        public static float MaxMumber(Vector3 vec)
        {
            if (vec.x > vec.y)
                return vec.x > vec.z ? vec.x : vec.z;
            else
                return vec.y > vec.z ? vec.y : vec.z;
        }

        public static float MinMumber(Vector3 vec)
        {
            if (vec.x < vec.y)
                return vec.x < vec.z ? vec.x : vec.z;
            else
                return vec.y < vec.z ? vec.y : vec.z;
        }

        //for mouse pos, the left-bottom is (0, 0),
        //for gui pos, the left-top is (0, 0)
        public static Vector3 MousePosToGUIPos(Vector3 pos)
        {
            Vector3 newPos = pos;
            newPos.y = Screen.height - pos.y;
            return newPos;
        }

        public static Bounds DimensionToBound(Vector3 dim, BoundOriginType type)
        {
            Vector3 center = Vector3.zero;
            switch (type)
            {
                case BoundOriginType.BOT_CENTER:
                    center = Vector3.zero;
                    break;
                case BoundOriginType.BOT_BOTTOM_CENTER:
                    center = Vector3.zero;
                    center.y = dim.y / 2;
                    break;
                case BoundOriginType.BOT_LEFT_BOTTOM:
                    center = dim / 2;
                    break;
                default:
                    //GLogger.GetFile( GOEngine.LogFile.Global ).LogWarning("invalid bound origin type");
                    break;
            }

            return new Bounds(center, dim);
        }

        public static bool RaycastBound(Ray ray, Bounds bound)
        {
            return bound.IntersectRay(ray);
        }

        public static bool RaycastBound(Ray ray, Bounds bound, out float distance)
        {
            return bound.IntersectRay(ray, out distance);
        }

        public static bool BoundIntersect(Bounds bound0, Bounds bound1)
        {
            if (bound0.min.x > bound1.max.x) return false;
            if (bound0.min.y > bound1.max.y) return false;
            if (bound0.min.z > bound1.max.z) return false;

            if (bound0.max.x < bound1.min.x) return false;
            if (bound0.max.y < bound1.min.y) return false;
            if (bound0.max.z < bound1.min.z) return false;

            return true;
        }

        public static bool BoundIntersect(Bounds bound0, Bounds bound1, ref Bounds intersectBounds)
        {
            Vector3 intersectMin = Vector3.zero;
            intersectMin.x = Math.Max(bound0.min.x, bound1.min.x);
            intersectMin.y = Math.Max(bound0.min.y, bound1.min.y);
            intersectMin.z = Math.Max(bound0.min.z, bound1.min.z);

            Vector3 intersectMax = Vector3.zero;
            intersectMax.x = Math.Min(bound0.max.x, bound1.max.x);
            intersectMax.y = Math.Min(bound0.max.y, bound1.max.y);
            intersectMax.z = Math.Min(bound0.max.z, bound1.max.z);

            if (intersectMax.x <= intersectMin.x) return false;
            if (intersectMax.y <= intersectMin.y) return false;
            if (intersectMax.z <= intersectMin.z) return false;

            Vector3 center = Vector3.zero;
            center.x = (intersectMin.x + intersectMax.x) / 2;
            center.y = (intersectMin.y + intersectMax.y) / 2;
            center.z = (intersectMin.z + intersectMax.z) / 2;

            Vector3 size = Vector3.zero;
            size.x = intersectMax.x - intersectMin.x;
            size.y = intersectMax.y - intersectMin.y;
            size.z = intersectMax.z - intersectMin.z;

            intersectBounds = new Bounds(center, size);
            return true;
        }

        public static bool BoundContainsPoint(Bounds bound, Vector3 pos)
        {
            return bound.Contains(pos);
        }

        public static bool Bound0ContainsBound1(Bounds bound0, Bounds bound1)
        {
            if (bound1.min.x >= bound0.min.x && bound1.max.x <= bound0.max.x
                && bound1.min.y >= bound0.min.y && bound1.max.y <= bound0.max.y
                && bound1.min.z >= bound0.min.z && bound1.max.z <= bound0.max.z)
                return true;

            return false;
        }

        public static Bounds TransformBounds(Transform tm, Bounds bd)
        {
            if (tm == null) return bd;

            Vector3[] posList = new Vector3[8];
            Vector3 pos = bd.min;
            posList[0] = tm.TransformPoint(pos);

            pos.z = bd.max.z;
            posList[1] = tm.TransformPoint(pos);

            pos.y = bd.max.y;
            posList[2] = tm.TransformPoint(pos);

            pos.z = bd.min.z;
            posList[3] = tm.TransformPoint(pos);

            pos.x = bd.max.x;
            posList[4] = tm.TransformPoint(pos);

            pos.z = bd.max.z;
            posList[5] = tm.TransformPoint(pos);

            pos.y = bd.min.y;
            posList[6] = tm.TransformPoint(pos);

            pos.z = bd.min.z;
            posList[7] = tm.TransformPoint(pos);

            Vector3 min = posList[0];
            Vector3 max = posList[0];
            for (int i = 1; i < 8; i++)
            {
                Vector3 v = posList[i];
                if (min.x > v.x) min.x = v.x;
                if (min.y > v.y) min.y = v.y;
                if (min.z > v.z) min.z = v.z;

                if (max.x < v.x) max.x = v.x;
                if (max.y < v.y) max.y = v.y;
                if (max.z < v.z) max.z = v.z;
            }
            Bounds bound = new Bounds(Vector3.zero, Vector3.zero);
            bound.min = min;
            bound.max = max;
            return bound;
        }

        public static Bounds InverseTransformBounds(Transform tm, Bounds bd)
        {
            if (tm == null) return bd;

            Vector3[] posList = new Vector3[8];
            Vector3 pos = bd.min;
            posList[0] = tm.InverseTransformPoint(pos);

            pos.z = bd.max.z;
            posList[1] = tm.InverseTransformPoint(pos);

            pos.y = bd.max.y;
            posList[2] = tm.InverseTransformPoint(pos);

            pos.z = bd.min.z;
            posList[3] = tm.InverseTransformPoint(pos);

            pos.x = bd.max.x;
            posList[4] = tm.InverseTransformPoint(pos);

            pos.z = bd.max.z;
            posList[5] = tm.InverseTransformPoint(pos);

            pos.y = bd.min.y;
            posList[6] = tm.InverseTransformPoint(pos);

            pos.z = bd.min.z;
            posList[7] = tm.InverseTransformPoint(pos);

            Vector3 min = posList[0];
            Vector3 max = posList[0];
            for (int i = 1; i < 8; i++)
            {
                Vector3 v = posList[i];
                if (min.x > v.x) min.x = v.x;
                if (min.y > v.y) min.y = v.y;
                if (min.z > v.z) min.z = v.z;

                if (max.x < v.x) max.x = v.x;
                if (max.y < v.y) max.y = v.y;
                if (max.z < v.z) max.z = v.z;
            }
            Bounds bound = new Bounds(Vector3.zero, Vector3.zero);
            bound.min = min;
            bound.max = max;
            return bound;
        }

        public static Bounds TransformBounds(Matrix4x4 tm, Bounds bd)
        {
            Vector3[] posList = new Vector3[8];
            Vector3 pos = bd.min;
            posList[0] = tm.MultiplyPoint3x4(pos);

            pos.z = bd.max.z;
            posList[1] = tm.MultiplyPoint3x4(pos);

            pos.y = bd.max.y;
            posList[2] = tm.MultiplyPoint3x4(pos);

            pos.z = bd.min.z;
            posList[3] = tm.MultiplyPoint3x4(pos);

            pos.x = bd.max.x;
            posList[4] = tm.MultiplyPoint3x4(pos);

            pos.z = bd.max.z;
            posList[5] = tm.MultiplyPoint3x4(pos);

            pos.y = bd.min.y;
            posList[6] = tm.MultiplyPoint3x4(pos);

            pos.z = bd.min.z;
            posList[7] = tm.MultiplyPoint3x4(pos);

            Vector3 min = posList[0];
            Vector3 max = posList[0];
            for (int i = 1; i < 8; i++)
            {
                Vector3 v = posList[i];
                if (min.x > v.x) min.x = v.x;
                if (min.y > v.y) min.y = v.y;
                if (min.z > v.z) min.z = v.z;

                if (max.x < v.x) max.x = v.x;
                if (max.y < v.y) max.y = v.y;
                if (max.z < v.z) max.z = v.z;
            }
            Bounds bound = new Bounds(Vector3.zero, Vector3.zero);
            bound.min = min;
            bound.max = max;
            return bound;
        }

        public static bool IsAABBInvalide(Bounds aabb)
        {
            return FloatEqual(aabb.extents.x, 0.0f) || FloatEqual(aabb.extents.y, 0.0f) || FloatEqual(aabb.extents.z, 0.0f);
        }

        public static bool IsBoundsInvalide(Bounds bd)
        {
            return FloatEqual(bd.size.x, 0.0f) || FloatEqual(bd.size.y, 0.0f) || FloatEqual(bd.size.z, 0.0f);
        }

        public static int HexChar2Int(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';

            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;

            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            return 0;
        }

        public static Vector2 ScreenPosToGUI(Vector2 pos)
        {
            Vector2 p = pos;
            p.y = Screen.height - pos.y;
            return p;
        }

        public static Vector2 GUIPosToScreen(Vector2 pos)
        {
            Vector2 p = pos;
            p.y = Screen.height - pos.y;
            return p;
        }

        public static Color StringToColor(string value)
        {
            string tmp = value.Substring(1, value.Length - 2);
            string[] s = tmp.Split(new char[] { ',' });

            float r, g, b, a;
            r = float.Parse(s[0]);
            g = float.Parse(s[1]);
            b = float.Parse(s[2]);
            a = float.Parse(s[3]);

            return new Color(r, g, b, a);
        }

        public static string ColorToString(Color col)
        {
            string s = "(" + col.r.ToString() + "," + col.g.ToString()
                + "," + col.b.ToString() + "," + col.a.ToString() + ")";

            return s;
        }

        public static long DateToMillisecond(int date)
        {
            long millisecond = date * 86400000;
            return millisecond;
        }

        public static int MillisecondToHour(long millisecond)
        {
            int hour = (int)(millisecond / 3600000);
            return hour;
        }

        public static string LimitStringByByte(string src, int MaxBytes)
        {
            if (src == " " || src == ""
                || src == string.Empty || src == null)
                return string.Empty;

            string temp = string.Empty;

            int t = 0;
            char[] q = src.ToCharArray();
            for (int i = 0; i < q.Length; ++i)
            {
                if ((int)q[i] >= 0x4E00 && (int)q[i] <= 0x9FA5)//是否汉字
                {
                    temp += q[i];
                    t += 2;
                }
                else
                {
                    temp += q[i];
                    t += 1;
                }
                if (t >= MaxBytes)
                {
                    //FireEngine.GLogger.GetFile( FireEngine.LogFile.Global ).LogWarning("����̫����\n");
                    break;
                }
            }
            return temp;
        }

        public static string LimitNumberString(string src, int MinNum, int MaxNum)
        {
            if (src == " " || src == ""
                || src == string.Empty || src == null)
                return string.Empty;

            string temp = string.Empty;
            char[] q = src.ToCharArray();
            for (int i = 0; i < q.Length; ++i)
            {
                if ((int)q[i] >= 48 && (int)q[i] <= 57)
                {
                    temp += q[i];
                }
                else
                {
                    break;
                }
            }
            if (int.Parse(temp) < MinNum)
                temp = MinNum.ToString();

            if (int.Parse(temp) > MaxNum)
                temp = MaxNum.ToString();

            return temp;
        }

        public static bool GetNearestCrossPoint(Vector2 src, Vector2 dst,
                                                Vector2 min, Vector2 max,
                                                out Vector2 cross)
        {
            cross = Vector2.zero;

            if (src.x < min.x && dst.x < min.x)
                return false;
            if (src.x > max.x && dst.x > max.x)
                return false;
            if (src.y < min.y && dst.y < min.y)
                return false;
            if (src.y > max.y && dst.y > max.y)
                return false;

            if (src.x > min.x && src.x < max.x && src.y > min.y && src.y < max.y
                && dst.x > min.x && dst.x < max.x && dst.y > min.y && dst.y < max.y)
            {
                //GLogger.GetFile( LogFile.Global ).LogWarning("segment in rect: " + src + "" + dst + ""
                //                  + min + "" + max);
                return false;
            }

            Vector2 r0;
            r0.x = min.x;
            r0.y = min.y;

            Vector2 r1;
            r1.x = max.x;
            r1.y = min.y;

            Vector2 r2;
            r2.x = max.x;
            r2.y = max.y;

            Vector2 r3;
            r3.x = min.x;
            r3.y = max.y;

            Vector2 p0;
            p0.x = src.x;
            p0.y = src.y;

            Vector2 p1;
            p1.x = dst.x;
            p1.y = dst.y;

            Vector2 c = Vector2.zero;
            bool ok = GetNearestCrossPoint(p0, p1, r0, r1, r2, r3, out c);

            cross = c;
            return ok;
        }

        public static bool GetNearestCrossPoint(Vector2 p0, Vector2 p1,
                                                Vector2 r0, Vector2 r1,
                                                Vector2 r2, Vector2 r3,
                                                out Vector2 pos)
        {
            pos = Vector2.zero;

            Vector2 c0 = Vector2.zero;
            bool i0 = CrossPoint(p0, p1, r0, r1, out c0);

            Vector2 c1 = Vector2.zero;
            bool i1 = CrossPoint(p0, p1, r1, r2, out c1);

            Vector2 c2 = Vector2.zero;
            bool i2 = CrossPoint(p0, p1, r2, r3, out c2);

            Vector2 c3 = Vector2.zero;
            bool i3 = CrossPoint(p0, p1, r3, r0, out c3);

            if (!i0 && !i1 && !i2 && !i3)
                return false;

            float d0 = (c0 - p0).sqrMagnitude;
            float d1 = (c1 - p0).sqrMagnitude;
            float d2 = (c2 - p0).sqrMagnitude;
            float d3 = (c3 - p0).sqrMagnitude;

            Vector2 c = Vector3.zero;
            float d = 0.0f;
            bool find = false;
            if (i0)
            {
                find = true;
                d = d0;
                c = c0;
            }
            if (i1)
            {
                if (!find)
                {
                    find = true;
                    d = d1;
                    c = c1;
                }
                else if (d1 < d)
                {
                    d = d1;
                    c = c1;
                }
            }
            if (i2)
            {
                if (!find)
                {
                    find = true;
                    d = d2;
                    c = c2;
                }
                else if (d2 < d)
                {
                    d = d2;
                    c = c2;
                }
            }
            if (i3)
            {
                if (!find)
                {
                    find = true;
                    d = d3;
                    c = c3;
                }
                else if (d3 < d)
                {
                    d = d3;
                    c = c3;
                }
            }

            if (!find)
            {
                //GLogger.GetFile( LogFile.Global ).LogWarning("error");
                return false;
            }

            pos = c;
            return true;
        }

        public static bool CrossPoint(Vector2 p0, Vector2 p1, Vector2 q0,
                                      Vector2 q1, out Vector2 c)
        {
            Vector2 p = Vector2.zero;
            c = p;

            Vector2 dp = p1 - p0;
            Vector2 dq = q1 - q0;

            if (FloatEqual(dp.sqrMagnitude, 0) || FloatEqual(dq.sqrMagnitude, 0))
                return false;

            if (FloatEqual(dp.x, 0) && FloatEqual(dq.x, 0))
                return false;

            if (FloatEqual(dp.y, 0) && FloatEqual(dq.y, 0))
                return false;

            if (!FloatEqual(dp.x, 0) && !FloatEqual(dq.x, 0))
            {
                if (FloatEqual(dp.y / dp.x, dq.y / dq.x)
                   || FloatEqual(dp.y / dp.x, -dq.y / dq.x))
                    return false;
            }

            if (!FloatEqual(dp.y, 0) && !FloatEqual(dq.y, 0))
            {
                if (FloatEqual(dp.x / dp.y, dq.x / dq.y)
                   || FloatEqual(dp.x / dp.y, -dq.x / dq.y))
                    return false;
            }

            float a0 = p0.y * dq.x;
            float a1 = dp.y * dq.x;
            float a2 = q0.y * dq.x;
            float b0 = dq.y * p0.x;
            float b1 = dq.y * q0.x;
            float b2 = dq.y * dp.x;

            float a = a0 - a2 - b0 + b1;
            float b = b2 - a1;

            if (FloatEqual(b, 0.0f))
                return false;

            float vp = a / b;
            float vq = -1;
            if (!FloatEqual(dq.x, 0))
                vq = (p0.x + dp.x * vp - q0.x) / dq.x;
            else if (!FloatEqual(dq.y, 0))
                vq = (p0.y + dp.y * vp - q0.y) / dq.y;
            else
            {
                //GLogger.GetFile(LogFile.Global).LogWarning("cross cal error");
            }

            if (vp > 0 - FLOAT_EPSILON && vp < 1 + FLOAT_EPSILON
                && vq > 0 - FLOAT_EPSILON && vq < 1 + FLOAT_EPSILON)
            {
                p.x = p0.x + vp * dp.x;
                p.y = p0.y + vp * dp.y;
                c = p;
                return true;
            }
            else
                return false;
        }

        public static float SymmetricRandom()
        {
            float f = (((uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue) & 0x7fff) << 8) | 0x40000000;
            return f - 3.0f;
        }

        public static float UnitRandom()
        {
            float f = (((uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue) & 0x7fff) << 8) | 0x3f800000;
            return f - 1.0f;
        }

        //-----------------------------------------------------------------------------
        // Name: GEO_2DIntersectSS()
        // Desc: 计算2D空间两条线段的交点(-1:两线段平行或重合,0:无效交点,1:有效交点)//
        //-----------------------------------------------------------------------------
        public static int UE_2DIntersectSS(ref float Px, ref float Py,
                    float P1x, float P1y, float P2x, float P2y,
                    float P3x, float P3y, float P4x, float P4y)
        {
            float a, b, c, d, e;
            float eps = 0.01f;		        //计算精度//
            a = P2x - P1x;
            b = P4x - P3x;
            c = P2y - P1y;
            d = P4y - P3y;
            e = a * d - b * c;
            if (Mathf.Abs(e) <= eps) return -1;	//两线段平行或重合//
            Py = (c * d * (P3x - P1x) + a * d * P1y - b * c * P3y) / e;
            if (Mathf.Abs(c) <= eps) Px = P3x + b * (Py - P3y) / d;
            else Px = P1x + a * (Py - P1y) / c;


            bool b1 = false; bool b2 = false;
            //判断交点是否在p1p2上//
            if (Mathf.Abs(Px - P1x) < eps) a = 0;
            else a = Px - P1x;

            if (Mathf.Abs(Px - P2x) < eps) b = 0;
            else b = Px - P2x;

            if (Mathf.Abs(Py - P1y) < eps) c = 0;
            else c = Py - P1y;

            if (Mathf.Abs(Py - P2y) < eps) d = 0;
            else d = Py - P2y;

            if (a * b < 0 && c * d < 0)
                b1 = true;
            //判断交点是否在p3p4上//
            if (Mathf.Abs(Px - P3x) < eps) a = 0;
            else a = Px - P3x;

            if (Mathf.Abs(Px - P4x) < eps) b = 0;
            else b = Px - P4x;

            if (Mathf.Abs(Py - P3y) < eps) c = 0;
            else c = Py - P3y;

            if (Mathf.Abs(Py - P4y) < eps) d = 0;
            else d = Py - P4y;

            if (a * b < 0 && c * d < 0)
                b2 = true;

            if (b1 && b2) return 1;	//有效交点//
            else return 0;			//无效交点//

        }

        public static int Ceil(float f)
        {
            if (IsIntegerValue(f))
            {
                return (int)f;
            }
            else
            {
                return (int)Mathf.Ceil(f);
            }
        }

        public static int Floor(float f)
        {
            if (IsIntegerValue(f))
            {
                return (int)f;
            }
            else
            {
                return (int)Mathf.Floor(f);
            }
        }

        public static bool IsIntegerValue(float f)
        {
            return Mathf.Abs(f - ((int)f)) < 1e-3;
        }

        public static float LinearInterpolate(float a, float b, float k)
        { 
            return (1- k) * a + k * b;
        }

        public static Vector3 LinearInterpolate(Vector3 a, Vector3 b, float k)
        {
            Vector3 c = new Vector3();
            c.x = LinearInterpolate(a.x, b.x, k);
            c.y = LinearInterpolate(a.y, b.y, k);
            c.z = LinearInterpolate(a.z, b.z, k);
            return c;
        }

        public static int RoundToInt(float input)
        {
            int result = (int)(input + 0.5f + FLOAT_EPSILON);
            return result;
        }

        public static int FloatToInt(float f)
        {
            if (!IsIntegerValue(f))
            {
                return (int)f;
            }
            f += FLOAT_EPSILON;
            return (int)f;
        }
    }
