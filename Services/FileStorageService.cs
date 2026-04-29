namespace KinectCare.API.Services;

public class FileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveVideoAsync(
        IFormFile file, string subFolder = "videos")
    {
        var folder = Path.Combine(
            _env.WebRootPath, subFolder);

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        // اسم فريد للملف
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, fileName);

        using var stream = new FileStream(
            filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{subFolder}/{fileName}";
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        var fullPath = Path.Combine(
            _env.WebRootPath,
            relativePath.TrimStart('/'));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(
            _env.WebRootPath,
            relativePath.TrimStart('/'));
    }
}