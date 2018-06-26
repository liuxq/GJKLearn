
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;


public class LEditTextFile : LTextFile
{
    public enum MISS_OPT
    {
        THROW = 0,
        SKIP,
        SETDEFAULT,
        CLEAR_LINE,
    }

    private string mLineCache;

    public LEditTextFile()
        : base()
    {

    }

    public LEditTextFile(Encoding encode)
        : base(encode)
    {

    }

    public override bool Open(string path, OPEN_MODE mode)
    {
        if (!base.Open(path, mode))
            return false;

        return true;
    }

    public override void Close()
    {
        ClearLineCache();
        base.Close();
    }

    private void WriteStr(StreamWriter sw, string str)
    {
        if (string.IsNullOrEmpty(str))
            return;

        sw.Write(str);
    }

    private bool _IsSpace(char ch)
    {
        return (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t');
    }

    private bool _IsSeperator(char ch)
    {
        return (ch == ' ' || ch == ',' || ch == ':' || ch == '\r' || ch == '\n' || ch == '\t' || ch == ')' || ch == ']' || ch == '}');
    }

    private void SkipSpaces(StreamReader sr)
    {
        int peek = sr.Peek();
        while (peek > -1)
        {
            char ch = Convert.ToChar(peek);
            if (!_IsSpace(ch))
                break;

            sr.Read();
            peek = sr.Peek();
        }
    }

    private void _SkipSeperator(StreamReader sr)
    {
        int peek = sr.Peek();
        while (peek > -1)
        {
            char ch = Convert.ToChar(peek);
            if (!_IsSeperator(ch))
                break;

            sr.Read();
            peek = sr.Peek();
        }
    }

    private bool SkipCharacter(StreamReader sr, char ch)
    {
        int peek = sr.Peek();
        if (peek > -1 && (char)peek == ch)
        {
            sr.Read();
            return true;
        }
        return false;
    }

    private string GetWord(StreamReader sr)
    {
        string word = "";
        _SkipSeperator(sr);
        int peek = sr.Peek();
        while (peek > -1)
        {
            char ch = Convert.ToChar(peek);
            if (_IsSeperator(ch))
                break;

            word += ch;
            sr.Read();
            peek = sr.Peek();
        }

        return word;
    }

    private void _SaveKey(StreamWriter sw, string key)
    {
        sw.Write(key + ": ");
    }

    private void LoadKey(StreamReader sr, string key)
    {
        SkipSpaces(sr);
        string load = GetWord(sr);
        if (load != key)
        {
            throw new Exception("LoadKey " + key + " Except with: " + load);
        }

        // the next char must be ':'
        int read = sr.Read();
        if (read < 0 || (char)read != ':')
        {
            throw new Exception("LoadKey " + key + " Except miss \":\"");
        }
    }

    private T _ParseValue<T>(string str)
    {
        T ret = default(T);
        if (string.IsNullOrEmpty(str))
            return ret;

        if (typeof(T) == typeof(int))
        {
            int tmp = int.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(uint))
        {
            uint tmp = uint.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(short))
        {
            short tmp = short.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(ushort))
        {
            ushort tmp = ushort.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(byte))
        {
            byte tmp = byte.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(sbyte))
        {
            sbyte tmp = sbyte.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(char))
        {
            char tmp = char.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(long))
        {
            long tmp = long.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(ulong))
        {
            ulong tmp = ulong.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(bool))
        {
            bool tmp = bool.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(ulong))
        {
            ulong tmp = ulong.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(float))
        {
            float tmp = float.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(double))
        {
            double tmp = double.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(decimal))
        {
            decimal tmp = decimal.Parse(str);
            ret = (T)(object)tmp;
        }

        return ret;
    }

    private bool _SaveValue<T>(StreamWriter sw, string key, T val)
    {
        _SaveKey(sw, key);
        sw.Write(val);
        return true;
    }

    private T _LoadValue<T>(StreamReader sr, string key)
    {
        T val = default(T);
        try
        {
            LoadKey(sr, key);
            val = _ParseValue<T>(GetWord(sr));
        }
        catch (Exception)
        {
            throw;
        }

        return val;
    }

    private StreamReader BuildCurLineReader()
    {
        if (string.IsNullOrEmpty(mLineCache))
        {
            mLineCache = Reader.ReadLine();
        }

        return new StreamReader(new MemoryStream(CurEncoding.GetBytes(mLineCache)), CurEncoding);            
    }

    private void ClearLineCache()
    {
        mLineCache = string.Empty;
    }

    public bool SkipLine(string key)
    {
        if (string.IsNullOrEmpty(key))
            return true;

        try
        {
		    using(StreamReader sr = BuildCurLineReader())
	        {
		        LoadKey(sr, key);
	        }
            ClearLineCache();
        }
        catch (Exception)
        {
        }

        return true;
    }

    public void SaveValueLine<T>(string key, T val)
    {
        Writer.WriteLine(key + ": " + val.ToString());
    }

    public T LoadValueLine<T>(string key, MISS_OPT opt = MISS_OPT.SKIP, T def = default(T))
    {
        T val = default(T);
        try
        {
		    using(StreamReader sr = BuildCurLineReader())
	        {
		        val = _LoadValue<T>(sr, key);
	        }            
            ClearLineCache();
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                val = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }
        return val;
    }

    public void SaveValueListLine<T>(string key, List<T> lstVal)
    {
        if (lstVal == null)
            return;

        string line = key + ": ";
        line += '{';
        for (int i = 0; i < lstVal.Count; i++)
        {
            line += lstVal[i].ToString();
            if (i < lstVal.Count - 1)
                line += ", ";
        }
        line += "}";
        Writer.WriteLine(line);
    }

    public List<T> LoadValueListLine<T>(string key, MISS_OPT opt = MISS_OPT.SKIP, List<T> def = null)
    {
        List<T> val = new List<T>();
        try
        {
		    using(StreamReader sr = BuildCurLineReader())
	        {
		        LoadKey(sr, key);
                SkipSpaces(sr);
                if (!SkipCharacter(sr, '{'))
                    throw new Exception(string.Format("LoadValueListLine {0} exception", key));
                string word = GetWord(sr);
                while (!string.IsNullOrEmpty(word))
                {
                    val.Add(_ParseValue<T>(word));
                }
                SkipSpaces(sr);
                if (!SkipCharacter(sr, '}'))
                    throw new Exception(string.Format("LoadValueListLine {0} exception", key));
	        }            
            ClearLineCache();
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                val = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }
        return val;
    }

    public void SaveNeatLine(string line)
    {
        Writer.WriteLine(line);
    }

    public string LoadNeatLine()
    {
        string line = "";
        try
        {
            using (StreamReader sr = BuildCurLineReader())
            {
                line = sr.ReadLine();
            }
            ClearLineCache();
        }
        catch (Exception)
        {
        }

        return line;
    }

    public void SaveStrLine(string key, string val)
    {
        if(string.IsNullOrEmpty(key))
        {
            key = "";
        }

        if(string.IsNullOrEmpty(val))
        {
            val = "";
        }

        key = key.Replace("\n", "");
        val = val.Replace("\n", "");
        Writer.WriteLine(string.Format("{0}: \"{1}\"", key, val));
    }

    public string LoadStrLine(string key, MISS_OPT opt = MISS_OPT.SKIP, string def = "")
    {
        string val = "";
        try
        {
		    using(StreamReader sr = BuildCurLineReader())
	        {
                LoadKey(sr, key);
                SkipSpaces(sr);

                // check first quotation
                int read = sr.Read();
                if (read < 0 || (char)read != '"')
                    throw new Exception("LoadStrLine " + key + "Exception");
                read = sr.Read();
                while (read >= 0)
                {
                    // read to next quotation
                    char ch = (char)read;
                    if (ch == '"')
                        break;
                    val += ch;
                    read = sr.Read();
                }
	        }
            ClearLineCache();
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                val = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }

        return val;
    }

    public void SaveVector3Line(string key, Vector3 vec)
    {
        string line = key + ": (" + vec.x.ToString() + ", " + vec.y.ToString() + ", " + vec.z.ToString() + ")";
        Writer.WriteLine(line);
    }

    public Vector3 LoadVector3Line(string key, MISS_OPT opt = MISS_OPT.SKIP, Vector3 def = default(Vector3))
    {
        Vector3 val = default(Vector3);
        try
        {
		    using(StreamReader sr = BuildCurLineReader())
	        {
                LoadKey(sr, key);
                SkipSpaces(sr);
                SkipCharacter(sr, '(');
                val.x = float.Parse(GetWord(sr));
                val.y = float.Parse(GetWord(sr));
                val.z = float.Parse(GetWord(sr));
                SkipCharacter(sr, ')');
	        }
            ClearLineCache();            
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                val = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }
        return val;
    }

    public void SaveQuaternionLine(string key, Quaternion qua)
    {
        string line = key + ": (" + qua.x.ToString() + ", " + qua.y.ToString() + ", " + qua.z.ToString() + ", " + qua.w.ToString() + ")";
        Writer.WriteLine(line);
    }

    public Quaternion LoadQuaternionLine(string key, MISS_OPT opt = MISS_OPT.SKIP, Quaternion def = default(Quaternion))
    {
        Quaternion val = default(Quaternion);
        try
        {
            using (StreamReader sr = BuildCurLineReader())
            {
                LoadKey(sr, key);
                SkipSpaces(sr);
                SkipCharacter(sr, '(');
                val.x = float.Parse(GetWord(sr));
                val.y = float.Parse(GetWord(sr));
                val.z = float.Parse(GetWord(sr));
                val.w = float.Parse(GetWord(sr));
                SkipCharacter(sr, ')');
            }
            ClearLineCache();
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                val = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }
        return val;
    }

    public void SaveColorLine(string key, Color col)
    {
        string line = key + ": (" + col.r.ToString() + ", " + col.g.ToString() + ", " + col.b.ToString() + ", " + col.a.ToString() + ")";
        Writer.WriteLine(line);
    }

    public Color LoadColoerLine(string key, MISS_OPT opt = MISS_OPT.SKIP, Color def = default(Color))
    {
        Color col = default(Color);
        try
        {
		    using(StreamReader sr = BuildCurLineReader())
	        {
                LoadKey(sr, key);
                SkipSpaces(sr);
                SkipCharacter(sr, '(');
                col.r = float.Parse(GetWord(sr));
                col.g = float.Parse(GetWord(sr));
                col.b = float.Parse(GetWord(sr));
                col.a = float.Parse(GetWord(sr));
                SkipCharacter(sr, ')');
	        }
            ClearLineCache();  
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                col = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }
        return col;
    }

    public void SaveDateLine(string key, DateTime date)
    {
        string line = key + ": "
            + date.Year.ToString() + ","
            + date.Month.ToString() + ","
            + date.Day.ToString() + " "
            + date.Hour.ToString() + ":"
            + date.Minute.ToString() + ":"
            + date.Second.ToString();
        Writer.WriteLine(line);
    }

    public DateTime LoadDateLine(string key, MISS_OPT opt = MISS_OPT.SKIP, DateTime def = default(DateTime))
    {
        DateTime dt = default(DateTime);
        try
        {
            using (StreamReader sr = BuildCurLineReader())
            {
                LoadKey(sr, key);
                SkipSpaces(sr);
                int year = int.Parse(GetWord(sr));
                int month = int.Parse(GetWord(sr));
                int day = int.Parse(GetWord(sr));
                int hour = int.Parse(GetWord(sr));
                int minute = int.Parse(GetWord(sr));
                int second = int.Parse(GetWord(sr));
                dt = new DateTime(year, month, day, hour, minute, second);
            }
            ClearLineCache();
        }
        catch (Exception)
        {
            if (opt == MISS_OPT.THROW)
                throw;
            else if (opt == MISS_OPT.SETDEFAULT)
                dt = def;
            else if (opt == MISS_OPT.CLEAR_LINE)
                ClearLineCache();
        }
        return dt;
    }

    public static T ParseValueByStr<T>(string str)
    {
        T ret = default(T);
        if (string.IsNullOrEmpty(str))
            return ret;

        if (typeof(T) == typeof(int))
        {
            int tmp = int.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(uint))
        {
            uint tmp = uint.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(short))
        {
            short tmp = short.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(ushort))
        {
            ushort tmp = ushort.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(byte))
        {
            byte tmp = byte.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(sbyte))
        {
            sbyte tmp = sbyte.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(char))
        {
            char tmp = char.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(long))
        {
            long tmp = long.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(ulong))
        {
            ulong tmp = ulong.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(bool))
        {
            bool tmp = bool.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(ulong))
        {
            ulong tmp = ulong.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(float))
        {
            float tmp = float.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(double))
        {
            double tmp = double.Parse(str);
            ret = (T)(object)tmp;
        }
        else if (typeof(T) == typeof(decimal))
        {
            decimal tmp = decimal.Parse(str);
            ret = (T)(object)tmp;
        }

        return ret;
    }
}
