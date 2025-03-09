namespace Rasp {
    public static class DeltaStorage {
        public static void CreateDiff( string oldFile, string newFile, string diffFile ) {
            byte[] oldBytes = File.Exists(oldFile) ? File.ReadAllBytes(oldFile) : [];
            byte[] newBytes = File.ReadAllBytes(newFile);

            List<byte> diffBytes =[];
            int minLength = Math.Min(oldBytes.Length, newBytes.Length);
            for ( int i = 0; i < minLength; i++ ) {
                if ( oldBytes[i] != newBytes[i] ) {
                    diffBytes.Add((byte)i);  // Store the position of the change
                    diffBytes.Add(newBytes[i]);  // Store the new value
                }
            }

            // If the new file is longer, add extra bytes
            for ( int i = minLength; i < newBytes.Length; i++ ) {
                diffBytes.Add((byte)i);
                diffBytes.Add(newBytes[i]);
            }

            string destinationDir = Path.GetDirectoryName(diffFile) ?? "";
            Directory.CreateDirectory(destinationDir);

            File.WriteAllBytes(diffFile, [.. diffBytes]);
        }

        public static void ApplyDiff( string oldFile, string diffFile, string restoredFile ) {
            byte[] oldBytes = File.Exists(oldFile) ? File.ReadAllBytes(oldFile) : [];
            byte[] diffBytes = File.ReadAllBytes(diffFile);

            byte[] restoredBytes = new byte[Math.Max(oldBytes.Length, diffBytes.Length / 2)];
            Array.Copy(oldBytes, restoredBytes, oldBytes.Length);

            for ( int i = 0; i < diffBytes.Length; i += 2 ) {
                if ( i + 1 >= diffBytes.Length )
                    break; // Prevent out-of-bounds error

                int position = diffBytes[i];
                byte newValue = diffBytes[i + 1];

                if ( position < restoredBytes.Length ) {
                    restoredBytes[position] = newValue;
                } else {
                    Console.WriteLine($"Warning: Diff position {position} exceeds file size.");
                }
            }

            File.WriteAllBytes(restoredFile, restoredBytes);
        }
    }
}
