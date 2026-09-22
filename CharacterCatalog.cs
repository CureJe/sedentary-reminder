using System;
using System.Security.Cryptography;
using System.Text;

namespace StandUpBuddy
{
    internal enum CharacterKind
    {
        OrangeCat,
        Corgi,
        RedPanda,
        MechGuardian,
        WebRanger
    }

    internal static class CharacterCatalog
    {
        public static readonly string[] DisplayNames =
        {
            "Rotate characters",
            "Orange Cat",
            "Corgi",
            "Red Panda",
            "Mech Guardian",
            "Web Ranger"
        };

        public const int Count = 5;

        // Fingerprints of the six pre-English labels, in DisplayNames order.
        // Only used to read old settings; all new values and UI text are English.
        private static readonly string[] LegacyFingerprints =
        {
            "9cb5893f1f9e56fcbff35e18f67cb00da68a01caac99bf058ff0be3a1e9bea99",
            "e4aa2a7aeb049761d9ccd3b92ae358e33ba98e968ae900929e0dbc7140d562c7",
            "7f43dc3a15e364f88e5e6701c1a29e338d3f1d5e6118632aa235b48b320fdccc",
            "a7fc7f81bd33fcc7aa72638b6433cf25415b54d40de78f81fb430a46a1167ee6",
            "aed20bc4fce5d14aad718165e1d2df06d58c4df5005a9aebb24c108ea758cbfd",
            "9b8d24426f089a8fdc3dc7052fe33a7fe65b2cee1f691f0daac3371a9d043a07"
        };

        internal static string Fingerprint(string value)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant();
        }

        public static string NormalizeDisplayName(string name)
        {
            if (Array.IndexOf(DisplayNames, name) >= 0) return name;
            string fingerprint = Fingerprint(name);
            int legacyIndex = Array.IndexOf(LegacyFingerprints, fingerprint);
            if (legacyIndex >= 0) return DisplayNames[legacyIndex];
            if (fingerprint == "cbf13619427a09501e136570963430e5075bf9f70dd52c5b85b08e16aa678663")
                return DisplayNames[5];
            return DisplayNames[0];
        }

        public static CharacterKind FromDisplayName(string name)
        {
            name = NormalizeDisplayName(name);
            for (int i = 1; i < DisplayNames.Length; i++)
                if (string.Equals(DisplayNames[i], name, StringComparison.Ordinal))
                    return (CharacterKind)(i - 1);
            return CharacterKind.OrangeCat;
        }
    }
}
