
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

    
public enum FILE_TYPE
{
    BINARY = 0,
    TEXT,        
}

public enum OPEN_MODE
{
    OPEN_READ,
    OPEN_WRITE,
    OPEN_WRITE_CREATE,
    OPEN_APPEND,
}

public abstract class LFile   
{
    public string FilePath { get; set; }
    public Encoding CurEncoding { get; set; }
    protected bool mOpened = false;
    protected Stream mStream = null;        

    public LFile(FILE_TYPE ftype)
    {
        CurEncoding = Encoding.Default;
    }

    public LFile(FILE_TYPE ftype, System.Text.Encoding encode)
    {
        CurEncoding = encode;
    }

    public virtual bool Open(string path, OPEN_MODE mode)
    {
        if (mOpened)
            Close();

        FileMode filemode = FileMode.Open;
        if(mode == OPEN_MODE.OPEN_WRITE)
        {
            filemode = FileMode.OpenOrCreate;
        }
        else if(mode == OPEN_MODE.OPEN_WRITE_CREATE)
        {
            filemode = FileMode.Create;
        }
        else if(mode == OPEN_MODE.OPEN_APPEND)
        {
            filemode = FileMode.Append;
        }

        FileAccess access = (mode == OPEN_MODE.OPEN_READ) ? FileAccess.Read : FileAccess.ReadWrite;
        if (filemode == FileMode.Append) access = FileAccess.Write;

        if (access == FileAccess.Read)
        {
            // read, check exist
            if (!File.Exists(path))
            {
                return false;
            }                
        }
        else
        {
            // write or create
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        try
        {
            mStream = new FileStream(path, filemode, access, FileShare.ReadWrite);
            if (mStream == null)
                return false;

            FilePath = path;
            mOpened = true;
        }
        catch (Exception ex)
        {
                
        }

        return true;
    }

    public virtual bool OpenMem(byte[] buf, OPEN_MODE mode)
    {
        if (mOpened)
            Close();

        if(null == buf)
        {
            return false;
        }

        mStream = new MemoryStream(buf, mode == OPEN_MODE.OPEN_READ ? false : true);
        if (mStream == null)
            return false;

        mOpened = true;

        return true;
    }

    public virtual bool OpenRead(string path)
    {
        return Open(path, OPEN_MODE.OPEN_READ);
    }

    public virtual bool OpenWrite(string path,OPEN_MODE mode = OPEN_MODE.OPEN_WRITE)
    {
        return Open(path, mode);
    }

    public virtual bool OpenRead(byte[] mem)
    {
        return OpenMem(mem, OPEN_MODE.OPEN_READ);
    }

    public virtual bool OpenWriter(byte[] mem)
    {
        return OpenMem(mem, OPEN_MODE.OPEN_WRITE);
    }

    public virtual void Close()
    {
        if (!mOpened)
            return;

        if (mStream != null)
	    {
		    mStream.Close();
            mStream = null;
	    }

        mOpened = false;
        FilePath = null;
    }
}

public class LBinaryFile : LFile
{
    public BinaryReader Reader { get; set; }
    public BinaryWriter Writer { get; set; }

    public LBinaryFile() 
        : base(FILE_TYPE.BINARY)
    {

    }

    public LBinaryFile(Encoding encode) 
        : base(FILE_TYPE.BINARY, encode)
    {

    }

    public override bool Open(string path, OPEN_MODE mode)
    {
        if (!base.Open(path, mode))
            return false;

        if (mode == OPEN_MODE.OPEN_READ)
        {
            Reader = new BinaryReader(mStream, CurEncoding);
        }
        else
        {
            Writer = new BinaryWriter(mStream, CurEncoding);
        }

        return true;
    }

    public override bool OpenMem(byte[] buf, OPEN_MODE mode)
    {
        if (!base.OpenMem(buf, mode))
            return false;

        if (mode == OPEN_MODE.OPEN_READ)
        {
            Reader = new BinaryReader(mStream, CurEncoding);
        }
        else
        {
            Writer = new BinaryWriter(mStream, CurEncoding);
        }

        return true;
    }

    public override void Close()
    {
        if (Writer != null)
        {
            Writer.Close();
        }

        if (Reader != null)
        {
            Reader.Close();
        }

        base.Close();
    }
}

public class LTextFile : LFile
{
    public StreamReader Reader { get; set; }
    public StreamWriter Writer { get; set; }

    public LTextFile() 
        : base(FILE_TYPE.TEXT)
    {

    }

    public LTextFile(Encoding encode)
        : base(FILE_TYPE.TEXT, encode)
    {

    }

    public override bool Open(string path, OPEN_MODE mode)
    {
        if (!base.Open(path, mode))
            return false;

        if (mode == OPEN_MODE.OPEN_READ)
        {
            Reader = new StreamReader(mStream, CurEncoding);
        }
        else
        {
            Writer = new StreamWriter(mStream, CurEncoding);
        }

        return true;
    }

    public override bool OpenMem(byte[] buf, OPEN_MODE mode)
    {
        if (!base.OpenMem(buf, mode))
            return false;

        if (mode == OPEN_MODE.OPEN_READ)
        {
            Reader = new StreamReader(mStream, CurEncoding);
        }
        else
        {
            Writer = new StreamWriter(mStream, CurEncoding);
        }

        return true;
    }

    public override void Close()
    {
        if (Writer != null)
        {
            Writer.Close();
        }

        if (Reader != null)
        {
            Reader.Close();
        }

        base.Close();
    }

    public void WriteLine(string format)
    {
        Writer.WriteLine(format);
    }

    public string ReadLine(string format = null)
    {
        string result = "";
        try
        {
            result = Reader.ReadLine();
        }
        catch (Exception)
        {
            result = "";
        }

        if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(format))
        {
            result = result.Replace(format, "");
        }

        return result;
    }
}

