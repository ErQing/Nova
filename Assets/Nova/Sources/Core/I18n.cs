﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    // object can be string or string[]
    //
    // For the i-th (zero-based) string in the string[],
    // it will be used as the translation if the first argument provided to __ is i
    //
    // Example:
    // "ivebeenthere": ["I've never been there", "I've been there once", "I've been there twice", "I've been there {0} times"]
    // __("ivebeenthere", 2) == "I've been there twice"
    // __("ivebeenthere", 4) == "I've been there 4 times"
    using TranslationBundle = Dictionary<string, object>;

    [ExportCustomType]
    public static class I18n
    {
        public const string LocalizedResourcesPath = "LocalizedResources/";
        public const string LocalizedStringsPath = "LocalizedStrings/";

        public static readonly SystemLanguage[] SupportedLocales =
            {SystemLanguage.ChineseSimplified, SystemLanguage.English};

        public static SystemLanguage DefaultLocale => SupportedLocales[0];

        private static SystemLanguage _currentLocale = FallbackLocale(Application.systemLanguage);

        public static SystemLanguage CurrentLocale
        {
            get => _currentLocale;
            set
            {
                value = FallbackLocale(value);
                if (_currentLocale == value)
                {
                    return;
                }

                _currentLocale = value;
                LocaleChanged.Invoke();
            }
        }

        private static SystemLanguage FallbackLocale(SystemLanguage locale)
        {
            if (locale == SystemLanguage.Chinese || locale == SystemLanguage.ChineseSimplified ||
                locale == SystemLanguage.ChineseTraditional)
            {
                return SystemLanguage.ChineseSimplified;
            }
            else
            {
                return SystemLanguage.English;
            }
        }

        public static readonly UnityEvent LocaleChanged = new UnityEvent();

        private static bool Inited;

        private static void Init()
        {
            if (Inited) return;
            LoadTranslationBundles();
            Inited = true;
        }

        private static readonly Dictionary<SystemLanguage, TranslationBundle> TranslationBundles =
            new Dictionary<SystemLanguage, TranslationBundle>();

        private static void LoadTranslationBundles()
        {
            foreach (var locale in SupportedLocales)
            {
                var textAsset = Resources.Load(LocalizedStringsPath + locale) as TextAsset;
                TranslationBundles[locale] = JsonConvert.DeserializeObject<TranslationBundle>(textAsset.text);
            }
        }

        /// <summary>
        /// Get the translation specified by key and optionally deal with the plurals and format arguments. (Shorthand)<para />
        /// Translation will be automatically reloaded if the JSON file is changed.
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="key">Key to specify the translation</param>
        /// <param name="args">Arguments to provide to the translation as a format string.<para />
        /// The first argument will be used to determine the quantity if needed.</param>
        /// <returns>The translated string.</returns>
        public static string __(SystemLanguage locale, string key, params object[] args)
        {
#if UNITY_EDITOR
            EditorOnly_GetLatestTranslation();
#endif

            Init();

            string translation = key;

            if (TranslationBundles[locale].TryGetValue(key, out var raw))
            {
                if (raw is string value)
                {
                    translation = value;
                }
                else if (raw is string[] formats)
                {
                    if (formats.Length == 0)
                    {
                        Debug.LogWarningFormat("Nova: Empty translation string list for: {0}", key);
                    }
                    else if (args.Length == 0)
                    {
                        translation = formats[0];
                    }
                    else
                    {
                        // The first argument will determine the quantity
                        object arg1 = args[0];
                        if (arg1 is int i)
                        {
                            translation = formats[Math.Min(i, formats.Length - 1)];
                        }
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Nova: Invalid translation format for: {0}", key);
                }

                if (args.Length > 0)
                {
                    translation = string.Format(translation, args);
                }
            }
            else
            {
                Debug.LogWarningFormat("Nova: Missing translation for: {0}", key);
            }

            return translation;
        }

        public static string __(string key, params object[] args)
        {
            return __(CurrentLocale, key, args);
        }

        // Get localized string with fallback to DefaultLocale
        public static string __(Dictionary<SystemLanguage, string> dict)
        {
            if (dict.ContainsKey(CurrentLocale))
            {
                return dict[CurrentLocale];
            }
            else
            {
                return dict[DefaultLocale];
            }
        }

#if UNITY_EDITOR
        private static string EditorTranslationPath => "Assets/Nova/Resources/" + LocalizedStringsPath + CurrentLocale + ".json";

        private static DateTime LastWriteTime;

        private static void EditorOnly_GetLatestTranslation()
        {
            var writeTime = File.GetLastWriteTime(EditorTranslationPath);
            if (writeTime != LastWriteTime)
            {
                LastWriteTime = writeTime;
                Inited = false;
            }
        }
#endif
    }
}