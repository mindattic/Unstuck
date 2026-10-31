using System.IO;
using MindAttic.Vault.Paths;
using NUnit.Framework;

namespace UnstuckTaskbar.Tests;

/// <summary>Every test in this assembly sees temp Vault roots, so nothing can write the real AppData.</summary>
[SetUpFixture]
public sealed class TestVaultRoots
{
    public static string Root { get; private set; } = "";

    [OneTimeSetUp]
    public void SetUp()
    {
        Root = Path.Combine(Path.GetTempPath(), "UnstuckTaskbarVault-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(VaultPaths.RoamingRootEnvVar, Path.Combine(Root, "roaming"));
        Environment.SetEnvironmentVariable(VaultPaths.LocalRootEnvVar, Path.Combine(Root, "local"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(VaultPaths.RoamingRootEnvVar, null);
        Environment.SetEnvironmentVariable(VaultPaths.LocalRootEnvVar, null);
        try { Directory.Delete(Root, recursive: true); } catch { }
    }
}
