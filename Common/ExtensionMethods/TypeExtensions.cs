using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Utility;

namespace Common.ExtensionMethods
{
    public static class TypeExtensions
    {
        public static string FriendlyName(this Type t)
        {
            return TypeUtility.GetFriendlyName(t);
        }
    }
}
