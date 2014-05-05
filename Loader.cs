namespace migrate
{
    using System;
    using System.IO;
    using System.Reflection;

    public class Loader : MarshalByRefObject
    {
        public void Do(string basePath)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {                               
                var name = new AssemblyName(e.Name);

                var potentialFile = Path.Combine(basePath, name.Name + ".dll");

                if (File.Exists(potentialFile))
                {
                    return Assembly.LoadFrom(potentialFile);
                }

                return null;
            };
        }
    }
}