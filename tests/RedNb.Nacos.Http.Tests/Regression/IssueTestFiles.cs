namespace RedNb.Nacos.Http.Tests.Regression;

internal static class IssueTestFiles
{
    public static void RemoveDirectory(string path)
    {
        var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        var roots = new[] { Path.GetTempPath(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nacos", "naming") };
        if (!roots.Any(root => full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Refusing to remove a directory outside the test cache roots.");
        if (Directory.Exists(full)) Directory.Delete(full, true);
    }
}
