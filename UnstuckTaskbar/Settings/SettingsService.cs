using System.IO;
using MindAttic.Vault.Settings;

namespace UnstuckTaskbar.Settings;

/// <summary>
/// Reads <see cref="TaskbarSettings"/> from its per-machine Vault file and reports when it changes
/// (MindAttic.Vault.Desktop saves it), so the running tray app applies edits without a restart.
/// </summary>
public sealed class SettingsService : IDisposable
{
    public const string AppId = "UnstuckTaskbar";

    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);
    private readonly ISettingsStore<TaskbarSettings> store;
    private readonly string directory;
    private FileSystemWatcher? watcher;
    private Timer? debounce;

    public SettingsService() : this(JsonSettingsStore<TaskbarSettings>.ForSettingsApp()) { }

    public SettingsService(JsonSettingsStore<TaskbarSettings> store)
    {
        this.store = store;
        directory = store.Directory;
    }

    public string FilePath => store.FilePath;

    /// <summary>Raised (on a worker thread) after the settings file changes on disk.</summary>
    public event Action<TaskbarSettings>? Changed;

    /// <summary>First start seeds the defaults and writes the manifest Vault.Desktop renders.</summary>
    public TaskbarSettings LoadOrSeed() => store.LoadOrSeed();

    public TaskbarSettings Load() => store.Load();

    public TaskbarSettings Update(Action<TaskbarSettings> mutate) => store.Update(mutate);

    /// <summary>Starts raising <see cref="Changed"/> when the file is saved.</summary>
    public void Watch()
    {
        if (watcher is not null) return;
        Directory.CreateDirectory(directory);
        watcher = new FileSystemWatcher(directory, JsonSettingsStore<TaskbarSettings>.DefaultFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };
        FileSystemEventHandler onChange = (_, _) => (debounce ??= new Timer(_ => Changed?.Invoke(Load()))).Change(Debounce, Timeout.InfiniteTimeSpan);
        watcher.Changed += onChange;
        watcher.Created += onChange;
        watcher.Renamed += (s, e) => onChange(s, e);
        watcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        watcher?.Dispose();
        debounce?.Dispose();
    }
}
