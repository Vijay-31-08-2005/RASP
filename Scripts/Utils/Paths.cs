namespace Rasp {
    public static class Paths {
        public static string RaspDir => Path.Combine(Directory.GetCurrentDirectory(), Dir.dotRasp);
        public static string LogsFile => Path.Combine(RaspDir, Files.logs);
        public static string BranchesDir => Path.Combine(RaspDir, Dir.branches);
        public static string BackupDir => Path.Combine(RaspDir, Dir.backup);
        public static string BranchesFile => Path.Combine(BranchesDir, Files.branches);
        public static string MainDir => Path.Combine(BranchesDir, Value.main);
        public static string MainIndexFile => Path.Combine(MainDir, Files.index);
        public static string MainCommitsDir => Path.Combine(MainDir, Dir.commits);
        public static string RaspAppDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Dir.rasp);
        public static string ConfigFile => Path.Combine(RaspAppDir, Files.config);
        public static string AzConfigFile => Path.Combine(RaspAppDir, Files.azConfig);

    }
}
