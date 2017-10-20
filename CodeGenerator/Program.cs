using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Reflection;
using CommonLibs;
using PowerArgs;

namespace CodeGenerator
{
    public class MyArgs
    {
        [ArgShortcut("-c"), ArgDescription("Generate client")]
        public bool ClientApi { get; set; } = false;

        [ArgRequired(PromptIfMissing = true)]
        [ArgShortcut("-t")]
        public string TemplatePath { get; set; }

        //[ArgPosition(2)]
        [ArgRequired(PromptIfMissing = true)]
        [ArgShortcut("-d")]
        public string TargetDll { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            try
            {
                var arguments = Args.Parse<MyArgs>(args);
                Console.WriteLine("TemplatePath={0}, TargetDll={1}", arguments.TemplatePath, arguments.TargetDll);

                Process pInfo = Process.GetCurrentProcess();
                //运行目录
                string runPath = pInfo.MainModule.FileName;
                if (!runPath.Contains("CodeGenerator"))
                    runPath = Directory.GetCurrentDirectory();
                else
                    runPath = runPath.Substring(0, runPath.LastIndexOf(@"\"));

                //工程目录
                string solutionPath = runPath;
                int lastOfPath = runPath.LastIndexOf(@"\CodeGenerator");
                if (lastOfPath > 0)
                    solutionPath = runPath.Substring(0, lastOfPath);
                lastOfPath = solutionPath.LastIndexOf(@"\Common");
                if (lastOfPath > 0)
                    solutionPath = solutionPath.Substring(0, lastOfPath);

                //模版地址
                string template_path = arguments.TemplatePath;
                if (!template_path.EndsWith(@"\"))
                    template_path += @"\";
                Console.WriteLine("template_path={0}", template_path);

                //目标项目文件
                string dllfile = arguments.TargetDll;

                int lastOfP = dllfile.LastIndexOf(@"\");
                //dll目录
                var dllPath = dllfile.Substring(0, lastOfP);
                if (!dllPath.Contains(":"))
                    dllPath = runPath + @"\" + dllPath;
                Console.WriteLine("dllPath={0}", dllPath);

                //取项目名称
                string project_name = dllfile.Substring(lastOfP + 1, dllfile.LastIndexOf(".") - lastOfP - 1);
                Console.WriteLine("project_name={0}", project_name);

                string server_path = "";
                string client_path = "";
                //string api_client_path = solutionPath + @"\ClientApiConnector\WebApi\";

                Assembly myAssembly = null;
                Assembly commonAssembly = null;
                List<Assembly> modelsAssemblyList = new List<Assembly>();

                //加载同一目录下的所有dll
                var files = Directory.GetFiles(dllPath, "*.dll");
                foreach (var file in files)
                {
                    Console.WriteLine("AssemblyLoadContext dllfile={0}", file);
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);

                    var filename = file.Substring(file.LastIndexOf(@"\") + 1);

                    //特别指出CommonLibs
                    if (filename.Contains("CommonLibs"))
                        commonAssembly = assembly;
                    //XXXModels
                    else if (filename.Contains("Models"))
                        modelsAssemblyList.Add(assembly);
                    //主程序
                    else if (filename.ToLower().Contains(project_name.ToLower()))
                        myAssembly = assembly;
                }

                Type[] types;
                try
                {
                    types = myAssembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                string serviceName = project_name + ".Services";
                Console.WriteLine("CodeGenerator types={0}, serviceName={1}", types.Length, serviceName);

                //寻找相同的model
                Assembly modelsAssembly = null;

                List<Type> socketTypeList = new List<Type>();
                List<Type> dataTypeList = new List<Type>();
                for (int i = 0; i < types.Length; i++)
                {
                    var myType = types[i];
                    if (myType != null)
                    {
                        var tName = myType.FullName;

                        //恢复正确的大小写
                        project_name = tName.Split('.')[0];
                        server_path = solutionPath + @"\" + project_name;

                        if (modelsAssemblyList.Count > 0)
                        {
                            modelsAssembly = modelsAssemblyList[0];
                            for (int m = 0; m < modelsAssemblyList.Count; m++)
                            {
                                if (modelsAssemblyList[m].FullName.StartsWith(project_name.Substring(0, 4)))
                                {
                                    modelsAssembly = modelsAssemblyList[m];
                                    break;
                                }
                            }
                        }

                        if (myType.GetTypeInfo().IsDefined(commonAssembly.GetType(typeof(WebApiAttribute).FullName), false))
                        {
                            client_path = solutionPath + @"\Client\" + project_name + @"Client\WebApi\";
                            
                            GenerateWebApiConnector.InitPath(modelsAssembly, template_path, project_name, server_path, client_path);
                            GenerateWebApiConnector.GenerateFromService(myType, arguments.ClientApi);
                        }
                        if (myType.GetTypeInfo().IsDefined(typeof(WebSocketAttribute), false))
                        {
                            client_path = solutionPath + @"\Client\" + project_name + @"Client\Socket\";

                            GenerateSocketClient.InitPath(modelsAssembly, template_path, project_name, server_path, client_path);
                            GenerateSocketClient.GenerateFromService(myType, arguments.ClientApi);

                            socketTypeList.Add(myType);
                        }
                    }
                }

                if (socketTypeList.Count > 0)
                {
                    client_path = "";

                    GenerateSocketClasses.InitPath(modelsAssembly, template_path, project_name, server_path, client_path);
                    GenerateSocketClasses.GenerateFromService(socketTypeList.ToArray(), arguments.ClientApi);
                }

                string modelProjectName = "";
                Type[] modelTypes;
                try
                {
                    //modelTypes = modelsAssembly.GetTypes();

                    var typelist = new List<Type>();
                    for (int m = 0; m < modelsAssemblyList.Count; m++)
                    {
                        var mTypes = modelsAssemblyList[m].GetTypes();
                        typelist.AddRange(mTypes);

                        if (!modelsAssemblyList[m].FullName.Contains("GodModels") || modelsAssemblyList.Count == 1)
                            modelProjectName = modelsAssemblyList[m].FullName.Split(',')[0];
                    }
                    modelTypes = typelist.ToArray();
                }
                catch (ReflectionTypeLoadException e)
                {
                    modelTypes = e.Types;
                }
                for (int i = 0; i < modelTypes.Length; i++)
                {
                    var myType = modelTypes[i];
                    if (myType != null)
                    {
                        var tName = myType.FullName;

                        //project_name = tName.Split('.')[0];

                        if (myType.GetTypeInfo().IsDefined(commonAssembly.GetType(typeof(DataModelsAttribute).FullName), false))
                        {
                            dataTypeList.Add(myType);
                        }
                    }
                }

                if (dataTypeList.Count > 0)
                {
                    string models_path = solutionPath + @"\Models\" + modelProjectName + @"\ViewModels\";

                    GenerateDataModel.InitPath(template_path, project_name, modelProjectName, server_path, models_path);
                    GenerateDataModel.GenerateFromData(dataTypeList.ToArray());
                }
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<MyArgs>());
            }
        }
    }
}