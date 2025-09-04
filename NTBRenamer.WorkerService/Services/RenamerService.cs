using NTBRenamer.WorkerService.Core;
using System.Diagnostics;

namespace NTBRenamer.WorkerService.Services;

public interface IRenamerService
{
    Task ProcessFiles(CancellationToken cancellationToken = default);
}

public class RenamerService(ILogger<RenamerService> logger) : IRenamerService
{
    #region Private Properties
    private readonly string[] directories =
    [
        @"Y:\Departments\NTB-Archives\Imaging\cdv1",
        @"Y:\Departments\NTB-Archives\Imaging\cdv2",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4"
    ];

    private readonly List<string> Directories = [];
    private readonly List<string> Files = [];
    #endregion

    #region Private Functions
    private async Task PopulateAllDirectories(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];

        foreach (var dir in directories)
            tasks.Add(Task.Run(() =>
            {
                Directories.Add(dir);
                Directories.AddRange(GetDirectories(dir));
            }, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task PopulateAllFiles(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        object fileslock = new();

        foreach (var dir in Directories)
            tasks.Add(Task.Run(() =>
            {
                List<string> filesToAdd = [.. Directory.GetFiles(dir)];
                if (filesToAdd.Count > 0)
                    lock (fileslock)
                        Files.AddRange(filesToAdd);
            }, cancellationToken));

        await Task.WhenAll([.. tasks]);
    }

    private async Task ProcessAllFiles(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];

        int count = 0, takeQty = 100;
        for (int i = 0; i < Files.Count; i = i + takeQty)
        {
            int skip = takeQty * count;
            int take = Files.Count - skip > takeQty ? takeQty : Files.Count - skip;
            tasks.Add(Task.Run(() => ProcessFiles([.. Files.Skip(skip).Take(take)]), cancellationToken));
            count++;
        }

        await Task.WhenAll([.. tasks]);
    }

    private async Task ProcessBatchFile(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        Stopwatch stopwatch = Stopwatch.StartNew();

        int count = 0, takeQty = 100;
        for (int i = 0; i < Files.Count; i = i + takeQty)
        {
            int skip = takeQty * count;
            int take = Files.Count - skip > takeQty ? takeQty : Files.Count - skip;
            tasks.Add(Task.Run(async () => await RunBatchProcess([.. Files.Skip(skip).Take(take)]), cancellationToken));
            count++;
        }

        await Task.WhenAll([.. tasks]);

        stopwatch.Stop();

        Console.WriteLine($"Processed batch file in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");
    }

    private async Task FileCleanup(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        Stopwatch stopwatch = Stopwatch.StartNew();

        await Task.Run(() =>
        {
            List<string> pdfs = [.. Files.Where(w => w.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))];
            List<string> pss = [.. Files.Where(w => w.EndsWith(".ps", StringComparison.InvariantCultureIgnoreCase))];
            List<string> oss = [.. Files.Where(w => w.EndsWith(".octet-stream", StringComparison.InvariantCultureIgnoreCase))];

            foreach (string pdf in pdfs)
            {
                string? ps = pss.FirstOrDefault(f => f == pdf.Replace(".pdf", ".ps", StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrWhiteSpace(ps))
                {
                    try
                    {
                        File.Delete(ps);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ps} failed to delete: {ex.Message}.");
                    }
                }

                string? os = oss.FirstOrDefault(f => f == pdf.Replace(".pdf", ".octet-stream", StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrWhiteSpace(os))
                {
                    try
                    {
                        File.Delete(os);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{os} failed to delete: {ex.Message}.");
                    }
                }
            }
        }, cancellationToken);

        stopwatch.Stop();

        Console.WriteLine($"Files cleanup {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");
    }
    #endregion

    #region Private Static Functions
    private static void ProcessFiles(List<string> files)
    {
        foreach (string file in files)
        {
            FileInfo fileInfo = new(file);
            if (!fileInfo.Exists)
                continue;

            if (!string.IsNullOrEmpty(fileInfo.Extension))
                continue;

            string? mime = FileExtension.GetMimeType(File.ReadAllBytes(file), file);
            string? ext = string.IsNullOrEmpty(mime)
                ? ".ps"
                : mime.Replace("application/", "").Replace("audio/", "").Replace("image/", "").Replace("video/", "").Replace(".", "");

            if (string.IsNullOrEmpty(ext))
                continue;

            File.Move(file, Path.ChangeExtension(file, ext));
        }
    }

    private static async Task RunBatchProcess(List<string> files)
    {
        List<Task> tasks = [];

        tasks.Add(Task.Run(() =>
        {
            List<string> psFiles = [.. files.Where(w => w.EndsWith(".ps", StringComparison.InvariantCultureIgnoreCase))];
            psFiles.ForEach(file =>
            {
                string output = file.Replace(".ps", ".pdf");
                if (!File.Exists(output)) //Run Batch file
                    try
                    {
                        BatchFileExecutor.ConvertToPdf(file, output);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{file} failed batch process. ({ex.Message})");
                    }
            });
        }));

        tasks.Add(Task.Run(() =>
        {
            List<string> osFiles = [.. files.Where(w => w.EndsWith(".octet-stream", StringComparison.InvariantCultureIgnoreCase))];
            osFiles.ForEach(file =>
            {
                string output = file.Replace(".octet-stream", ".pdf");
                if (!File.Exists(output)) //Run Batch file
                    try
                    {
                        BatchFileExecutor.ConvertToPdf(file, output);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{file} failed batch process. ({ex.Message})");
                    }
            });
        }));

        await Task.WhenAll([.. tasks]);
    }

    private static List<string> GetDirectories(string path, string searchPattern)
    {
        try
        {
            return [.. Directory.GetDirectories(path, searchPattern)];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static List<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (searchOption == SearchOption.TopDirectoryOnly)
            return [.. Directory.GetDirectories(path, searchPattern)];

        var directories = new List<string>(GetDirectories(path, searchPattern));

        for (var i = 0; i < directories.Count; i++)
            directories.AddRange(GetDirectories(directories[i], searchPattern));

        return directories;
    }
    #endregion

    public async Task ProcessFiles(CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        logger.LogInformation($"Started processing files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");

        //Populate Directories
        await PopulateAllDirectories(cancellationToken);
        stopwatch.Stop();
        logger.LogInformation($"Directories populated in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");

        //Populate Files to Process
        stopwatch = Stopwatch.StartNew();
        await Task.Run(async () => await PopulateAllFiles(cancellationToken), cancellationToken);
        stopwatch.Stop();
        logger.LogInformation($"Files populated in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");

        //Process Files that don't need Batch File
        stopwatch = Stopwatch.StartNew();
        await Task.Run(() => ProcessAllFiles(cancellationToken), cancellationToken);
        stopwatch.Stop();
        logger.LogInformation($"Files processed in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");

        //Initial File Cleanup so there are no duplicate PDF files created by the Batch File
        stopwatch = Stopwatch.StartNew();
        await FileCleanup(cancellationToken);
        stopwatch.Stop();
        logger.LogInformation($"Files cleaned up in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");

        //Run the Batch File to convert all .ps files to .pdf files
        stopwatch = Stopwatch.StartNew();
        await ProcessBatchFile(cancellationToken);
        stopwatch.Stop();
        logger.LogInformation($"Batch File ran in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");

        //Final File Cleanup to remove .ps files that have .pdf files created from them
        stopwatch = Stopwatch.StartNew();
        await FileCleanup(cancellationToken);
        stopwatch.Stop();
        logger.LogInformation($"Files cleaned up in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}");

        logger.LogInformation($"Stopped processing files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
    }
}
