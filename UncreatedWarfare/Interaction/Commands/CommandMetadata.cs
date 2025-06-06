using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Interaction.Commands.Syntax;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;

/// <summary>
/// Information about the layout of a command from a configuration file.
/// </summary>
public class CommandMetadata : ICommandParameterDescriptor
{
    [JsonIgnore]
    private CommandMetadata? _parent;

#nullable disable
    /// <summary>
    /// Name of the parameter in proper-case format.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Values that can be entered instead of <see cref="Name"/>.
    /// </summary>
    public string[] Aliases { get; set; }

    /// <summary>
    /// Array of types after <see cref="Clean"/> is ran.
    /// </summary>
    /// <remarks>Verbatim parameter types will have the type of <see cref="VerbatimParameterType"/>.</remarks>
    public Type[] ResolvedTypes { get; internal set; }

    /// <summary>
    /// List of sub-parameters.
    /// </summary>
    public CommandMetadata[] Parameters { get; set; }

    /// <summary>
    /// List of all valid flags.
    /// </summary>
    public FlagMetadata[] Flags { get; set; }

#nullable restore

    /// <summary>
    /// Translatable description of the command or sub-command.
    /// </summary>
    public TranslationList? Description { get; set; }

    /// <summary>
    /// Single value that can be entered instead of <see cref="Name"/>.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Value type, or 'Verbatim' if the parameter name itself should be entered.
    /// </summary>
    /// <remarks>Types can be fully qualified type names, namespace names in Uncreated.Warfare or mscorlib, language type keywords, or 'Verbatim'.</remarks>
    public string? Type { get; set; }

    /// <summary>
    /// Value type, or 'Verbatim' if the parameter name itself should be entered.
    /// </summary>
    /// <remarks>Types can be fully qualified type names, namespace names in Uncreated.Warfare or mscorlib, language type keywords, or 'Verbatim'.</remarks>
    public string?[]? Types { get; set; }

    /// <summary>
    /// If the parameter is optional. All other parameters on this level must also be marked optional.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// If the parameter spans the rest of the command arguments, including spaces.
    /// </summary>
    public bool Remainder { get; set; }

    /// <summary>
    /// The permission needed to execute the command.
    /// </summary>
    public PermissionLeaf Permission { get; set; }

    /// <summary>
    /// Number of sub-parameters to chain into one 'parameter' including this one. Requires that all child parameters up to that amount have only one parameter.
    /// </summary>
    /// <remarks>Example: <c>/tp [x y z|location|player]</c> where <c>x.ChainDisplayAmount = 3</c>.</remarks>
    public int Chain { get; set; }

    /// <summary>
    /// Recursively clean this metadata and all it's parameters.
    /// </summary>
    /// <param name="commandType">The class of the parent command.</param>
    public void Clean(Type commandType)
    {
        if (_parent == null && Name == null)
        {
            Name = commandType.TryGetAttributeSafe(out CommandAttribute command) ? command.CommandName : commandType.Name;
        }

        if (Chain < 0)
            Chain = 0;

        // aliases
        List<string> tempAliases = new List<string>(Aliases?.Length ?? 1);
        if (Aliases != null)
        {
            foreach (string? alias in Aliases)
            {
                if (alias == null || tempAliases.FindIndex(a => a.Equals(alias, StringComparison.InvariantCultureIgnoreCase)) >= 0)
                    continue;

                tempAliases.Add(alias);
            }
        }
        if (Alias != null && tempAliases.FindIndex(a => a.Equals(Alias, StringComparison.InvariantCultureIgnoreCase)) < 0)
        {
            tempAliases.Add(Alias);
        }

        Aliases = tempAliases.ToArray();
        Alias = Aliases.Length == 1 ? Aliases[0] : null;

        // types
        if (_parent == null)
        {
            Types = [ CommandSyntaxFormatter.Verbatim ];
            Type = CommandSyntaxFormatter.Verbatim;
            ResolvedTypes = [ typeof(VerbatimParameterType) ];
        }
        else
        {
            List<Type> tempTypes = new List<Type>(Types?.Length ?? 1);

            Type? resolvedType;
            if (Types != null)
            {
                foreach (string? typeName in Types)
                {
                    resolvedType = ResolveType(typeName);
                    if (resolvedType != null && !tempTypes.Contains(resolvedType))
                        tempTypes.Add(resolvedType);
                }
            }

            resolvedType = ResolveType(Type);
            if (resolvedType != null && !tempTypes.Contains(resolvedType))
                tempTypes.Add(resolvedType);

            ResolvedTypes = tempTypes.ToArray();
            string[] typeNames = new string[ResolvedTypes.Length];
            for (int i = 0; i < typeNames.Length; ++i)
            {
                resolvedType = ResolvedTypes[i];
                typeNames[i] = resolvedType == typeof(VerbatimParameterType)
                    ? CommandSyntaxFormatter.Verbatim
                    : resolvedType.AssemblyQualifiedName!;
            }

            Types = typeNames;
            Type = typeNames.Length == 1 ? typeNames[0] : null;
        }

        // flags
        int nonNull = 0;
        if (Flags is not { Length: > 0 })
        {
            Flags = Array.Empty<FlagMetadata>();
        }
        else
        {
            for (int i = 0; i < Flags.Length; ++i)
            {
                if (Flags[i]?.Name == null)
                    continue;

                ++nonNull;
            }

            if (nonNull < Flags.Length)
            {
                FlagMetadata[] newFlags = new FlagMetadata[nonNull];
                nonNull = -1;
                for (int i = 0; i < Flags.Length; ++i)
                {
                    FlagMetadata flag = Flags[i];
                    if (flag?.Name == null)
                        continue;

                    newFlags[++nonNull] = flag;
                }

                Flags = newFlags;
            }
        }

        // parameters
        if (Parameters == null)
        {
            Parameters = Array.Empty<CommandMetadata>();
            return;
        }

        nonNull = 0;
        for (int i = 0; i < Parameters.Length; ++i)
        {
            CommandMetadata? meta = Parameters[i];
            if (meta?.Name == null)
                continue;

            ++nonNull;
            meta._parent = this;
        }

        if (nonNull < Parameters.Length)
        {
            CommandMetadata[] newParameters = new CommandMetadata[nonNull];
            nonNull = -1;
            for (int i = 0; i < Parameters.Length; ++i)
            {
                CommandMetadata? meta = Parameters[i];
                if (meta?.Name == null)
                    continue;

                newParameters[++nonNull] = meta;
            }

            Parameters = newParameters;
        }

        for (int i = 0; i < Parameters.Length; ++i)
        {
            Parameters[i].Clean(commandType);
        }
    }

    private static Type? ResolveType([NotNullWhen(true)] string? typeName)
    {
        if (typeName == null)
            return null;

        if (typeName.Equals(CommandSyntaxFormatter.Verbatim, StringComparison.OrdinalIgnoreCase))
            return typeof(VerbatimParameterType);

        if (typeName.StartsWith("Look/", StringComparison.OrdinalIgnoreCase))
        {
            Type? resolvedInner = ResolveType(typeName.Substring(5));
            if (resolvedInner != null)
                return typeof(LookAtInteractionParameterType<>).MakeGenericType(resolvedInner);
        }

        return ContextualTypeResolver.ResolveType(typeName);
    }

    internal static bool IsParameterMatchOrLookAtMatch(CommandMetadata paramMeta, string commandName, CommandMetadata? updateMetadata, [MaybeNullWhen(false)] out CommandMetadata actualMatch)
    {
        if (paramMeta.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase))
        {
            if (updateMetadata != null)
            {
                int index = paramMeta._parent == null ? -1 : Array.IndexOf(paramMeta._parent.Parameters, paramMeta);
                if (index != -1)
                {
                    if (updateMetadata.Name == null)
                        paramMeta._parent!.Parameters = CollectionUtility.RemoveFromArray(paramMeta._parent!.Parameters, index);
                    else
                        paramMeta._parent!.Parameters[index] = updateMetadata;
                }
            }
            actualMatch = paramMeta;
            return true;
        }

        actualMatch = null;
        if (paramMeta.Parameters.Length <= 0)
            return false;

        bool anyLooks = false;
        foreach (Type type in paramMeta.ResolvedTypes)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(LookAtInteractionParameterType<>))
            {
                anyLooks = true;
                break;
            }
        }

        if (!anyLooks)
        {
            return false;
        }
        
        for (int i = 0; i < paramMeta.Parameters.Length; ++i)
        {
            if (IsParameterMatchOrLookAtMatch(paramMeta.Parameters[i], commandName, updateMetadata, out actualMatch))
                return true;
        }

        return false;
    }
    internal static bool IsParameterMatchOrLookAtMatch(ICommandParameterDescriptor paramMeta, string commandName, [MaybeNullWhen(false)] out ICommandParameterDescriptor actualMatch, bool aliases)
    {
        if (paramMeta.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase))
        {
            actualMatch = paramMeta;
            return true;
        }

        if (aliases)
        {
            for (int i = 0; i < paramMeta.Aliases.Count; ++i)
            {
                if (paramMeta.Aliases[i].Equals(commandName, StringComparison.InvariantCultureIgnoreCase))
                {
                    actualMatch = paramMeta;
                    return true;
                }
            }
        }

        actualMatch = null;
        if (paramMeta.Parameters.Count <= 0)
            return false;

        bool anyLooks = false;
        foreach (Type type in paramMeta.Types)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(LookAtInteractionParameterType<>))
            {
                anyLooks = true;
                break;
            }
        }

        if (!anyLooks)
        {
            return false;
        }
        
        for (int i = 0; i < paramMeta.Parameters.Count; ++i)
        {
            if (IsParameterMatchOrLookAtMatch(paramMeta.Parameters[i], commandName, out actualMatch, aliases))
                return true;
        }

        return false;
    }
    ICommandParameterDescriptor? ICommandParameterDescriptor.Parent => _parent;
    IReadOnlyList<string> ICommandParameterDescriptor.Aliases => Aliases;
    IReadOnlyList<Type> ICommandParameterDescriptor.Types => ResolvedTypes;
    IReadOnlyList<ICommandParameterDescriptor> ICommandParameterDescriptor.Parameters => Parameters;
    IReadOnlyList<ICommandFlagDescriptor> ICommandParameterDescriptor.Flags => Flags;

    public class FlagMetadata : ICommandFlagDescriptor
    {
#nullable disable
        /// <summary>
        /// Flag name without the dash.
        /// </summary>
        /// <remarks>Example: -e would be "e".</remarks>
        public string Name { get; set; } = null!;

        /// <summary>
        /// General description of what the flag does.
        /// </summary>
        public TranslationList Description { get; set; }

        /// <summary>
        /// The permission needed to use the flag.
        /// </summary>
        public PermissionLeaf Permission { get; set; }
#nullable restore
    }
}

/// <summary>
/// Type representing the 'verbatim' parameter type.
/// </summary>
public static class VerbatimParameterType;

/// <summary>
/// Type representing the 'looking at <typeparamref name="TInteractionType"/>' parameter type.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public static class LookAtInteractionParameterType<TInteractionType>;