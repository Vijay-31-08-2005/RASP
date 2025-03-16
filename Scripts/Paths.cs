namespace Rasp {
    public static class Paths {
        public static string RaspDir => Path.Combine(Directory.GetCurrentDirectory(), ".rasp");
        public static string LogsFile => Path.Combine(RaspDir, ".logs.json");
        public static string BranchesDir => Path.Combine(RaspDir, "branches");
        public static string BranchesFile => Path.Combine(BranchesDir, "branches.json");
        public static string MainDir => Path.Combine(BranchesDir, "main");
        public static string MainIndexFile => Path.Combine(MainDir, "index.json");
        public static string MainCommitsDir => Path.Combine(MainDir, "commits");
        public static string MainCommitFile => Path.Combine(MainCommitsDir, "index.json");
        public static string ConfigFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/config.json");
        public static string AzConfigFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rasp/azConfig.json");

    }
}
