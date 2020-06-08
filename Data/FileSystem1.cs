using HttpDataServerProject5;
using Nskd;
using System;
using System.Collections;
using System.Data;

namespace DocsRd.Data
{
    public class Fs
    {
        private static String dataServicesHost = "127.0.0.1"; // localhost

        public static DataSet GetDirectoryInfo(Guid sessionId, String path)
        {
            DataSet ds = null;
            RequestPackage rqp = new RequestPackage();
            rqp.SessionId = sessionId;
            rqp.Command = "GetDirectoryInfo";
            rqp.Parameters = new RequestParameter[] {
                new RequestParameter("alias", null),
                new RequestParameter("path", path)
            };
            ResponsePackage rsp = ExecuteInFs(rqp);
            ds = rsp.Data;
            return ds;
        }
        public static Byte[] GetFileContents(Guid sessionId, String path = null)
        {
            Byte[] contents = null;
            RequestPackage rqp = new RequestPackage();
            rqp.SessionId = sessionId;
            rqp.Command = "GetFileContents";
            rqp.Parameters = new RequestParameter[2];
            rqp.Parameters[0] = new RequestParameter("alias", "docs_rd");
            rqp.Parameters[1] = new RequestParameter("path", path);
            ResponsePackage rsp = ExecuteInFs(rqp);
            if (rsp.Data != null)
            {
                if (rsp.Data.Tables.Count > 0)
                {
                    DataTable dt = rsp.Data.Tables[0];
                    if (dt.Columns.Count > 0)
                    {
                        if ((dt.Columns[0].ColumnName == "contents") && (dt.Columns[0].DataType == typeof(String)))
                        {
                            if (dt.Rows.Count > 0)
                            {
                                String base64String = dt.Rows[0][0] as String;
                                if (!String.IsNullOrWhiteSpace(base64String))
                                {
                                    contents = System.Convert.FromBase64String(base64String);
                                }
                            }
                        }
                    }
                }
            }
            return contents;
        }
        public static ResponsePackage ExecuteInFs(RequestPackage rqp)
        {
#if DEBUG
            ResponsePackage rsp = CommandSwitcher.Exec(rqp);
#else
            ResponsePackage rsp = rqp.GetResponse("http://" + dataServicesHost + ":11005/");
#endif
            return rsp;
        }
        public static DataSet Execute(RequestPackage rqp)
        {
            ResponsePackage rsp = rqp.GetResponse("http://" + dataServicesHost + ":11012/");
            return rsp.Data;
        }
        public static DataTable GetFirstTable(DataSet ds)
        {
            DataTable dt = null;
            if ((ds != null) && (ds.Tables.Count > 0))
            {
                dt = ds.Tables[0];
            }
            return dt;
        }
        public static Object GetScalar(DataSet ds)
        {
            Object r = null;
            DataTable dt = GetFirstTable(ds);
            if (dt != null && dt.Rows.Count > 0 && dt.Columns.Count >= 0)
            {
                r = dt.Rows[0][0];
            }
            return r;
        }
        public static DataTable GetFsInfo(String path)
        {
            RequestPackage rqp = new RequestPackage()
            {
                Command = "[dbo].[рег_уд__файлы__get]",
                Parameters = new RequestParameter[] {
                    new RequestParameter("path", path )
                }
            };
            DataTable dt = GetFirstTable(Execute(rqp));
            return dt;
        }
        public static void SetFsInfo(Hashtable data)
        {
            RequestPackage rqp = new RequestPackage()
            {
                Command = "[dbo].[рег_уд__файлы__set]",
                Parameters = new RequestParameter[] {
                    new RequestParameter("path", data.ContainsKey("path") ? data["path"] : null),
                    new RequestParameter("номер", data.ContainsKey("номер") ? data["номер"] : null),
                    new RequestParameter("дата_регистрации", data.ContainsKey("дата_регистрации") ? data["дата_регистрации"] : null),
                    new RequestParameter("дата_перерегистрации", data.ContainsKey("дата_перерегистрации") ? data["дата_перерегистрации"] : null),
                    new RequestParameter("дата_окончания", data.ContainsKey("дата_окончания") ? data["дата_окончания"] : null),
                    new RequestParameter("комментарий", data.ContainsKey("комментарий") ? data["комментарий"] : null)
                }
            };
            Execute(rqp);
        }
    }
}