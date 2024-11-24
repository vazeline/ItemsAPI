using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ExtensionMethods
{
    public static class StreamExtensions
    {
        public static MemoryStream CopyToMemoryStream(this Stream input)
        {
            var memoryStream = new MemoryStream();
            input.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static async Task<MemoryStream> CopyToMemoryStreamAsync(this Stream input)
        {
            var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
