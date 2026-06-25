
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace Application.Services.System
{

    public class FileService : IFileService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FileService> _log;
        private readonly IWebHostEnvironment _env;

        // Allowed MIME types
        private static readonly HashSet<string> ImageMimes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "image/gif" };

        private static readonly HashSet<string> DocMimes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "application/pdf" };

        private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private static readonly HashSet<string> DocExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

        private const long MaxImageBytes = 5 * 1024 * 1024;   // 5 MB
        private const long MaxDocBytes = 10 * 1024 * 1024;  // 10 MB

        private string UploadsRoot => Path.Combine(
            _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            _config["Storage:UploadFolder"] ?? "uploads");

        private string BaseUrl => _config["App:BaseUrl"] ?? "https://app.invoicehub.in";

        public FileService(IConfiguration config, ILogger<FileService> log, IWebHostEnvironment env)
        {
            _config = config; _log = log; _env = env;
        }

        // ── Save file ─────────────────────────────────────────────────────
        public async Task<string> SaveAsync(
            IFormFile file, string folder, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var relativePath = Path.Combine(folder, safeFileName)
                .Replace('\\', '/'); // always forward slashes

            var absoluteDir = Path.Combine(UploadsRoot, folder);
            Directory.CreateDirectory(absoluteDir);

            var absolutePath = Path.Combine(absoluteDir, safeFileName);

            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            _log.LogInformation("File saved: {RelativePath} ({Bytes} bytes)",
                relativePath, file.Length);

            return relativePath; // stored in DB — relative to UploadsRoot
        }

        // ── Delete file ───────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;

            var absolute = Path.Combine(UploadsRoot, relativePath.TrimStart('/'));
            if (!File.Exists(absolute))
            {
                _log.LogWarning("Delete: file not found at {Path}", absolute);
                return false;
            }

            await Task.Run(() => File.Delete(absolute));
            _log.LogInformation("File deleted: {RelativePath}", relativePath);
            return true;
        }

        // ── Get file bytes + content type ──────────────────────────────────
        public async Task<(byte[] Bytes, string ContentType, string FileName)> GetAsync(
            string relativePath)
        {
            var absolute = Path.Combine(UploadsRoot, relativePath.TrimStart('/'));
            if (!File.Exists(absolute))
                throw new FileNotFoundException($"File not found: {relativePath}");

            var bytes = await File.ReadAllBytesAsync(absolute);
            var ext = Path.GetExtension(absolute).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return (bytes, contentType, Path.GetFileName(absolute));
        }

        // ── Public URL for client ─────────────────────────────────────────
        public string GetPublicUrl(string relativePath)
            => $"{BaseUrl.TrimEnd('/')}/uploads/{relativePath.TrimStart('/')}";

        // ── Image validation ──────────────────────────────────────────────
        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.Length > MaxImageBytes) return false;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return ImageExts.Contains(ext) && ImageMimes.Contains(file.ContentType);
        }

        // ── Document validation ───────────────────────────────────────────
        public bool IsValidDocument(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.Length > MaxDocBytes) return false;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return DocExts.Contains(ext) && DocMimes.Contains(file.ContentType);
        }

        // ── Image compression via ImageSharp ─────────────────────────────
        public IFormFile CompressImage(IFormFile file, int maxWidthPx = 800)
        {
            try
            {
                using var inputStream = file.OpenReadStream();
                using var image = Image.Load(inputStream);

                // Only resize if wider than maxWidthPx
                if (image.Width > maxWidthPx)
                {
                    var ratio = (double)maxWidthPx / image.Width;
                    var newHeight = (int)(image.Height * ratio);
                    image.Mutate(x => x.Resize(maxWidthPx, newHeight));
                }

                var ms = new MemoryStream();
                // Always output as JPEG for compression (except PNG which we keep)
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext == ".png")
                    image.SaveAsPng(ms);
                else
                    image.SaveAsJpeg(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                    { Quality = 85 });

                ms.Position = 0;

                return new FormFileWrapper(ms, file.FileName, file.ContentType);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Image compression failed, using original file");
                return file; // fall back to original
            }
        }
    }

    // Minimal IFormFile wrapper around a MemoryStream (used by CompressImage)
    internal sealed class FormFileWrapper : IFormFile
    {
        private readonly Stream _stream;
        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _stream.Length;
        public string Name => "file";
        public string FileName { get; }

        public FormFileWrapper(Stream stream, string fileName, string contentType)
        {
            _stream = stream; FileName = fileName; ContentType = contentType;
        }

        public void CopyTo(Stream target) => _stream.CopyTo(target);
        public Task CopyToAsync(Stream target, CancellationToken ct = default)
            => _stream.CopyToAsync(target, ct);
        public Stream OpenReadStream() { _stream.Position = 0; return _stream; }
    }
}
