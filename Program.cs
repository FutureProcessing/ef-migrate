using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace migrate
{    
    using System.IO;
    using System.Reflection;

    class Program
    {
        private static Assembly entityFrameworkAssembly;

        static void Main(string[] args)
        {
            if (args.Length != 3 && args.Length != 4)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine();
                Console.WriteLine("\tmigrate <context assembly path> <provider name> <connection string> [<config file>]");
                return;
            }

            var migrationsAsemblyPath = Path.GetFullPath(args[0]);

            string configFile;

            if (args.Length == 4)
            {
                configFile = args[3];
            }
            else
            {
                configFile = Path.ChangeExtension(migrationsAsemblyPath, ".dll.config");      
            }

            var connectionProvider = args[1];
            var connectionString = args[2];                        

            var basePath = Path.GetDirectoryName(migrationsAsemblyPath);

            var name = Path.GetFileNameWithoutExtension(migrationsAsemblyPath);

            var toolDir = Path.GetDirectoryName(typeof (Program).Assembly.Location);

            new Loader().Do(basePath);

            var migrationsAssembly = Assembly.LoadFrom(migrationsAsemblyPath);

            var entityFrameworkAssemblyName = migrationsAssembly.GetReferencedAssemblies().Single(x => x.Name == "EntityFramework");

            entityFrameworkAssembly = Assembly.Load(entityFrameworkAssemblyName);

            Console.WriteLine("Found EF assembly: {0}", entityFrameworkAssembly.GetName().Version);            

            var connectionInfo = NewDbConnectionInfo(connectionString, connectionProvider);
            
            using (dynamic tooling = NewToolingFacade(name, toolDir, connectionInfo, configFile))
            {
                var domain = (AppDomain)tooling.GetType().GetField("_appDomain", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tooling);                

                var loader = (Loader)domain.CreateInstanceAndUnwrap(typeof (Loader).Assembly.FullName, typeof (Loader).FullName);

                tooling.LogInfoDelegate += new Action<string>(msg => Log(ConsoleColor.White, msg));
                tooling.LogWarningDelegate +=  new Action<string>(msg => Log(ConsoleColor.Yellow, msg));                

                loader.Do(basePath);

                tooling.Update(null, false);
            }
        }

        private static dynamic NewToolingFacade(string name, string toolDir, object connectionInfo, string configFile)
        {
            if (entityFrameworkAssembly.GetName().Version.Major == 5)
            {
                return NewToolingFacadeForEf5(name, toolDir, connectionInfo, configFile);
            }

            return NewToolingFacadeForEf6(name, toolDir, connectionInfo, configFile);
        }

        private static dynamic NewToolingFacadeForEf5(string name, string toolDir, object connectionInfo, string configFile)
        {
            var t = entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.Design.ToolingFacade");

            return Activator.CreateInstance(t, name, "", toolDir, configFile, ".", connectionInfo);
        }

        private static dynamic NewToolingFacadeForEf6(string name, string toolDir, object connectionInfo, string configFile)
        {
            var t = entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.Design.ToolingFacade");

            return Activator.CreateInstance(t, name, name, "", toolDir, configFile, ".", connectionInfo);
        }

        private static object NewDbConnectionInfo(string connectionString, string connectionProvider)
        {
            var t = entityFrameworkAssembly.GetType("System.Data.Entity.Infrastructure.DbConnectionInfo");

            return Activator.CreateInstance(t, connectionString, connectionProvider);
        }

        private static void Log(ConsoleColor color, string msg)
        {
            var old = Console.ForegroundColor;            
            
            Console.ForegroundColor = color;

            Console.WriteLine(msg);

            Console.ForegroundColor = old;
        }
    }
}
