﻿using DanielWillett.JavaPropertiesParser;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Translations.Storage;
public class PropertiesTranslationStorage : ITranslationStorage
{
    private readonly LanguageService _languageService;

    private readonly string _basePath;

    public string FileName { get; set; }
    public PropertiesTranslationStorage(string fileName, IServiceProvider serviceProvider)
    {
        if (!fileName.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
            fileName += ".properties";

        FileName = fileName;
        _basePath = Path.Combine(serviceProvider.GetRequiredService<WarfareModule>().HomeDirectory, TranslationService.TranslationsFolder);

        _languageService = serviceProvider.GetRequiredService<LanguageService>();
    }

    private string GetFilePath(string langCode)
    {
        return Path.Combine(_basePath, langCode, FileName);
    }

    public void Save(IEnumerable<Translation> translations, LanguageInfo? language = null)
    {
        string languageFileName = GetFilePath(language?.Code ?? _languageService.DefaultLanguageCode);
        using PropertiesWriter writer = new PropertiesWriter(languageFileName);

        foreach (Translation translation in translations)
        {
            if (writer.Stream.Position != 0)
                writer.WriteLine();

            WriteTranslation(writer, translation, language);
        }
    }

    public IReadOnlyDictionary<TranslationLanguageKey, string> Load()
    {
        // ensure default directory is there
        string defaultDir = Path.Combine(_basePath, _languageService.DefaultLanguageCode);
        Directory.CreateDirectory(defaultDir);

        // find and parse files
        string[] languageDirs = Directory.GetDirectories(_basePath, "*", SearchOption.TopDirectoryOnly);

        Dictionary<TranslationLanguageKey, string> translationDict = new Dictionary<TranslationLanguageKey, string>(32);

        foreach (string languageDirectory in languageDirs)
        {
            string languageId = Path.GetFileName(languageDirectory)!;
            if (languageId.Length != 5)
                continue;

            string languageFileName = GetFilePath(languageId);

            if (!File.Exists(languageFileName))
                continue;

            using PropertiesReader reader = new PropertiesReader(languageFileName);

            // todo more error handling (duplicate handling)
            while (reader.TryReadPair(out string key, out string value))
            {
                TranslationLanguageKey dictKey = new TranslationLanguageKey(languageId, key);
                translationDict[dictKey] = value;
            }
        }

        return translationDict;
    }

    internal static void WriteTranslation(PropertiesWriter writer, Translation translation, LanguageInfo? language = null)
    {
        translation.AssertInitialized();

        Type type = translation.GetType();

        string value = translation.Original.Value;
        if (language != null && translation.Table.TryGetValue(language.Code, out TranslationValue valueEntry))
        {
            value = valueEntry.Value;
        }

        if (!string.IsNullOrWhiteSpace(translation.Data.Description))
        {
            writer.WriteComment("Description: " + translation.Data.Description);
        }

        // write argument descriptions
        for (int i = 0; i < translation.ArgumentCount; ++i)
        {
            Type argumentType = type.GenericTypeArguments[i];
            string comment = $" {{{i}}} - {argumentType.Name}";

            if (translation.Data.ParameterDescriptions is { } descs && descs.Length > i && !string.IsNullOrWhiteSpace(descs[i]))
            {
                comment += " " + descs[i];
            }

            writer.WriteComment(comment);
        }

        // write default value if it doesn't match current value.
        if (!translation.Original.Value.Equals(value, StringComparison.Ordinal))
        {
            writer.WriteComment("Default: " + translation.Original.Value);
        }

        writer.WriteKeyValue(translation.Key, value);
    }

    public override string ToString() => FileName;
}