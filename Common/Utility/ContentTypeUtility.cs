using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.VisualBasic;

namespace Common.Utility
{
    public static class ContentTypeUtility
    {
        private static readonly Dictionary<string, string> FileExtensionToContentTypeMap = new()
        {
            { ".pdf", ContentType.PDF },
            { ".png", ContentType.PNG },
            { ".jpg", ContentType.JPEG },
            { ".jpeg", ContentType.JPEG },
            { ".doc", ContentType.Word },
            { ".docx", ContentType.Word },
            { ".zip", ContentType.Zip },
            { ".htm", ContentType.HTML },
            { ".html", ContentType.HTML },
            { ".txt", ContentType.Text }
        };

        public static bool TryGetFileExtensionForContentType(string contentType, out string extension)
        {
            extension = null;

            var extensionKeyValuePair = new FileExtensionContentTypeProvider().Mappings.FirstOrDefault(x => x.Value.ToLower() == contentType.ToLower());

            if (extensionKeyValuePair.Equals(default(KeyValuePair<string, string>)))
            {
                return false;
            }

            extension = extensionKeyValuePair.Key;

            return true;
        }

        public static bool IsContentTypeZip(string contentType)
        {
            return new[]
            {
                ContentType.Zip,
                ContentType.ZipAlternative1,
                ContentType.ZipAlternative2,
                ContentType.ZipAlternative3
            }.Contains(contentType.ToLower());
        }

        public static bool TryGetContentTypeForFileExtension(string fileName, out string contentType)
        {
            contentType = null;

            var extension = Path.GetExtension(fileName ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(extension))
            {
                return FileExtensionToContentTypeMap.TryGetValue(extension.ToLower(), out contentType);
            }

            return false;
        }
    }

    public static class ContentType
    {
        public static readonly string Default = "application/octet-stream";
        public static readonly string HTML = "text/html";
        public static readonly string JPEG = "image/jpeg";
        public static readonly string JSON = "application/json";
        public static readonly string PDF = "application/pdf";
        public static readonly string PNG = "image/png";
        public static readonly string Text = "text/plain";
        public static readonly string Word = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public static readonly string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public static readonly string OutlookEmail = "application/vnd.ms-outlook";
        public static readonly string Zip = "application/zip";
        public static readonly string ZipAlternative1 = "multipart/x-zip";
        public static readonly string ZipAlternative2 = "application/zip-compressed";
        public static readonly string ZipAlternative3 = "application/x-zip-compressed";
    }
}
