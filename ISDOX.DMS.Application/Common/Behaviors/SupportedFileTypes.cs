namespace ISDOX.DMS.Application.Common.Behaviors
{
    public static class SupportedFileTypes
    {
        public static readonly string[] AllowedExtensions = new[]
        {
            ".pdf",
            ".doc", ".docx",
            ".xls", ".xlsx",
            ".jpg", ".jpeg", ".png", ".tiff"
        };

        public static bool IsSupported(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(ext);
        }
    }
}
