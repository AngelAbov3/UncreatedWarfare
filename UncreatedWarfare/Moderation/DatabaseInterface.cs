﻿using MySqlConnector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Commendation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Vehicles;
using Report = Uncreated.Warfare.Moderation.Reports.Report;

namespace Uncreated.Warfare.Moderation;
public abstract class DatabaseInterface
{
    public static readonly TimeSpan DefaultInvalidateDuration = TimeSpan.FromSeconds(3);
    private readonly Dictionary<ulong, string> _iconUrlCacheSmall = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheMedium = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheFull = new Dictionary<ulong, string>(128);
    public ModerationCache Cache { get; } = new ModerationCache(64);
    public bool TryGetAvatar(ulong steam64, AvatarSize size, out string avatar)
    {
        Dictionary<ulong, string> dict = size switch
        {
            AvatarSize.Full => _iconUrlCacheFull,
            AvatarSize.Medium => _iconUrlCacheMedium,
            _ => _iconUrlCacheSmall
        };
        return dict.TryGetValue(steam64, out avatar);
    }
    public void UpdateAvatar(ulong steam64, AvatarSize size, string value)
    {
        Dictionary<ulong, string> dict = size switch
        {
            AvatarSize.Full => _iconUrlCacheFull,
            AvatarSize.Medium => _iconUrlCacheMedium,
            _ => _iconUrlCacheSmall
        };
        dict[steam64] = value;
    }
    public abstract IWarfareSql Sql { get; }
    public Task VerifyTables(CancellationToken token = default) => Sql.VerifyTables(Schema, token);
    public async Task<PlayerNames> GetUsernames(ulong id, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded)
            return await F.GetPlayerOriginalNamesAsync(id, token).ConfigureAwait(false);

        return await Sql.GetUsernamesAsync(id, token).ConfigureAwait(false);
    }
    public async Task<T?> ReadOne<T>(PrimaryKey id, CancellationToken token = default) where T : ModerationEntry
    {
        string query = $"SELECT {SqlTypes.ColumnList(ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason)} " +
                       $"FROM `{TableEntries}` WHERE `{ColumnEntriesPrimaryKey}`=@0 LIMIT 1;";
        object[] pkArgs = { id.Key };
        ModerationEntry? entry = null;
        await Sql.QueryAsync(query, pkArgs, reader =>
        {
            entry = ReadEntry(reader, 0);
            if (entry != null)
                entry.Id = id;
            return true;
        }, token).ConfigureAwait(false);

        if (entry == null)
        {
            Cache.Remove(id.Key);
            return null;
        }

        await Fill(new ModerationEntry[] { entry }, token).ConfigureAwait(false);
        
        return entry as T;
    }
    public async Task<T[]> ReadAll<T>(ulong actor, ActorRelationType relation, DateTimeOffset? start = null, DateTimeOffset? end = null, string? orderBy = null, string? condition = null, object[]? conditionArgs = null, CancellationToken token = default) where T : ModerationEntry
        => (T[])await ReadAll(typeof(T), actor, relation, start, end, orderBy, condition, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, ulong actor, ActorRelationType relation, DateTimeOffset? start = null, DateTimeOffset? end = null, string? orderBy = null, string? condition = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        string query = $"SELECT {SqlTypes.ColumnList(ColumnEntriesPrimaryKey, ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason)} " +
                       $"FROM `{TableEntries}` WHERE";


        switch (relation)
        {
            default:
            case ActorRelationType.IsTarget:
                query += $" `{ColumnEntriesSteam64}`=@0;";
                break;
            case ActorRelationType.IsActor:
                query += $" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0) > 0";
                break;
            case ActorRelationType.IsAdminActor:
                query += $" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` != 0) > 0";
                break;
            case ActorRelationType.IsNonAdminActor:
                query += $" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` == 0) > 0";
                break;
        }
        
        ModerationEntryType[]? types = null;
        if (typeof(T) != typeof(ModerationEntry))
            ModerationReflection.TypeInheritance.TryGetValue(typeof(T), out types);
        object[] actorArgs;
        int offset;
        int xtraLength = (types == null ? 0 : types.Length) + (conditionArgs == null || condition == null ? 0 : conditionArgs.Length);
        if (start.HasValue && end.HasValue)
        {
            offset = 3;
            actorArgs = new object[3 + xtraLength];
            actorArgs[1] = start.Value.UtcDateTime;
            actorArgs[2] = end.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` >= @1 AND `{ColumnEntriesStartTimestamp}` <= @2";
        }
        else if (start.HasValue)
        {
            offset = 2;
            actorArgs = new object[2 + xtraLength];
            actorArgs[1] = start.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` >= @1";
        }
        else if (end.HasValue)
        {
            offset = 2;
            actorArgs = new object[2 + xtraLength];
            actorArgs[1] = end.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` <= @1";
        }
        else
        {
            offset = 1;
            actorArgs = new object[1 + xtraLength];
        }

        actorArgs[0] = actor;
        if (types is { Length: > 0 })
        {
            for (int i = 0; i < types.Length; ++i)
                actorArgs[offset + i] = types[i].ToString();
            query += $" AND `{ColumnEntriesType} IN (";
            if (types.Length == 1)
            {
                query += "@" + offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                StringBuilder sb = new StringBuilder(types.Length * 12);
                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                        sb.Append(',');
                    sb.Append('@').Append((i + offset).ToString(CultureInfo.InvariantCulture));
                }
            }

            query += ")";
        }

        if (types != null)
            offset += types.Length;
        if (conditionArgs != null && condition != null)
        {
            for (int i = 0; i < conditionArgs.Length; ++i)
            {
                actorArgs[offset + i] = conditionArgs[i];
                condition = Util.QuickFormat(condition, "@" + (offset + i).ToString(CultureInfo.InvariantCulture), i);
            }
        }

        if (condition != null)
            query += " AND " + condition;

        if (orderBy != null)
            query += " ORDER BY " + orderBy;

        query += ";";


        ArrayList entries = new ArrayList(4);
        await Sql.QueryAsync(query, actorArgs, reader =>
        {
            ModerationEntry? entry = ReadEntry(reader, 1);
            if (type.IsInstanceOfType(entry))
            {
                entry!.Id = reader.GetInt32(0);
                entries.Add(entry);
            }
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, token).ConfigureAwait(false);
        
        return rtn;
    }
    public async Task<T[]> ReadAll<T>(DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default) where T : ModerationEntry
        => (T[])await ReadAll(typeof(T), start, end, condition, orderBy, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        string query = $"SELECT {SqlTypes.ColumnList(ColumnEntriesPrimaryKey, ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason)} " +
                       $"FROM `{TableEntries}` WHERE";

        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry))
            ModerationReflection.TypeInheritance.TryGetValue(type, out types);
        bool and = false;
        object[] args;
        int offset;
        int xtraLength = (types == null ? 0 : types.Length) + (conditionArgs == null || condition == null ? 0 : conditionArgs.Length);
        if (start.HasValue && end.HasValue)
        {
            offset = 2;
            args = new object[2 + xtraLength];
            args[0] = start.Value.UtcDateTime;
            args[1] = end.Value.UtcDateTime;
            query += $" `{ColumnEntriesStartTimestamp}` >= @1 AND `{ColumnEntriesStartTimestamp}` <= @2";
            and = true;
        }
        else if (start.HasValue)
        {
            offset = 1;
            args = new object[1 + xtraLength];
            args[0] = start.Value.UtcDateTime;
            query += $" `{ColumnEntriesStartTimestamp}` >= @1";
            and = true;
        }
        else if (end.HasValue)
        {
            offset = 1;
            args = new object[1 + xtraLength];
            args[0] = end.Value.UtcDateTime;
            query += $" `{ColumnEntriesStartTimestamp}` <= @1";
            and = true;
        }
        else
        {
            offset = 0;
            args = new object[xtraLength];
        }
        
        if (types is { Length: > 0 })
        {
            for (int i = 0; i < types.Length; ++i)
                args[offset + i] = types[i].ToString();
            if (and)
                query += " AND";
            query += $" `{ColumnEntriesType} IN (";
            and = true;
            if (types.Length == 1)
            {
                query += "@" + offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                StringBuilder sb = new StringBuilder(types.Length * 12);
                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                        sb.Append(',');
                    sb.Append('@').Append((i + offset).ToString(CultureInfo.InvariantCulture));
                }
            }

            query += ")";
        }

        if (types != null)
            offset += types.Length;
        if (conditionArgs != null && condition != null)
        {
            for (int i = 0; i < conditionArgs.Length; ++i)
            {
                args[offset + i] = conditionArgs[i];
                condition = Util.QuickFormat(condition, "@" + (offset + i).ToString(CultureInfo.InvariantCulture), i);
            }
        }

        if (condition != null)
            query += (and ? " AND " : " ") + condition;

        if (orderBy != null)
            query += " ORDER BY " + orderBy;

        query += ";";


        ArrayList entries = new ArrayList(4);
        await Sql.QueryAsync(query, args, reader =>
        {
            ModerationEntry? entry = ReadEntry(reader, 1);
            if (type.IsInstanceOfType(entry))
            {
                entry!.Id = reader.GetInt32(0);
                entries.Add(entry);
            }
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, token).ConfigureAwait(false);
        
        return rtn;
    }
    private async Task Fill(ModerationEntry[] entries, CancellationToken token = default)
    {
        if (entries.Length == 0) return;
        string inArg;
        if (entries.Length == 1)
            inArg = " IN (" + entries[0].Id.Key.ToString(CultureInfo.InvariantCulture) + ")";
        else
        {
            StringBuilder sb = new StringBuilder("IN (", entries.Length * 4 + 6);
            for (int i = 0; i < entries.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(entries[i].Id.Key.ToString(CultureInfo.InvariantCulture));
            }

            inArg = sb.Append(')').ToString();
        }

        bool anyPunishments = false;
        bool anyDurationPunishments = false;
        for (int i = 0; i < entries.Length; ++i)
        {
            if (entries[i] is Punishment p)
            {
                anyPunishments = true;
                if (p is DurationPunishment)
                {
                    anyDurationPunishments = true;
                    break;
                }
            }
        }
        string query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnActorsId, ColumnActorsRole, ColumnActorsAsAdmin, ColumnActorsIndex)} " +
                $"FROM `{TableActors}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";

        List<(int, List<RelatedActor>)> actors = new List<(int, List<RelatedActor>)>();
        await Sql.QueryAsync(query, null, reader =>
        {
            int pk = reader.GetInt32(0);
            int index = reader.GetInt32(4);
            int arrIndex = actors.FindIndex(x => x.Item1 == pk);
            RelatedActor actor = ReadActor(reader, 1);
            List<RelatedActor> list;
            if (arrIndex == -1)
                actors.Add((pk, list = new List<RelatedActor>()));
            else list = actors[arrIndex].Item2;

            if (actors.Count <= index)
                list.Add(actor);
            else
                list.Insert(index, actor);
        }, token).ConfigureAwait(false);

        for (int i = 0; i < actors.Count; ++i)
        {
            int pk = actors[i].Item1;
            for (int j = 0; j < entries.Length; ++j)
            {
                if (entries[j].Id.Key == pk)
                {
                    entries[j].Actors = actors[i].Item2.AsArrayFast();
                    break;
                }
            }
        }

        query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnEvidenceId, ColumnEvidenceLink,
            ColumnEvidenceLocalSource, ColumnEvidenceMessage, ColumnEvidenceIsImage,
            ColumnEvidenceTimestamp, ColumnEvidenceActorId)} " +
                $"FROM `{TableEvidence}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";

        List<(int, List<Evidence>)> evidence = new List<(int, List<Evidence>)>();
        await Sql.QueryAsync(query, null, reader =>
        {
            int pk = reader.GetInt32(0);
            int arrIndex = evidence.FindIndex(x => x.Item1 == pk);
            Evidence piece = ReadEvidence(reader, 1);
            List<Evidence> list;
            if (arrIndex == -1)
                evidence.Add((pk, list = new List<Evidence>()));
            else list = evidence[arrIndex].Item2;
            list.Add(piece);
        }, token).ConfigureAwait(false);

        for (int i = 0; i < evidence.Count; ++i)
        {
            int pk = evidence[i].Item1;
            for (int j = 0; j < entries.Length; ++j)
            {
                if (entries[j].Id.Key == pk)
                {
                    entries[j].Evidence = evidence[i].Item2.AsArrayFast();
                    break;
                }
            }
        }

        if (anyDurationPunishments)
        {
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnDuationsDurationSeconds)} FROM `{TableDurationPunishments}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                long sec = reader.GetInt64(1);
                if (sec < 0)
                    sec = -1;
                for (int i = 0; i < entries.Length; ++i)
                {
                    if (entries[i].Id.Key == pk && entries[i] is DurationPunishment duration)
                    {
                        duration.Duration = TimeSpan.FromSeconds(sec);
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
        }

        if (anyPunishments)
        {
            List<(int, List<PrimaryKey>)> links = new List<(int, List<PrimaryKey>)>();

            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedAppealsAppeal)} FROM `{TableLinkedAppeals}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                int arrIndex = links.FindIndex(x => x.Item1 == pk);
                PrimaryKey val = reader.GetInt32(1);
                List<PrimaryKey> list;
                if (arrIndex == -1)
                    links.Add((pk, list = new List<PrimaryKey>()));
                else list = links[arrIndex].Item2;
                list.Add(val);
            }, token).ConfigureAwait(false);
            for (int i = 0; i < links.Count; ++i)
            {
                int pk = links[i].Item1;
                List<PrimaryKey> vals = links[i].Item2;
                for (int j = 0; j < entries.Length; ++j)
                {
                    if (entries[j].Id.Key == pk && entries[j] is Punishment p)
                    {
                        p.AppealKeys = vals.AsArrayFast();
                        break;
                    }
                }
                vals.Clear();
            }

            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedReportsReport)} FROM `{TableLinkedReports}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                int arrIndex = links.FindIndex(x => x.Item1 == pk);
                PrimaryKey val = reader.GetInt32(1);
                List<PrimaryKey> list;
                if (arrIndex == -1)
                    links.Add((pk, list = new List<PrimaryKey>()));
                else list = links[arrIndex].Item2;
                list.Add(val);
            }, token).ConfigureAwait(false);
            for (int i = 0; i < links.Count; ++i)
            {
                int pk = links[i].Item1;
                List<PrimaryKey> vals = links[i].Item2;
                for (int j = 0; j < entries.Length; ++j)
                {
                    if (entries[j].Id.Key == pk && entries[j] is Punishment p)
                    {
                        p.ReportKeys = vals.AsArrayFast();
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < entries.Length; ++i)
        {
            Cache.AddOrUpdate(entries[i]);
            await entries[i].FillDetail(this).ConfigureAwait(false);
        }
    }
    private static ModerationEntry? ReadEntry(MySqlDataReader reader, int offset)
    {
        ModerationEntryType? type = reader.ReadStringEnum<ModerationEntryType>(offset);
        Type? csType = type.HasValue ? ModerationReflection.GetType(type.Value) : null;
        if (csType == null)
        {
            Logging.LogWarning($"Invalid type while reading moderation entry: {reader.GetString(offset)}.");
            return null;
        }

        ModerationEntry entry = (ModerationEntry)Activator.CreateInstance(csType);
        entry.Player = reader.GetUInt64(1 + offset);
        entry.Message = reader.IsDBNull(2 + offset) ? null : reader.GetString(2 + offset);
        entry.IsLegacy = reader.GetBoolean(3 + offset);
        entry.StartedTimestamp = DateTime.SpecifyKind(reader.GetDateTime(4 + offset), DateTimeKind.Utc);
        entry.ResolvedTimestamp = reader.IsDBNull(5 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(5 + offset), DateTimeKind.Utc);
        entry.Reputation = reader.GetDouble(6 + offset);
        entry.ReputationApplied = reader.GetBoolean(7 + offset);
        entry.LegacyId = reader.IsDBNull(8 + offset) ? null : reader.GetUInt32(8 + offset);
        entry.RelevantLogsBegin = reader.IsDBNull(9 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(9 + offset), DateTimeKind.Utc);
        entry.RelevantLogsEnd = reader.IsDBNull(10 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(10 + offset), DateTimeKind.Utc);
        entry.Removed = !reader.IsDBNull(11 + offset) && reader.GetBoolean(11 + offset);
        entry.RemovedBy = reader.IsDBNull(12 + offset) ? null : Actors.GetActor(reader.GetUInt64(12 + offset));
        entry.RemovedTimestamp = reader.IsDBNull(13 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(13 + offset), DateTimeKind.Utc);
        entry.Message = reader.IsDBNull(14 + offset) ? null : reader.GetString(14 + offset);
        return entry;
    }
    private static Evidence ReadEvidence(MySqlDataReader reader, int offset)
    {
        return new Evidence(
            reader.GetInt32(offset),
            reader.GetString(1 + offset),
            reader.IsDBNull(3 + offset) ? null : reader.GetString(3 + offset),
            reader.IsDBNull(2 + offset) ? null : reader.GetString(2 + offset),
            reader.GetBoolean(4 + offset),
            Actors.GetActor(reader.GetUInt64(6 + offset)),
            DateTime.SpecifyKind(reader.GetDateTime(5 + offset), DateTimeKind.Utc));
    }
    private static RelatedActor ReadActor(MySqlDataReader reader, int offset)
    {
        return new RelatedActor(
            reader.GetString(1 + offset),
            reader.GetBoolean(2 + offset),
            Actors.GetActor(reader.GetUInt64(offset)));
    }
    public async Task AddOrUpdate(ModerationEntry entry, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        PrimaryKey pk = entry.Id;
        object[] objs = new object[pk.IsValid ? 16 : 15];
        objs[0] = (ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None).ToString();
        objs[1] = entry.Player;
        objs[2] = (object?)entry.Message ?? DBNull.Value;
        objs[3] = entry.IsLegacy;
        objs[4] = entry.StartedTimestamp.UtcDateTime;
        objs[5] = entry.ResolvedTimestamp.HasValue ? entry.ResolvedTimestamp.Value.UtcDateTime : DBNull.Value;
        objs[6] = entry.Reputation;
        objs[7] = entry.ReputationApplied;
        objs[8] = entry.LegacyId.HasValue ? entry.LegacyId.Value : DBNull.Value;
        objs[9] = entry.RelevantLogsBegin.HasValue ? entry.RelevantLogsBegin.Value.UtcDateTime : DBNull.Value;
        objs[10] = entry.RelevantLogsEnd.HasValue ? entry.RelevantLogsEnd.Value.UtcDateTime : DBNull.Value;
        objs[11] = entry.Removed;
        objs[12] = entry.RemovedBy == null ? DBNull.Value : entry.RemovedBy.Id;
        objs[13] = entry.RemovedTimestamp.HasValue ? entry.RemovedTimestamp.Value.UtcDateTime : DBNull.Value;
        objs[14] = (object?)entry.RemovedMessage ?? DBNull.Value;

        if (pk.IsValid)
            objs[15] = pk.Key;

        string query = F.BuildInitialInsertQuery(TableEntries, ColumnEntriesPrimaryKey, pk.IsValid, null, null,

            ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason);

        await Sql.QueryAsync(query, objs, reader =>
        {
            pk = reader.GetInt32(0);
        }, token).ConfigureAwait(false);

        if (pk.IsValid)
            entry.Id = pk;

        List<object> args = new List<object>(entry.EstimateColumnCount()) { pk.Key };

        StringBuilder builder = new StringBuilder(82);

        bool hasNewEvidence = entry.AppendWriteCall(builder, args);

        if (!hasNewEvidence)
        {
            await Sql.NonQueryAsync(builder.ToString(), args.ToArray(), token).ConfigureAwait(false);
        }
        else
        {
            await Sql.QueryAsync(builder.ToString(), args.ToArray(), reader =>
            {
                Evidence read = ReadEvidence(reader, 0);
                for (int i = 0; i < entry.Evidence.Length; ++i)
                {
                    ref Evidence existing = ref entry.Evidence[i];
                    if (existing.Id.IsValid)
                    {
                        if (read.Id.Key == existing.Id.Key)
                        {
                            existing = read;
                            return;
                        }
                    }
                    else if (string.Equals(existing.URL, read.URL, StringComparison.OrdinalIgnoreCase) && existing.Timestamp == read.Timestamp)
                    {
                        existing = read;
                    }
                }
            }, token).ConfigureAwait(false);
        }

        Cache.AddOrUpdate(entry);
    }

    public const string TableEntries = "moderation_entries";
    public const string TableActors = "moderation_actors";
    public const string TableEvidence = "moderation_evidence";
    public const string TableAssetBanFilters = "moderation_asset_ban_filters";
    public const string TableDurationPunishments = "moderation_durations";
    public const string TableLinkedAppeals = "moderation_linked_appeals";
    public const string TableLinkedReports = "moderation_linked_reports";
    public const string TableMutes = "moderation_mutes";
    public const string TableWarnings = "moderation_warnings";
    public const string TablePlayerReportAccepteds = "moderation_accepted_player_reports";
    public const string TableBugReportAccepteds = "moderation_accepted_bug_reports";

    public const string ColumnExternalPrimaryKey = "Entry";
    
    public const string ColumnEntriesPrimaryKey = "Id";
    public const string ColumnEntriesType = "Type";
    public const string ColumnEntriesSteam64 = "Steam64";
    public const string ColumnEntriesMessage = "Message";
    public const string ColumnEntriesIsLegacy = "IsLegacy";
    public const string ColumnEntriesStartTimestamp = "StartTimeUTC";
    public const string ColumnEntriesResolvedTimestamp = "ResolvedTimeUTC";
    public const string ColumnEntriesReputation = "Reputation";
    public const string ColumnEntriesReputationApplied = "ReputationApplied";
    public const string ColumnEntriesLegacyId = "LegacyId";
    public const string ColumnEntriesMessageId = "OffenseMessageId";
    public const string ColumnEntriesInvalidated = "Invalidated";
    public const string ColumnEntriesInvalidatedActor = "InvalidatedActor";
    public const string ColumnEntriesInvalidatedTimestamp = "InvalidatedTimestamp";
    public const string ColumnEntriesRelavantLogsStartTimestamp = "RelavantLogsStartTimeUTC";
    public const string ColumnEntriesRelavantLogsEndTimestamp = "RelavantLogsEndTimeUTC";
    public const string ColumnEntriesRemoved = "Removed";
    public const string ColumnEntriesRemovedBy = "RemovedBy";
    public const string ColumnEntriesRemovedTimestamp = "RemovedTimeUTC";
    public const string ColumnEntriesRemovedReason = "RemovedReason";

    public const string ColumnActorsId = "ActorId";
    public const string ColumnActorsRole = "ActorRole";
    public const string ColumnActorsAsAdmin = "ActorAsAdmin";
    public const string ColumnActorsIndex = "ActorIndex";

    public const string ColumnEvidenceId = "Id";
    public const string ColumnEvidenceLink = "EvidenceURL";
    public const string ColumnEvidenceLocalSource = "EvidenceLocalSource";
    public const string ColumnEvidenceMessage = "EvidenceMessage";
    public const string ColumnEvidenceIsImage = "EvidenceIsImage";
    public const string ColumnEvidenceActorId = "EvidenceActor";
    public const string ColumnEvidenceTimestamp = "EvidenceTimestampUTC";

    public const string ColumnAssetBanFiltersAsset = "AssetBanAsset";

    public const string ColumnDuationsDurationSeconds = "Duration";

    public const string ColumnLinkedAppealsAppeal = "LinkedAppeal";
    public const string ColumnLinkedReportsReport = "LinkedReport";

    public const string ColumnMutesType = "MuteType";

    public const string ColumnWarningsHasBeenDisplayed = "WarningHasBeenDisplayed";

    public const string ColumnPlayerReportAcceptedsReport = "AcceptedReport";

    public const string ColumnTableBugReportAcceptedsCommit = "AcceptedCommit";
    public const string ColumnTableBugReportAcceptedsIssue = "AcceptedIssue";

    public static Schema[] Schema =
    {
        new Schema(TableEntries, new Schema.Column[]
        {
            new Schema.Column(ColumnEntriesPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnEntriesType, SqlTypes.Enum(exclude: ModerationEntryType.None)),
            new Schema.Column(ColumnEntriesSteam64, SqlTypes.STEAM_64)
            {
                Indexed = true
            },
            new Schema.Column(ColumnEntriesMessage, SqlTypes.String(1024))
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesIsLegacy, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesLegacyId, SqlTypes.UINT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesStartTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnEntriesResolvedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesReputationApplied, SqlTypes.BOOLEAN)
            {
                Nullable = true,
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesReputation, SqlTypes.DOUBLE)
            {
                Default = "'0'"
            },
            new Schema.Column(ColumnEntriesRelavantLogsStartTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRelavantLogsEndTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesInvalidated, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesInvalidatedActor, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesInvalidatedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRemoved, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesRemovedBy, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRemovedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRemovedReason, SqlTypes.String(1024))
            {
                Nullable = true
            }
        }, true, typeof(ModerationEntry)),
        new Schema(TableActors, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnActorsRole, SqlTypes.STRING_255),
            new Schema.Column(ColumnActorsId, SqlTypes.STEAM_64),
            new Schema.Column(ColumnActorsAsAdmin, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnActorsIndex, SqlTypes.INT)
        }, true, typeof(RelatedActor)),
        new Schema(TableEvidence, new Schema.Column[]
        {
            new Schema.Column(ColumnEvidenceId, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnEvidenceLink, SqlTypes.String(512)),
            new Schema.Column(ColumnEvidenceLocalSource, SqlTypes.String(512))
            {
                Nullable = true
            },
            new Schema.Column(ColumnEvidenceIsImage, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnEvidenceTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnEvidenceActorId, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEvidenceMessage, SqlTypes.String(1024))
            {
                Nullable = true
            }

        }, false, typeof(Evidence)),
        new Schema(TableAssetBanFilters, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAssetBanFiltersAsset, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = VehicleBay.TABLE_MAIN,
                ForeignKeyColumn = VehicleBay.COLUMN_PK
            }
        }, false, typeof(VehicleData)),
        new Schema(TableDurationPunishments, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnDuationsDurationSeconds, SqlTypes.LONG)
        }, false, typeof(TimeSpan)),
        new Schema(TableLinkedAppeals, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnLinkedAppealsAppeal, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
        }, false, typeof(Appeal)),
        new Schema(TableLinkedReports, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnLinkedReportsReport, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
        }, false, typeof(Report)),
        new Schema(TableMutes, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnMutesType, SqlTypes.Enum(exclude: MuteType.None))
        }, false, typeof(Mute)),
        new Schema(TableWarnings, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnWarningsHasBeenDisplayed, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
        }, false, typeof(Warning)),
        new Schema(TablePlayerReportAccepteds, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnPlayerReportAcceptedsReport, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries,
                Nullable = true
            },
        }, false, typeof(PlayerReportAccepted)),
        new Schema(TableBugReportAccepteds, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnTableBugReportAcceptedsCommit, "char(7)")
            {
                Nullable = true
            },
            new Schema.Column(ColumnTableBugReportAcceptedsIssue, SqlTypes.INT)
            {
                Nullable = true
            }
        }, false, typeof(PlayerReportAccepted)),
    };
}

public enum ActorRelationType
{
    IsTarget,
    IsAdminActor,
    IsNonAdminActor,
    IsActor
}

internal class WarfareDatabaseInterface : DatabaseInterface
{
    public override IWarfareSql Sql => Data.AdminSql;
}