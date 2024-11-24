using System;
using System.Security.Cryptography;
using System.Text;

namespace Items.Common.Utility.Hashing
{
    public static class StringHashingUtil
    {
        private const int PBKDF2Iterations = 64000;
        private const int NumHashBytes = 18;

        public static string HashString(string str)
        {
#pragma warning disable SYSLIB0041 // Type or member is obsolete - we have to use this for legacy compatibility
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(str, Encoding.UTF8.GetBytes(Constants.HashingConstants.HashSalt)))
            {
                pbkdf2.IterationCount = PBKDF2Iterations;
                return Convert.ToBase64String(pbkdf2.GetBytes(NumHashBytes));
            }
#pragma warning restore SYSLIB0041
        }
    }
}
