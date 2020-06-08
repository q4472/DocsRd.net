using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Nskd
{
    public static class Json
    {
        private static CultureInfo ic = CultureInfo.InvariantCulture;

        private class Reader
        {
            private string jsonString { get; set; }
            private int len { get; set; }
            private int cpi { get; set; }
            private object v { get; set; }

            public Reader(string jsonString)
            {
                this.jsonString = jsonString;
                this.len = jsonString.Length;
                this.cpi = 0;
                this.v = ReadValue();
            }

            public object GetResult()
            {
                return this.v;
            }

            private object ReadValue()
            {
                object o = null;
                SkipWhiteSpace();
                char ch = jsonString[cpi];
                switch (ch)
                {
                    case '"':
                        o = ReadString();
                        /*
                        if (((string)o).Length > 0 && ((string)o)[0] == '\\')
                        {
                            o = ReadDate((string)o);
                        }
                        */
                        break;
                    case 'f':
                        if (jsonString.Substring(cpi, 5) == "false")
                        {
                            cpi += 5;
                            o = false;
                        }
                        break;
                    case 't':
                        if (jsonString.Substring(cpi, 4) == "true")
                        {
                            cpi += 4;
                            o = true;
                        }
                        break;
                    case 'n':
                        if (jsonString.Substring(cpi, 4) == "null")
                        {
                            cpi += 4;
                            o = null;
                        }
                        else if (jsonString.Substring(cpi, 9) == "new Date(")
                        {
                            cpi += 9;
                            o = ReadDate();
                        }
                        break;
                    case '{':
                        o = ReadObject();
                        break;
                    case '[':
                        o = ReadArray();
                        break;
                    default:
                        if (ch == '-' || char.IsDigit(ch))
                        {
                            o = ReadNumber();
                        }
                        break;
                }
                return o;
            }

            private object ReadNumber()
            {
                object o = null;
                int spi = cpi;
                cpi++; // пропуск '-' или первой цифры
                while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                if (cpi >= len || jsonString[cpi] != '.')
                {
                    long temp;
                    if (long.TryParse(jsonString.Substring(spi, (cpi - spi)), out temp))
                        if ((int.MinValue <= temp) && (temp <= int.MaxValue)) o = (int)temp;
                        else o = temp;
                }
                else // '.'
                {
                    cpi++; // пропуск '.'
                    if (char.IsDigit(jsonString[cpi])) // после точки должна быть цифра
                    {
                        while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                        char ch = jsonString[cpi];
                        if ((ch == 'e') || (ch == 'E')) // есть степень
                        {
                            cpi++; // пропуск 'e'
                            ch = jsonString[cpi];
                            if ((ch == '+') || (ch == '-'))
                            {
                                cpi++; // пропуск знака
                                if (char.IsDigit(jsonString[cpi])) // после знака должна быть цифра
                                {
                                    while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                                }
                            }
                            double temp;
                            if (double.TryParse(jsonString.Substring(spi, (cpi - spi)), NumberStyles.Float, ic, out temp))
                                o = temp;
                        }
                        else
                        { // нет степени
                            decimal temp;
                            if (decimal.TryParse(jsonString.Substring(spi, (cpi - spi)), NumberStyles.Number, ic, out temp))
                                o = temp;
                        }
                    }
                }
                return o;
            }

            private object ReadDate()
            {
                long t = (long)ReadNumber(); // UTC Ticks
                cpi += 1; // ")"
                t = (t + 62135596800000) * 10000;
                t = t + DateTimeOffset.Now.Offset.Ticks; // local Ticks
                return new DateTime(t, DateTimeKind.Local);
            }

            private object ReadString()
            {
                StringBuilder sb = new StringBuilder();
                int spi = cpi;
                cpi++; // пропускаем '"'
                while (cpi < len)
                {
                    char ch = jsonString[cpi];
                    if (ch == '\\')
                    { // после \ только ["\/bfnrt] или u0000
                        cpi++;
                        ch = jsonString[cpi];
                        if (ch == 'u')
                        {
                            sb.Append((char)int.Parse(jsonString.Substring(cpi + 1, 4), NumberStyles.HexNumber));
                            cpi += 5;
                        }
                        else
                        {
                            if (ch == '"') sb.Append('"');
                            if (ch == '\\') sb.Append('\\');
                            if (ch == '/') sb.Append('/');
                            if (ch == 'b') sb.Append('\b');
                            if (ch == 'f') sb.Append('\f');
                            if (ch == 'n') sb.Append('\n');
                            if (ch == 'r') sb.Append('\r');
                            if (ch == 't') sb.Append('\t');
                            cpi++;
                        }
                        continue;
                    }
                    if (ch == '"') break;
                    else
                    {
                        sb.Append(ch);
                        cpi++;
                    }
                }
                cpi++;  // пропускаем '"'
                return sb.ToString();
            }

            private object ReadObject()
            {
                object result = null;
                Hashtable o = new Hashtable();
                cpi++; // пропустить "{"
                SkipWhiteSpace();
                if (cpi < len && jsonString[cpi] != '}')
                {
                    do
                    {
                        SkipWhiteSpace();
                        string key = (string)ReadString();
                        SkipWhiteSpace();
                        cpi++; // пропустить ":"
                        SkipWhiteSpace();
                        object value = ReadValue();
                        SkipWhiteSpace();
                        o.Add(key, value);
                        // проверить и пропустить "," или "}"
                    } while (cpi < len && jsonString[cpi++] == ',');
                }
                else
                {
                    cpi++; // пропустить "}"
                }

                if ((o.ContainsKey("__type")) && ((o["__type"] as string) == "Nskd.Data.DataTable"))
                {
                    result = ToDataTable(o);
                }
                else { result = o; }

                return result;
            }

            private object ReadArray()
            {
                ArrayList a = new ArrayList();
                cpi++; // пропуск "["
                SkipWhiteSpace();
                if (jsonString[cpi] != ']')
                {
                    do
                    {
                        a.Add(ReadValue());
                        SkipWhiteSpace();
                    } while (jsonString[cpi++] == ','); // проверка и пропуск "," или "]"
                }
                else cpi++; // пропуск "]"
                //throw new Exception("Nskd.Json.Json.Reader.ReadArray(): " + a.ToArray().Length.ToString());
                return a.ToArray();
            }

            private void SkipWhiteSpace()
            {
                while (cpi < len && char.IsWhiteSpace(jsonString[cpi])) cpi++;
            }

            private static DataTable ToDataTable(Hashtable o)
            {
                DataTable dt = new DataTable();
                dt.TableName = o["tableName"] as string;
                Array cols = o["columns"] as Array;
                foreach (object col in cols)
                {
                    Hashtable jcol = col as Hashtable;
                    string columnName = jcol["columnName"] as string;
                    string dataType = jcol["dataType"] as string;
                    DataColumn dc = new DataColumn(columnName, Type.GetType(dataType));
                    dt.Columns.Add(dc);
                }
                Array rows = o["rows"] as Array;
                foreach (object r in rows)
                {
                    Array row = r as Array;
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < row.Length; i++)
                    {
                        object cellValue = row.GetValue(i);
                        if (cellValue == null) { dr[i] = DBNull.Value; }
                        else
                        {
                            try
                            {
                                switch (dt.Columns[i].DataType.ToString())
                                {
                                    case "System.Guid":
                                        dr[i] = new System.Guid(cellValue as String);
                                        break;
                                    case "System.Byte[]":
                                        Object[] bs = cellValue as Object[];
                                        Byte[] buff = new Byte[bs.Length];
                                        for (int j = 0; j < bs.Length; j++) { buff[j] = System.Convert.ToByte(bs[j]); }
                                        dr[i] = buff;
                                        break;
                                    default:
                                        dr[i] = System.Convert.ChangeType(cellValue, dt.Columns[i].DataType, ic);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                dt.ExtendedProperties.Add(
                                    dt.ExtendedProperties.Count + " Nskd.Json.ToDataTable() error message: ",
                                    ex.Message);
                            }
                        }
                    }
                    dt.Rows.Add(dr);
                }
                return dt;
            }
        }

        private class Writer
        {
            private StringBuilder sb;

            public Writer(object v)
            {
                sb = new StringBuilder();
                WriteValue(v);
            }

            public string GetResult()
            {
                return sb.ToString();
            }

            private void WriteValue(object v)
            {
                if (v == null || v == DBNull.Value)
                {
                    sb.Append("null");
                }
                else if (v is string || v is Guid)
                {
                    WriteString(v.ToString());
                }
                else if (v is bool)
                {
                    sb.Append(v.ToString().ToLower());
                }
                else if (v is double ||
                    v is float ||
                    v is long ||
                    v is int ||
                    v is short ||
                    v is byte ||
                    v is decimal)
                {
                    sb.AppendFormat(ic, "{0}", v);
                }
                else if (v.GetType().IsEnum)
                {
                    sb.Append((int)v);
                }
                else if (v is DateTime)
                {
                    DateTime d = (DateTime)v;
                    long t = new DateTimeOffset(d).UtcTicks;
                    t = t / 10000 - 62135596800000;
                    sb.AppendFormat("new Date({0})", t);
                }
                else if (v is DataSet)
                {
                    WriteDataSet(v as DataSet);
                }
                else if (v is DataTable)
                {
                    WriteDataTable(v as DataTable);
                }
                else if (v is IEnumerable)
                {
                    WriteEnumerable(v as IEnumerable);
                }
                else
                {
                    //sb.Append("\"" + v.GetType().ToString() + "\"");
                    WriteObject(v);
                }
            }

            private void WriteString(string s)
            {
                sb.Append("\"");
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            int i = (int)c;
                            if ((i >= 0x20 && i < 0x80) || (i >= 0x400 && i < 0x460))
                            {
                                sb.Append(c);
                            }
                            else
                            {
                                sb.AppendFormat("\\u{0:X04}", i);
                            }
                            break;
                    }
                }
                sb.Append("\"");
            }

            private void WriteDataSet(DataSet ds)
            {
                sb.Append("{\"__type\":\"Nskd.Data.DataSet\",");
                sb.Append("\"tables\":[");
                if (ds.Tables.Count > 0)
                {
                    foreach (DataTable dt in ds.Tables)
                    {
                        WriteDataTable(dt);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("],");
                sb.Append("\"relations\":[");
                if (ds.Relations.Count > 0)
                {
                    foreach (DataRelation r in ds.Relations)
                    {
                        WriteDataRelation(r);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]}");
            }

            private void WriteDataRelation(DataRelation r)
            {
                sb.Append("{");
                sb.Append("\"parentTable\": " + r.DataSet.Tables.IndexOf(r.ParentTable) + ",");
                sb.Append("\"parentColumn\": " + r.ParentTable.Columns.IndexOf(r.ParentColumns[0]) + ",");
                sb.Append("\"childTable\": " + r.DataSet.Tables.IndexOf(r.ChildTable) + ",");
                sb.Append("\"childColumn\": " + r.ChildTable.Columns.IndexOf(r.ChildColumns[0]) + "");
                sb.Append("}");
            }

            private void WriteDataTable(DataTable dt)
            {
                sb.Append("{\"__type\":\"Nskd.Data.DataTable\",");
                sb.AppendFormat("\"tableName\":\"{0}\",", dt.TableName);
                sb.Append("\"columns\":[");
                if (dt.Columns.Count > 0)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        WriteDataColumn(dc);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("],");

                if (dt.PrimaryKey.Length > 0)
                {
                    sb.Append("\"primaryKey\":[");
                    foreach (DataColumn dc in dt.PrimaryKey)
                    {
                        sb.Append(dc.Ordinal);
                        sb.Append(",");
                    }
                    --sb.Length;
                    sb.Append("],");
                }

                if (dt.ExtendedProperties.Keys.Count > 0)
                {
                    sb.Append("\"extendedProperties\":{");
                    foreach (string key in dt.ExtendedProperties.Keys)
                    {
                        sb.Append("\"" + key + "\":");
                        WriteValue(dt.ExtendedProperties[key]);
                        sb.Append(",");
                    }
                    --sb.Length;
                    sb.Append("},");
                }

                sb.Append("\"rows\":[");
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        WriteDataRow(dr);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]");
                sb.Append("}");
            }

            private void WriteDataColumn(DataColumn dc)
            {
                sb.Append("{");
                {
                    sb.Append("\"columnName\":");
                    WriteValue(dc.ColumnName);
                    sb.Append(",");

                    if (dc.Caption != dc.ColumnName)
                    {
                        sb.Append("\"caption\":\"");
                        sb.Append(dc.Caption);
                        sb.Append("\",");
                    }

                    sb.Append("\"dataType\":\"");
                    sb.Append(dc.DataType.ToString());
                    sb.Append("\",");

                    if (dc.ExtendedProperties.Keys.Count > 0)
                    {
                        sb.Append("\"extendedProperties\":{");
                        foreach (string key in dc.ExtendedProperties.Keys)
                        {
                            sb.Append("\"" + key + "\":\"");
                            sb.Append(dc.ExtendedProperties[key]);
                            sb.Append("\",");
                        }
                        --sb.Length;
                        sb.Append("}");
                    }
                    else { --sb.Length; }
                }
                sb.Append("}");
            }

            private void WriteDataRow(DataRow dr)
            {
                sb.Append("[");
                if (dr.Table.Columns.Count > 0)
                {
                    foreach (DataColumn dc in dr.Table.Columns)
                    {
                        WriteValue(dr[dc]);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]");
            }

            private void WriteEnumerable(IEnumerable e)
            {
                bool hasItems = false;
                sb.Append("[");
                foreach (object v in e)
                {
                    WriteValue(v);
                    sb.Append(",");
                    hasItems = true;
                }
                if (hasItems) --sb.Length;
                sb.Append("]");
            }

            private void WriteObject(object o)
            {
                MemberInfo[] members = o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public);
                sb.Append("{");
                bool hasMembers = false;
                foreach (MemberInfo member in members)
                {
                    bool hasValue = false;
                    object v = null;
                    if ((member.MemberType & MemberTypes.Field) == MemberTypes.Field)
                    {
                        FieldInfo field = (FieldInfo)member;
                        v = field.GetValue(o);
                        hasValue = true;
                    }
                    else if ((member.MemberType & MemberTypes.Property) == MemberTypes.Property)
                    {
                        PropertyInfo property = (PropertyInfo)member;
                        if (property.CanRead && property.GetIndexParameters().Length == 0)
                        {
                            v = property.GetValue(o, null);
                            hasValue = true;
                        }
                    }
                    if (hasValue)
                    {
                        sb.Append("\"");
                        sb.Append(member.Name);
                        sb.Append("\":");
                        WriteValue(v);
                        sb.Append(",");
                        hasMembers = true;

                    }
                }
                if (hasMembers) { --sb.Length; }
                sb.Append("}");
            }
        }

        public static object Parse(string jsonString)
        {
            Reader r = new Reader(jsonString);
            return r.GetResult();
        }

        public static string ToString(object value)
        {
            Writer w = new Writer(value);
            return w.GetResult();
        }
    }
    public static class JsonV2
    {
        private static CultureInfo ic = CultureInfo.InvariantCulture;
        private class ObjectLink
        {
            Int32 Id { get; set; }
            String Name { get; set; }
        }
        private class ObjectNameList
        {
            private ArrayList list;
            public ObjectNameList()
            {
                list = new ArrayList();
            }
        }
        private class Reader
        {
            private string jsonString { get; set; }
            private int len { get; set; }
            private int cpi { get; set; }
            private object v { get; set; }
            public Reader(string jsonString)
            {
                this.jsonString = jsonString;
                this.len = jsonString.Length;
                this.cpi = 0;
                this.v = ReadValue();
            }
            public object GetResult()
            {
                return this.v;
            }
            private object ReadValue()
            {
                object o = null;
                SkipWhiteSpace();
                char ch = jsonString[cpi];
                switch (ch)
                {
                    case '"':
                        o = ReadString();
                        /*
                        if (((string)o).Length > 0 && ((string)o)[0] == '\\')
                        {
                            o = ReadDate((string)o);
                        }
                        */
                        break;
                    case 'f':
                        if (jsonString.Substring(cpi, 5) == "false")
                        {
                            cpi += 5;
                            o = false;
                        }
                        break;
                    case 't':
                        if (jsonString.Substring(cpi, 4) == "true")
                        {
                            cpi += 4;
                            o = true;
                        }
                        break;
                    case 'n':
                        if (jsonString.Substring(cpi, 4) == "null")
                        {
                            cpi += 4;
                            o = null;
                        }
                        else if (jsonString.Substring(cpi, 9) == "new Date(")
                        {
                            cpi += 9;
                            o = ReadDate();
                        }
                        break;
                    case '{':
                        Hashtable ht = ReadObject();
                        o = ConvertToType(ht);
                        break;
                    case '[':
                        o = ReadArray();
                        break;
                    default:
                        if (ch == '-' || char.IsDigit(ch))
                        {
                            o = ReadNumber();
                        }
                        break;
                }
                return o;
            }
            private object ReadNumber()
            {
                object o = null;
                int spi = cpi;
                cpi++; // пропуск '-' или первой цифры
                while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                if (cpi >= len || jsonString[cpi] != '.')
                {
                    long temp;
                    if (long.TryParse(jsonString.Substring(spi, (cpi - spi)), out temp))
                        if ((int.MinValue <= temp) && (temp <= int.MaxValue)) o = (int)temp;
                        else o = temp;
                }
                else // '.'
                {
                    cpi++; // пропуск '.'
                    if (char.IsDigit(jsonString[cpi])) // после точки должна быть цифра
                    {
                        while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                        char ch = jsonString[cpi];
                        if ((ch == 'e') || (ch == 'E')) // есть степень
                        {
                            cpi++; // пропуск 'e'
                            ch = jsonString[cpi];
                            if ((ch == '+') || (ch == '-'))
                            {
                                cpi++; // пропуск знака
                                if (char.IsDigit(jsonString[cpi])) // после знака должна быть цифра
                                {
                                    while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                                }
                            }
                            double temp;
                            if (double.TryParse(jsonString.Substring(spi, (cpi - spi)), NumberStyles.Float, ic, out temp))
                                o = temp;
                        }
                        else
                        { // нет степени
                            decimal temp;
                            if (decimal.TryParse(jsonString.Substring(spi, (cpi - spi)), NumberStyles.Number, ic, out temp))
                                o = temp;
                        }
                    }
                }
                return o;
            }
            private object ReadDate()
            {
                long t = (long)ReadNumber(); // UTC Ticks
                cpi += 1; // ")"
                t = (t + 62135596800000) * 10000;
                t = t + DateTimeOffset.Now.Offset.Ticks; // local Ticks
                return new DateTime(t, DateTimeKind.Local);
            }
            private object ReadString()
            {
                StringBuilder sb = new StringBuilder();
                int spi = cpi;
                cpi++; // пропускаем '"'
                while (cpi < len)
                {
                    char ch = jsonString[cpi];
                    if (ch == '\\')
                    { // после \ только ["\/bfnrt] или u0000
                        cpi++;
                        ch = jsonString[cpi];
                        if (ch == 'u')
                        {
                            sb.Append((char)int.Parse(jsonString.Substring(cpi + 1, 4), NumberStyles.HexNumber));
                            cpi += 5;
                        }
                        else
                        {
                            if (ch == '"') sb.Append('"');
                            if (ch == '\\') sb.Append('\\');
                            if (ch == '/') sb.Append('/');
                            if (ch == 'b') sb.Append('\b');
                            if (ch == 'f') sb.Append('\f');
                            if (ch == 'n') sb.Append('\n');
                            if (ch == 'r') sb.Append('\r');
                            if (ch == 't') sb.Append('\t');
                            cpi++;
                        }
                        continue;
                    }
                    if (ch == '"') break;
                    else
                    {
                        sb.Append(ch);
                        cpi++;
                    }
                }
                cpi++;  // пропускаем '"'
                return sb.ToString();
            }
            private Hashtable ReadObject()
            {
                Hashtable ht = new Hashtable();
                cpi++; // пропустить "{"
                SkipWhiteSpace();
                if (cpi < len && jsonString[cpi] != '}')
                {
                    do
                    {
                        SkipWhiteSpace();
                        string key = (string)ReadString();
                        SkipWhiteSpace();
                        cpi++; // пропустить ":"
                        SkipWhiteSpace();
                        object value = ReadValue();
                        SkipWhiteSpace();
                        ht.Add(key, value);
                        // проверить и пропустить "," или "}"
                    } while (cpi < len && jsonString[cpi++] == ',');
                }
                else
                {
                    cpi++; // пропустить "}"
                }
                return ht;
            }
            private object ConvertToType(Hashtable ht)
            {
                object result = ht;
                if (ht != null && ht.ContainsKey("__type"))
                {
                    string type = ht["__type"] as string;
                    switch (type)
                    {
                        case "Nskd.Data.DataSet":
                            result = ToDataSet(ht);
                            break;
                        case "Nskd.Data.DataTable":
                            result = ToDataTable(ht);
                            break;
                        default:
                            break;
                    }
                }
                return result;
            }
            private object ReadArray()
            {
                ArrayList a = new ArrayList();
                cpi++; // пропуск "["
                SkipWhiteSpace();
                if (jsonString[cpi] != ']')
                {
                    do
                    {
                        a.Add(ReadValue());
                        SkipWhiteSpace();
                    } while (jsonString[cpi++] == ','); // проверка и пропуск "," или "]"
                }
                else cpi++; // пропуск "]"
                //throw new Exception("Nskd.Json.Json.Reader.ReadArray(): " + a.ToArray().Length.ToString());
                return a.ToArray();
            }
            private void SkipWhiteSpace()
            {
                while (cpi < len && char.IsWhiteSpace(jsonString[cpi])) cpi++;
            }
            private static DataSet ToDataSet(Hashtable set)
            {
                DataSet ds = new DataSet();
                if (set != null)
                {
                    if (set.ContainsKey("tables"))
                    {
                        Array tables = set["tables"] as Array;
                        foreach (object table in tables)
                        {
                            ds.Tables.Add(table as DataTable);
                        }
                    }
                    if (set.ContainsKey("relations"))
                    {
                        //ds.Relations = ...
                    }
                }
                return ds;
            }
            private static DataTable ToDataTable(Hashtable table)
            {
                DataTable dt = new DataTable();
                if (table != null)
                {
                    dt.TableName = table["tableName"] as string;
                    Array cols = table["columns"] as Array;
                    foreach (object col in cols)
                    {
                        Hashtable jcol = col as Hashtable;
                        string columnName = jcol["columnName"] as string;
                        string dataType = jcol["dataType"] as string;

                        Type t = Type.GetType(dataType);
                        if (t == null) t = typeof(Object);

                        DataColumn dc = new DataColumn(columnName, t);
                        dt.Columns.Add(dc);
                    }
                    Array rows = table["rows"] as Array;
                    foreach (object row in rows)
                    {
                        AddDataRowFromHashtableRow(dt, row as Hashtable);
                    }
                }
                return dt;
            }
            private static void AddDataRowFromHashtableRow(DataTable dt, Hashtable row)
            {
                DataRow dr = dt.NewRow();
                dt.Rows.Add(dr);
                if (row != null && row.ContainsKey("itemArray"))
                {
                    if (row.ContainsKey("rowState"))
                    {
                        String rowState = row["rowState"] as String;
                        switch (rowState)
                        {
                            case "Added":
                                //dr.SetAdded();
                                break;
                            case "Deleted":
                                //dr.SetDeleted();
                                break;
                            case "Detached":
                                //dr.SetDetached();
                                break;
                            case "Modified":
                                dr.SetModified();
                                break;
                            case "Unchanged":
                                //dr.SetUnchanged();
                                break;
                            default:
                                break;
                        }
                    }
                    if (row.ContainsKey("itemArray"))
                    {
                        Array cells = row["itemArray"] as Array;
                        for (int i = 0; i < cells.Length; i++)
                        {
                            object cellValue = cells.GetValue(i);
                            if (cellValue == null) { dr[i] = DBNull.Value; }
                            else
                            {
                                try
                                {
                                    switch (dt.Columns[i].DataType.ToString())
                                    {
                                        case "System.String[]":
                                            Object[] ss = cellValue as Object[];
                                            if (ss != null)
                                            {
                                                String[] buff = new String[ss.Length];
                                                for (int j = 0; j < ss.Length; j++)
                                                {
                                                    buff[j] = System.Convert.ToString(ss[j]);
                                                }
                                                dr[i] = buff;
                                            }
                                            else dr[i] = DBNull.Value;
                                            break;
                                        case "System.Guid":
                                            dr[i] = new System.Guid(cellValue as String);
                                            break;
                                        case "System.Byte[]":
                                            Object[] bs = cellValue as Object[];
                                            if (bs != null)
                                            {
                                                Byte[] buff = new Byte[bs.Length];
                                                for (int j = 0; j < bs.Length; j++)
                                                {
                                                    buff[j] = System.Convert.ToByte(bs[j]);
                                                }
                                                dr[i] = buff;
                                            }
                                            else dr[i] = DBNull.Value;
                                            break;
                                        default:
                                            dr[i] = System.Convert.ChangeType(cellValue, dt.Columns[i].DataType, ic);
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    dt.ExtendedProperties.Add(
                                        dt.ExtendedProperties.Count + " Nskd.Json.ToDataTable() error message: ",
                                        ex.Message);
                                }
                            }
                        }
                    }
                }
            }
        }
        private class Writer
        {
            private StringBuilder sb;
            private ObjectNameList onl;
            public Writer(object v)
            {
                sb = new StringBuilder();
                onl = new ObjectNameList();
                WriteValue(v);
            }
            public string GetResult()
            {
                return sb.ToString();
            }
            private void WriteValue(object v)
            {
                if (v == null || v == DBNull.Value)
                {
                    sb.Append("null");
                }
                else if (v is string || v is Guid)
                {
                    WriteString(v.ToString());
                }
                else if (v is bool)
                {
                    sb.Append(v.ToString().ToLower());
                }
                else if (v is double ||
                    v is float ||
                    v is long ||
                    v is int ||
                    v is short ||
                    v is byte ||
                    v is decimal)
                {
                    sb.AppendFormat(ic, "{0}", v);
                }
                else if (v.GetType().IsEnum)
                {
                    sb.Append((int)v);
                }
                else if (v is DateTime)
                {
                    DateTime d = (DateTime)v;
                    long t = new DateTimeOffset(d).UtcTicks;
                    t = t / 10000 - 62135596800000;
                    sb.AppendFormat("new Date({0})", t);
                }
                else if (v is DataSet)
                {
                    WriteDataSet(v as DataSet);
                }
                else if (v is DataTable)
                {
                    WriteDataTable(v as DataTable);
                }
                else if (v is IEnumerable)
                {
                    WriteEnumerable(v as IEnumerable);
                }
                else
                {
                    //sb.Append("\"" + v.GetType().ToString() + "\"");
                    WriteObject(v);
                }
            }
            private void WriteString(string s)
            {
                sb.Append("\"");
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            int i = (int)c;
                            if ((i >= 0x20 && i < 0x80) || (i >= 0x400 && i < 0x460))
                            {
                                sb.Append(c);
                            }
                            else
                            {
                                sb.AppendFormat("\\u{0:X04}", i);
                            }
                            break;
                    }
                }
                sb.Append("\"");
            }
            private void WriteDataSet(DataSet ds)
            {
                sb.Append("{\"__type\":\"Nskd.Data.DataSet\",");
                sb.Append("\"tables\":[");
                if (ds.Tables.Count > 0)
                {
                    foreach (DataTable dt in ds.Tables)
                    {
                        WriteDataTable(dt);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("],");
                sb.Append("\"relations\":[");
                if (ds.Relations.Count > 0)
                {
                    foreach (DataRelation r in ds.Relations)
                    {
                        WriteDataRelation(r);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]}");
            }
            private void WriteDataRelation(DataRelation r)
            {
                sb.Append("{");
                sb.AppendFormat("\"parentTable\":{0},", r.DataSet.Tables.IndexOf(r.ParentTable));
                sb.Append("\"parentColumns\":[");
                if (r.ParentColumns.Length > 0)
                {
                    foreach (DataColumn dc in r.ParentColumns)
                    {
                        sb.AppendFormat("{0},", r.ParentTable.Columns.IndexOf(dc));
                    }
                    --sb.Length;
                }
                sb.Append("],");
                sb.AppendFormat("\"childTable\":{0},", r.DataSet.Tables.IndexOf(r.ChildTable));
                sb.Append("\"childColumns\":[");
                if (r.ChildColumns.Length > 0)
                {
                    foreach (DataColumn dc in r.ChildColumns)
                    {
                        sb.AppendFormat("{0},", r.ChildTable.Columns.IndexOf(dc));
                    }
                    --sb.Length;
                }
                sb.Append("]}");
            }
            private void WriteDataTable(DataTable dt)
            {
                sb.Append("{\"__type\":\"Nskd.Data.DataTable\",");
                sb.AppendFormat("\"tableName\":\"{0}\",", dt.TableName);
                sb.Append("\"columns\":[");
                if (dt.Columns.Count > 0)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        WriteDataColumn(dc);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("],");

                if (dt.PrimaryKey.Length > 0)
                {
                    sb.Append("\"primaryKey\":[");
                    foreach (DataColumn dc in dt.PrimaryKey)
                    {
                        sb.Append(dc.Ordinal);
                        sb.Append(",");
                    }
                    --sb.Length;
                    sb.Append("],");
                }

                if (dt.ExtendedProperties.Keys.Count > 0)
                {
                    sb.Append("\"extendedProperties\":{");
                    foreach (string key in dt.ExtendedProperties.Keys)
                    {
                        sb.Append("\"" + key + "\":");
                        WriteValue(dt.ExtendedProperties[key]);
                        sb.Append(",");
                    }
                    --sb.Length;
                    sb.Append("},");
                }

                sb.Append("\"rows\":[");
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        WriteDataRow(dr);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]");
                sb.Append("}");
            }
            private void WriteDataColumn(DataColumn dc)
            {
                sb.Append("{");
                {
                    sb.Append("\"columnName\":");
                    WriteValue(dc.ColumnName);
                    sb.Append(",");

                    if (dc.Caption != dc.ColumnName)
                    {
                        sb.Append("\"caption\":\"");
                        sb.Append(dc.Caption);
                        sb.Append("\",");
                    }

                    sb.Append("\"dataType\":\"");
                    sb.Append(dc.DataType.ToString());
                    sb.Append("\",");

                    if (dc.ExtendedProperties.Keys.Count > 0)
                    {
                        sb.Append("\"extendedProperties\":{");
                        foreach (string key in dc.ExtendedProperties.Keys)
                        {
                            sb.Append("\"" + key + "\":\"");
                            sb.Append(dc.ExtendedProperties[key]);
                            sb.Append("\",");
                        }
                        --sb.Length;
                        sb.Append("}");
                    }
                    else { --sb.Length; }
                }
                sb.Append("}");
            }
            private void WriteDataRow(DataRow dr)
            {
                sb.Append("{");
                {
                    sb.Append("\"itemArray\":[");
                    if (dr.Table.Columns.Count > 0)
                    {
                        foreach (DataColumn dc in dr.Table.Columns)
                        {
                            WriteValue(dr[dc]);
                            sb.Append(",");
                        }
                        --sb.Length;
                    }
                    sb.Append("],");
                    sb.AppendFormat("\"rowState\":\"{0}\"", dr.RowState.ToString());
                }
                sb.Append("}");
            }
            private void WriteEnumerable(IEnumerable e)
            {
                bool hasItems = false;
                sb.Append("[");
                foreach (object v in e)
                {
                    WriteValue(v);
                    sb.Append(",");
                    hasItems = true;
                }
                if (hasItems) --sb.Length;
                sb.Append("]");
            }
            private void WriteObject(object o)
            {
                MemberInfo[] members = o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public);
                sb.Append("{");
                bool hasMembers = false;
                foreach (MemberInfo member in members)
                {
                    bool hasValue = false;
                    object v = null;
                    if ((member.MemberType & MemberTypes.Field) == MemberTypes.Field)
                    {
                        FieldInfo field = (FieldInfo)member;
                        v = field.GetValue(o);
                        hasValue = true;
                    }
                    else if ((member.MemberType & MemberTypes.Property) == MemberTypes.Property)
                    {
                        PropertyInfo property = (PropertyInfo)member;
                        if (property.CanRead && property.GetIndexParameters().Length == 0)
                        {
                            v = property.GetValue(o, null);
                            hasValue = true;
                        }
                    }
                    if (hasValue)
                    {
                        sb.Append("\"");
                        sb.Append(member.Name);
                        sb.Append("\":");
                        WriteValue(v);
                        sb.Append(",");
                        hasMembers = true;

                    }
                }
                if (hasMembers) { --sb.Length; }
                sb.Append("}");
            }
        }
        public static object Parse(string jsonString)
        {
            Reader r = new Reader(jsonString);
            return r.GetResult();
        }
        public static string ToString(object value)
        {
            Writer w = new Writer(value);
            return w.GetResult();
        }
    }
    public static class JsonV3
    {
        private static CultureInfo ic = CultureInfo.InvariantCulture;
        private class ObjectLink
        {
            Int32 Id { get; set; }
            String Name { get; set; }
        }
        private class ObjectNameList
        {
            private ArrayList list;
            public ObjectNameList()
            {
                list = new ArrayList();
            }
        }
        private class Reader
        {
            private string jsonString { get; set; }
            private int len { get; set; }
            private int cpi { get; set; }
            private object v { get; set; }
            public Reader(string jsonString)
            {
                this.jsonString = jsonString;
                this.len = jsonString.Length;
                this.cpi = 0;
                this.v = ReadValue();
            }
            public object GetResult()
            {
                return this.v;
            }
            private object ReadValue()
            {
                object o = null;
                SkipWhiteSpace();
                char ch = jsonString[cpi];
                switch (ch)
                {
                    case '"':
                        o = ReadString();
                        /*
                        if (((string)o).Length > 0 && ((string)o)[0] == '\\')
                        {
                            o = ReadDate((string)o);
                        }
                        */
                        break;
                    case 'f':
                        if (jsonString.Substring(cpi, 5) == "false")
                        {
                            cpi += 5;
                            o = false;
                        }
                        break;
                    case 't':
                        if (jsonString.Substring(cpi, 4) == "true")
                        {
                            cpi += 4;
                            o = true;
                        }
                        break;
                    case 'n':
                        if (jsonString.Substring(cpi, 4) == "null")
                        {
                            cpi += 4;
                            o = null;
                        }
                        else if (jsonString.Substring(cpi, 9) == "new Date(")
                        {
                            cpi += 9;
                            o = ReadDate();
                        }
                        break;
                    case '{':
                        Hashtable ht = ReadObject();
                        o = ConvertToType(ht);
                        break;
                    case '[':
                        o = ReadArray();
                        break;
                    default:
                        if (ch == '-' || char.IsDigit(ch))
                        {
                            o = ReadNumber();
                        }
                        break;
                }
                return o;
            }
            private object ReadNumber()
            {
                object o = null;
                int spi = cpi;
                cpi++; // пропуск '-' или первой цифры
                while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                if (cpi >= len || jsonString[cpi] != '.')
                {
                    long temp;
                    if (long.TryParse(jsonString.Substring(spi, (cpi - spi)), out temp))
                        if ((int.MinValue <= temp) && (temp <= int.MaxValue)) o = (int)temp;
                        else o = temp;
                }
                else // '.'
                {
                    cpi++; // пропуск '.'
                    if (char.IsDigit(jsonString[cpi])) // после точки должна быть цифра
                    {
                        while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                        char ch = jsonString[cpi];
                        if ((ch == 'e') || (ch == 'E')) // есть степень
                        {
                            cpi++; // пропуск 'e'
                            ch = jsonString[cpi];
                            if ((ch == '+') || (ch == '-'))
                            {
                                cpi++; // пропуск знака
                                if (char.IsDigit(jsonString[cpi])) // после знака должна быть цифра
                                {
                                    while (cpi < len && char.IsDigit(jsonString[cpi])) cpi++;
                                }
                            }
                            double temp;
                            if (double.TryParse(jsonString.Substring(spi, (cpi - spi)), NumberStyles.Float, ic, out temp))
                                o = temp;
                        }
                        else
                        { // нет степени
                            decimal temp;
                            if (decimal.TryParse(jsonString.Substring(spi, (cpi - spi)), NumberStyles.Number, ic, out temp))
                                o = temp;
                        }
                    }
                }
                return o;
            }
            private object ReadDate()
            {
                long t = (long)ReadNumber(); // UTC Ticks
                cpi += 1; // ")"
                t = (t + 62135596800000) * 10000;
                t = t + DateTimeOffset.Now.Offset.Ticks; // local Ticks
                return new DateTime(t, DateTimeKind.Local);
            }
            private object ReadString()
            {
                StringBuilder sb = new StringBuilder();
                int spi = cpi;
                cpi++; // пропускаем '"'
                while (cpi < len)
                {
                    char ch = jsonString[cpi];
                    if (ch == '\\')
                    { // после \ только ["\/bfnrt] или u0000
                        cpi++;
                        ch = jsonString[cpi];
                        if (ch == 'u')
                        {
                            sb.Append((char)int.Parse(jsonString.Substring(cpi + 1, 4), NumberStyles.HexNumber));
                            cpi += 5;
                        }
                        else
                        {
                            if (ch == '"') sb.Append('"');
                            if (ch == '\\') sb.Append('\\');
                            if (ch == '/') sb.Append('/');
                            if (ch == 'b') sb.Append('\b');
                            if (ch == 'f') sb.Append('\f');
                            if (ch == 'n') sb.Append('\n');
                            if (ch == 'r') sb.Append('\r');
                            if (ch == 't') sb.Append('\t');
                            cpi++;
                        }
                        continue;
                    }
                    if (ch == '"') break;
                    else
                    {
                        sb.Append(ch);
                        cpi++;
                    }
                }
                cpi++;  // пропускаем '"'
                return sb.ToString();
            }
            private Hashtable ReadObject()
            {
                Hashtable ht = new Hashtable();
                cpi++; // пропустить "{"
                SkipWhiteSpace();
                if (cpi < len && jsonString[cpi] != '}')
                {
                    do
                    {
                        SkipWhiteSpace();
                        string key = (string)ReadString();
                        SkipWhiteSpace();
                        cpi++; // пропустить ":"
                        SkipWhiteSpace();
                        object value = ReadValue();
                        SkipWhiteSpace();
                        ht.Add(key, value);
                        // проверить и пропустить "," или "}"
                    } while (cpi < len && jsonString[cpi++] == ',');
                }
                else
                {
                    cpi++; // пропустить "}"
                }
                return ht;
            }
            private object ConvertToType(Hashtable ht)
            {
                object result = ht;
                if (ht != null && ht.ContainsKey("__type"))
                {
                    string type = ht["__type"] as string;
                    switch (type)
                    {
                        case "Nskd.Data.DataSet":
                            result = ToDataSet(ht);
                            break;
                        case "Nskd.Data.DataTable":
                            result = ToDataTable(ht);
                            break;
                        default:
                            break;
                    }
                }
                return result;
            }
            private object ReadArray()
            {
                ArrayList a = new ArrayList();
                cpi++; // пропуск "["
                SkipWhiteSpace();
                if (jsonString[cpi] != ']')
                {
                    do
                    {
                        a.Add(ReadValue());
                        SkipWhiteSpace();
                    } while (jsonString[cpi++] == ','); // проверка и пропуск "," или "]"
                }
                else cpi++; // пропуск "]"
                //throw new Exception("Nskd.Json.Json.Reader.ReadArray(): " + a.ToArray().Length.ToString());
                return a.ToArray();
            }
            private void SkipWhiteSpace()
            {
                while (cpi < len && char.IsWhiteSpace(jsonString[cpi])) cpi++;
            }
            private static DataSet ToDataSet(Hashtable set)
            {
                DataSet ds = new DataSet();
                if (set != null)
                {
                    if (set.ContainsKey("tables"))
                    {
                        Array tables = set["tables"] as Array;
                        foreach (object table in tables)
                        {
                            ds.Tables.Add(table as DataTable);
                        }
                    }
                    if (set.ContainsKey("relations"))
                    {
                        //ds.Relations = ...
                    }
                }
                return ds;
            }
            private static DataTable ToDataTable(Hashtable table)
            {
                DataTable dt = new DataTable();
                if (table != null)
                {
                    dt.TableName = table["tableName"] as string;
                    Array cols = table["columns"] as Array;
                    foreach (object col in cols)
                    {
                        Hashtable jcol = col as Hashtable;
                        string columnName = jcol["columnName"] as string;
                        string dataType = jcol["dataType"] as string;

                        Type t = Type.GetType(dataType);
                        if (t == null) t = typeof(Object);

                        DataColumn dc = new DataColumn(columnName, t);
                        dt.Columns.Add(dc);
                    }
                    Array rows = table["rows"] as Array;
                    foreach (object row in rows)
                    {
                        AddDataRowFromHashtableRow(dt, row as Hashtable);
                    }
                }
                return dt;
            }
            private static void AddDataRowFromHashtableRow(DataTable dt, Hashtable row)
            {
                DataRow dr = dt.NewRow();
                dt.Rows.Add(dr);
                if (row != null && row.ContainsKey("itemArray"))
                {
                    if (row.ContainsKey("rowState"))
                    {
                        String rowState = row["rowState"] as String;
                        switch (rowState)
                        {
                            case "Added":
                                //dr.SetAdded();
                                break;
                            case "Deleted":
                                //dr.SetDeleted();
                                break;
                            case "Detached":
                                //dr.SetDetached();
                                break;
                            case "Modified":
                                dr.SetModified();
                                break;
                            case "Unchanged":
                                //dr.SetUnchanged();
                                break;
                            default:
                                break;
                        }
                    }
                    if (row.ContainsKey("itemArray"))
                    {
                        Array cells = row["itemArray"] as Array;
                        for (int i = 0; i < cells.Length; i++)
                        {
                            object cellValue = cells.GetValue(i);
                            if (cellValue == null) { dr[i] = DBNull.Value; }
                            else
                            {
                                try
                                {
                                    switch (dt.Columns[i].DataType.ToString())
                                    {
                                        case "System.String[]":
                                            Object[] ss = cellValue as Object[];
                                            if (ss != null)
                                            {
                                                String[] buff = new String[ss.Length];
                                                for (int j = 0; j < ss.Length; j++)
                                                {
                                                    buff[j] = System.Convert.ToString(ss[j]);
                                                }
                                                dr[i] = buff;
                                            }
                                            else dr[i] = DBNull.Value;
                                            break;
                                        case "System.Guid":
                                            dr[i] = new System.Guid(cellValue as String);
                                            break;
                                        case "System.Byte[]":
                                            Object[] bs = cellValue as Object[];
                                            if (bs != null)
                                            {
                                                Byte[] buff = new Byte[bs.Length];
                                                for (int j = 0; j < bs.Length; j++)
                                                {
                                                    buff[j] = System.Convert.ToByte(bs[j]);
                                                }
                                                dr[i] = buff;
                                            }
                                            else dr[i] = DBNull.Value;
                                            break;
                                        case "System.Object":
                                            if (cellValue is Hashtable)
                                            {
                                                dr[i] = cellValue;
                                            }
                                            else dr[i] = DBNull.Value;
                                            break;
                                        default:
                                            try
                                            {
                                                dr[i] = System.Convert.ChangeType(cellValue, dt.Columns[i].DataType, ic);
                                            }
                                            catch (Exception) { dr[i] = DBNull.Value; }
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    dt.ExtendedProperties.Add(
                                        dt.ExtendedProperties.Count + " Nskd.Json.ToDataTable() error message: ",
                                        ex.Message);
                                }
                            }
                        }
                    }
                }
            }
        }
        private class Writer
        {
            private StringBuilder sb;
            private ObjectNameList onl;
            public Writer(object v)
            {
                sb = new StringBuilder();
                onl = new ObjectNameList();
                WriteValue(v);
            }
            public string GetResult()
            {
                return sb.ToString();
            }
            private void WriteValue(object v)
            {
                if (v == null || v == DBNull.Value)
                {
                    sb.Append("null");
                }
                else if (v is string || v is Guid)
                {
                    WriteString(v.ToString());
                }
                else if (v is bool)
                {
                    sb.Append(v.ToString().ToLower());
                }
                else if (v is double ||
                    v is float ||
                    v is long ||
                    v is int ||
                    v is short ||
                    v is byte ||
                    v is decimal)
                {
                    sb.AppendFormat(ic, "{0}", v);
                }
                else if (v.GetType().IsEnum)
                {
                    sb.Append((int)v);
                }
                else if (v is DateTime)
                {
                    DateTime d = (DateTime)v;
                    long t = new DateTimeOffset(d).UtcTicks;
                    t = t / 10000 - 62135596800000;
                    sb.AppendFormat("new Date({0})", t);
                }
                else if (v is DataSet)
                {
                    WriteDataSet(v as DataSet);
                }
                else if (v is DataTable)
                {
                    WriteDataTable(v as DataTable);
                }
                else if (v is IEnumerable)
                {
                    WriteEnumerable(v as IEnumerable);
                }
                else
                {
                    //sb.Append("\"" + v.GetType().ToString() + "\"");
                    WriteObject(v);
                }
            }
            private void WriteString(string s)
            {
                sb.Append("\"");
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            int i = (int)c;
                            if ((i >= 0x20 && i < 0x80) || (i >= 0x400 && i < 0x460))
                            {
                                sb.Append(c);
                            }
                            else
                            {
                                sb.AppendFormat("\\u{0:X04}", i);
                            }
                            break;
                    }
                }
                sb.Append("\"");
            }
            private void WriteDataSet(DataSet ds)
            {
                sb.Append("{\"__type\":\"Nskd.Data.DataSet\",");
                sb.Append("\"tables\":[");
                if (ds.Tables.Count > 0)
                {
                    foreach (DataTable dt in ds.Tables)
                    {
                        WriteDataTable(dt);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("],");
                sb.Append("\"relations\":[");
                if (ds.Relations.Count > 0)
                {
                    foreach (DataRelation r in ds.Relations)
                    {
                        WriteDataRelation(r);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]}");
            }
            private void WriteDataRelation(DataRelation r)
            {
                sb.Append("{");
                sb.AppendFormat("\"parentTable\":{0},", r.DataSet.Tables.IndexOf(r.ParentTable));
                sb.Append("\"parentColumns\":[");
                if (r.ParentColumns.Length > 0)
                {
                    foreach (DataColumn dc in r.ParentColumns)
                    {
                        sb.AppendFormat("{0},", r.ParentTable.Columns.IndexOf(dc));
                    }
                    --sb.Length;
                }
                sb.Append("],");
                sb.AppendFormat("\"childTable\":{0},", r.DataSet.Tables.IndexOf(r.ChildTable));
                sb.Append("\"childColumns\":[");
                if (r.ChildColumns.Length > 0)
                {
                    foreach (DataColumn dc in r.ChildColumns)
                    {
                        sb.AppendFormat("{0},", r.ChildTable.Columns.IndexOf(dc));
                    }
                    --sb.Length;
                }
                sb.Append("]}");
            }
            private void WriteDataTable(DataTable dt)
            {
                sb.Append("{\"__type\":\"Nskd.Data.DataTable\",");
                sb.AppendFormat("\"tableName\":\"{0}\",", dt.TableName);
                sb.Append("\"columns\":[");
                if (dt.Columns.Count > 0)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        WriteDataColumn(dc);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("],");

                if (dt.PrimaryKey.Length > 0)
                {
                    sb.Append("\"primaryKey\":[");
                    foreach (DataColumn dc in dt.PrimaryKey)
                    {
                        sb.Append(dc.Ordinal);
                        sb.Append(",");
                    }
                    --sb.Length;
                    sb.Append("],");
                }

                if (dt.ExtendedProperties.Keys.Count > 0)
                {
                    sb.Append("\"extendedProperties\":{");
                    foreach (string key in dt.ExtendedProperties.Keys)
                    {
                        sb.Append("\"" + key + "\":");
                        WriteValue(dt.ExtendedProperties[key]);
                        sb.Append(",");
                    }
                    --sb.Length;
                    sb.Append("},");
                }

                sb.Append("\"rows\":[");
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        WriteDataRow(dr);
                        sb.Append(",");
                    }
                    --sb.Length;
                }
                sb.Append("]");
                sb.Append("}");
            }
            private void WriteDataColumn(DataColumn dc)
            {
                sb.Append("{");
                {
                    sb.Append("\"columnName\":");
                    WriteValue(dc.ColumnName);
                    sb.Append(",");

                    if (dc.Caption != dc.ColumnName)
                    {
                        sb.Append("\"caption\":\"");
                        sb.Append(dc.Caption);
                        sb.Append("\",");
                    }

                    sb.Append("\"dataType\":\"");
                    sb.Append(dc.DataType.ToString());
                    sb.Append("\",");

                    if (dc.ExtendedProperties.Keys.Count > 0)
                    {
                        sb.Append("\"extendedProperties\":{");
                        foreach (string key in dc.ExtendedProperties.Keys)
                        {
                            sb.Append("\"" + key + "\":\"");
                            sb.Append(dc.ExtendedProperties[key]);
                            sb.Append("\",");
                        }
                        --sb.Length;
                        sb.Append("}");
                    }
                    else { --sb.Length; }
                }
                sb.Append("}");
            }
            private void WriteDataRow(DataRow dr)
            {
                sb.Append("{");
                {
                    sb.Append("\"itemArray\":[");
                    if (dr.Table.Columns.Count > 0)
                    {
                        foreach (DataColumn dc in dr.Table.Columns)
                        {
                            WriteValue(dr[dc]);
                            sb.Append(",");
                        }
                        --sb.Length;
                    }
                    sb.Append("],");
                    sb.AppendFormat("\"rowState\":\"{0}\"", dr.RowState.ToString());
                }
                sb.Append("}");
            }
            private void WriteEnumerable(IEnumerable e)
            {
                bool hasItems = false;
                sb.Append("[");
                foreach (object v in e)
                {
                    WriteValue(v);
                    sb.Append(",");
                    hasItems = true;
                }
                if (hasItems) --sb.Length;
                sb.Append("]");
            }
            private void WriteObject(object o)
            {
                MemberInfo[] members = o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public);
                sb.Append("{");
                bool hasMembers = false;
                foreach (MemberInfo member in members)
                {
                    bool hasValue = false;
                    object v = null;
                    if ((member.MemberType & MemberTypes.Field) == MemberTypes.Field)
                    {
                        FieldInfo field = (FieldInfo)member;
                        v = field.GetValue(o);
                        hasValue = true;
                    }
                    else if ((member.MemberType & MemberTypes.Property) == MemberTypes.Property)
                    {
                        PropertyInfo property = (PropertyInfo)member;
                        if (property.CanRead && property.GetIndexParameters().Length == 0)
                        {
                            v = property.GetValue(o, null);
                            hasValue = true;
                        }
                    }
                    if (hasValue)
                    {
                        sb.Append("\"");
                        sb.Append(member.Name);
                        sb.Append("\":");
                        WriteValue(v);
                        sb.Append(",");
                        hasMembers = true;

                    }
                }
                if (hasMembers) { --sb.Length; }
                sb.Append("}");
            }
        }
        public static object Parse(string jsonString)
        {
            Reader r = new Reader(jsonString);
            return r.GetResult();
        }
        public static string ToString(object value)
        {
            Writer w = new Writer(value);
            return w.GetResult();
        }
    }
}
