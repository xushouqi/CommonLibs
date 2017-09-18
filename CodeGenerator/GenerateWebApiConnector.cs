using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using CommonLibs;

namespace CodeGenerator
{
    class GenerateWebApiConnector
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
            //m_accountservice = accountservice;
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
            var webapiAttri = (WebApiAttribute)vType.GetTypeInfo().GetCustomAttribute(typeof(WebApiAttribute), false);

            string connectorName = vType.Name.Replace("Service", "Api");
            string controllerName = vType.Name.Replace("Service", "Controller");
            //string interfaceName = "I" + vType.Name;
            string interfaceName = vType.FullName;

            //服务端Controller
            string server_controller_template = CodeCommon.GetTemplate(m_template_path, "ServerController.txt");
            //string server_controller_method_encrypt_template = CodeCommon.GetTemplate(m_template_path, "ServerControllerMethodAsyncEncrypt.txt");
            string server_controller_method_template = CodeCommon.GetTemplate(m_template_path, "ServerControllerMethodAsyncEncrypt.txt");
            string server_controller_method_auth_template = CodeCommon.GetTemplate(m_template_path, "ServerControllerMethodAysncAuthEncrypt.txt");
            //string server_controller_method_auth_encrypt_template = CodeCommon.GetTemplate(m_template_path, "ServerControllerMethodAysncAuthEncrypt.txt");

            string client_connector_template = CodeCommon.GetTemplate(m_template_path, "ClientConnector.txt");
            string client_connector_mtehod_template = CodeCommon.GetTemplate(m_template_path, "ClientConnectorMethodEncrypt.txt");
            //string client_connector_mtehodget_template = CodeCommon.GetTemplate(m_template_path, "ClientConnectorMethodGet.txt");
            //string client_connector_mtehodencrypt_template = CodeCommon.GetTemplate(m_template_path, "ClientConnectorMethodEncrypt.txt");

            //服务端Controller里的Method群
            string server_controller_methods = "";
            //客户端
            string client_connector_methods = "";

            string modelPrject = m_modelAssembly.FullName.Split(",")[0];

            //方法生成请求接口
            MethodInfo[] vMethodInfos = vType.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
            if (vMethodInfos.Length > 0)
            {
                for (int i = 0; i < vMethodInfos.Length; i++)
                {
                    MethodInfo vMethodInfo = vMethodInfos[i];
                    if (!vMethodInfo.Name.StartsWith("get_") && !vMethodInfo.Name.StartsWith("set_"))
                    {
                        if (vMethodInfo.IsDefined(typeof(ApiAttribute), false))
                        {
                            var pars = vMethodInfo.ReturnParameter;
                            //返回值
                            System.Type methodReturnType = vMethodInfo.ReturnType;
                            var attributes = (ApiAttribute)vMethodInfo.GetCustomAttribute(typeof(ApiAttribute), false);

                            //需要验证account
                            bool needauthaccount = attributes.AuthPolicy == UserTypeEnum.Member;

                            string client_connector_method = client_connector_mtehod_template;
                            //if (attributes.Encrypt)
                            //    client_connector_method = client_connector_mtehodencrypt_template;
                            //else if (attributes.IsGet)
                            //    client_connector_method = client_connector_mtehodget_template;

                            string clientRoute = vType.Name.Replace("Service", "/" + vMethodInfo.Name);

                            string methodParams = "";
                            string inputParams = "";

                            string clientMethodParams = "";
                            string dicParams = "";
                            string paramsFromDic = "";
                            string encryptParams = "";
                            string queryString = "";
                            //string decryptString = "";
                            string ValidDic = "";
                            string serverUseParams = "";

                            //遍历入参
                            ParameterInfo[] paramInfos = vMethodInfo.GetParameters();
                            for (int j = 0; j < paramInfos.Length; j++)
                            {
                                ParameterInfo paramInfo = paramInfos[j];
                                System.Type stype = paramInfo.ParameterType;
                                string typestr = Common.GetSimpleTypeName(stype.ToString());
                                //curaccnout
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
                                        methodParams += typestr + " " + paramInfo.Name;
                                        clientMethodParams += typestr + " " + paramInfo.Name;
                                        ValidDic += "tmp.ContainsKey(\"" + paramInfo.Name + "\")";

                                        //是枚举类型
                                        if (stype.Name.ToLower().Contains("enum"))
                                        {
                                            dicParams += "{ \"" + paramInfo.Name + "\", ((int)" + paramInfo.Name + ").ToString()}";
                                            queryString += "\"" + paramInfo.Name + "=\" + ((int)" + paramInfo.Name + ").ToString()";

                                            paramsFromDic += "(" + typestr + ")(int.Parse(tmp[\"" + paramInfo.Name + "\"]))";
                                        }
                                        else
                                        {
                                            dicParams += "{ \"" + paramInfo.Name + "\", " + paramInfo.Name + ".ToString()}";
                                            queryString += "\"" + paramInfo.Name + "=\" + " + paramInfo.Name + ".ToString()";
                                            encryptParams += paramInfo.Name + ".ToString()";

                                            if (typestr.ToLower().Contains("string"))
                                                paramsFromDic += "tmp[\"" + paramInfo.Name + "\"]";
                                            else
                                                paramsFromDic += typestr + ".Parse(tmp[\"" + paramInfo.Name + "\"])";
                                        }
                                    }
                                }
                                else
                                    paramsFromDic += paramInfo.Name;
                                if (!CodeCommon.CheckParamSocket(paramInfo))
                                    inputParams += paramInfo.Name;

                                if (j < paramInfos.Length - 1)
                                {
                                    if (paramInfos.Length > j + 1 && CodeCommon.CheckParamSocket(paramInfos[j + 1]))
                                    {

                                    }
                                    else
                                    {
                                        if (!skipMethd)
                                        {
                                            methodParams += ", ";
                                            clientMethodParams += ", ";
                                            dicParams += ", ";
                                            //paramsFromDic += ", ";
                                            queryString += "+\"&\" + ";
                                            encryptParams += " + ";
                                            ValidDic += " && ";
                                        }
                                        inputParams += ", ";
                                        paramsFromDic += ", ";
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(ValidDic))
                                ValidDic = " && " + ValidDic;

                            //需要加密
                            if (!string.IsNullOrEmpty(methodParams))
                                methodParams += ", string sign";
                            else
                                methodParams += "string sign";
                            if (!string.IsNullOrEmpty(dicParams)
                                //&& attributes.Encrypt
                                )
                                dicParams += ", { \"sign\", RsaService.Encrypt(" + encryptParams + ")}";

                            if (string.IsNullOrEmpty(queryString))
                                queryString = "\"\"";

                            if (attributes.AuthPolicy != UserTypeEnum.None)
                            {
                                if (!string.IsNullOrEmpty(clientMethodParams))
                                    clientMethodParams = "string token, " + clientMethodParams;
                                else
                                    clientMethodParams = "string token";
                            }

                            string methodReturnTypeName = methodReturnType.FullName;
                            bool isReturnData = methodReturnTypeName.Contains("ReturnData");

                            //提取实际返回值类型
                            string returnTypeName = methodReturnTypeName;
                            //获取<>前的类型
                            string innerType = returnTypeName;
                            string mapperReturn = "";

                            CodeCommon.ParseReturnType(methodReturnType, attributes.ReturnType, modelPrject,
                                ref methodReturnTypeName, ref returnTypeName, ref mapperReturn, ref innerType);


                            //method模版
                            string server_controller_method = needauthaccount ? server_controller_method_auth_template : server_controller_method_template;

                            server_controller_method = server_controller_method.Replace("#MethodName#", vMethodInfo.Name);
                            server_controller_method = server_controller_method.Replace("#ModelProject#", modelPrject);
                            server_controller_method = server_controller_method.Replace("#ParamsDeclare#", methodParams);
                            server_controller_method = server_controller_method.Replace("#ParamsInput#", inputParams);
                            server_controller_method = server_controller_method.Replace("#MethodRetunType#", methodReturnTypeName);
                            //server_controller_method = server_controller_method.Replace("#Decrypt#", decryptString);
                            if (attributes.AuthPolicy == UserTypeEnum.Member)
                            {
                                server_controller_method = server_controller_method.Replace("#AuthPolicy#", "[Authorize(Policy = \"Member\")]");
                            }
                            else if (attributes.AuthPolicy == UserTypeEnum.SuperAdmin)
                            {
                                server_controller_method = server_controller_method.Replace("#AuthPolicy#", "[Authorize(Policy = \"SuperAdmin\")]");
                            }
                            else if (attributes.AuthPolicy == UserTypeEnum.Admin)
                                server_controller_method = server_controller_method.Replace("#AuthPolicy#", "[Authorize(Policy = \"Admin\")]");
                            else
                                server_controller_method = server_controller_method.Replace("#AuthPolicy#", "[AllowAnonymous]");

                            server_controller_method = server_controller_method.Replace("#MapperReturn#", mapperReturn);
                            server_controller_method = server_controller_method.Replace("#ParamsFromDic#", paramsFromDic);
                            server_controller_method = server_controller_method.Replace("#ValidDic#", ValidDic);

                            //这个必须最后
                            server_controller_method = server_controller_method.Replace("#ReturnType#", returnTypeName);

                            //if (attributes.IsGet)
                            //    server_controller_method = server_controller_method.Replace("[HttpPost]", "[HttpGet]");

                            if (!methodReturnType.FullName.Contains("Task"))
                                server_controller_method = server_controller_method.Replace(" await ", " ");

                            server_controller_methods += server_controller_method;

                            string WithReturnType = returnTypeName;
                            if (isReturnData)
                                WithReturnType = "ReturnData<" + returnTypeName + ">";

                            //client
                            client_connector_method = client_connector_method.Replace("#ServerName#", m_project_name);
                            client_connector_method = client_connector_method.Replace("#ModelProject#", modelPrject);
                            client_connector_method = client_connector_method.Replace("#MethodName#", vMethodInfo.Name);
                            client_connector_method = client_connector_method.Replace("#ReturnType#", returnTypeName);
                            client_connector_method = client_connector_method.Replace("#WithReturnType#", WithReturnType);
                            client_connector_method = client_connector_method.Replace("#MethodParams#", clientMethodParams);
                            client_connector_method = client_connector_method.Replace("#Route#", clientRoute);
                            client_connector_method = client_connector_method.Replace("#QueryString#", queryString);
                            client_connector_method = client_connector_method.Replace("#DicParams#", dicParams);
                            client_connector_method = client_connector_method.Replace("#TokenHeader#", attributes.AuthPolicy != UserTypeEnum.None ?
                                                                                                        "client.DefaultRequestHeaders.Add(\"Authorization\", \"Bearer \" + token);"
                                                                                                        : "");
                            if (!string.IsNullOrEmpty(attributes.Tips))
                                client_connector_method = client_connector_method.Replace("#Tips#", attributes.Tips);
                            else
                                client_connector_method = client_connector_method.Replace("#Tips#", vMethodInfo.Name);

                            client_connector_methods += client_connector_method;
                        }
                    }
                }
            }
                        
            //服务端Controller
            string server_controller_file = server_controller_template;
            server_controller_file = server_controller_file.Replace("#PrivKeyName#", webapiAttri.PrivKeyName);
            server_controller_file = server_controller_file.Replace("#ControllerName#", controllerName);
            server_controller_file = server_controller_file.Replace("#ModelProject#", modelPrject);
            server_controller_file = server_controller_file.Replace("#Interface#", interfaceName);
            server_controller_file = server_controller_file.Replace("#ProjectName#", m_project_name);

            server_controller_file = server_controller_file.Replace("#Methods#", server_controller_methods);

            string controllerFilename = m_server_path + @"\Controllers\" + controllerName + ".cs";
            CodeCommon.WriteFile(controllerFilename, server_controller_file);

            //客户端
            if (!string.IsNullOrEmpty(m_client_path))
            {
                string client_connector_file = client_connector_template;
                client_connector_file = client_connector_file.Replace("#ClassName#", connectorName);
                client_connector_file = client_connector_file.Replace("#ServerName#", m_project_name);
                client_connector_file = client_connector_file.Replace("#ModelProject#", modelPrject);

                client_connector_file = client_connector_file.Replace("#Methods#", client_connector_methods);

                string connectorFilename = m_client_path + m_project_name + "." + connectorName + ".cs";
                CodeCommon.WriteFile(connectorFilename, client_connector_file);
            }
        }

    }
}
