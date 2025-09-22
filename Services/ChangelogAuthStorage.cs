using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Bohemia_Solutions.Services
{
    public sealed class ChangelogAuthConfig
    {
        public string SaltBase64 { get; set; } = "";
        public string HashBase64 { get; set; } = "";
        public int Iterations { get; set; } = 120_000;
    }

    public static class ChangelogAuthStorage
    {
        // umísti vedle ostatních JSONů
        public static string GetPath()
        {
            var appDir = AppContext.BaseDirectory;
            return Path.Combine(appDir, "changelog_admin.json");
        }

        public static ChangelogAuthConfig LoadOrSeed(string initialPlain = "adminPH")
        {
            var path = GetPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ChangelogAuthConfig>(json) ?? Seed(initialPlain);
            }
            var cfg = Seed(initialPlain);
            Save(cfg);
            return cfg;
        }

        public static void Save(ChangelogAuthConfig cfg)
        {
            var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetPath(), json);
        }

        private static ChangelogAuthConfig Seed(string initialPlain)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            var cfg = new ChangelogAuthConfig();
            cfg.SaltBase64 = Convert.ToBase64String(salt);
            cfg.HashBase64 = Hash(initialPlain, salt, cfg.Iterations);
            return cfg;
        }

        public static bool Verify(string plain, ChangelogAuthConfig cfg)
        {
            var salt = Convert.FromBase64String(cfg.SaltBase64);
            var computed = Hash(plain, salt, cfg.Iterations);
            // constant-time porovnání
            return FixedTimeEquals(Convert.FromBase64String(computed), Convert.FromBase64String(cfg.HashBase64));
        }

        private static string Hash(string plain, byte[] salt, int iterations)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(plain, salt, iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(key);
        }

        private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
