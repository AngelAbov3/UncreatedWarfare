using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;

/// <summary>
/// Command interaction helper inheriting <see cref="ControlException"/>, allowing an action to be taken and this to be thrown in the same line.
/// </summary>
public class CommandContext : ControlException
{
    public const string Default = "-";

    private readonly UserPermissionStore _permissionsStore;
    private readonly IPlayerService _playerService;
    private readonly CooldownManager? _cooldownManager;
    private readonly ChatService _chatService;
    private readonly ActionLoggerService? _actionLoggerService;
    private readonly int _argumentCount;
    private ILogger? _logger;

#if TELEMETRY
    private readonly Activity? _activity;
#endif

    private int _argumentOffset;

    internal readonly string[] OriginalParameters;

    internal Type? SwitchCommand;

    /// <summary>
    /// Useful for sub-commands, offsets any parsing methods.
    /// </summary>
    /// <remarks>Increment this to skip one argument, for example.</remarks>
    public int ArgumentOffset
    {
        get => _argumentOffset;
        set
        {
            if (value == _argumentOffset)
                return;

#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Adjust argument offset: {_argumentOffset} -> {value}"));
#endif

            _argumentOffset = value;
            if (OriginalParameters.Length - _argumentOffset <= 0)
            {
                ArgumentCount = 0;
                Parameters = new ArraySegment<string>(OriginalParameters, 0, 0);
            }
            else
            {
                ArgumentCount = _argumentCount - _argumentOffset;
                Parameters = new ArraySegment<string>(OriginalParameters, _argumentOffset, ArgumentCount);
            }
        }
    }

#nullable disable
    /// <summary>
    /// Player that called the command. Will be <see langword="null"/> if the command was called from console or some other source, but not marked as nullable for ease of use.
    /// </summary>
    /// <remarks>Always call <see cref="AssertRanByPlayer"/> before using this property.</remarks>
    public WarfarePlayer Player { get; }
#nullable restore

    /// <summary>
    /// User that called the command. Will never be <see langword="null"/>.
    /// </summary>
    public ICommandUser Caller { get; }

    /// <summary>
    /// A token that is cancelled when the command finishes.s
    /// </summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Command arguments not including the name or flags.
    /// </summary>
    /// <remarks>Adjusts depending on <see cref="ArgumentOffset"/>.</remarks>
    public ArraySegment<string> Parameters { get; private set; }

    /// <summary>
    /// Number of arguments provided not including the name or flags.
    /// </summary>
    /// <remarks>Adjusts depending on <see cref="ArgumentOffset"/>.</remarks>
    public int ArgumentCount { get; private set; }

    /// <summary>
    /// All command flags.
    /// </summary>
    /// <remarks>This is not affected by <see cref="ArgumentOffset"/>.</remarks>
    public CommandFlagInfo[] Flags { get; }

    /// <summary>
    /// Steam 64 id of the caller.
    /// </summary>
    /// <remarks><see cref="CSteamID.Nil"/> when called by console.</remarks>
    public CSteamID CallerId { get; }

    /// <summary>
    /// Original command message sent by the caller.
    /// </summary>
    public string OriginalMessage { get; }

    /// <summary>
    /// If this interaction has been responded to yet.
    /// </summary>
    public bool Responded { get; private set; }

    /// <summary>
    /// The locale to use for this command.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// The language to use for this command.
    /// </summary>
    public LanguageInfo Language { get; }

    /// <summary>
    /// Format used to parse numbers for this command.
    /// </summary>
    public NumberFormatInfo ParseFormat { get; }

    /// <summary>
    /// Culture used to parse information for this command.
    /// </summary>
    public CultureInfo ParseCulture { get; }

    /// <summary>
    /// If the player has the <see cref="PlayerSave.IMGUI"/> setting ticked.
    /// </summary>
    public bool IMGUI { get; }

    /// <summary>
    /// Base cooldown time for a command.
    /// </summary>
    public float? CommandCooldownTime { get; set; }

    /// <summary>
    /// Manually set cooldown time for a command. Should be set before exiting the command.
    /// </summary>
    public float? IsolatedCommandCooldownTime { get; set; }

    /// <summary>
    /// If this command is already on the isolated cooldown.
    /// </summary>
    public bool OnIsolatedCooldown { get; private set; }

    /// <summary>
    /// The isolated cooldown this command is already on.
    /// </summary>
    public Cooldown IsolatedCooldown { get; internal set; }

    /// <summary>
    /// Command instance being executed.
    /// </summary>
    public IExecutableCommand Command { get; internal set; }

    /// <summary>
    /// The current scoped service provider.
    /// </summary>
    public IServiceProvider ServiceProvider { get; internal set; }

    /// <summary>
    /// Type information about the command.
    /// </summary>
    public CommandInfo CommandInfo { get; }

    /// <summary>
    /// Reference to the common translation set.
    /// </summary>
    public CommonTranslations CommonTranslations { get; }

    /// <summary>
    /// Logger created for the executing command type.
    /// </summary>
    public ILogger Logger
    {
        get => _logger ??= ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(CommandInfo.Type);
    }

    private CommandContext(ICommandUser user, IServiceProvider serviceProvider
#if TELEMETRY
        , Activity? activity
#endif
    )
    {
        Caller = user;
        Player = user as WarfarePlayer;

        ServiceProvider = serviceProvider;

#if TELEMETRY
        _activity = activity;
#endif

        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _actionLoggerService = serviceProvider.GetRequiredService<ActionLoggerService>();
        _permissionsStore = serviceProvider.GetRequiredService<UserPermissionStore>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _cooldownManager = serviceProvider.GetService<CooldownManager>();
        CommonTranslations = serviceProvider.GetRequiredService<TranslationInjection<CommonTranslations>>().Value;
        LanguageService languageService = serviceProvider.GetRequiredService<LanguageService>();

        if (Player == null)
        {
            Language = languageService.GetDefaultLanguage();
            Culture = languageService.GetDefaultCulture();
            ParseFormat = Culture.NumberFormat;
            ParseCulture = Culture;
        }
        else
        {
            Language = Player.Locale.LanguageInfo;
            Culture = Player.Locale.CultureInfo;
            ParseFormat = Player.Locale.ParseFormat;
            IMGUI = Player is { Save.IMGUI: true };
            ParseCulture = Player.Locale.Preferences.UseCultureForCommandInput ? Culture : languageService.GetDefaultCulture();
        }

        OriginalParameters = Array.Empty<string>();
        Flags = Array.Empty<CommandFlagInfo>();
        OriginalMessage = string.Empty;
        Command = null!;
        CommandInfo = null!;
        Parameters = new ArraySegment<string>(OriginalParameters, 0, 0);

        CallerId = user.Steam64;

#if TELEMETRY
        if (_activity == null)
            return;

        _activity.AddTag("imgui", IMGUI);
        _activity.AddTag("language", Language.Code);
        _activity.AddTag("culture", Culture.Name);
        _activity.AddTag("parse-culture", ParseCulture.Name);
        _activity.AddTag("caller-id", CallerId.m_SteamID);
#endif
    }

    public CommandContext(ICommandUser user, CancellationToken token, string[] args, CommandFlagInfo[] flags, string originalMessage, CommandInfo commandInfo, IServiceProvider serviceProvider
#if TELEMETRY
        , Activity? activity
#endif
    ) : this(user, serviceProvider
#if TELEMETRY
        , activity
#endif
    )
    {
        CommandInfo = commandInfo;

        if (user is TerminalUser)
            user = new TerminalUser(Logger);

        Token = token;
        Caller = user;
        Player = user as WarfarePlayer;

        OriginalMessage = originalMessage;
        args ??= Array.Empty<string>();

        // flag parsing

        OriginalParameters = args;
        _argumentCount = args.Length;
        Flags = flags;
        ArgumentCount = _argumentCount;
        Parameters = new ArraySegment<string>(OriginalParameters, 0, OriginalParameters.Length);

#if TELEMETRY
        if (_activity == null)
            return;

        _activity.AddTag("flags", Flags.Length);
        for (int i = 0; i < flags.Length; i++)
        {
            CommandFlagInfo flag = flags[i];
            _activity.AddTag($"flag-{i}", flag.ToString());
        }

        _activity.AddTag("parameters", args.Length);
        for (int i = 0; i < args.Length; i++)
        {
            _activity.AddTag($"parameter-{i}", args[i]);
        }
#endif
    }

    /// <summary>
    /// Creates a temporary <see cref="CommandContext"/> that can only be used for sending messages.
    /// </summary>
    public static CommandContext CreateTemporary(ICommandUser user, IServiceProvider serviceProvider)
    {
        return new CommandContext(user ?? throw new ArgumentNullException(nameof(user)), serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))
#if TELEMETRY
            , null
#endif
        );
    }

    /// <summary>
    /// Keep the command from sending the 'no response' message without sending anything.
    /// </summary>
    /// <returns>The instance of this <see cref="CommandContext"/> for chaining or throwing.</returns>
    public CommandContext Defer()
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent("Deferred"));
#endif
        Responded = true;
        return this;
    }

    /// <summary>
    /// Check if there is an argument at <paramref name="position"/>.
    /// </summary>
    /// <param name="position">Zero-based argument index not including the command name.</param>
    public bool HasArgument(int position)
    {
        position += _argumentOffset;
        return position > -1 && position < _argumentCount;
    }

    /// <summary>
    /// Check if there are at least <paramref name="count"/> arguments.
    /// </summary>
    /// <param name="count">One-based argument index not including the command name.</param>
    public bool HasArgs(int count)
    {
        count += _argumentOffset;
        return count > -1 && count <= _argumentCount;
    }

    /// <summary>
    /// Check if there are exactly <paramref name="count"/> arguments.
    /// </summary>
    /// <param name="count">One-based argument index not including the command name.</param>
    public bool HasArgsExact(int count)
    {
        count += _argumentOffset;
        return count == _argumentCount;
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="value"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches <paramref name="value"/>.</returns>
    public bool MatchParameter(int parameter, string value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
            return false;

        return OriginalParameters[parameter].Equals(value, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="value"/> and <paramref name="alternate"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches <paramref name="value"/> or <paramref name="alternate"/>.</returns>
    public bool MatchParameter(int parameter, string value, string alternate)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
            return false;

        string v = OriginalParameters[parameter];
        return v.Equals(value, StringComparison.InvariantCultureIgnoreCase) || v.Equals(alternate, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="value"/>, <paramref name="alternate1"/>, and <paramref name="alternate2"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches <paramref name="value"/>, <paramref name="alternate1"/>, or <paramref name="alternate2"/>.</returns>
    public bool MatchParameter(int parameter, string value, string alternate1, string alternate2)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
            return false;

        string v = OriginalParameters[parameter];
        return v.Equals(value, StringComparison.InvariantCultureIgnoreCase) || v.Equals(alternate1, StringComparison.InvariantCultureIgnoreCase) || v.Equals(alternate2, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="alternates"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches one of the values in <paramref name="alternates"/>.</returns>
    public bool MatchParameter(int parameter, params string[] alternates)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
            return false;

        string v = OriginalParameters[parameter];
        for (int i = 0; i < alternates.Length; ++i)
        {
            if (v.Equals(alternates[i], StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    private bool CheckFlag(ReadOnlySpan<char> check, bool canBeWord)
    {
        ReadOnlySpan<char> flagPrefixes = [ '-', '–', '—', '−' ];

        int flagStart = 0;
        while (flagStart < check.Length && flagPrefixes.IndexOf(check[flagStart]) >= 0)
            ++flagStart;

        if (flagStart > 2 || flagStart == check.Length)
            return false;

        check = check[flagStart..];

        if (canBeWord)
        {
            for (int i = 0; i < Flags.Length; ++i)
            {
                ref CommandFlagInfo info = ref Flags[i];
                if (info.DashCount == 2 && check.Equals(info.FlagName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        if (check.Length != 1)
            return false;

        for (int i = 0; i < Flags.Length; ++i)
        {
            ref CommandFlagInfo info = ref Flags[i];
            if (info.DashCount == 1 && info.FlagName.IndexOf(check[0]) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compare the value of all flags with <paramref name="letter"/>. Case and culture insensitive.
    /// </summary>
    /// <returns><see langword="true"/> if the parameter matches.</returns>
    public bool MatchFlag(string letter)
    {
        CheckFlag(letter, true);
        return false;
    }

    /// <summary>
    /// Checks to see if a flag with the given <paramref name="letter"/> and <paramref name="word"/> is matched, where <paramref name="word"/> is case-insensitive.
    /// </summary>
    /// <remarks>Example: <c>('e', "enter")</c></remarks>
    public bool MatchFlag(char letter, string word)
    {
        return CheckFlag(word, true) || CheckFlag(MemoryMarshal.CreateSpan(ref letter, 1), false);
    }


    /// <summary>
    /// Returns the <paramref name="parameter"/> at a given index, or <see langword="null"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public string? Get(int parameter)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
            return null;
        return OriginalParameters[parameter];
    }

    /// <summary>
    /// Returns a range of parameters from a given <paramref name="start"/> index along a given <paramref name="length"/> (joined by spaces), or <see langword="null"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public string? GetRange(int start, int length = -1)
    {
        if (length == 1) return Get(start);
        start += _argumentOffset;
        if (start < 0 || start >= _argumentCount)
            return null;
        if (start == _argumentCount - 1)
            return OriginalParameters[start];
        if (length == -1)
            return string.Join(" ", OriginalParameters, start, _argumentCount - start);
        if (length < 1) return null;
        if (start + length >= _argumentCount)
            length = _argumentCount - start;
        return string.Join(" ", OriginalParameters, start, length);
    }

    /// <summary>
    /// Gets a range of parameters from a given <paramref name="start"/> index along a given <paramref name="length"/> (joined by spaces), or returns <see langword="false"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGetRange(int start, [MaybeNullWhen(false)] out string value, int length = -1)
    {
        value = GetRange(start, length);
        return value is not null;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, or returns <see langword="false"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, [MaybeNullWhen(false)] out string value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = null;
            return false;
        }
        value = OriginalParameters[parameter];
        return true;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <typeparamref name="TEnum"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet<TEnum>(int parameter, out TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = default;
            return false;
        }

        return Enum.TryParse(OriginalParameters[parameter], true, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="Color"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out Color value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = Color.white;
            return false;
        }

        return HexStringHelper.TryParseColor(OriginalParameters[parameter], CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="Color32"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out Color32 value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = Color.white;
            return false;
        }

        return HexStringHelper.TryParseColor32(OriginalParameters[parameter], CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="int"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out int value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return int.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="byte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out byte value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return byte.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="short"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out short value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return short.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="sbyte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out sbyte value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return sbyte.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="Guid"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out Guid value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = default;
            return false;
        }
        return Guid.TryParse(OriginalParameters[parameter], out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="uint"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out uint value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return uint.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ushort"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out ushort value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return ushort.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ulong"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use <see cref="TryGet(int,out ulong,out WarfarePlayer?, bool)"/> instead for Steam64 IDs.</remarks>
    public bool TryGet(int parameter, out ulong value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return ulong.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="bool"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out bool value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = false;
            return false;
        }

        string p = OriginalParameters[parameter];
        if (p.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            p.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
            p.Equals("1", StringComparison.InvariantCultureIgnoreCase) ||
            p.Equals("y", StringComparison.InvariantCultureIgnoreCase))
        {
            value = true;
        }
        else if (p.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
                 p.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
                 p.Equals("0", StringComparison.InvariantCultureIgnoreCase) ||
                 p.Equals("n", StringComparison.InvariantCultureIgnoreCase))
        {
            value = false;
        }
        else
        {
            value = false;
            return false;
        }


        return true;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="float"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out float value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return float.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="double"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out double value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return double.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="decimal"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out decimal value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        return decimal.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <typeparamref name="TEnum"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef<TEnum>(int parameter, ref TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = default;
            return false;
        }
        if (Enum.TryParse(OriginalParameters[parameter], true, out TEnum value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="int"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref int value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (int.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out int value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="byte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref byte value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (byte.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out byte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="sbyte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref sbyte value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (sbyte.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out sbyte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="Guid"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref Guid value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = default;
            return false;
        }
        if (Guid.TryParse(OriginalParameters[parameter], out Guid value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="uint"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref uint value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (uint.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out uint value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ushort"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref ushort value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (ushort.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out ushort value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ulong"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref ulong value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (ulong.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out ulong value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="float"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref float value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (float.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out float value2) && !float.IsNaN(value2) && !float.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="double"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref double value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (double.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out double value2) && !double.IsNaN(value2) && !double.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="decimal"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref decimal value)
    {
        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            value = 0;
            return false;
        }
        if (decimal.TryParse(OriginalParameters[parameter], NumberStyles.Number, ParseFormat, out decimal value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="CSteamID"/> or steam profile URL, or returns <see langword="null"/> if out of range or unable to parse.
    /// </summary>
    public ValueTask<CSteamID?> TryGetSteamId(int parameter)
    {
        if (CallerId.IsIndividual() && MatchParameter(parameter, "me"))
        {
            return new ValueTask<CSteamID?>(CallerId);
        }

        parameter += _argumentOffset;
        if (parameter < 0 || parameter >= _argumentCount)
        {
            return new ValueTask<CSteamID?>(CSteamID.Nil);
        }

        string param = OriginalParameters[parameter];
        return SteamIdHelper.TryParseSteamId(param, out CSteamID id) ? new ValueTask<CSteamID?>(id) : SteamIdHelper.TryParseSteamIdOrUrl(param, Token);
    }

    /// <summary>
    /// Find a user or Steam64 ID from an argument. Will take either Steam64, name, or profile URL. Offline players will only be returned when the input is a Steam ID or profile URL.
    /// </summary>
    /// <param name="remainder">Select the rest of the arguments instead of just one.</param>
    /// <remarks>Zero based indexing.</remarks>
    /// <returns>The Steam64 ID if it is found, otehrwise <see langword="null"/>. If the player represented by the Steam64 ID is online, it will also be returned.</returns>
    public async ValueTask<(CSteamID? Steam64, WarfarePlayer? OnlinePlayer)> TryGetPlayer(int parameter, bool remainder = false, PlayerNameType searchType = PlayerNameType.CharacterName)
    {
        parameter += _argumentOffset;
        if (CallerId.IsIndividual() && MatchParameter(parameter, "me"))
        {
            return (CallerId, Player);
        }

        if (parameter < 0 || parameter >= _argumentCount)
        {
            return default;
        }

        string? s = remainder ? GetRange(parameter - _argumentOffset) : OriginalParameters[parameter];
        if (s != null)
        {
            CSteamID? steamId = await SteamIdHelper.TryParseSteamIdOrUrl(s, Token).ConfigureAwait(false);
            if (steamId.HasValue)
            {
                return (steamId, _playerService.GetOnlinePlayerOrNullThreadSafe(steamId.Value));
            }

            WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(s, ParseCulture, searchType);
            if (onlinePlayer is { IsOnline: true })
            {
                return (onlinePlayer.Steam64, onlinePlayer);
            }
        }

        return default;
    }

    /// <summary>
    /// Find a user or Steam64 ID from an argument. Will take either Steam64, name, or profile URL. Searches all players in <paramref name="selection"/>.
    /// </summary>
    /// <param name="remainder">Select the rest of the arguments instead of just one.</param>
    /// <remarks>Zero based indexing.</remarks>
    /// <returns>The player that is found (who may be offline).</returns>
    public async ValueTask<WarfarePlayer?> TryGetPlayer(int parameter, IEnumerable<WarfarePlayer> selection, bool remainder = false, PlayerNameType searchType = PlayerNameType.CharacterName)
    {
        parameter += _argumentOffset;
        if (CallerId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual && MatchParameter(parameter, "me"))
        {
            return selection.Contains(Player) ? Player : null;
        }

        if (parameter < 0 || parameter >= _argumentCount)
        {
            return null;
        }

        string? s = remainder ? GetRange(parameter - _argumentOffset) : OriginalParameters[parameter];
        if (s == null)
        {
            return null;
        }

        CSteamID? steam64 = await SteamIdHelper.TryParseSteamIdOrUrl(s, Token).ConfigureAwait(false);
        if (steam64.HasValue && steam64.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            ulong steamId = steam64.Value.m_SteamID;
            foreach (WarfarePlayer player in selection)
            {
                if (player.Steam64.m_SteamID == steamId)
                    return player;
            }
        }

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(s, selection, ParseCulture, searchType)!;
        return onlinePlayer ?? null;
    }


    /// <summary>
    /// Get an asset based on a <see cref="Guid"/> search, <see cref="ushort"/> search, then <see cref="Asset.FriendlyName"/> search.
    /// </summary>
    /// <typeparam name="TAsset"><see cref="Asset"/> type to find.</typeparam>
    /// <param name="len">Set to 1 to only get one parameter (default), set to -1 to get any remaining Arguments.</param>
    /// <param name="multipleResultsFound"><see langword="true"/> if <paramref name="allowMultipleResults"/> is <see langword="false"/> and multiple results were found.</param>
    /// <param name="allowMultipleResults">Set to <see langword="false"/> to make the function return <see langword="false"/> if multiple results are found. <paramref name="asset"/> will still be set.</param>
    /// <param name="selector">Filter assets to pick from.</param>
    /// <remarks>Zero based indexing. Do not use <see cref="ushort"/>s to search for objects, this is a deprecated feature by Unturned.</remarks>
    /// <returns><see langword="true"/> If a <typeparamref name="TAsset"/> is found or multiple are found and <paramref name="allowMultipleResults"/> is <see langword="true"/>.</returns>
    public bool TryGet<TAsset>(int parameter, [NotNullWhen(true)] out TAsset? asset, out bool multipleResultsFound, bool remainder = false, int len = 1, bool allowMultipleResults = false, Predicate<TAsset>? selector = null) where TAsset : Asset
    {
        if (!TryGetRange(parameter, out string? p, remainder ? -1 : len) || p.Length == 0)
        {
            multipleResultsFound = false;
            asset = null;
            return false;
        }
        return AssetUtility.TryGetAsset(p, out asset, out multipleResultsFound, allowMultipleResults, selector);
    }

    /// <summary>
    /// Get the transform the caller is looking at.
    /// </summary>
    /// <param name="mask">Raycast mask, could also use <see cref="ERayMask"/>. Defaults to <see cref="RayMasks.PLAYER_INTERACT"/>.</param>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetTargetRootTransform([MaybeNullWhen(false)] out Transform transform, int mask = 0, float distance = 4)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            transform = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, mask == 0 ? (RayMasks.PLAYER_INTERACT & ~RayMasks.ENEMY) : mask, Player.UnturnedPlayer);
        if (info.transform == null)
        {
            transform = null;
            return false;
        }

        transform = info.transform.root;
        return true;
    }

    /// <summary>
    /// Get <see cref="RaycastInfo"/> from the user.
    /// </summary>
    /// <param name="mask">Raycast mask, could also use <see cref="ERayMask"/>.</param>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetTargetInfo([MaybeNullWhen(false)] out RaycastInfo info, int mask = 0, float distance = 4)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            info = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, mask == 0 ? (RayMasks.PLAYER_INTERACT & ~RayMasks.ENEMY) : mask, Player.UnturnedPlayer);
        return info.transform != null;
    }

    /// <summary>
    /// Get the <see cref="Interactable"/> the user is looking at.
    /// </summary>
    /// <param name="mask">Raycast mask, could also use <see cref="ERayMask"/>. Defaults to <see cref="RayMasks.PLAYER_INTERACT"/>.</param>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetInteractableTarget<TInteractable>([MaybeNullWhen(false)] out TInteractable interactable, int mask = 0, float distance = 4f) where TInteractable : Interactable
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            interactable = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, mask == 0 ? (RayMasks.PLAYER_INTERACT & ~RayMasks.ENEMY) : mask, Player.UnturnedPlayer);
        if (info.transform == null)
        {
            interactable = null!;
            return false;
        }

        if (typeof(InteractableVehicle).IsAssignableFrom(typeof(TInteractable)))
        {
            interactable = (TInteractable)(object)info.vehicle;
            return interactable != null;
        }

        if (typeof(InteractableForage).IsAssignableFrom(typeof(TInteractable)))
        {
            if (info.transform.TryGetComponent(out InteractableForage forage))
            {
                interactable = (TInteractable)(object)forage;
                return interactable != null;
            }
        }

        if (ObjectManager.tryGetRegion(info.transform, out byte objX, out byte objY, out ushort index))
        {
            LevelObject obj = LevelObjects.objects[objX, objY][index];
            interactable = obj.interactable as TInteractable;
            return interactable != null;
        }

        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        interactable = drop?.interactable as TInteractable;
        return interactable != null;
    }

    /// <summary>
    /// Get the <see cref="BarricadeDrop"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetBarricadeTarget([MaybeNullWhen(false)] out BarricadeDrop drop, float distance = 4f)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            drop = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.BARRICADE, Player.UnturnedPlayer);
        if (info.transform == null)
        {
            drop = null;
            return false;
        }

        drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        return drop != null;
    }

    /// <summary>
    /// Get the <see cref="StructureDrop"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetStructureTarget([MaybeNullWhen(false)] out StructureDrop drop, float distance = 4f)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            drop = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.STRUCTURE, Player.UnturnedPlayer);
        if (info.transform == null)
        {
            drop = null;
            return false;
        }

        drop = StructureManager.FindStructureByRootTransform(info.transform);
        return drop != null;
    }

    /// <summary>
    /// Get the <see cref="BarricadeDrop"/> or <see cref="StructureDrop"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetBuildableTarget([MaybeNullWhen(false)] out IBuildable drop, float distance = 4f)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            drop = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.BARRICADE | RayMasks.STRUCTURE, Player.UnturnedPlayer);
        if (info.transform == null)
        {
            drop = null;
            return false;
        }

        BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        if (barricade != null)
        {
            drop = new BuildableBarricade(barricade);
            return true;
        }

        StructureDrop? structure = StructureManager.FindStructureByRootTransform(info.transform);
        if (structure != null)
        {
            drop = new BuildableStructure(structure);
            return true;
        }

        drop = null;
        return false;
    }

    /// <summary>
    /// Get the <see cref="InteractableVehicle"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetVehicleTarget([MaybeNullWhen(false)] out InteractableVehicle vehicle, float distance = 4f, bool tryCallersVehicleFirst = true, bool allowDead = false)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            vehicle = null;
            return false;
        }

        if (tryCallersVehicleFirst)
        {
            vehicle = Player.UnturnedPlayer.movement.getVehicle();
            if (vehicle != null && (allowDead || !vehicle.isDead))
                return true;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.VEHICLE, Player.UnturnedPlayer);
        if (info.transform == null)
        {
            vehicle = null;
            return false;
        }

        vehicle = info.vehicle;
        return vehicle != null && (allowDead || !vehicle.isDead);
    }

    private static readonly RaycastHit[] PlayerHitBuffer = new RaycastHit[16];

    /// <summary>
    /// Get the <see cref="WarfarePlayer"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public bool TryGetPlayerTarget([MaybeNullWhen(false)] out WarfarePlayer player, float distance = 4f)
    {
        GameThread.AssertCurrent();

        if (Player is null || !Player.IsOnline)
        {
            player = null;
            return false;
        }

        Transform aim = Player.UnturnedPlayer.look.aim;

        int hits = Physics.SphereCastNonAlloc(new Ray(aim.position, aim.forward), radius: 1.1f, PlayerHitBuffer, distance, RayMasks.ENEMY, QueryTriggerInteraction.Ignore);

        Player? playerHit = null;
        for (int i = 0; i < hits; ++i)
        {
            ref RaycastHit hit = ref PlayerHitBuffer[i];

            Player pl = DamageTool.getPlayer(hit.transform);
            if (pl is null || pl.life.isDead || Player.Equals(pl))
                continue;

            if (playerHit is not null)
            {
                Vector3 existingVector = Vector3.Normalize((playerHit.transform.position - Player.Position) with { z = 0 });
                Vector3 newVector = Vector3.Normalize((pl.transform.position - Player.Position) with { z = 0 });

                Vector3 forwardVector = Vector3.Normalize(aim.forward with { z = 0 });
                if (Math.Abs(Vector3.Dot(forwardVector, existingVector)) > Math.Abs(Vector3.Dot(forwardVector, newVector)))
                {
                    // player is closer to looking at the currently selected player than the old one
                    continue;
                }
            }
            playerHit = pl;
        }

        Array.Clear(PlayerHitBuffer, 0, PlayerHitBuffer.Length);

        if (playerHit is not null)
        {
            player = _playerService.GetOnlinePlayerOrNull(playerHit);
            return player != null;
        }

        player = null;
        return false;
    }

    /// <summary>
    /// Add an entry to the <see cref="ActionLog"/>.
    /// </summary>
    public void LogAction([ValueProvider("Uncreated.Warfare.Events.Logging.ActionLogTypes")] ActionLogType type, string? data = null)
    {
        ActionLogEntry action = new ActionLogEntry(type, data ?? string.Empty, CallerId.m_SteamID);
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Log Action: {action}"));
#endif
        _actionLoggerService?.AddAction(in action);
    }

    /// <summary>
    /// Check if <see cref="Caller"/> has <paramref name="permission"/>. Always returns <see langword="true"/> when ran with console.
    /// </summary>
    public ValueTask<bool> HasPermission(PermissionLeaf permission, CancellationToken token = default)
    {
        if (Caller.IsSuperUser)
            return new ValueTask<bool>(true);

        return _permissionsStore.HasPermissionAsync(Caller, permission, token);
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have <paramref name="permission"/>.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissions(PermissionLeaf permission, CancellationToken token = default)
    {
        if (Caller.IsSuperUser)
            return default;

        ValueTask<bool> vt = HasPermission(permission, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, permission));
        }

        if (!vt.Result)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Missing permission: {permission}"));
#endif
            throw SendNoPermission(permission);
        }
            
        return default;

        async Task Core(ValueTask<bool> vt, PermissionLeaf permission)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission: {permission}"));
#endif
                throw SendNoPermission(permission);
            }
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsOr(PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token = default)
    {
        if (Caller.IsSuperUser)
            return default;

        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, true, permission1, permission2, token));
        }

        if (vt.Result)
            return default;

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, false, permission1, permission2, token));
        }

        if (vt.Result)
            return default;

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Missing permission: {permission1} or {permission2}"));
#endif
        throw SendNoPermission(permission1);

        async Task Core(ValueTask<bool> vt, bool isFirst, PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (hasPerm)
                return;
            
            if (!isFirst)
                return;

            hasPerm = await HasPermission(permission2, token);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission: {permission1} or {permission2}"));
#endif
                throw SendNoPermission(permission1);
            }
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsOr(PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token = default)
    {
        if (Caller.IsSuperUser)
            return default;

        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 0, permission1, permission2, permission3, token));
        }

        if (vt.Result)
            return default;

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 1, permission1, permission2, permission3, token));
        }

        if (vt.Result)
            return default;

        vt = HasPermission(permission3, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 2, permission1, permission2, permission3, token));
        }

        if (vt.Result)
            return default;

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Missing permission: {permission1} or {permission2} or {permission3}"));
#endif
        throw SendNoPermission(permission1);

        async Task Core(ValueTask<bool> vt, int ctDone, PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (hasPerm)
                return;

            if (ctDone == 2)
                return;

            hasPerm = await HasPermission(ctDone == 1 ? permission3 : permission2, token);
            if (hasPerm)
                return;

            if (ctDone == 1)
                return;

            hasPerm = await HasPermission(permission3, token);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission: {permission1} or {permission2} or {permission3}"));
#endif
                throw SendNoPermission(permission1);
            }
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <remarks>If <paramref name="permissions"/> is empty, nothing will happen.</remarks>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsOr(params PermissionLeaf[] permissions) => AssertPermissionsOr(default, permissions);

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <remarks>If <paramref name="permissions"/> is empty, nothing will happen.</remarks>
    /// <exception cref="CommandContext"/>
    public async ValueTask AssertPermissionsOr(CancellationToken token, params PermissionLeaf[] permissions)
    {
        if (Caller.IsSuperUser || permissions.Length == 0)
            return;

        for (int i = 0; i < permissions.Length; i++)
        {
            if (await HasPermission(permissions[i], token))
                return;
        }

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Missing one of permissions: {string.Join(", ", permissions)}"));
#endif
        throw SendNoPermission(permissions[0]);
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsAnd(PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token = default)
    {
        if (Caller.IsSuperUser)
            return default;

        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, true, permission1, permission2, token));
        }

        if (!vt.Result)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Missing permission {permission1} of: {permission1} and {permission2}"));
#endif
            throw SendNoPermission(permission1);
        }

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, false, permission1, permission2, token));
        }

        if (!vt.Result)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Missing permission {permission2} of: {permission1} and {permission2}"));
#endif
            throw SendNoPermission(permission2);
        }

        return default;

        async Task Core(ValueTask<bool> vt, bool isFirst, PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission {permission1} of: {permission1} and {permission2}"));
#endif
                throw SendNoPermission(permission1);
            }

            if (!isFirst)
                return;

            hasPerm = await HasPermission(permission2, token);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission {permission2} of: {permission1} and {permission2}"));
#endif
                throw SendNoPermission(permission2);
            }
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsAnd(PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token = default)
    {
        if (Caller.IsSuperUser)
            return default;

        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 0, permission1, permission2, permission3, token));
        }

        if (!vt.Result)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Missing permission {permission1} of: {permission1}, {permission2}, {permission3}"));
#endif
            throw SendNoPermission(permission1);
        }

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 1, permission1, permission2, permission3, token));
        }

        if (!vt.Result)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Missing permission {permission2} of: {permission1}, {permission2}, {permission3}"));
#endif
            throw SendNoPermission(permission2);
        }

        vt = HasPermission(permission3, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 2, permission1, permission2, permission3, token));
        }

        if (!vt.Result)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Missing permission {permission3} of: {permission1}, {permission2}, {permission3}"));
#endif
            throw SendNoPermission(permission3);
        }

        return default;

        async Task Core(ValueTask<bool> vt, int ctDone, PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission {permission1} of: {permission1}, {permission2}, {permission3}"));
#endif
                throw SendNoPermission(permission1);
            }

            if (ctDone == 2)
                return;

            hasPerm = await HasPermission(ctDone == 1 ? permission3 : permission2, token);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission {permission2} of: {permission1}, {permission2}, {permission3}"));
#endif
                throw SendNoPermission(permission2);
            }

            if (ctDone == 1)
                return;

            hasPerm = await HasPermission(permission3, token);
            if (!hasPerm)
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission {permission3} of: {permission1}, {permission2}, {permission3}"));
#endif
                throw SendNoPermission(permission3);
            }
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsAnd(params PermissionLeaf[] permissions) => AssertPermissionsAnd(default, permissions);

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public async ValueTask AssertPermissionsAnd(CancellationToken token, params PermissionLeaf[] permissions)
    {
        if (Caller.IsSuperUser)
            return;

        for (int i = 0; i < permissions.Length; i++)
        {
            if (!await HasPermission(permissions[i], token))
            {
#if TELEMETRY
                _activity?.AddEvent(new ActivityEvent($"Missing permission {permissions[i]} of: {string.Join(", ", permissions)}"));
#endif
                throw SendNoPermission(permissions[i]);
            }
        }
    }

    /// <summary>
    /// Throws an exception if the isolated cooldown was still active on <see cref="Player"/> when the command was first started.
    /// </summary>
    public void AssertCommandNotOnIsolatedCooldown()
    {
        if (!OnIsolatedCooldown || _cooldownManager == null)
            return;

        if (Command is ICompoundingCooldownCommand compounding)
        {
            TimeSpan duration = IsolatedCooldown.Duration * compounding.CompoundMultiplier;
            if (compounding.MaxCooldown > 0 && duration.TotalSeconds > compounding.MaxCooldown)
                duration = TimeSpan.FromSeconds(compounding.MaxCooldown);

            IsolatedCooldown = new Cooldown(IsolatedCooldown.StartTime, duration, IsolatedCooldown.Config, IsolatedCooldown.Data);
            _cooldownManager.StartCooldown(Player, IsolatedCooldown);
        }

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent("On isolated cooldown"));
#endif
        throw Reply(CommonTranslations.CommandCooldown, IsolatedCooldown, CommandInfo.CommandName);
    }

    /// <exception cref="CommandContext"/>
    public void AssertRanByPlayer()
    {
        if (Player == null || !Player.IsOnline)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent("Not ran by player"));
#endif
            throw SendPlayerOnlyError();
        }
    }

    /// <exception cref="CommandContext"/>
    public void AssertRanByTerminal()
    {
        if (!Caller.IsTerminal)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent("Not ran by terminal"));
#endif
            throw SendConsoleOnlyError();
        }
    }

    /// <exception cref="CommandContext"/>
    public void AssertRanBy<TUser>() where TUser : ICommandUser
    {
        if (Caller is not TUser)
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Not ran by {Accessor.Formatter.Format(typeof(TUser))}"));
#endif
            throw SendConsoleOnlyError();
        }
    }

    /// <exception cref="CommandContext"/>
    public void AssertArgs(int count, string usage)
    {
        if (!HasArgs(count))
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Not enough args: ({count}, \"{usage}\")"));
#endif
            throw SendCorrectUsage(usage);
        }
    }

    /// <exception cref="CommandContext"/>
    public void AssertArgsExact(int count, string usage)
    {
        if (!HasArgsExact(count))
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Not exactly {count} args: \"{usage}\""));
#endif
            throw SendCorrectUsage(usage);
        }
    }

    /// <exception cref="CommandContext"/>
    public void AssertArgs(int count)
    {
        if (!HasArgs(count))
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Not enough args: ({count})"));
#endif
            throw SendHelp();
        }
    }

    /// <exception cref="CommandContext"/>
    public void AssertArgsExact(int count)
    {
        if (!HasArgsExact(count))
        {
#if TELEMETRY
            _activity?.AddEvent(new ActivityEvent($"Not exactly {count} args"));
#endif
            throw SendHelp();
        }
    }

    /// <summary>
    /// Switch the current command context to run in /help.
    /// </summary>
    /// <exception cref="InvalidOperationException">Already in /help.</exception>
    public Exception SendHelp()
    {
        return SwitchToCommand<HelpCommand>();
    }

    /// <summary>
    /// Switch the current command context to run another command type by throwing on the result of this function.
    /// </summary>
    /// <exception cref="ArgumentException">Not a command type.</exception>
    /// <exception cref="InvalidOperationException">Already in that command.</exception>
    public Exception SwitchToCommand<TCommandType>() where TCommandType : ICommand
    {
        Type type = typeof(TCommandType);
        if (type.IsAbstract || !type.IsClass)
            throw new ArgumentException($"{Accessor.ExceptionFormatter.Format<TCommandType>()} is not a command type.");

        if (Command is TCommandType)
            throw new InvalidOperationException("Can not call SwitchToCommand from the same command type.");

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Switched to command {Accessor.Formatter.Format(type)}"));
#endif
        Responded = true;
        SwitchCommand = type;
        return this;
    }

    /// <summary>
    /// Switch the current command context to run another command type by throwing on the result of this function.
    /// </summary>
    /// <exception cref="ArgumentException">Not a command type.</exception>
    /// <exception cref="InvalidOperationException">Already in that command.</exception>
    public Exception SwitchToCommand(Type commandType)
    {
        if (!typeof(ICommand).IsAssignableFrom(commandType) || commandType.IsAbstract || !commandType.IsClass)
            throw new ArgumentException($"{Accessor.ExceptionFormatter.Format(commandType)} is not a command type.");

        if (commandType.IsInstanceOfType(Command))
            throw new InvalidOperationException("Can not call SwitchToCommand from the same command type.");

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Switched to command {Accessor.Formatter.Format(commandType)}"));
#endif
        Responded = true;
        SwitchCommand = commandType;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception SendNotImplemented() => Reply(CommonTranslations.NotImplemented);

    /// <remarks>Thread Safe</remarks>
    public Exception SendNotEnabled() => Reply(CommonTranslations.NotEnabled);

    /// <remarks>Thread Safe</remarks>
    public Exception SendGamemodeError() => Reply(CommonTranslations.GamemodeError);

    /// <remarks>Thread Safe</remarks>
    public Exception SendPlayerOnlyError() => Reply(CommonTranslations.PlayersOnly);

    /// <remarks>Thread Safe</remarks>
    public Exception SendConsoleOnlyError() => Reply(CommonTranslations.ConsoleOnly);

    /// <remarks>Thread Safe</remarks>
    public Exception SendUnknownError() => Reply(CommonTranslations.UnknownError);

    /// <remarks>Thread Safe</remarks>
    public Exception SendNoPermission() => Reply(CommonTranslations.NoPermissions);

    /// <remarks>Thread Safe</remarks>
    public Exception SendNoPermission(PermissionLeaf permission) => Reply(CommonTranslations.NoPermissionsSpecific, permission);

    /// <remarks>Thread Safe</remarks>
    public Exception SendPlayerNotFound() => Reply(CommonTranslations.PlayerNotFound);

    /// <remarks>Thread Safe</remarks>
    public Exception SendCorrectUsage(string usage) => Reply(CommonTranslations.CorrectUsage, usage);

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied \"{message}\""));
#endif
        _chatService.Send(Caller, message);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message, Color color)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied \"{message}\""));
#endif
        _chatService.Send(Caller, message, color);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message, ConsoleColor color)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied \"{message}\""));
#endif
        _chatService.Send(Caller, message, color);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message, string hex)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied \"{message}\""));
#endif
        HexStringHelper.TryParseColor32(hex, Culture, out Color32 color);
        _chatService.Send(Caller, message, color);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyUrl(string message, string url)
    {
        if (Player == null)
        {
            ReplyString(message + ": " + url);
            return this;
        }

#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with browser request to \"{url}\" with message \"{message}\"."));
#endif
        if (GameThread.IsCurrent)
        {
            Player.UnturnedPlayer.sendBrowserRequest(message, url);
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                Player.UnturnedPlayer.sendBrowserRequest(message, url);
            });
        }

        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplySteamProfileUrl(string message, CSteamID profileId)
    {
        return ReplyUrl(message, $"https://steamcommunity.com/profiles/{profileId.m_SteamID.ToString("D17", CultureInfo.InvariantCulture)}/");
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply(Translation translation)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with 0 args."));
#endif
        _chatService.Send(Caller, translation);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0>(Translation<T0> translation, T0 arg0)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 1[ {arg0} ]."));
#endif
        _chatService.Send(Caller, translation, arg0);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 2[ {arg0}, {arg1} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 3[ {arg0}, {arg1}, {arg2} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3>(Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 4[ {arg0}, {arg1}, {arg2}, {arg3} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4>(Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 5[ {arg0}, {arg1}, {arg2}, {arg3}, {arg4} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3, arg4);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5>(Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 6[ {arg0}, {arg1}, {arg2}, {arg3}, {arg4}, {arg5} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3, arg4, arg5);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6>(Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 7[ {arg0}, {arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6, T7>(Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 8[ {arg0}, {arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6}, {arg7} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 9[ {arg0}, {arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6}, {arg7}, {arg8} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
#if TELEMETRY
        _activity?.AddEvent(new ActivityEvent($"Replied with translation {translation.Collection}.{translation.Key} with args: 10[ {arg0}, {arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6}, {arg7}, {arg8}, {arg9} ]."));
#endif
        _chatService.Send(Caller, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        Responded = true;
        return this;
    }

    internal void CheckIsolatedCooldown()
    {
        if (Player != null
            && _cooldownManager != null
            && CommandInfo != null
            && _cooldownManager.HasCooldown(Player, KnownCooldowns.IsolatedCommand, out Cooldown cooldown, CommandInfo))
        {
            OnIsolatedCooldown = true;
            IsolatedCooldown = cooldown;
        }
        else
        {
            OnIsolatedCooldown = false;
            IsolatedCooldown = default;
        }
    }
}
