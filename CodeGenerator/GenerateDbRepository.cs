//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Reflection;
//using System.IO;
//using CommonLibs;

//namespace CodeGenerator
//{
//    class GenerateDbRepository
//    {
//        static string m_template_path = string.Empty;
//        static string m_server_path = string.Empty;
//        static string m_type_name = string.Empty;

//        public static void InitPath(string template, string server_path)
//        {
//            m_template_path = template;
//            m_server_path = server_path;
//        }

//        public static void GenerateFromDbContext<T>()
//        {
//            GenerateFromDbContext(typeof(T));
//        }
//        public static void GenerateFromDbContext(Type vType)
//        {
//            m_type_name = vType.Name;

//            string funcTemplate = CodeCommon.GetTemplate(m_template_path, "ServerRepositoryFunc.txt");

//            string funcStr = "";
//            string modelStr = "";

//            //方法生成请求接口
//            var vPropertiesInfos = vType.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
//            for (int i = 0; i < vPropertiesInfos.Length; i++)
//            {
//                var vPropertyInfo = vPropertiesInfos[i];
//                if (vPropertyInfo.PropertyType.Name.Contains("DbSet"))
//                {
//                    string propertyName = vPropertyInfo.Name;
//                    var proTypeName = vPropertyInfo.PropertyType.FullName;
//                    proTypeName = CodeCommon.GetReturnTypeName(proTypeName);
//                    proTypeName = CodeCommon.GetSimpleTypeName(proTypeName);
//                    string className = proTypeName;
//                    if (className.StartsWith("GodModels."))
//                        className = className.Substring("GodModels.".Length);

//                    modelStr += "                cfg.CreateMap<"+ className + ", "+ className + "Data>();\n";

//                    //注册数据仓库
//                    string funcfile = funcTemplate;
//                    funcfile = funcfile.Replace("#Type#", proTypeName);
//                    funcfile = funcfile.Replace("#PropertyName#", propertyName);
//                    funcStr += funcfile;

//                    string dataStr = "";
//                    int idx = 0;
//                    //Model里的字段
//                    var an = new AssemblyName("GodModels");
//                    var ptype = Assembly.Load(an).GetType(proTypeName);
//                    var childMembers = ptype.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
//                    for (int j = 0; j < childMembers.Length; j++)
//                    {
//                        var pMember = childMembers[j];
//                        string mName = pMember.Name;
//                        //字段类型
//                        var mpType = pMember.PropertyType.FullName;
//                        mpType = CodeCommon.GetReturnTypeName(mpType);
//                        mpType = CodeCommon.GetSimpleTypeName(mpType);

//                        if (pMember.IsDefined(typeof(DataViewAttribute)))
//                        {
//                            var attributes = (DataViewAttribute)pMember.GetCustomAttribute(typeof(DataViewAttribute), false);
//                            string tips = attributes.Tips;
//                            if (!string.IsNullOrEmpty(tips))
//                            {
//                                dataStr += "        /// <summary>\n";
//                                dataStr += "        /// " + tips + "\n";
//                                dataStr += "        /// </summary>\n";
//                            }
//                        }
//                        idx++;
//                        if (pMember.CanWrite)
//                        {
//                            dataStr += "        [ProtoMember(" + idx + ")]\n";
//                            dataStr += "        public " + mpType + " " + mName + " { get; set; }\n";
//                        }
//                        else
//                        {
//                            string subName = mName;
//                            if (subName.Contains("My"))
//                                subName = subName.Substring(2);
//                            else if (subName.Contains("Cur"))
//                                subName = subName.Substring(3);
//                            dataStr += "        public " + mpType + " " + mName + " { get { return ("+mpType+")"+ subName + "; } }\n";
//                        }
//                    }

//                    //ViewModels
//                    string vm_class = CodeCommon.GetTemplate(m_template_path, "ViewModel.txt");
//                    vm_class = vm_class.Replace("#TypeName#", className);
//                    vm_class = vm_class.Replace("#Datas#", dataStr);
//                    string vfileName = m_server_path + @"\ViewModels\" + className + "Data.cs";
//                    CodeCommon.WriteFile(vfileName, vm_class);
//                }
//            }

//            //注册数据仓库
//            //string classTemplate = CodeCommon.GetTemplate(m_template_path, "ServerRepositoryExtension.txt");
//            //string server_class = classTemplate;
//            //server_class = server_class.Replace("#RepositoryFunc#", funcStr);
//            //string fileName = m_server_path + @"\Data\RepositoryServiceExtensions.cs";
//            //CodeCommon.WriteFile(fileName, server_class);

//            //automapper
//            string server_class = CodeCommon.GetTemplate(m_template_path, "ServerMapperModelsExtension.txt");
//            server_class = server_class.Replace("#MapperData#", modelStr);
//            string fileName = m_server_path + @"\Data\AutoMapperExtensions.cs";
//            CodeCommon.WriteFile(fileName, server_class);
//        }

//    }
//}
