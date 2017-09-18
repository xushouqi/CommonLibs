using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using CommonLibs;

namespace CodeGenerator
{
    class GenerateWebSocketClient
    {
        static string m_template_path = string.Empty;
        static string m_server_path = string.Empty;
        static string m_client_path = string.Empty;
        static string m_service_name = string.Empty;
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

        public static void GenerateFromService<T>()
        {
            GenerateFromService(typeof(T));
        }

        public static void GenerateFromService(System.Type vType)
        {
            m_service_name = vType.Name;

            string modelPrject = m_modelAssembly.FullName.Split(",")[0];

            //eg: User
            string className = m_service_name.Replace("Service", "");

            string clientTemplate = CodeCommon.GetTemplate(m_template_path, "ClientServiceClient.txt");
            string clientMethodTemplate = CodeCommon.GetTemplate(m_template_path, "ClientServiceMethod.txt");
            string clientMethodAsyncTemplate = CodeCommon.GetTemplate(m_template_path, "ClientServiceMethodAsync.txt");
            string clientCallbackTemplate = CodeCommon.GetTemplate(m_template_path, "ClientServiceCallback.txt");
            string clientRegAddTemplate = CodeCommon.GetTemplate(m_template_path, "ClientRegisterPushAdd.txt");

            string serverActionTemplate = CodeCommon.GetTemplate(m_template_path, "ServerAction.txt");
            string serverActionAuthTemplate = CodeCommon.GetTemplate(m_template_path, "ServerAction.txt");

            string client_methods = "";
            string client_callbacks = "";
            string client_regcallbacks = "";
            string client_regcallbackadds = "";

            //方法生成请求接口
            MethodInfo[] vMethodInfos = vType.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

            for (int i = 0; i < vMethodInfos.Length; i++)
            {
                MethodInfo vMethodInfo = vMethodInfos[i];
                if (vMethodInfo.IsDefined(typeof(ApiAttribute), false))
                {
                    var attributes = (ApiAttribute)vMethodInfo.GetCustomAttribute(typeof(ApiAttribute), false);

                    //客户端注册回调函数
                    client_regcallbacks += "            client.RegActions[" + attributes.ActionId + "] = On" + vMethodInfo.Name + "CallBack;\n";
              
                    //返回值
                    Type methodReturnType = vMethodInfo.ReturnType;
                    string methodReturnTypeName = methodReturnType.FullName;
                    bool isReturnData = methodReturnTypeName.Contains("ReturnData");

                    //提取实际返回值类型
                    string returnTypeName = methodReturnTypeName;
                    //获取<>前的类型
                    string innerType = returnTypeName;
                    string mapperReturn = "";

                    CodeCommon.ParseReturnType(methodReturnType, attributes.ReturnType, modelPrject,
                        ref methodReturnTypeName, ref returnTypeName, ref mapperReturn, ref innerType);

                    string serverInputParams = "";
                    string serverUseParams = "";
                    string serverReadBytes = "";
                    string clientInputParams = "";
                    string clientWriteBytes = "";

                    //遍历入参
                    ParameterInfo[] paramInfos = vMethodInfo.GetParameters();
                    for (int j = 0; j < paramInfos.Length; j++)
                    {
                        ParameterInfo paramInfo = paramInfos[j];
                        System.Type stype = paramInfo.ParameterType;
                        string typestr = Common.GetSimpleTypeName(stype.ToString());

                        bool skipMethd = attributes.AuthPolicy != UserTypeEnum.None && paramInfo.Name.Equals("accountId");
                        if (skipMethd)
                            serverUseParams = "m_accountId";
                        //if (attributes.AuthIDType != AuthIDTypeEnum.None && j == 0)
                        //{
                        //    if (attributes.AuthIDType == AuthIDTypeEnum.AccountId && paramInfo.Name.Equals("accountId"))
                        //    {
                        //        skipMethd = true;
                        //        serverUseParams = "m_accountId";
                        //    }
                        //    else if (attributes.AuthIDType == AuthIDTypeEnum.RoleId && paramInfo.Name.Equals("roleId"))
                        //    {
                        //        skipMethd = true;
                        //        serverUseParams = "m_roleId";
                        //    }
                        //    else if (attributes.AuthIDType == AuthIDTypeEnum.TeamId && paramInfo.Name.Equals("teamId"))
                        //    {
                        //        skipMethd = true;
                        //        serverUseParams = "m_teamId";
                        //    }
                        //}
                        if (!skipMethd)
                        { 
                            if (CodeCommon.CheckParamSocket(paramInfo))
                            {
                                serverUseParams += "m_socket";
                                skipMethd = true;
                            }
                            else
                            {
                                serverUseParams += paramInfo.Name;
                                serverInputParams += typestr + " " + paramInfo.Name;

                                if (stype.Name.ToLower().Contains("enum"))
                                {
                                    serverReadBytes += "                var " + paramInfo.Name + " = (" + typestr + ")m_params.ReadInt();\n";
                                    clientWriteBytes += "			    pars.WriteByte((int)" + paramInfo.Name + ");\n";
                                }
                                else
                                {
                                    string read = CodeCommon.GetReadString(stype.ToString());
                                    serverReadBytes += "                var " + paramInfo.Name + " = m_params." + read + "();\n";
                                    clientWriteBytes += "			    pars.WriteByte(" + paramInfo.Name + ");\n";
                                }
                            }
                        }

                        if (j < paramInfos.Length - 1)
                        {
                            if (paramInfos.Length > j + 1 && CodeCommon.CheckParamSocket(paramInfos[j + 1]))
                            {

                            }
                            else if (!skipMethd)
                            {
                                serverInputParams += ", ";
                            }
                            serverUseParams += ", ";
                        }
                    }
                    clientInputParams = serverInputParams;
                    //末尾带逗号
                    var clientInputParamsWithComma = clientInputParams;
                    if (!string.IsNullOrEmpty(clientInputParamsWithComma))
                        clientInputParamsWithComma += ", ";

                    //method模版
                    string client_method = clientMethodTemplate;
                    client_method = client_method.Replace("#ProjectName#", m_project_name);
                    client_method = client_method.Replace("#ReturnType#", returnTypeName);
                    client_method = client_method.Replace("#MethodName#", vMethodInfo.Name);
                    client_method = client_method.Replace("#MethodParams#", clientInputParamsWithComma);
                    client_method = client_method.Replace("#ActionId#", attributes.ActionId.ToString());
                    client_method = client_method.Replace("#WriteParams#", clientWriteBytes);
                    client_methods += client_method;

                    string client_methodAsync = clientMethodAsyncTemplate;
                    client_methodAsync = client_methodAsync.Replace("#ProjectName#", m_project_name);
                    client_methodAsync = client_methodAsync.Replace("#ReturnType#", returnTypeName);
                    client_methodAsync = client_methodAsync.Replace("#MethodName#", vMethodInfo.Name);
                    client_methodAsync = client_methodAsync.Replace("#MethodParams#", clientInputParams);
                    client_methodAsync = client_methodAsync.Replace("#ActionId#", attributes.ActionId.ToString());
                    client_methodAsync = client_methodAsync.Replace("#WriteParams#", clientWriteBytes);
                    client_methods += client_methodAsync;

                    //callback模版
                    string client_callback = clientCallbackTemplate;
                    client_callback = client_callback.Replace("#ProjectName#", m_project_name);
                    client_callback = client_callback.Replace("#ReturnType#", returnTypeName);
                    client_callback = client_callback.Replace("#MethodName#", vMethodInfo.Name);
                    client_callback = client_callback.Replace("#MethodParams#", clientInputParams);
                    client_callback = client_callback.Replace("#ActionId#", attributes.ActionId.ToString());
                    client_callback = client_callback.Replace("#WriteParams#", clientWriteBytes);
                    client_callbacks += client_callback;

                    //callbackadd模版
                    string client_callbackadd = clientRegAddTemplate;
                    client_callbackadd = client_callbackadd.Replace("#ReturnType#", returnTypeName);
                    client_callbackadd = client_callbackadd.Replace("#MethodName#", vMethodInfo.Name);
                    client_regcallbackadds += client_callbackadd;

                    string server_action = attributes.AuthPolicy != UserTypeEnum.None ? serverActionAuthTemplate : serverActionTemplate;
                    server_action = server_action.Replace("#ProjectName#", m_project_name);
                    server_action = server_action.Replace("#ModelProject#", modelPrject);
                    server_action = server_action.Replace("#ServiceName#", m_service_name);
                    server_action = server_action.Replace("#MethodName#", vMethodInfo.Name);
                    server_action = server_action.Replace("#ActionId#", attributes.ActionId.ToString());
                    server_action = server_action.Replace("#ClassName#", className);
                    server_action = server_action.Replace("#ReadParams#", serverReadBytes);
                    server_action = server_action.Replace("#Params#", serverUseParams);
                    server_action = server_action.Replace("#ReturnType#", returnTypeName);
                    server_action = server_action.Replace("#MapperReturn#", mapperReturn);

                    server_action = server_action.Replace("#Attribute#", "[AuthPolicy(AuthPolicy = UserTypeEnum." + attributes.AuthPolicy.ToString() + ")]");

                    string serverFileName = "Action" + attributes.ActionId.ToString();
                    serverFileName = m_server_path + @"\Actions\" + serverFileName + ".cs";
                    CodeCommon.WriteFile(serverFileName, server_action);
                }
            }

            //客户端submit接口
            if (!string.IsNullOrEmpty(m_client_path))
            {
                string client_class = clientTemplate;
                client_class = client_class.Replace("#ProjectName#", m_project_name);
                client_class = client_class.Replace("#ModelProject#", modelPrject);
                client_class = client_class.Replace("#ClassName#", className);
                client_class = client_class.Replace("#Methods#", client_methods);
                client_class = client_class.Replace("#Callbacks#", client_callbacks);
                client_class = client_class.Replace("#RegActions#", client_regcallbacks);
                client_class = client_class.Replace("#AddRemove#", client_regcallbackadds);

                string clientFileName = m_service_name.Replace("Service", "Client");
                clientFileName = m_client_path + m_project_name + "." + clientFileName + ".cs";
                CodeCommon.WriteFile(clientFileName, client_class);
            }
            
        }


    }
}
