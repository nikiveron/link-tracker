namespace LinkTracker.Bot;

public static class LoadEnvs
{
    private static readonly string fileName = ".env";
    private static readonly string projectRootPath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string fullPath = Path.Combine(projectRootPath, fileName);

    public static void Run()
    {
        if (File.Exists(fullPath))
        {
            foreach (var line in File.ReadAllLines(fullPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }
        }
    }
}
