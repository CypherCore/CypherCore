using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Activators;

namespace Game.Extendability
{
    public static class IOHelpers
    {
        private static Dictionary<string, List<Assembly>> _loadedAssemblies= new Dictionary<string, List<Assembly>>();

        public static List<Assembly> GetAllAssembliesInDir(string path, bool loadGameAssembly = true)
        {
            List<Assembly> assemblies = _loadedAssemblies.LookupByKey(path);

            if (assemblies !=null)
                return assemblies;
            else 
                assemblies= new List<Assembly>();

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var dir = new DirectoryInfo(path);

            var dlls = dir.GetFiles("*.dll", SearchOption.AllDirectories);

            foreach (var dll in dlls)
                assemblies.Add(Assembly.LoadFile(dll.FullName));

            if (loadGameAssembly)
                assemblies.Add(typeof(IScriptActivator).Assembly);

            _loadedAssemblies[path] = assemblies;

            return assemblies;
        }

        public static bool DoesTypeSupportInterface(Type type, Type inter)
        {
            if (type == inter) return false;

            if (inter.IsAssignableFrom(type))
                return true;

            if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == inter))
                return true;

            return false;
        }

        public static bool AreObjectsNotEqual(object obj1, object obj2)
        {
            return !AreObjectsEqual(obj1, obj2);
        }

        /// <summary>
        ///     Compares the values of 2 objects
        /// </summary>
        /// <returns>if types are equal and have the same property values</returns>
        public static bool AreObjectsEqual(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null)
                return obj1 == obj2;

            Type type = obj1.GetType();

            if (type != obj2.GetType())
                return false;
            
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                object value1 = property.GetValue(obj1);
                object value2 = property.GetValue(obj2);

                if (!Equals(value1, value2))
                    return false;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                var value1 = field.GetValue(obj1);
                var value2 = field.GetValue(obj2);
                if (!Equals(value1, value2))
                    return false;
            }

            return true;
        }
    }
}
