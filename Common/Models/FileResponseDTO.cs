namespace Common.Models
{
    public class FileResponseDTO
    {
        public FileResponseDTO(byte[] data, string filename, string contentType)
        {
            this.Data = data;
            this.Filename = filename;
            this.ContentType = contentType;
        }

        public byte[] Data { get; set; }

        public string Filename { get; set; }

        public string ContentType { get; set; }
    }
}
