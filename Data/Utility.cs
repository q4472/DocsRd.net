using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Nskd
{
    public class Utility
    {
        public static String UnEscape(String str)
        {
            String result = str ?? "";
            Regex re = new Regex(@"%[0-9A-Fa-f]{2}|%u[0-9A-Fa-f]{4}");
            result = re.Replace(result, new MatchEvaluator(MeUriHexToChar));
            return result;
        }
        private static String MeUriHexToChar(Match m)
        {
            String s = "";
            String v = m.Value;
            Int32 si = (v.Length == 3) ? 1 : 2;
            Int32 len = v.Length - si;
            UInt16 code = System.Convert.ToUInt16(v.Substring(si, len), 16);
            if (code != 0)
            {
                Char c = System.Convert.ToChar(code);
                s = System.Convert.ToString(c);
            }
            return s;
        }
        /*
        public static Dictionary<String, String> ParsePostRequestBody(HttpRequestBase request)
        {
            Dictionary<String, String> pars = new Dictionary<String, String>();
            if (request != null)
            {
                String body = null;
                using (StreamReader sr = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = sr.ReadToEnd();
                }
                if (!String.IsNullOrWhiteSpace(body))
                {
                    String[] kvps = body.Split('&');
                    for (int i = 0; i < kvps.Length; i++)
                    {
                        String[] kv = kvps[i].Split('=');
                        String key = kv[0];
                        String value = kv[1];
                        if (!String.IsNullOrWhiteSpace(key))
                        {
                            pars.Add(key, Utility.UnEscape(value));
                        }
                    }
                }
            }
            return pars;
        }
        */
    }
}