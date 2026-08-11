using System;

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
            "随机轮换",
            "橘猫团子",
            "柯基短腿",
            "小熊猫阿卷",
            "机甲守卫",
            "蛛网侠客 · Web Ranger"
        };

        public const int Count = 5;

        public static CharacterKind FromDisplayName(string name)
        {
            if (string.Equals(name, "蛛网侠客", StringComparison.Ordinal))
                return CharacterKind.WebRanger;
            for (int i = 1; i < DisplayNames.Length; i++)
                if (string.Equals(DisplayNames[i], name, StringComparison.Ordinal))
                    return (CharacterKind)(i - 1);
            return CharacterKind.OrangeCat;
        }
    }
}
