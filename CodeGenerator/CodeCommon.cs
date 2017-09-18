using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using CommonLibs;

namespace CodeGenerator
{
    class CodeCommon
    {
        public static void ParseReturnType(Type methodReturnType, Type attriReturnType, string modelPrject, 
            ref string methodReturnTypeName, ref string returnTypeName, ref string mapperReturn, ref string innerType)
        {

            methodReturnTypeName = methodReturnType.FullName;
            bool isReturnData = methodReturnTypeName.Contains("ReturnData");
            bool needMapper = methodReturnTypeName.Contains(modelPrject);

            //使用返回值结构
            methodReturnTypeName = Common.GetReturnTypeName(methodReturnTypeName);
            methodReturnTypeName = Common.GetSimpleTypeName(methodReturnTypeName);
            //提取实际返回值类型
            returnTypeName = methodReturnTypeName;
            //获取<>前的类型
            innerType = returnTypeName;

            //api设定的返回类型
            if (attriReturnType != null)
            {
                needMapper = true;
                returnTypeName = attriReturnType.FullName;
                returnTypeName = Common.GetReturnTypeName(returnTypeName);
                returnTypeName = Common.GetSimpleTypeName(returnTypeName);
                innerType = returnTypeName;

                Console.WriteLine("attriReturnType.returnTypeName={0}", returnTypeName);
            }
            
            //todo: 总是mapper类型
            if (needMapper)
            {
                //获取List中的类型
                var mc = Regex.Match(returnTypeName, "[A-Za-z]*(?=>)");
                if (mc != null && mc.Length > 0)
                {
                    innerType = mc.Value;
                    if (!innerType.Contains("Data"))
                        innerType = innerType + "Data";
                    Console.WriteLine("innerType={0}", innerType);

                    //获取<>前的类型
                    mc = Regex.Match(returnTypeName, "[A-Za-z]*(?=<)");
                    if (mc != null && mc.Length > 0)
                    {
                        returnTypeName = mc.Value + "<" + innerType + ">";

                        Console.WriteLine("获取<>前的类型.returnTypeName={0}", returnTypeName);
                    }
                }
                else
                {
                    if (!returnTypeName.Contains("Data"))
                        returnTypeName = returnTypeName + "Data";

                    Console.WriteLine("returnTypeName + \"Data\"={0}", returnTypeName);
                }
            }

            mapperReturn = "";
            if (needMapper)
            {
                if (isReturnData)
                {
                    var mc = Regex.Match(returnTypeName, "[A-Za-z]*(?=>)");
                    if (mc != null && mc.Length > 0)
                    {
                        mapperReturn += "var dataList = new " + returnTypeName + "();\n";
                        mapperReturn += "                    for (int i = 0; i < retData.Data.Count; i++)\n";
                        mapperReturn += "                        dataList.Add(Mapper.Map<" + innerType + ">(retData.Data[i]));\n";

                        mapperReturn += "                    var data = new ReturnData<" + returnTypeName + ">{\n";
                        mapperReturn += "                        ErrorCode = retData.ErrorCode,\n";
                        mapperReturn += "                        Data = dataList,\n";
                        mapperReturn += "                    };\n";
                    }
                    else
                    {
                        mapperReturn += "var data = new ReturnData<" + returnTypeName + ">{\n";
                        mapperReturn += "                    ErrorCode = retData.ErrorCode,\n";
                        mapperReturn += "                    Data = Mapper.Map<" + innerType + ">(retData.Data),\n";
                        mapperReturn += "                };\n";
                    }
                }
                else
                {
                    var mc = Regex.Match(returnTypeName, "[A-Za-z]*(?=>)");
                    if (mc != null && mc.Length > 0)
                    {
                        mapperReturn += "var dataValue = new " + returnTypeName + "();\n";
                        mapperReturn += "                    for (int i = 0; i < retData.Count; i++)\n";
                        mapperReturn += "                        dataValue.Add(Mapper.Map<" + innerType + ">(retData[i]));\n";
                        mapperReturn += "var data = new ReturnData<" + returnTypeName + ">(dataValue);\n";
                    }
                    else
                    {
                        mapperReturn += "var dataValue = Mapper.Map<" + returnTypeName + ">(retData);\n";
                        mapperReturn += "var data = new ReturnData<" + returnTypeName + ">(dataValue);\n";
                    }
                }
            }
            else
            {
                if (isReturnData)
                    mapperReturn += "var data = retData;\n";
                else
                    mapperReturn += "var data = new ReturnData<" + returnTypeName + ">(retData);\n";
            }
        }

        public static string GetTemplate(string temp_path, string filename)
        {
            string temp = "";
            using (var stream = new FileStream(temp_path + filename, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
                {
                    temp = sr.ReadToEnd();
                }
            }
            return temp;
        }

        public static void WriteFile(string filename, string content)
        {
            var path = filename.Substring(0, filename.LastIndexOf(@"\"));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            else
            {
                if (File.Exists(filename))
                    File.Delete(filename);
            }
            using (var sw = File.CreateText(filename))
            {
                sw.Write(content);
            }
            Console.WriteLine("Write ClientFile: " + filename);
        }

        public static bool CheckParamSocket(ParameterInfo paramInfo)
        {
            return paramInfo.Name.Equals("socket");
        }

        public static string GetReadString(string stype)
        {
            string ret = "";
            if (stype.Contains("System.Int32"))
                ret = "ReadInt";
            else if (stype.Contains("System.Int64"))
                ret = "ReadLong";
            else if (stype.Contains("System.String"))
                ret = "ReadString";
            else if (stype.Contains("System.Single"))
                ret = "ReadFloat";
            else if (stype.Contains("System.Double"))
                ret = "ReadDouble";
            else if (stype.Contains("System.Bool"))
                ret = "ReadBool";
            else
                ret = "ReadString";
            return ret;
        }

    }
}
