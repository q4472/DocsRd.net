using System;
using System.Collections;
using System.Data;

namespace DocsRd.Models
{
    public class RdModel
    {
        public static DataTable GetFsInfo(String path)
        {
            DataTable dt = Data.Fs.GetFsInfo(path);
            return dt;
        }
        public static void SetFsInfo(Hashtable data)
        {
            Data.Fs.SetFsInfo(data);
        }
    }
}