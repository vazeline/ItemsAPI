using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.Utility
{
    public static class Base64DataUrlUtility
    {
        private static readonly Regex RgxValidate = new Regex(@"^data:image\/[a-zA-Z0-9]+;base64,[a-zA-Z0-9+/]+={0,3}$");

        public static bool IsValid(string dataUrl) => RgxValidate.IsMatch(dataUrl);

        public static string BytesToBase64DataUrl(byte[] bytes, string contentType)
        {
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
