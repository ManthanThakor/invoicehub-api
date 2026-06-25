using Microsoft.AspNetCore.Http;

namespace Application.Services.System
{
    public interface IFileService
    {
        Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default);
        Task<bool> DeleteAsync(string relativePath);
        Task<(byte[] Bytes, string ContentType, string FileName)> GetAsync(string relativePath);
        string GetPublicUrl(string relativePath);
        bool IsValidImage(IFormFile file);
        bool IsValidDocument(IFormFile file);
        IFormFile CompressImage(IFormFile file, int maxWidthPx = 800);
    }
}