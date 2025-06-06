﻿namespace Uncreated.Warfare.Translations.ValueFormatters;

/// <summary>
/// Allows objects to define how they're translated in their own definition instead of creating a value converter.
/// </summary>
public interface ITranslationArgument
{
    /// <summary>
    /// Allows objects to define how they're translated in their own definition instead of creating a value converter.
    /// </summary>
    string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters);
}