using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;

namespace Common.Utility
{
    public class AssemblyUtility
    {
        public static Assembly[] GetProjectAndItemsAssemblies(string rootSolutionNamespaceToSearch, List<Type> excludeAssembliesContainingTypes = null)
        {
            excludeAssembliesContainingTypes ??= new List<Type>();
            var excludeAssemblies = excludeAssembliesContainingTypes.Select(x => x.Assembly).Distinct().ToList();

            // AppDomain.CurrentDomain.GetAssemblies() contains only currently loaded assemblies. Not
            // always all referenced assemblies !!!
            var possibleAssemblies = DependencyContext.Default.GetDefaultAssemblyNames()
                .Where(x => x.FullName.StartsWith(rootSolutionNamespaceToSearch, StringComparison.InvariantCultureIgnoreCase)
                    || x.FullName.StartsWith("Items", StringComparison.InvariantCultureIgnoreCase))
                .Select(x => Assembly.Load(x.FullName))
                .Where(x => !excludeAssemblies.Contains(x))
                .ToArray();

            return possibleAssemblies;
        }
    }
}
