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
            if (args.Length != 3)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine();
                Console.WriteLine("\tmigrate <context assembly path> <provider name> <connection string>");
                return;
            }

            var migrationsAsemblyPath = Path.GetFullPath(args[0]);

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
            
            using (dynamic tooling = NewToolingFacade(name, toolDir, migrationsAsemblyPath, connectionInfo))
            {
                var domain = (AppDomain)tooling.GetType().GetField("_appDomain", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tooling);                

                var loader = (Loader)domain.CreateInstanceAndUnwrap(typeof (Loader).Assembly.FullName, typeof (Loader).FullName);

                tooling.LogInfoDelegate += new Action<string>(msg => Log(ConsoleColor.White, msg));
                tooling.LogWarningDelegate +=  new Action<string>(msg => Log(ConsoleColor.Yellow, msg));                

                loader.Do(basePath);

                tooling.Update(null, false);
            }
        }

        private static dynamic NewToolingFacade(string name, string toolDir, string migrationsAsemblyPath, object connectionInfo)
        {
            var t = entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.Design.ToolingFacade");

            return Activator.CreateInstance(t, name, name, "", toolDir, Path.ChangeExtension(migrationsAsemblyPath, ".dll.config"), ".", connectionInfo);
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
