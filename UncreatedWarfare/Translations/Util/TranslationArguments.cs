using System;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Translations.Util;

/// <summary>
/// Shared structure of common data for processing translations.
/// </summary>
public readonly ref struct TranslationArguments
{
    /// <summary>
    /// The set of values relating to this translation.
    /// </summary>
    public readonly TranslationValue ValueSet;

    /// <summary>
    /// If Unity Rich Text formatted text should be used instead of TextMeshPro formatted text.
    /// </summary>
    public readonly bool UseIMGUI;

    /// <summary>
    /// If the starting color tag should be removed and returned separately (this allows us to get a few more characters out of chat messages).
    /// </summary>
    public readonly bool UseUncoloredTranslation;

    /// <summary>
    /// The unprocessed translation value based on the language given.
    /// </summary>
    public readonly ReadOnlySpan<char> PreformattedValue;

    /// <summary>
    /// Specific player this translation is being formatted for, if any.
    /// </summary>
    public readonly WarfarePlayer? Player;

    /// <summary>
    /// Specific team this translation is being formatted for, if any.
    /// </summary>
    public readonly Team? Team;

    /// <summary>
    /// Flag options for this translation.
    /// </summary>
    public readonly TranslationOptions Options;

    /// <summary>
    /// The language to use for this translation.
    /// </summary>
    public readonly LanguageInfo Language;

    /// <summary>
    /// The format provider to use for this translation.
    /// </summary>
    public readonly CultureInfo Culture;

    /// <summary>
    /// The time zone to use for formatting dates.
    /// </summary>
    public readonly TimeZoneInfo TimeZone;

    public TranslationArguments(TranslationValue valueSet, bool useIMGUI, bool useUncoloredTranslation, WarfarePlayer player, TranslationOptions options)
        : this(valueSet, useIMGUI, useUncoloredTranslation, player.Locale.LanguageInfo, player, player.Team, options, player.Locale.CultureInfo, player.Locale.TimeZone)
    {

    }

    public TranslationArguments(TranslationValue valueSet, bool useIMGUI, bool useUncoloredTranslation, LanguageInfo language, WarfarePlayer? player, Team? team, TranslationOptions options, CultureInfo culture, TimeZoneInfo timeZone)
    {
        ValueSet = valueSet;
        UseIMGUI = useIMGUI;
        UseUncoloredTranslation = useUncoloredTranslation;
        PreformattedValue = valueSet.GetValueSpan(useIMGUI, useUncoloredTranslation, (options & TranslationOptions.ForTerminal) != 0);
        Language = language;
        Player = player;
        Team = team is { IsValid: true } ? team : null;
        Options = options;
        Culture = culture;
        TimeZone = timeZone;
    }
}