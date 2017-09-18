using System;
using System.Collections.Generic;
using System.Reflection;
using CommonLibs;

namespace CodeGenerator
{
    class GenerateWebSocketClasses
    {
        static string m_template_path = string.Empty;
        static string m_server_path = string.Empty;
        static string m_client_path = string.Empty;
        static string m_project_name = string.Empty;
        static Assembly m_modelAssembly = null;

        public static void InitPath(Assembly modelAssembly, string template, string projectname, string server, string client)
        {
            m_project_name = projectname;
            m_template_path = template;
            m_server_path = server;
            m_client_path = client;
            m_modelAssembly = modelAssembly;
        }

        public static void GenerateFromService(Type[] types)
        {
            string regModuleStr = "";
            string regActionIdByType = "";
            string defineClient = "";
            string declareClient = "";

            string modelPrject = m_modelAssembly.FullName.Split(",")[0];

            for (int i = 0; i < types.Length; i++)
            {
                var vType = types[i];
                if (vType != null)
                {
                    var fullName = vType.FullName;
                    var service_name = vType.Name;

                    string className = service_name.Replace("Service", "");

                    defineClient += "        public " + className + "Client " + className + "Client = null;\n";
                    declareClient += "            " + className + "Client = new " + className + "Client(m_socket);\n";

                    //方法生成请求接口
                    MethodInfo[] vMethodInfos = vType.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
                    for (int j = 0; j < vMethodInfos.Length; j++)
                    {
                        MethodInfo vMethodInfo = vMethodInfos[j];
                        if (vMethodInfo.IsDefined(typeof(ApiAttribute), false))
                        {
                            var attributes = (ApiAttribute)vMethodInfo.GetCustomAttribute(typeof(ApiAttribute), false);

                            //返回值
                            string methodReturnTypeName = vMethodInfo.ReturnType.FullName;
                            //使用返回值结构
                            methodReturnTypeName = Common.GetReturnTypeName(methodReturnTypeName);
                            methodReturnTypeName = Common.GetSimpleTypeName(methodReturnTypeName);
                            //提取实际返回值类型
                            string returnTypeName = methodReturnTypeName;
                            //api设定的返回类型
                            if (attributes.ReturnType != null)
                            {
                                returnTypeName = attributes.ReturnType.FullName;
                                returnTypeName = Common.GetReturnTypeName(returnTypeName);
                                returnTypeName = Common.GetSimpleTypeName(returnTypeName);
                            }

                            //注册action
                            //regModuleStr += "            builder.RegisterType<Action" + attributes.ActionId + ">().InstancePerDependency();\n";
                            regModuleStr += "            services.AddTransient<Action" + attributes.ActionId + ">();\n";

                            if (attributes.RegPushData)
                                regActionIdByType += "            m_actionIdByType[typeof(" + returnTypeName + ")] = " + attributes.ActionId + ";\n";

                        }
                    }
                }
            }

            //注册模块
            string server_regmodule = CodeCommon.GetTemplate(m_template_path, "RegisterActionExtensions.txt");
            server_regmodule = server_regmodule.Replace("#Action#", regModuleStr);
            server_regmodule = server_regmodule.Replace("#ProjectName#", m_project_name);

            string regFileName = m_server_path + @"\Actions\" + "RegisterActionExtensions.cs";
            CodeCommon.WriteFile(regFileName, server_regmodule);

            //PushManager
            string pushmanager = CodeCommon.GetTemplate(m_template_path, "PushManager.txt");
            pushmanager = pushmanager.Replace("#RegActionIdByType#", regActionIdByType);
            pushmanager = pushmanager.Replace("#ModelProject#", modelPrject);

            string pushFile = m_server_path + @"\Actions\" + "PushManager.cs";
            CodeCommon.WriteFile(pushFile, pushmanager);

            //NetworkClient
            if (!string.IsNullOrEmpty(m_client_path))
            {
                string networkd_client = CodeCommon.GetTemplate(m_template_path, "NetworkClient.txt");
                networkd_client = networkd_client.Replace("#DefineClient#", defineClient);
                networkd_client = networkd_client.Replace("#DeclareClient#", declareClient);

                string clientFile = m_client_path + "NetworkClient.cs";
                CodeCommon.WriteFile(clientFile, networkd_client);
            }
        }

    }
}
