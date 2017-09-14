using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CommonLibs
{
    public enum LogTypeEnum
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    public class Common
    {
        /// <summary>
        /// 根據cookie name 获取cookie value
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetCookieValue(string cookies, string name)
        {
            string value = string.Empty;
            string namePrefix = name + "=";
            var mc = Regex.Match(cookies, "(?=" + namePrefix + ")[^;]*");
            if (mc != null && mc.Length > 0)
                value = mc.Value.Substring(namePrefix.Length);
            return value;
        }

        /// <summary>
        /// url 编码
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string DataToUrl(string url, IEnumerable<KeyValuePair<string, string>> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            bool first = true;
            var sb = new StringBuilder(url);
            foreach (var item in data)
            {
                if (first)
                {
                    sb.Append('?');
                    first = false;
                }
                else
                    sb.Append('&');

                sb.Append(Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value));
            }

            return sb.ToString();
        }

        /// <summary>
        /// url解析
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Tuple<string, IEnumerable<KeyValuePair<string, string>>> UrlToData(string url)
        {
            if (url == null)
                throw new ArgumentNullException("url");

            url = url.Trim();
            try
            {
                var split = url.Split(new[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 1)
                    return new Tuple<string, IEnumerable<KeyValuePair<string, string>>>(url, null);

                //获取前面的URL地址
                var host = split[0];

                var pairs = split.Skip(1).Select(s =>
                {
                    //没有用String.Split防止某些少见Query String中出现多个=，要把后面的无法处理的=全部显示出来
                    var idx = s.IndexOf('=');
                    return new KeyValuePair<string, string>(Uri.UnescapeDataString(s.Substring(0, idx)), Uri.UnescapeDataString(s.Substring(idx + 1)));
                }).ToList();

                return new Tuple<string, IEnumerable<KeyValuePair<string, string>>>(host, pairs);
            }
            catch (Exception ex)
            {
                throw new FormatException("URL格式错误", ex);
            }
        }

        /// <summary>
        /// 解析拼接的查询字符串
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Dictionary<string, string> QueryStringToData(string url)
        {
            if (url == null)
                throw new ArgumentNullException("url");

            url = url.Trim();
            try
            {
                var split = url.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 0)
                    return null;

                Dictionary<string, string> pairs = new Dictionary<string, string>();
                for (int i = 0; i < split.Length; i++)
                {
                    var s = split[i];
                    //没有用String.Split防止某些少见Query String中出现多个=，要把后面的无法处理的=全部显示出来
                    var idx = s.IndexOf('=');
                    pairs.Add(Uri.UnescapeDataString(s.Substring(0, idx)), Uri.UnescapeDataString(s.Substring(idx + 1)));
                }
                return pairs;
            }
            catch (Exception ex)
            {
                throw new FormatException("URL格式错误", ex);
            }
        }

        /// <summary>
        /// 实现数据的四舍五入法
        　　 /// </summary>
        /// <param name="v">要进行处理的数据</param>
        /// <param name="x">保留的小数位数</param>
        /// <returns>四舍五入后的结果</returns>
        public static double Round(double v, int x)
        {
            bool isNegative = false;
            //如果是负数
            if (v < 0)
            {
                isNegative = true;
                v = -v;
            }

            int IValue = 1;
            for (int i = 1; i <= x; i++)
            {
                IValue = IValue * 10;
            }
            double Int = Math.Round(v * IValue + 0.5, 0);
            v = Int / IValue;

            if (isNegative)
            {
                v = -v;
            }

            return v;
        }

        public static string GetSimpleTypeName(string stype)
        {
            string ret = "";
            if (stype.Contains("Dictionary"))
            {
                string att_str = stype.Substring(stype.LastIndexOf("["));
                string[] atts = att_str.Split(',');
                if (atts != null && atts.Length >= 2)
                    //todo: 第二个参数为什么不对？
                    ret = "Dictionary<" + GetSimpleTypeName(atts[0]) + ", " + GetSimpleTypeName(atts[0]) + ">";
            }
            else if (stype.Contains("System.Collections.Generic.List`1"))
            {
                string clid_type = "int";
                if (stype.Contains("DateTime"))
                {
                    clid_type = "System.DateTime";
                }
                else
                {
                    Match mc = Regex.Match(stype, @"Models\.(?!ViewModels)[A-Za-z]*");
                    if (mc != null && mc.Length > 0)
                        clid_type = mc.Value.Substring("Models.".Length, mc.Value.Length - 0);
                }
                ret = "List<" + clid_type + "> ";
            }
            else if (stype.Contains("System.Int32[]"))
                ret = "int[]";
            else if (stype.Contains("System.Int32"))
                ret = "int";
            else if (stype.Contains("System.Int64"))
                ret = "long";
            else if (stype.Contains("System.Int"))
                ret = "int";
            else if (stype.Contains("System.String"))
                ret = "string";
            else if (stype.Contains("System.Single"))
                ret = "float";
            else if (stype.Contains("System.Double"))
                ret = "double";
            else if (stype.Contains("System.Bool"))
                ret = "bool";
            else
            {
                ret = stype.Replace("+", ".");
            }
            return ret;
        }

        public static string GetReturnTypeName(string methodReturnTypeName)
        {
            bool isCollections = false;

            string taskPrefix = "System.Threading.Tasks.Task`1";
            if (methodReturnTypeName.Contains(taskPrefix))
                methodReturnTypeName = methodReturnTypeName.Substring((taskPrefix + @"[[").Length);

            string returnDataPrefix = "[A-Za-z]*.ReturnData`1";
            var mc1 = Regex.Match(methodReturnTypeName, returnDataPrefix);
            if (mc1 != null && mc1.Length > 0)
                methodReturnTypeName = methodReturnTypeName.Substring((mc1.Value + @"[[").Length);

            returnDataPrefix = "[A-Za-z]*.ReturnMessage`1";
            mc1 = Regex.Match(methodReturnTypeName, returnDataPrefix);
            if (mc1 != null && mc1.Length > 0)
                methodReturnTypeName = methodReturnTypeName.Substring((mc1.Value + @"[[").Length);


            //是List<>
            string collectionsPrefix = "System.Collections.Generic.List`1";
            if (methodReturnTypeName.Contains(collectionsPrefix))
            {
                methodReturnTypeName = methodReturnTypeName.Substring((collectionsPrefix + @"[[").Length);
                isCollections = true;
            }

            methodReturnTypeName = TryParseTypeName(methodReturnTypeName);
            if (isCollections)
                methodReturnTypeName = string.Format("List<{0}>", methodReturnTypeName);
            return methodReturnTypeName;
        }

        static string TryParseTypeName(string methodReturnTypeName)
        {
            var mc = GetSubTypeName(ref methodReturnTypeName, @"Models\.(?!ViewModels)[A-Za-z]*");
            if (mc == null || mc.Length == 0)
            {
                mc = GetSubTypeName(ref methodReturnTypeName, @"System\.[A-Za-z]*");
                if (mc == null || mc.Length == 0)
                {
                    GetSubTypeName(ref methodReturnTypeName, "");
                }
            }
            else
            {
                int start = "Models.".Length;
                methodReturnTypeName = mc.Value.Substring(start, mc.Value.Length - start);
            }
            return methodReturnTypeName;
        }
        static Match GetSubTypeName(ref string typeName, string pattern)
        {
            bool ret = false;
            var mc1 = Regex.Match(typeName, pattern);
            if (mc1 != null && mc1.Length > 0)
            {
                ret = true;
                typeName = mc1.Value;
            }
            return mc1;
        }

    }
}
