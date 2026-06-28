namespace PulseHR.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folder);
    Task DeleteFileAsync(string relativePath);
    string GetContentType(string filePath);
    FileStream? OpenRead(string relativePath);
}

public class LocalFileStorageService(IConfiguration config, IWebHostEnvironment env) : IFileStorageService
{
    private readonly string _basePath = Path.Combine(
        env.ContentRootPath,
        config["FileStorage:BasePath"] ?? "wwwroot/uploads"
    );

    public async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        var dir = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(dir);

        var ext      = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{fileName}";
    }

    public Task DeleteFileAsync(string relativePath)
    {
        var fullPath = Path.Combine(env.ContentRootPath, "wwwroot", relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf"  => "application/pdf",
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif"  => "image/gif",
            _       => "application/octet-stream"
        };
    }

    public FileStream? OpenRead(string relativePath)
    {
        var fullPath = Path.Combine(env.ContentRootPath, "wwwroot", relativePath.TrimStart('/'));
        return File.Exists(fullPath) ? new FileStream(fullPath, FileMode.Open, FileAccess.Read) : null;
    }
}
