namespace ISDOX.DMS.Application.Interfaces
{
    public interface IDocumentTextExtractor
    {
        string ExtractText(string filePath, string extension);
    }
}
