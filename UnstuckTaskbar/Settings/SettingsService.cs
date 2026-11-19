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
    private const int MaxReadAttempts = 5;
    private int failedReads;

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
        FileSystemEventHandler onChange = (_, _) =>
        {
            failedReads = 0;
            (debounce ??= new Timer(_ => OnSettled())).Change(Debounce, Timeout.InfiniteTimeSpan);
        };
        watcher.Changed += onChange;
        watcher.Created += onChange;
        watcher.Renamed += (s, e) => onChange(s, e);
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Load() turns any read error into defaults, so a read that lands while Vault is still writing the file
    /// (sharing violation, half-written JSON) would report "no hotkey" and unregister it. A strict read that fails
    /// is retried a few times instead, and if it never succeeds nothing is reported, so the current settings stay.
    /// </summary>
    private void OnSettled()
    {
        if (IsReadable(store.FilePath)) { Changed?.Invoke(Load()); return; }
        if (++failedReads < MaxReadAttempts) debounce?.Change(Debounce, Timeout.InfiniteTimeSpan);
    }

    /// <summary>True when the file can be read and holds complete JSON (or is missing/empty, which really means
    /// defaults); false while it is locked or half-written.</summary>
    internal static bool IsReadable(string path)
    {
        try
        {
            if (!File.Exists(path)) return true;
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return true;
            using var _ = System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { return false; }
    }

    public void Dispose()
    {
        watcher?.Dispose();
        debounce?.Dispose();
    }
}
