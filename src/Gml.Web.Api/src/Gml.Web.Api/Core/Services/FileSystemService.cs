using System.IO.Compression;
using Gml.Web.Api.Dto.Files;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Services;

public class FileSystemService
{
    private readonly IGmlManager _gmlManager;

    public FileSystemService(IGmlManager gmlManager)
    {
        _gmlManager = gmlManager;
    }

    public IEnumerable<FileSystemItemDto> GetDirectoryContents(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
        var directory = new DirectoryInfo(fullPath);

        if (!directory.Exists)
            throw new DirectoryNotFoundException($"Директория не найдена: {path}");

        var items = new List<FileSystemItemDto>();

        // Добавляем папки
        foreach (var dir in directory.GetDirectories())
        {
            items.Add(new FileSystemItemDto
            {
                Name = dir.Name,
                Path = Path.GetRelativePath(_gmlManager.LauncherInfo.InstallationDirectory, dir.FullName),
                FullPath = dir.FullName,
                IsDirectory = true,
                Size = GetDirectorySize(dir),
                CreatedAt = dir.CreationTime,
                ModifiedAt = dir.LastWriteTime
            });
        }

        // Добавляем файлы
        foreach (var file in directory.GetFiles())
        {
            items.Add(new FileSystemItemDto
            {
                Name = file.Name,
                Path = Path.GetRelativePath(_gmlManager.LauncherInfo.InstallationDirectory, file.FullName),
                FullPath = file.FullName,
                IsDirectory = false,
                Size = file.Length,
                CreatedAt = file.CreationTime,
                ModifiedAt = file.LastWriteTime,
                Extension = file.Extension
            });
        }

        return items;
    }

    public async Task<string> CreateArchive(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);

        // Проверяем существование файла или директории
        bool isDirectory = Directory.Exists(fullPath);
        bool isFile = File.Exists(fullPath);

        if (!isDirectory && !isFile)
            throw new DirectoryNotFoundException($"Путь не найден: {path}");

        // Получаем имя для архива
        string baseName = isDirectory ? new DirectoryInfo(fullPath).Name : Path.GetFileNameWithoutExtension(fullPath);
        var archiveName = $"{baseName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";

        // Создаем архив в той же директории, где находится исходный файл/папка
        var parentDirectory = isDirectory ? Directory.GetParent(fullPath)?.FullName : Path.GetDirectoryName(fullPath);
        var archivePath = Path.Combine(parentDirectory ?? _gmlManager.LauncherInfo.InstallationDirectory, archiveName);

        await Task.Run(() =>
        {
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                if (isDirectory)
                {
                    var baseDir = new DirectoryInfo(fullPath);

                    // Добавляем все директории (включая пустые)
                    foreach (var dir in baseDir.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(baseDir.FullName, dir.FullName);
                        archive.CreateEntry(Path.Combine(baseDir.Name, relativePath) + "/");
                    }

                    // Добавляем все файлы
                    var files = baseDir.GetFiles("*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var relativePath = Path.GetRelativePath(baseDir.FullName, file.FullName);
                        archive.CreateEntryFromFile(file.FullName, Path.Combine(baseDir.Name, relativePath));
                    }

                    // Добавляем корневую директорию, если она пустая
                    if (!baseDir.EnumerateFileSystemInfos().Any())
                    {
                        archive.CreateEntry(baseDir.Name + "/");
                    }
                }
                else
                {
                    // Для одного файла просто добавляем его в корень архива
                    archive.CreateEntryFromFile(fullPath, Path.GetFileName(fullPath));
                }
            }
        });

        // Возвращаем относительный путь к архиву
        return Path.GetRelativePath(_gmlManager.LauncherInfo.InstallationDirectory, archivePath);
    }

    public async Task<string> UploadFile(Stream fileStream, string targetPath, string fileName)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, targetPath);

        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        var filePath = Path.Combine(fullPath, fileName);

        using var fileStream2 = File.Create(filePath);
        await fileStream.CopyToAsync(fileStream2);

        return filePath;
    }

    private long GetDirectorySize(DirectoryInfo directory)
    {
        return directory.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }

    public void DeleteFile(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Файл не найден: {path}");

        File.Delete(fullPath);
    }

    public void DeleteDirectory(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
        var directory = new DirectoryInfo(fullPath);

        if (!directory.Exists)
            throw new DirectoryNotFoundException($"Директория не найдена: {path}");

        directory.Delete(true); // true для рекурсивного удаления
    }

    public async Task<string> ExtractArchive(string archivePath, string targetPath)
    {
        var fullArchivePath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, archivePath);
        var fullTargetPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, targetPath);

        if (!File.Exists(fullArchivePath))
            throw new FileNotFoundException($"Архив не найден: {archivePath}");

        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(fullArchivePath);
            foreach (var entry in archive.Entries)
            {
                var fullPath = Path.Combine(fullTargetPath, entry.FullName);
                var directory = Path.GetDirectoryName(fullPath);

                // Создаем директорию если её нет
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Если это не директория (т.е. файл), извлекаем его
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    // Если файл существует, удаляем его перед распаковкой
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);

                    entry.ExtractToFile(fullPath);
                }
            }
        });

        return fullTargetPath;
    }

    public void DeleteItems(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            throw new ArgumentException("Не указаны пути для удаления");

        var notFoundItems = new List<string>();
        var existingItems = new List<string>();

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
            
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                existingItems.Add(fullPath);
            }
            else
            {
                notFoundItems.Add(path);
            }
        }

        // Удаляем существующие элементы
        foreach (var item in existingItems)
        {
            try
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                }
                else if (Directory.Exists(item))
                {
                    Directory.Delete(item, true); // true для рекурсивного удаления
                }
            }
            catch (Exception)
            {
                // Пропускаем элементы, которые не удалось удалить
                continue;
            }
        }

        // Если есть элементы, которые не удалось найти, возвращаем информацию о них
        if (notFoundItems.Any())
        {
            if (existingItems.Any())
            {
                throw new FileNotFoundException($"Следующие пути не найдены (остальные удалены): {string.Join(", ", notFoundItems)}");
            }
            else
            {
                throw new FileNotFoundException($"Ни один из указанных путей не найден: {string.Join(", ", notFoundItems)}");
            }
        }
    }

    public void DeleteFiles(string[] paths)
    {
        DeleteItems(paths);
    }

    public void DeleteDirectories(string[] paths)
    {
        DeleteItems(paths);
    }

    public FileSystemItemDto GetRootDirectory()
    {
        var rootDir = new DirectoryInfo(_gmlManager.LauncherInfo.InstallationDirectory);

        if (!rootDir.Exists)
            throw new DirectoryNotFoundException("Корневая директория не найдена");

        return new FileSystemItemDto
        {
            Name = rootDir.Name,
            Path = string.Empty, // Пустой путь, так как это корень
            FullPath = rootDir.FullName,
            IsDirectory = true,
            Size = GetDirectorySize(rootDir),
            CreatedAt = rootDir.CreationTime,
            ModifiedAt = rootDir.LastWriteTime
        };
    }

    public async Task<string> CreateArchiveMultiple(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            throw new ArgumentException("Не указаны пути для создания архива");

        // Создаем имя архива на основе текущей даты и времени
        var archiveName = $"archive_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
        var archivePath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, archiveName);

        await Task.Run(() =>
        {
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (var path in paths)
                {
                    var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);

                    if (Directory.Exists(fullPath))
                    {
                        var baseDir = new DirectoryInfo(fullPath);

                        // Добавляем все директории (включая пустые)
                        foreach (var dir in baseDir.GetDirectories("*", SearchOption.AllDirectories))
                        {
                            var relativePath = Path.GetRelativePath(baseDir.FullName, dir.FullName);
                            archive.CreateEntry(Path.Combine(baseDir.Name, relativePath) + "/");
                        }

                        // Добавляем все файлы
                        var files = baseDir.GetFiles("*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            var relativePath = Path.GetRelativePath(baseDir.FullName, file.FullName);
                            archive.CreateEntryFromFile(file.FullName, Path.Combine(baseDir.Name, relativePath));
                        }

                        // Добавляем корневую директорию, если она пустая
                        if (!baseDir.EnumerateFileSystemInfos().Any())
                        {
                            archive.CreateEntry(baseDir.Name + "/");
                        }
                    }
                    else if (File.Exists(fullPath))
                    {
                        // Для файла просто добавляем его в корень архива
                        archive.CreateEntryFromFile(fullPath, Path.GetFileName(fullPath));
                    }
                    else
                    {
                        throw new FileNotFoundException($"Путь не найден: {path}");
                    }
                }
            }
        });

        // Возвращаем относительный путь к архиву
        return archiveName;
    }

    public async Task<string> ReadFileContent(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Файл не найден: {path}");

        return await File.ReadAllTextAsync(fullPath);
    }

    public async Task SaveFileContent(string path, string content)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(fullPath, content);
    }

    public void CreateDirectory(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
        
        if (Directory.Exists(fullPath))
            throw new InvalidOperationException($"Директория уже существует: {path}");

        Directory.CreateDirectory(fullPath);
    }

    public async Task CreateFile(string path)
    {
        var fullPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, path);
        
        if (File.Exists(fullPath))
            throw new InvalidOperationException($"Файл уже существует: {path}");

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(fullPath, string.Empty);
    }
}

