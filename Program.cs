using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace migrate
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Migrations.Design;
    using System.IO;
    using System.Reflection;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine();
                Console.WriteLine("\tmigrate <context assembly path> <provider name> <connection string>");
                return;
            }

            var migrationsAsembly = Path.GetFullPath(args[0]);

            var connectionProvider = args[1];
            var connectionString = args[2];
           
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                Console.WriteLine(e.Name);
                return null;
            };            

            var basePath = Path.GetDirectoryName(migrationsAsembly);

            var name = Path.GetFileNameWithoutExtension(migrationsAsembly);

            var toolDir = Path.GetDirectoryName(typeof (Program).Assembly.Location);
            
            var connectionInfo = new DbConnectionInfo(connectionString, connectionProvider);

            using (var tooling = new ToolingFacade(name, name, "", toolDir, Path.ChangeExtension(migrationsAsembly, ".dll.config"), ".", connectionInfo))
            {
                var domain = (AppDomain)tooling.GetType().GetField("_appDomain", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tooling);                

                var loader = (Loader)domain.CreateInstanceAndUnwrap(typeof (Loader).Assembly.FullName, typeof (Loader).FullName);

                tooling.LogInfoDelegate += msg => Log(ConsoleColor.White, msg);                
                tooling.LogWarningDelegate += msg => Log(ConsoleColor.Yellow, msg);                

                loader.Do(basePath);

                tooling.Update(null, false);
            }
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
