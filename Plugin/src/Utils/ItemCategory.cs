using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AdditionalNetworking.Utils;

public class ItemCategory
{
    internal static Item[] VanillaItems;
    internal static readonly Dictionary<Item, (string api, string modname)> ItemModMap = [];
    internal static readonly Dictionary<Item, string> ItemKeyMap = [];

    public static string GetPathForItem(Item item)
    {
        var modTag = GetTagForItem(item);

        return GetPathForTag(modTag, item);
    }

    public static (string api, string modname) GetTagForItem(Item item)
    {
        if (!ItemModMap.TryGetValue(item, out var modTag))
        {
            modTag = ("Unknown", "");
            ItemModMap[item] = modTag;
        }

        return modTag;
    }

    public static string GetPathForTag((string api, string modname) modTag, Item item)
    {
        var cleanName = string.Join("_",
                item.itemName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');

        var cleanMod = string.Join("_",
                modTag.modname.Split(Path.GetInvalidPathChars(), StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');

        var path = Path.Combine(modTag.api, cleanMod, cleanName);

        return path;
    }

    public static string GetKeyForItem(Item item)
    {
        if (ItemKeyMap.TryGetValue(item, out var key))
            return key;

        var itemTag = GetPathForItem(item);
        itemTag = itemTag.Replace(Path.DirectorySeparatorChar, '/');
        key = itemTag;
        ItemKeyMap[item] = key;

        return key;
    }

    private static readonly Regex ConfigFilterRegex = new Regex(@"[\n\t\\\'\[\]]");

    public static string SanitizeForConfig(string input)
    {
        return ConfigFilterRegex.Replace(input, "").Trim();
    }
}