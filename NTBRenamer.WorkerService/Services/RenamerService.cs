using NTBRenamer.WorkerService.Core;

namespace NTBRenamer.WorkerService.Services;

public interface IRenamerService
{
    Task ProcessFiles(CancellationToken cancellationToken = default);
}

public class RenamerService : IRenamerService
{
    #region Private Properties
    private readonly string[] directories =
    [
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z1",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z2",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z3",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z4",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z5",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z6",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z7",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z8",
        @"Y:\Departments\NTB-Archives\Imaging\cdv3\bol\z9",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z1",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z2",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z3",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z4",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z5",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z6",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z7",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z8",
        @"Y:\Departments\NTB-Archives\Imaging\cdv4\bol\z9"
    ];
    #endregion

    #region Private Functions
    private async Task PopulateAllDirectories(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        foreach (var dir in directories)
        {
            tasks.Add(Task.Run(async () =>
            {
                List<string> dirs = [];
                dirs.Add(dir);
                dirs.AddRange(GetDirectories(dir));
                await PopulateAllFiles(dirs, cancellationToken);
            }, cancellationToken));
        }
        await Task.WhenAll([.. tasks]);
    }

    private async Task PopulateAllFiles(List<string> dirs, CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        foreach (var dir in dirs)
        {
            tasks.Add(Task.Run(async () =>
            {
                List<string> files = [];
                files.AddRange(Directory.GetFiles(dir));
                ProcessAllFiles(files, cancellationToken);
                await RunBatchProcess(files, cancellationToken);
            }, cancellationToken));
        }
        await Task.WhenAll([.. tasks]);
    }

    private void ProcessAllFiles(List<string> files, CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        int count = 0, takeQty = 100;
        for (int i = 0; i < files.Count; i = i + takeQty)
        {
            int skip = takeQty * count;
            int take = files.Count - skip > takeQty ? takeQty : files.Count - skip;
            tasks.Add(Task.Run(() => ProcessFiles([.. files.Skip(skip).Take(take)]), cancellationToken));
            count++;
        }
    }

    private async Task RunBatchProcess(List<string> files, CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        int count = 0, takeQty = 100;
        List<string> batchFiles = [.. files.Where(w => w.EndsWith(".ps", StringComparison.InvariantCultureIgnoreCase))];
        for (int i = 0; i < files.Count; i = i + takeQty)
        {
            int skip = takeQty * count;
            int take = batchFiles.Count - skip > takeQty ? takeQty : batchFiles.Count - skip;
            tasks.Add(Task.Run(() => ExecuteBatchFile([.. batchFiles.Skip(skip).Take(take)]), cancellationToken));
            count++;
        }
        await Task.WhenAll([.. tasks]);
    }

    public void ExecuteBatchFile(List<string> files)
    {
        foreach (string file in files)
        {
            string output = file.Replace(".ps", ".pdf");
            BatchFileExecutor.ConvertToPdf(file, output);
        }
    }

    public void ProcessFiles(List<string> files)
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
        Console.WriteLine($"Started processing files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");

        Console.WriteLine($"Started populating directories at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
        await PopulateAllDirectories(cancellationToken);
        /*
        Console.WriteLine($"Completed populating directories at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
        
        Console.WriteLine($"Started populating files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
        await PopulateAllFiles(cancellationToken);
        Console.WriteLine($"Completed populating files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");

        Console.WriteLine($"Started processing files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
        await ProcessAllFiles(cancellationToken);
        Console.WriteLine($"Completed processing files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");

        Console.WriteLine($"Started running batch file at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
        await RunBatchProcess(cancellationToken);
        Console.WriteLine($"Completed running batch file at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
        */
        Console.WriteLine($"Stopped processing files at {DateTime.Now:MM dd, yyyy HH:mm:ss}");
    }
}
