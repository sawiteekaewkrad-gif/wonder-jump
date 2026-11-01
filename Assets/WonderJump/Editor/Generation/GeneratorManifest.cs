using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace WonderJump.EditorTools.Generation
{
    /// <summary>
    /// Records, for every asset the generator wrote: the file hash right after writing it and the
    /// fingerprint of the definition it was built from. This is what makes regeneration safe:
    ///   file hash differs  -> someone edited the generated asset -> skip it (unless forced)
    ///   fingerprint differs -> the catalog changed -> rebuild it in place (same GUID)
    ///   both equal          -> up to date -> do nothing (repeatable, no churn)
    /// Stored as "_GeneratorManifest.json" in the generated root. Commit it to Git.
    /// </summary>
    [Serializable]
    public sealed class GeneratorManifest
    {
        public const string FileName = "_GeneratorManifest.json";

        [Serializable]
        public sealed class Entry
        {
            public string path;
            public string fileHash;
            public string definition;
        }

        public int generatorVersion;
        public List<Entry> entries = new List<Entry>();

        public Entry Find(string assetPath)
        {
            for (int i = 0; i < entries.Count; i++)
                if (string.Equals(entries[i].path, assetPath, StringComparison.Ordinal)) return entries[i];
            return null;
        }

        public void Record(string assetPath, string fileHash, string definition)
        {
            Entry e = Find(assetPath);
            if (e == null)
            {
                e = new Entry { path = assetPath };
                entries.Add(e);
            }
            e.fileHash = fileHash;
            e.definition = definition;
        }

        public static GeneratorManifest Load(string assetPath)
        {
            string full = AssetPaths.ToFullPath(assetPath);
            if (!File.Exists(full)) return new GeneratorManifest();
            try
            {
                GeneratorManifest m = JsonUtility.FromJson<GeneratorManifest>(File.ReadAllText(full));
                return m ?? new GeneratorManifest();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WonderJump] Generator manifest unreadable, treating all existing assets as untracked: " + ex.Message);
                return new GeneratorManifest();
            }
        }

        public void Save(string assetPath)
        {
            entries.Sort((a, b) => string.CompareOrdinal(a.path, b.path)); // stable diffs in Git
            File.WriteAllText(AssetPaths.ToFullPath(assetPath), JsonUtility.ToJson(this, true));
        }

        /// <summary>SHA-256 of the asset file on disk (hex), or null if the file does not exist.</summary>
        public static string HashFile(string assetPath)
        {
            string full = AssetPaths.ToFullPath(assetPath);
            if (!File.Exists(full)) return null;
            byte[] bytes = File.ReadAllBytes(full);
            return HashBytes(bytes);
        }

        public static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(bytes);
                var sb = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++) sb.Append(digest[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }

    public static class AssetPaths
    {
        /// <summary>"Assets/Foo/Bar.prefab" -> absolute path on disk.</summary>
        public static string ToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        public static string Combine(string a, string b)
        {
            return a.TrimEnd('/') + "/" + b.TrimStart('/');
        }

        public static string Parent(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            return slash <= 0 ? assetPath : assetPath.Substring(0, slash);
        }
    }
}
