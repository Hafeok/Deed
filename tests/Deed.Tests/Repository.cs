namespace Deed.Tests;

/// <summary>Finds files in the repository the tests run from.</summary>
internal static class Repository
{
    public static string Root { get; } = FindRoot();

    public static string File(string relative) => Path.Combine(Root, relative);

    private static string FindRoot()
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (System.IO.File.Exists(Path.Combine(d.FullName, "Deed.slnx")))
            {
                return d.FullName;
            }
        }

        throw new InvalidOperationException("Cannot find the repository root (Deed.slnx).");
    }
}
