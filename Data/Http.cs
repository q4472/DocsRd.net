using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace Nskd
{
    public class Http
    {
        // список адресов компъютеров которые могут обмениваться запросами и ответами
        private static String[] trustedComputers = new String[] {
            "::1",
            "127.0.0.1",
            "localhost",
            "192.168.135.77",
            "192.168.135.14",
            "192.168.135.86"
        };
        public static Boolean RequestIsAcceptable(HttpListenerRequest incomingRequest)
        {
            Boolean result = false;
            String address = incomingRequest.RemoteEndPoint.Address.ToString();
            String method = incomingRequest.HttpMethod;
            String host = incomingRequest.Url.Host;
            String path = incomingRequest.Url.AbsolutePath;
            if (AddressIsAcceptable(address) &&
                MethodIsAcceptable(method) &&
                HostIsAcceptable(host) &&
                PathIsAcceptable(path))
            {
                result = true;
            }
            return result;
        }
        private static Boolean AddressIsAcceptable(String remoteEndPointAddress)
        {
            Boolean result = false;
            foreach (String address in trustedComputers)
            {
                if (remoteEndPointAddress == address)
                {
                    result = true;
                }
            }
            return result;
        }
        private static Boolean MethodIsAcceptable(String httpMethod)
        {
            Boolean result = false;
            switch (httpMethod)
            {
                //case "GET":
                //case "HEAD":
                case "POST":
                    result = true;
                    break;
                default:
                    break;
            }
            return result;
        }
        private static Boolean HostIsAcceptable(String host)
        {
            Boolean result = false;
            foreach (String address in trustedComputers)
            {
                if (host == address)
                {
                    result = true;
                }
            }
            return result;
        }
        private static Boolean PathIsAcceptable(String path)
        {
            Boolean result = false;
            // Приводим к изначальному виду, преобразуя экранированные символы.
            // Например, "%20" -> " ".
            path = Uri.UnescapeDataString(path);
            // Если в строке нет двоеточия, то годится.
            // Это нужно для защиты от path типа "/../../file.txt"
            if (path.IndexOf("..") < 0)
            {
                result = true;
            }
            return result;
        }
    }

    public class RequestPackage
    {
        public Guid SessionId;
        public String Command;
        public RequestParameter[] Parameters;

        public static RequestPackage ParseRequest(Stream inputStream, Encoding contentEncoding)
        {
            RequestPackage rqp = null;
            String body = null;
            inputStream.Position = 0;
            using (StreamReader sr = new StreamReader(inputStream, contentEncoding))
            {
                body = sr.ReadToEnd();
            }
            if (!String.IsNullOrWhiteSpace(body))
            {
                switch (body[0])
                {
                    case '{':
                        rqp = RequestPackage.ParseJson(body);
                        break;
                    case '<':
                        rqp = RequestPackage.ParseXml(body);
                        break;
                    default: // Parse 'GoToTheNewPage' - старая пересылка, созданная когда ещё небыло RequestPackage.
                        // sessionId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
                        if (body[0] == 's' && body.Length == 46)
                        {
                            if (Guid.TryParse(body.Substring(10, 36), out Guid sessionId))
                            {
                                rqp = new RequestPackage
                                {
                                    SessionId = sessionId
                                };
                            }
                        }
                        break;
                }
            }
            return rqp;
        }
        public static RequestPackage ParseXml(String xmlString)
        {
            RequestPackage rqp = null;
            Encoding enc = (xmlString.Contains("utf-8")) ? Encoding.UTF8 : Encoding.Unicode;
            try
            {
                Byte[] buff = enc.GetBytes(xmlString);
                using (MemoryStream ms = new MemoryStream(buff))
                {
                    using (StreamReader sr = new StreamReader(ms, enc))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(RequestPackage));
                        rqp = (RequestPackage)xs.Deserialize(sr);
                    }
                }
            }
            catch (Exception) { }
            return rqp;
        }
        public static RequestPackage ParseJson(String jsonString)
        {
            RequestPackage rqp = null;
            Object temp = Nskd.JsonV2.Parse(jsonString);
            String type = temp.GetType().ToString();
            if (type == "System.Collections.Hashtable")
            {
                Hashtable ht = (Hashtable)temp;
                rqp = new RequestPackage();
                if (ht.ContainsKey("SessionId"))
                {
                    Guid sessionId;
                    if (Guid.TryParse(ht["SessionId"] as String, out sessionId))
                    {
                        rqp.SessionId = sessionId;
                    }
                }
                if (ht.ContainsKey("Command"))
                {
                    rqp.Command = ht["Command"] as String;
                }
                if (ht.ContainsKey("Parameters"))
                {
                    Object[] parameters = ht["Parameters"] as Object[];
                    if (parameters != null)
                    {
                        rqp.Parameters = new RequestParameter[0];
                        foreach (Object parameter in parameters)
                        {
                            Hashtable par = parameter as Hashtable;
                            if (par != null && par.ContainsKey("Name") && par.ContainsKey("Value"))
                            {
                                RequestParameter p = new RequestParameter(par["Name"] as String, par["Value"]);
                                Array.Resize<RequestParameter>(ref rqp.Parameters, rqp.Parameters.Length + 1);
                                rqp.Parameters[rqp.Parameters.Length - 1] = p;
                            }
                        }
                    }
                }
            }
            else
            {
                //Log.Write("DeserializeJsonStringToObject: Can not deserialize '" + type + "'.");
            }
            return rqp;
        }
        public Object this[String key]
        {
            get
            {
                Object value = null;
                if (key != null && Parameters != null)
                {
                    foreach (var p in Parameters)
                    {
                        if (p != null && p.Name == key)
                        {
                            value = p.Value;
                            break;
                        }
                    }
                }
                return value;
            }
            set
            {
                if (!String.IsNullOrWhiteSpace(key))
                {
                    if (Parameters == null)
                    {
                        Parameters = new RequestParameter[] { new RequestParameter() { Name = key, Value = value } };
                    }
                    else
                    {
                        Boolean exists = false;
                        foreach (var p in Parameters)
                        {
                            if (p != null && p.Name == key)
                            {
                                exists = true;
                                p.Value = value;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            Array.Resize<RequestParameter>(ref Parameters, Parameters.Length + 1);
                            Parameters[Parameters.Length - 1] = new RequestParameter() { Name = key, Value = value };
                        }
                    }
                }
            }
        }
        public Byte[] ToJson(Encoding enc)
        {
            Byte[] buff = null;
            String str = Nskd.JsonV2.ToString(this);
            buff = enc.GetBytes(str);
            return buff;
        }
        public Byte[] ToXml(Encoding enc)
        {
            Byte[] buff = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (TextWriter tw = new StreamWriter(ms, enc))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(RequestPackage));
                    sr.Serialize(tw, this);
                    buff = ms.ToArray();
                }
            }
            return buff;
        }
        public ResponsePackage GetResponse(String uri)
        {
            ResponsePackage rsp = new ResponsePackage();
            try
            {
                WebRequest wrq = WebRequest.Create(uri);
                wrq.Method = "POST";
                Byte[] buff = this.ToXml(Encoding.UTF8);
                //Byte[] buff = this.ToJson(Encoding.UTF8);
                Stream s = wrq.GetRequestStream();
                s.Write(buff, 0, buff.Length);
                s.Close();

                WebResponse wrs = wrq.GetResponse();
                StreamReader sr = new StreamReader(wrs.GetResponseStream(), Encoding.UTF8);
                String body = sr.ReadToEnd();
                sr.Close();

                if (!String.IsNullOrWhiteSpace(body))
                {
                    switch (body[0])
                    {
                        case '{':
                            rsp = ResponsePackage.ParseJson(body);
                            break;
                        case '<':
                            rsp = ResponsePackage.ParseXml(body);
                            break;
                        default:
                            rsp.Status = String.Format("Nskd.RequestPackage.GetResponse(): Response body format error: '{0}'", body);
                            break;
                    }
                }
                else
                {
                    rsp.Status = "Nskd.RequestPackage.GetResponse(): Response body is null or white space.";
                }
            }
            catch (Exception e)
            {
                rsp.Status = String.Format("Nskd.RequestPackage.GetResponse(): {0}", e.Message);
            }
            return rsp;
        }
        public void AddSessionIdToParameters()
        {
            this["session_id"] = SessionId;
        }
        public class MdSqlParameter
        {
            public String Name;
            public SqlDbType Type;
        }
        public void ConvertParametersToSqlCompatibleType(MdSqlParameter[] mdSqlParameters)
        {
            if (Parameters == null || Parameters.Length == 0) { return; }
            foreach (RequestParameter p in Parameters)
            {
                if (p == null) { continue; }
                if (p.Value == null) { p.Value = DBNull.Value; continue; }
                SqlDbType sqlDbType = SqlDbType.NVarChar;
                Boolean sqlDbTypeIsFound = false;
                foreach (MdSqlParameter f in mdSqlParameters)
                {
                    if (f.Name == p.Name)
                    {
                        sqlDbType = f.Type;
                        sqlDbTypeIsFound = true;
                        break;
                    }
                }
                if (sqlDbTypeIsFound)
                {
                    p.Value = p.ConvertValueToSqlCompatibleType(sqlDbType);
                }
            }
        }
        public void SetDBNullValueForNullOrEmptyOrWhiteSpaceParameterValue()
        {
            if (Parameters == null || Parameters.Length == 0) { return; }
            foreach (RequestParameter p in Parameters)
            {
                if (p.Value == null || (p.Value.GetType() == typeof(String) && String.IsNullOrWhiteSpace((String)p.Value)))
                {
                    p.Value = DBNull.Value;
                }
            }
        }
    }

    [XmlInclude(typeof(DBNull))]
    public class RequestParameter
    {
        private String _name;
        public String Name
        {
            get { return _name; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException();
                }
                _name = value;
            }
        }
        public Object Value { get; set; }
        public RequestParameter() { }
        public RequestParameter(String name, Object value)
        {
            Name = name;
            Value = value;
        }
        public Object ConvertValueToSqlCompatibleType(SqlDbType sqlDbType)
        {
            Object result = DBNull.Value;
            try
            {
                switch (sqlDbType)
                {
                    case SqlDbType.UniqueIdentifier:
                        if (Value.GetType() == typeof(String))
                        {
                            String temp = Value as String;
                            if (String.IsNullOrWhiteSpace(temp))
                            {
                                result = DBNull.Value;
                            }
                            else
                            {
                                if (Guid.TryParse(temp, out Guid guid))
                                {
                                    result = guid;
                                }
                            }
                        }
                        else
                        {
                            result = System.Convert.ChangeType(Value, typeof(Guid));
                        }
                        break;

                    case SqlDbType.Decimal:
                        if (Value.GetType() == typeof(String))
                        {
                            String temp = Value as String;
                            if (String.IsNullOrWhiteSpace(temp))
                            {
                                result = DBNull.Value;
                            }
                            else
                            {
                                temp = temp.Replace(",", ".").Replace(" ", "");
                                result = System.Convert.ChangeType(temp, typeof(Decimal), CultureInfo.InvariantCulture);
                            }
                        }
                        else
                        {
                            result = System.Convert.ChangeType(Value, typeof(Decimal), CultureInfo.InvariantCulture);
                        }
                        break;

                    case SqlDbType.Int:
                        result = System.Convert.ChangeType(Value, typeof(Int32));
                        break;

                    case SqlDbType.NVarChar:

                    default:
                        result = System.Convert.ChangeType(Value, typeof(String));
                        break;
                }
            }
            catch (Exception) { result = DBNull.Value; }
            return result;
        }
    }

    public class ResponsePackage
    {
        public String Status;
        public DataSet Data;
        public DataTable GetFirstTable()
        {
            DataTable firstTable = null;
            if (Data != null && Data.Tables.Count > 0)
            {
                firstTable = Data.Tables[0];
            }
            return firstTable;
        }
        public Object GetScalar()
        {
            Object scalar = null;
            DataTable firstTable = GetFirstTable();
            if (firstTable != null && firstTable.Columns.Count > 0 && firstTable.Rows.Count > 0)
            {
                scalar = firstTable.Rows[0][0];
            }
            return scalar;
        }
        public Byte[] ToXml(Encoding enc)
        {
            Byte[] buff = null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, enc))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(ResponsePackage));
                        xs.Serialize(sw, this);
                    }
                    buff = ms.ToArray();
                }
            }
            catch (Exception) { }
            return buff;
        }
        public static ResponsePackage ParseJson(String str)
        {
            ResponsePackage rsp = new ResponsePackage();
            Object o = Nskd.JsonV2.Parse(str);
            if ((o != null) && (o.GetType() == typeof(Hashtable)))
            {
                Hashtable package = (Hashtable)o;
                if ((package.ContainsKey("Status")) && (package["Status"] != null))
                {
                    rsp.Status = (String)package["Status"];
                }
                if (package.ContainsKey("Data") && (package["Data"] != null))
                {
                    rsp.Data = new DataSet();
                    Hashtable data = (Hashtable)package["Data"];
                    if (data.ContainsKey("tables") && (data["tables"] != null))
                    {
                        Object[] tables = (Object[])data["tables"];
                        for (int ti = 0; ti < tables.Length; ti++)
                        {
                            rsp.Data.Tables.Add((DataTable)tables[ti]);
                        }
                    }
                }
            }
            return rsp;
        }
        public static ResponsePackage ParseXml(String str)
        {
            ResponsePackage rsp = null;
            Encoding enc = (str.Contains("utf-8")) ? Encoding.UTF8 : Encoding.Unicode;
            using (MemoryStream ms = new MemoryStream(enc.GetBytes(str)))
            {
                using (TextReader tr = new StreamReader(ms, enc))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(ResponsePackage));
                    rsp = (ResponsePackage)sr.Deserialize(tr);
                }
            }
            return rsp;
        }
    }

}
