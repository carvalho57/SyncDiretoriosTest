using System.Collections.Concurrent;
using System.Net;

namespace SyncDir;

class SyncDirectories : IDisposable
{
    public string OrigemDirectory { get; private set; }
    public string DestinyDirectory { get; private set; }
    private FileSystemWatcher? _watcher;
    private ConcurrentDictionary<string, bool> _filesOnCopy { get; set; }

    public SyncDirectories(string origemDirectory, string destinyDirectory)
    {
        OrigemDirectory = origemDirectory;
        DestinyDirectory = destinyDirectory;
        _filesOnCopy = new ConcurrentDictionary<string, bool>();
        _watcher = null;

        CreateDirectoryIfNotExist(OrigemDirectory, DestinyDirectory);
    }

    public void StartSync()
    {
        if (_watcher == null)
        {
            _watcher = new FileSystemWatcher(OrigemDirectory)
            {
                NotifyFilter = NotifyFilters.Size
                                | NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName
            };
            _watcher.Changed += OnChanged;
            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnError;
            _watcher.EnableRaisingEvents = true;
        }
    }

    public void StopSync() {
        _watcher?.Dispose();
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        string filename = Path.GetFileName(e.FullPath);
        string destinyPath = Path.Combine(DestinyDirectory, filename);
        Console.WriteLine($"Removido: {filename}");


        if (File.Exists(destinyPath))
            File.Delete(destinyPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {

        string oldFilename = Path.GetFileName(e.OldFullPath);
        string newFilename = Path.GetFileName(e.FullPath);
        string oldFilenameOnDestinyPath = Path.Combine(DestinyDirectory, oldFilename);
        string newFilenameOnDestinyPath = Path.Combine(DestinyDirectory, newFilename);

        Console.WriteLine($"Renomeado:");
        Console.WriteLine($"    Antigo: {oldFilename}");
        Console.WriteLine($"    Novo: {newFilename}");

        if (!FileHasSameLength(e.FullPath, oldFilenameOnDestinyPath))
        {
            File.Delete(oldFilenameOnDestinyPath);
            File.Copy(e.FullPath, newFilenameOnDestinyPath);
            return;
        }

        File.Move(oldFilenameOnDestinyPath, newFilenameOnDestinyPath);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("OnChanged: " + e.ChangeType.ToString());
        string filename = Path.GetFileName(e.FullPath);

        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        if (_filesOnCopy.ContainsKey(filename))
        {
            Console.WriteLine($"Arquivo sendo copiado: {filename}");
            return;
        }


        File.Copy(e.FullPath, Path.Combine(DestinyDirectory, filename), true);
        Console.WriteLine($"Modificado: {filename}");
    }

    private async void OnCreated(object sender, FileSystemEventArgs e)
    {
        string filename = Path.GetFileName(e.FullPath) ?? string.Empty;
        if (string.IsNullOrEmpty(filename))
        {
            return;
        }

        _filesOnCopy.TryAdd(filename, true);

        await IsFileReady(e.FullPath)
            .ContinueWith(c =>
                {
                    File.Copy(e.FullPath, Path.Combine(DestinyDirectory, Path.GetFileName(e.FullPath)),true);
                });

        _filesOnCopy.Remove(filename, out bool removed);

        if (!removed)
        {
            Console.WriteLine("Deu ruim");
        }

        Console.WriteLine($"Criado: {e.FullPath}");
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Exception? exception = e.GetException();

        if (exception is not null)
        {
            Console.WriteLine($"Mensagem: {exception.Message}");
            Console.WriteLine("Stacktrace:");
            Console.WriteLine(exception.StackTrace);
            Console.WriteLine();
        }
    }


    public static async Task IsFileReady(string filename)
    {
        await Task.Run(() =>
        {
            var isAvailable = false;

            while (!isAvailable)
            {
                try
                {
                    using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        isAvailable = inputStream.Length > 0;
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(IOException))
                    {
                        isAvailable = false;
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    Thread.Sleep(1000);
                }

            }
        });
    }

    private void CreateDirectoryIfNotExist(params string[] paths)
    {

        foreach (var path in paths)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }

    private bool FileHasSameLength(string firstFilePath, string lastFilePath)
    {

        if (!File.Exists(firstFilePath) || !File.Exists(lastFilePath))
        {
            return false;
        }

        var firstFile = new FileInfo(firstFilePath);
        var lastFile = new FileInfo(lastFilePath);

        return firstFile.Length == lastFile.Length;

    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
