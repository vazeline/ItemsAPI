using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using Common.ExtensionMethods;
using Common.Models;
using Common.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp.PixelFormats;

namespace Items.Common.WebAPI.Models
{
    public class Base64FormFile : FormFile
    {
        public Base64FormFile(string fileName, string contentType, string base64EncodedData)
            : base(
                baseStream: new MemoryStream(Convert.FromBase64String(base64EncodedData)),
                baseStreamOffset: 0,
                length: Convert.FromBase64String(base64EncodedData).Length,
                name: fileName,
                fileName: fileName)
        {
            this.Headers = new HeaderDictionary
            {
                [HeaderNames.ContentType] = contentType
            };
        }

        private Base64FormFile(string fileName, string contentType, byte[] data)
            : base(
                baseStream: new MemoryStream(data),
                baseStreamOffset: 0,
                length: data.Length,
                name: fileName,
                fileName: fileName)
        {
            this.Headers = new HeaderDictionary
            {
                [HeaderNames.ContentType] = contentType
            };
        }

        public static OperationResult<Base64FormFile> Create(string fileName, string contentType, string base64EncodedData)
        {
            var result = new OperationResult<Base64FormFile>();

            result
                .Validate(fileName, ValidationExtensions.StringIsNotNullOrWhiteSpace)
                .Validate(
                    Path.GetExtension(fileName ?? string.Empty),
                    ValidationExtensions.StringIsNotNullOrWhiteSpace,
                    customErrorMessage: $"{nameof(fileName).ToTitleCase()} must have a file extension");

            if (!result.IsSuccessful)
            {
                return result;
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                ContentTypeUtility.TryGetContentTypeForFileExtension(fileName, out contentType);
            }

            result
                .Validate(fileName, ValidationExtensions.StringIsNotNullOrWhiteSpace)
                .Validate(contentType, ValidationExtensions.StringIsNotNullOrWhiteSpace)
                .Validate(
                    contentType ?? string.Empty,
                    ValidationExtensions.IsTrue,
                    x => MediaTypeHeaderValue.TryParse(x, out _),
                    customErrorMessage: $"{nameof(contentType).ToTitleCase()} {contentType} is not valid")
                .Validate(base64EncodedData, ValidationExtensions.StringIsNotNullOrWhiteSpace);

            if (!result.IsSuccessful)
            {
                return result;
            }

            var data = new Span<byte>(new byte[base64EncodedData.Length]);

            if (!Convert.TryFromBase64String(base64EncodedData, data, out _))
            {
                result.AddError($"Supplied {nameof(base64EncodedData).ToTitleCase()} is not valid", OperationResultErrorType.Validation);
                return result;
            }

            result.Data = new Base64FormFile(fileName, contentType, data.ToArray());

            return result;
        }
    }
}
