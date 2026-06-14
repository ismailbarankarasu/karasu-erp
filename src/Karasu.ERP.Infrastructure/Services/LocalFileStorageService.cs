using Karasu.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Karasu.ERP.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct)
    {
        var safeName = Path.GetFileName(fileName);
        var targetDir = Path.Combine(_rootPath, folder);
        Directory.CreateDirectory(targetDir);

        var storedName = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(targetDir, storedName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return Path.Combine(folder, storedName).Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
