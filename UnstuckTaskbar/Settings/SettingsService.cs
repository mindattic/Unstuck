using MindAttic.Vault.Settings;

namespace UnstuckTaskbar.Settings;

/// <summary>
/// Persists <see cref="TaskbarSettings"/> under <c>%LocalAppData%\MindAttic\UnstuckTaskbar\settings.json</c>
/// via <see cref="JsonSettingsStore{T}"/> — a hotkey assignment and "start with Windows" are
/// per-machine state, not something that should roam with the user profile.
/// </summary>
public sealed class SettingsService
{
    public const string AppId = "UnstuckTaskbar";

    private readonly JsonSettingsStore<TaskbarSettings> store = JsonSettingsStore<TaskbarSettings>.ForLocalApp(AppId);

    public string FilePath => store.FilePath;

    public TaskbarSettings Load() => store.Load();

    public TaskbarSettings Update(Action<TaskbarSettings> mutate) => store.Update(mutate);
}
