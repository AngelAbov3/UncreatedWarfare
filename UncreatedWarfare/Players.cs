﻿using SDG.Unturned;
using System;
using Uncreated.Encoding;
using Uncreated.Warfare;
using Uncreated.Warfare.Events;
using UnityEngine;

namespace Uncreated.Players;

public struct PlayerNames : IPlayer
{
    public static readonly PlayerNames Nil = new PlayerNames() { CharacterName = string.Empty, NickName = string.Empty, PlayerName = string.Empty, Steam64 = 0 };
    public static readonly PlayerNames Console = new PlayerNames() { CharacterName = "Console", NickName = "Console", PlayerName = "Console", Steam64 = 0 };
    public ulong Steam64;
    public string PlayerName;
    public string CharacterName;
    public string NickName;
    public bool WasFound;
    ulong IPlayer.Steam64 => Steam64;

    public PlayerNames(SteamPlayerID player)
    {
        this.PlayerName = player.playerName;
        this.CharacterName = player.characterName;
        this.NickName = player.nickName;
        this.Steam64 = player.steamID.m_SteamID;
        WasFound = true;
    }
    public PlayerNames(ulong player)
    {
        string ts = player.ToString();
        this.PlayerName = ts;
        this.CharacterName = ts;
        this.NickName = ts;
        this.Steam64 = player;
        WasFound = true;
    }
    public PlayerNames(SteamPlayer player)
    {
        this.PlayerName = player.playerID.playerName;
        this.CharacterName = player.playerID.characterName;
        this.NickName = player.playerID.nickName;
        this.Steam64 = player.playerID.steamID.m_SteamID;
        WasFound = true;
    }
    public PlayerNames(Player player)
    {
        this.PlayerName = player.channel.owner.playerID.playerName;
        this.CharacterName = player.channel.owner.playerID.characterName;
        this.NickName = player.channel.owner.playerID.nickName;
        this.Steam64 = player.channel.owner.playerID.steamID.m_SteamID;
        WasFound = true;
    }
    public static void Write(ByteWriter W, PlayerNames N)
    {
        W.Write(N.Steam64);
        W.Write(N.PlayerName);
        W.Write(N.CharacterName);
        W.Write(N.NickName);
    }
    public static PlayerNames Read(ByteReader R) =>
        new PlayerNames
        {
            Steam64 = R.ReadUInt64(),
            PlayerName = R.ReadString(),
            CharacterName = R.ReadString(),
            NickName = R.ReadString()
        };
    public override string ToString() => PlayerName;
    public static bool operator ==(PlayerNames left, PlayerNames right) => left.Steam64 == right.Steam64;
    public static bool operator !=(PlayerNames left, PlayerNames right) => left.Steam64 != right.Steam64;
    public override bool Equals(object obj) => obj is PlayerNames pn && this.Steam64 == pn.Steam64;
    public override int GetHashCode() => Steam64.GetHashCode();
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags) => new OfflinePlayer(in this).Translate(language, format, target, ref flags);
}
public struct ToastMessage
{
    public readonly EToastMessageSeverity Severity;
    private readonly long time;
    public readonly string Message1;
    public readonly string? Message2;
    public readonly string? Message3;
    public const float FULL_TOAST_TIME = 12f;
    public const float MINI_TOAST_TIME = 4f;
    public const float BIG_TOAST_TIME = 5.5f;
    public readonly uint InstanceID;
    private static uint _lastInstId;
    public static bool operator ==(ToastMessage left, ToastMessage right) => left.time == right.time && left.Message1 == right.Message1;
    public static bool operator !=(ToastMessage left, ToastMessage right) => left.time != right.time || left.Message1 != right.Message1;
    public override int GetHashCode() => time.GetHashCode() / 2 + Message1.GetHashCode() / 2;
    public override bool Equals(object obj) => obj is ToastMessage msg && msg.time == time && msg.Message1 == Message1;
    public ToastMessage(string message1, EToastMessageSeverity severity)
    {
        this.time = DateTime.Now.Ticks;
        this.Message1 = message1;
        this.Message2 = null;
        this.Message3 = null;
        this.Severity = severity;
        InstanceID = ++_lastInstId;
    }
    public ToastMessage(string message1, string message2, EToastMessageSeverity severity) : this(message1, severity)
    {
        this.Message2 = message2;
    }
    public ToastMessage(string message1, string message2, string message3, EToastMessageSeverity severity) : this(message1, message2, severity)
    {
        this.Message3 = message3;
    }
    public static void QueueMessage(UCPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.Player, message, priority);
    public static void QueueMessage(SteamPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.player, message, priority);
    public static void QueueMessage(Player player, ToastMessage message, bool priority = false)
    {
        if (F.TryGetPlayerData(player, out Warfare.Components.UCPlayerData c))
            c.QueueMessage(message, priority);
    }
}
public enum EToastMessageSeverity : byte
{
    INFO = 0,
    WARNING = 1,
    SEVERE = 2,
    MINI = 3,
    MEDIUM = 4,
    BIG = 5,
    PROGRESS = 6,
    TIP = 7
}
public sealed class UCPlayerEvents : IDisposable
{
    public UCPlayer Player { get; private set; }
    public UCPlayerEvents(UCPlayer player)
    {
        this.Player = player;
        Player.Player.inventory.onDropItemRequested += OnDropItemRequested;
    }
    public void Dispose()
    {
        if (Player.Player != null)
        {
            Player.Player.inventory.onDropItemRequested -= OnDropItemRequested;
        }

        Player = null!;
    }
    private void OnDropItemRequested(PlayerInventory inventory, Item item, ref bool shouldAllow) => EventDispatcher.InvokeOnDropItemRequested(Player ?? UCPlayer.FromPlayer(inventory.player)!, inventory, item, ref shouldAllow);
}
public sealed class UCPlayerKeys : IDisposable
{
    private static readonly int KEY_COUNT = 10 + ControlsSettings.NUM_PLUGIN_KEYS;

    private static readonly KeyDown?[] _downEvents = new KeyDown?[KEY_COUNT];
    private static readonly KeyUp?[] _upEvents = new KeyUp?[KEY_COUNT];
    private static readonly bool[] eventMask = new bool[KEY_COUNT];
    private static bool anySubs = false;

    public readonly UCPlayer Player;
    private bool[] lastKeys = new bool[KEY_COUNT];
    private float[] keyDownTimes = new float[KEY_COUNT];
    private bool first = true;
    private bool disposed;
    public UCPlayerKeys(UCPlayer player)
    {
        Player = player;
        if (Player.Player.input.keys.Length != KEY_COUNT)
            throw new InvalidOperationException("Nelson changed the amount of keys in PlayerInput!");
        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < KEY_COUNT; ++i)
            keyDownTimes[i] = time;
    }
    public bool IsKeyDown(PlayerKey key)
    {
        CheckKey(key);
        return !disposed && Player.Player.input.keys[(int)key];
    }
    public static void SubscribeKeyUp(KeyUp action, PlayerKey key)
    {
        if (action == null) return;
        anySubs = true;
        CheckKey(key);
        ref KeyUp? d = ref _upEvents[(int)key];
        eventMask[(int)key] = true;
        d += action;
    }
    public static void SubscribeKeyDown(KeyDown action, PlayerKey key)
    {
        if (action == null) return;
        anySubs = true;
        CheckKey(key);
        ref KeyDown? d = ref _downEvents[(int)key];
        eventMask[(int)key] = true;
        d += action;
    }
#nullable disable
    public static void UnsubscribeKeyUp(KeyUp action, PlayerKey key)
    {
        if (action == null) return;
        CheckKey(key);
        ref KeyUp d = ref _upEvents[(int)key];
        if (d != null)
            d -= action;
        CheckAnySubs();
    }
    public static void UnsubscribeKeyDown(KeyDown action, PlayerKey key)
    {
        if (action == null) return;
        CheckKey(key);
        ref KeyDown d = ref _downEvents[(int)key];
        if (d != null)
            d -= action;
        CheckAnySubs();
    }
#nullable restore
    private static void CheckKey(PlayerKey key)
    {
#pragma warning disable CS0618
        if (key == PlayerKey.Reserved || (int)key < 0 || (int)key >= KEY_COUNT)
            throw new ArgumentOutOfRangeException(nameof(key), key.ToString() + " doesn't match a valid key.");
#pragma warning restore CS0618
    }
    private static void CheckAnySubs()
    {
        anySubs = false;
        for (int i = 0; i < _upEvents.Length; ++i)
        {
            if (_upEvents[i] != null)
            {
                anySubs = true;
                eventMask[i] = true;
            }
            else
                eventMask[i] = false;
        }
        for (int i = 0; i < _downEvents.Length; ++i)
        {
            if (_downEvents[i] != null)
            {
                anySubs = true;
                eventMask[i] = true;
            }
        }
    }
    internal void Simulate()
    {
        if (disposed) return;
        bool[] keys = Player.Player.input.keys;
        if (first || !anySubs)
        {
            for (int i = 0; i < KEY_COUNT; ++i)
                this.lastKeys[i] = keys[i];
            first = false;
        }
        else
        {
            for (int i = 0; i < KEY_COUNT; ++i)
            {
                bool st = keys[i];
                if (eventMask[i])
                {
                    bool ost = this.lastKeys[i];
                    if (st == ost) continue;
                    if (st)
                    {
                        EventDispatcher.OnKeyDown(Player, (PlayerKey)i, _downEvents[i]);
                        keyDownTimes[i] = Time.realtimeSinceStartup;
                    }
                    else
                    {
                        EventDispatcher.OnKeyUp(Player, (PlayerKey)i, Time.realtimeSinceStartup - keyDownTimes[i], _upEvents[i]);
                    }
                }
                this.lastKeys[i] = st;
            }
        }
    }
    public void Dispose()
    {
        if (!disposed)
        {
            lastKeys = null!;
            keyDownTimes = null!;
            first = false;
            disposed = true;
        }
    }
}

public delegate void KeyDown(UCPlayer player, ref bool handled);
public delegate void KeyUp(UCPlayer player, float timeDown, ref bool handled);

public enum PlayerKey
{
    Jump = 0,
    Primary = 1,
    Secondary = 2,
    Crouch = 3,
    Prone = 4,
    Sprint = 5,
    LeanLeft = 6,
    LeanRight = 7,
    [Obsolete("This is not in use right now.")]
    Reserved = 8,
    SteadyAim = 9,
    PluginKey1 = 10,
    PluginKey2 = 11,
    PluginKey3 = 12,
    PluginKey4 = 13,
    PluginKey5 = 14
}