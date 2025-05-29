using System.Net;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gml.Web.Api.Core.Handlers;

public class FileSystemHandler
{
    private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(nameof(FileSystemHandler));

    public static async Task<IResult> GetDirectoryContents(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            var contents = fileSystemService.GetDirectoryContents(path);
            return Results.Ok(ResponseMessage.Create(contents, "Содержимое директории успешно получено", HttpStatusCode.OK));
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Директория не найдена: {Path}", path);
            return Results.NotFound(ResponseMessage.Create("Директория не найдена", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении содержимого директории: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при получении содержимого директории", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> CreateArchive(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            _logger.LogInformation("Создание архива из пути: {Path}", path);

            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Не указан путь для создания архива");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь для создания архива", HttpStatusCode.BadRequest));
            }

            var archivePath = await fileSystemService.CreateArchive(path);
            return Results.Ok(ResponseMessage.Create(archivePath, "Архив успешно создан", HttpStatusCode.OK));
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Директория не найдена: {Path}", path);
            return Results.NotFound(ResponseMessage.Create("Указанная директория не найдена", HttpStatusCode.NotFound));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Файл не найден: {Path}", path);
            return Results.NotFound(ResponseMessage.Create("Указанный файл не найден", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании архива из пути: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при создании архива", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> CreateArchiveMultiple(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        [FromBody] CreateArchiveRequest request)
    {
        try
        {
            _logger.LogInformation("Создание архива из путей: {@Paths}", request.Paths);

            if (request.Paths == null || request.Paths.Length == 0)
            {
                _logger.LogWarning("Не указаны пути для создания архива");
                return Results.BadRequest(ResponseMessage.Create("Не указаны пути для создания архива", HttpStatusCode.BadRequest));
            }

            var archivePath = await fileSystemService.CreateArchiveMultiple(request.Paths);
            return Results.Ok(ResponseMessage.Create(archivePath, "Архив успешно создан", HttpStatusCode.OK));
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Директория не найдена в путях: {@Paths}", request.Paths);
            return Results.NotFound(ResponseMessage.Create("Одна из указанных директорий не найдена", HttpStatusCode.NotFound));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Файл не найден в путях: {@Paths}", request.Paths);
            return Results.NotFound(ResponseMessage.Create("Один из указанных файлов не найден", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании архива из путей: {@Paths}", request.Paths);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при создании архива", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> DownloadFile(
        HttpContext context,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            var fullPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, path);

            if (!File.Exists(fullPath))
                return Results.NotFound(ResponseMessage.Create("Файл не найден", HttpStatusCode.NotFound));

            var fileInfo = new FileInfo(fullPath);
            var memory = new MemoryStream();
            await using (var stream = new FileStream(fullPath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return Results.File(memory, "application/octet-stream", fileInfo.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при скачивании файла: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при скачивании файла", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> UploadFile(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        [FromForm] IFormFile file,
        string path)
    {
        try
        {
            _logger.LogInformation("Загрузка файла: {FileName}, Размер: {FileSize}, Путь: {Path}",
                file?.FileName, file?.Length, path);

            if (file == null)
            {
                _logger.LogWarning("Файл не был передан");
                return Results.BadRequest(ResponseMessage.Create("Файл не был передан", HttpStatusCode.BadRequest));
            }

            if (file.Length == 0)
            {
                _logger.LogWarning("Загружен пустой файл");
                return Results.BadRequest(ResponseMessage.Create("Загружен пустой файл", HttpStatusCode.BadRequest));
            }

            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Не указан путь для загрузки файла");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь для загрузки файла", HttpStatusCode.BadRequest));
            }

            using var stream = file.OpenReadStream();
            var filePath = await fileSystemService.UploadFile(stream, path, file.FileName);
            
            return Results.Ok(ResponseMessage.Create(filePath, "Файл успешно загружен", HttpStatusCode.OK));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке файла: {FileName} в {Path}", file?.FileName, path);
            return Results.BadRequest(ResponseMessage.Create($"Ошибка при загрузке файла: {ex.Message}", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> DeleteFile(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            fileSystemService.DeleteFile(path);
            return Results.Ok(ResponseMessage.Create("Файл успешно удален", HttpStatusCode.OK));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Файл не найден: {Path}", path);
            return Results.NotFound(ResponseMessage.Create("Файл не найден", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении файла: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при удалении файла", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> DeleteDirectory(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            fileSystemService.DeleteDirectory(path);
            return Results.Ok(ResponseMessage.Create("Директория успешно удалена", HttpStatusCode.OK));
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Директория не найдена: {Path}", path);
            return Results.NotFound(ResponseMessage.Create("Директория не найдена", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении директории: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при удалении директории", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> ExtractArchive(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        [FromBody] ExtractArchiveRequest request)
    {
        try
        {
            _logger.LogInformation("Распаковка архива: {Path}", request.ArchivePath);

            if (string.IsNullOrEmpty(request.ArchivePath))
            {
                _logger.LogWarning("Не указан путь к архиву");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь к архиву", HttpStatusCode.BadRequest));
            }

            // Распаковываем в ту же директорию, где лежит архив
            var targetPath = Path.GetDirectoryName(request.ArchivePath) ?? string.Empty;
            var extractPath = await fileSystemService.ExtractArchive(request.ArchivePath, targetPath);
            return Results.Ok(ResponseMessage.Create(extractPath, "Архив успешно распакован", HttpStatusCode.OK));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Архив не найден: {Path}", request.ArchivePath);
            return Results.NotFound(ResponseMessage.Create("Архив не найден", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при распаковке архива: {Path}", request.ArchivePath);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при распаковке архива", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> DeleteFiles(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        [FromBody] DeleteItemsRequest request)
    {
        try
        {
            _logger.LogInformation("Удаление файлов: {@Paths}", request.Paths);

            if (request.Paths == null || request.Paths.Length == 0)
            {
                _logger.LogWarning("Не указаны пути для удаления файлов");
                return Results.BadRequest(ResponseMessage.Create("Не указаны пути для удаления файлов", HttpStatusCode.BadRequest));
            }

            fileSystemService.DeleteFiles(request.Paths);
            return Results.Ok(ResponseMessage.Create("Файлы успешно удалены", HttpStatusCode.OK));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Один из файлов не найден: {@Paths}", request.Paths);
            return Results.NotFound(ResponseMessage.Create("Один или несколько файлов не найдены", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении файлов: {@Paths}", request.Paths);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при удалении файлов", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> DeleteDirectories(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        [FromBody] DeleteItemsRequest request)
    {
        try
        {
            _logger.LogInformation("Удаление директорий: {@Paths}", request.Paths);

            if (request.Paths == null || request.Paths.Length == 0)
            {
                _logger.LogWarning("Не указаны пути для удаления директорий");
                return Results.BadRequest(ResponseMessage.Create("Не указаны пути для удаления директорий", HttpStatusCode.BadRequest));
            }

            fileSystemService.DeleteDirectories(request.Paths);
            return Results.Ok(ResponseMessage.Create("Директории успешно удалены", HttpStatusCode.OK));
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Одна из директорий не найдена: {@Paths}", request.Paths);
            return Results.NotFound(ResponseMessage.Create("Одна или несколько директорий не найдены", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении директорий: {@Paths}", request.Paths);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при удалении директорий", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> GetRootDirectory(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager)
    {
        try
        {
            _logger.LogInformation("Получение содержимого директории лаунчера из: {Path}", gmlManager.LauncherInfo.InstallationDirectory);
            var contents = fileSystemService.GetDirectoryContents(string.Empty);
            _logger.LogInformation("Содержимое директории лаунчера: {@Contents}", contents);

            if (contents == null)
            {
                _logger.LogWarning("Содержимое директории лаунчера не найдено");
                return Results.NotFound(ResponseMessage.Create("Содержимое директории лаунчера не найдено", HttpStatusCode.NotFound));
            }

            return Results.Ok(ResponseMessage.Create(contents, "Содержимое директории лаунчера получено", HttpStatusCode.OK));
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Директория лаунчера не найдена");
            return Results.NotFound(ResponseMessage.Create("Директория лаунчера не найдена", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении содержимого директории лаунчера: {Error}", ex.Message);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при получении содержимого директории лаунчера", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> ReadFileContent(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            _logger.LogInformation("Чтение содержимого файла: {Path}", path);

            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Не указан путь к файлу");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь к файлу", HttpStatusCode.BadRequest));
            }

            var content = await fileSystemService.ReadFileContent(path);
            return Results.Ok(ResponseMessage.Create(content, "Содержимое файла успешно прочитано", HttpStatusCode.OK));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Файл не найден: {Path}", path);
            return Results.NotFound(ResponseMessage.Create("Файл не найден", HttpStatusCode.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при чтении файла: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при чтении файла", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> SaveFileContent(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        [FromBody] SaveFileRequest request)
    {
        try
        {
            _logger.LogInformation("Сохранение содержимого файла: {Path}", request.Path);

            if (string.IsNullOrEmpty(request.Path))
            {
                _logger.LogWarning("Не указан путь к файлу");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь к файлу", HttpStatusCode.BadRequest));
            }

            await fileSystemService.SaveFileContent(request.Path, request.Content);
            return Results.Ok(ResponseMessage.Create("Файл успешно сохранен", HttpStatusCode.OK));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении файла: {Path}", request.Path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при сохранении файла", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> CreateDirectory(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            _logger.LogInformation("Создание директории: {Path}", path);

            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Не указан путь для создания директории");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь для создания директории", HttpStatusCode.BadRequest));
            }

            fileSystemService.CreateDirectory(path);
            return Results.Ok(ResponseMessage.Create("Директория успешно создана", HttpStatusCode.OK));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Директория уже существует: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Директория уже существует", HttpStatusCode.BadRequest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании директории: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при создании директории", HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> CreateFile(
        HttpContext context,
        FileSystemService fileSystemService,
        IGmlManager gmlManager,
        string path)
    {
        try
        {
            _logger.LogInformation("Создание файла: {Path}", path);

            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Не указан путь для создания файла");
                return Results.BadRequest(ResponseMessage.Create("Не указан путь для создания файла", HttpStatusCode.BadRequest));
            }

            await fileSystemService.CreateFile(path);
            return Results.Ok(ResponseMessage.Create("Файл успешно создан", HttpStatusCode.OK));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Файл уже существует: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Файл уже существует", HttpStatusCode.BadRequest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании файла: {Path}", path);
            return Results.BadRequest(ResponseMessage.Create("Ошибка при создании файла", HttpStatusCode.BadRequest));
        }
    }
}
