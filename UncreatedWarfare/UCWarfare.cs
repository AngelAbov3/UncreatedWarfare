﻿#define USE_DEBUGGER
using JetBrains.Annotations;
using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public delegate void VoidDelegate();
public class UCWarfare : MonoBehaviour
{
    public static readonly TimeSpan RestartTime = new TimeSpan(1, 00, 0); // 9:00 PM EST
    public static readonly Version Version = new Version(3, 0, 0, 0);
    private readonly SystemConfig _config = new SystemConfig();
    private readonly List<UCTask> _tasks = new List<UCTask>(16);
    public static UCWarfare I;
    internal static UCWarfareNexus Nexus;
    public Coroutine? StatsRoutine;
    public UCAnnouncer Announcer;
    internal DebugComponent Debugger;
    public event EventHandler? UCWarfareLoaded;
    public event EventHandler? UCWarfareUnloading;
    internal Projectiles.ProjectileSolver Solver;
    public HomebaseClientComponent? NetClient;
    public bool CoroutineTiming = false;
    private DateTime _nextRestartTime;
    internal volatile bool ProcessTasks = true;
    private Task? _earlyLoadTask;
    private readonly CancellationTokenSource _unloadCancellationTokenSource = new CancellationTokenSource();
    public readonly SemaphoreSlim PlayerJoinLock = new SemaphoreSlim(0, 1);
    public bool FullyLoaded { get; private set; }
    public static CancellationToken UnloadCancel => IsLoaded ? I._unloadCancellationTokenSource.Token : CancellationToken.None;
    public static int Season => Version.Major;
    public static bool IsLoaded => I is not null;
    public static SystemConfigData Config => I is null ? throw new SingletonUnloadedException(typeof(UCWarfare)) : I._config.Data;
    public static bool CanUseNetCall => IsLoaded && Config.TCPSettings.EnableTCPServer && I.NetClient != null && I.NetClient.IsActive;
    [UsedImplicitly]
    private void Awake()
    {
        if (I != null) throw new SingletonLoadException(SingletonLoadType.Load, null, new Exception("Uncreated Warfare is already loaded."));
        I = this;
        FullyLoaded = false;
    }
    [UsedImplicitly]
    private void Start() => _earlyLoadTask = Task.Run( async () =>
    {
        try
        {
            await ToUpdate();
            await EarlyLoad(UnloadCancel).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            L.LogError("Error in early load!");
            L.LogError(ex);
            Provider.shutdown();
        }
    });
    private async Task EarlyLoad(CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Started loading - Uncreated Warfare version " + Version + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
              "Please stop using this plugin now.", ConsoleColor.Green);
        /* INITIALIZE UNCREATED NETWORKING */
        Logging.OnLogInfo += L.NetLogInfo;
        Logging.OnLogWarning += L.NetLogWarning;
        Logging.OnLogError += L.NetLogError;
        Logging.OnLogException += L.NetLogException;
        Logging.ExecuteOnMainThread = RunOnMainThread;
        NetFactory.Reflect(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

        L.Log("Registering Commands: ", ConsoleColor.Magenta);

        TeamManager.SetupConfig();

        OffenseManager.Init();

        CommandHandler.LoadCommands();

        DateTime loadTime = DateTime.Now;
        if (loadTime.TimeOfDay > RestartTime - TimeSpan.FromHours(2)) // don't restart if the restart would be in less than 2 hours
            _nextRestartTime = loadTime.Date + RestartTime + TimeSpan.FromDays(1);
        else
            _nextRestartTime = loadTime.Date + RestartTime;
        L.Log("Restart scheduled at " + _nextRestartTime.ToString("g"), ConsoleColor.Magenta);
        float seconds = (float)(_nextRestartTime - DateTime.Now).TotalSeconds;

        StartCoroutine(RestartIn(seconds));

        new PermissionSaver();
        await Data.LoadSQL(token).ConfigureAwait(false);
        await ItemIconProvider.DownloadConfig(token).ConfigureAwait(false);
        await TeamManager.ReloadFactions(token).ConfigureAwait(false);
        await ToUpdate(token);

        /* LOAD LOCALIZATION ASSETS */
        L.Log("Loading Localization and Color Data...", ConsoleColor.Magenta);
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Deaths.Localization.Reload();
        Data.Languages = JSONMethods.LoadLanguagePreferences();
        Data.LanguageAliases = JSONMethods.LoadLangAliases();
        Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
        Translation.ReadTranslations();

        CommandWindow.shouldLogDeaths = false;

        /* PATCHES */
        L.Log("Patching methods...", ConsoleColor.Magenta);
        try
        {
            Patches.DoPatching();
            LoadingQueueBlockerPatches.Patch();
        }
        catch (Exception ex)
        {
            L.LogError("Patching Error, perhaps Nelson changed something:");
            L.LogError(ex);
        }

        UCInventoryManager.OnLoad();

        if (Config.EnableSync)
            gameObject.AddComponent<ConfigSync>();
        gameObject.AddComponent<ActionLog>();
        Debugger = gameObject.AddComponent<DebugComponent>();
        Data.Singletons = gameObject.AddComponent<SingletonManager>();

        if (Config.EnableSync)
            ConfigSync.Reflect();

        Data.RegisterInitialSyncs();

        InitNetClient();

        if (!Config.DisableDailyQuests)
            Quests.DailyQuests.EarlyLoad();

        ActionLog.Add(ActionLogType.ServerStartup, $"Name: {Provider.serverName}, Map: {Provider.map}, Max players: {Provider.maxPlayers.ToString(Data.AdminLocale)}");
    }
    internal void InitNetClient()
    {
        if (NetClient != null)
        {
            Destroy(NetClient);
            NetClient = null;
        }
        if (Config.TCPSettings.EnableTCPServer)
        {
            L.Log("Attempting connection with Homebase...", ConsoleColor.Magenta);
            NetClient = gameObject.AddComponent<HomebaseClientComponent>();
            NetClient.OnClientVerified += Data.OnClientConnected;
            NetClient.OnClientDisconnected += Data.OnClientDisconnected;
            NetClient.OnSentMessage += Data.OnClientSentMessage;
            NetClient.OnReceivedMessage += Data.OnClientReceivedMessage;
            NetClient.ModifyVerifyPacketCallback += OnVerifyPacketMade;
            NetClient.Init(Config.TCPSettings.TCPServerIP, Config.TCPSettings.TCPServerPort, Config.TCPSettings.TCPServerIdentity);
        }
    }
    private void OnVerifyPacketMade(ref VerifyPacket packet)
    {
        packet = new VerifyPacket(packet.Identity, packet.SecretKey, packet.ApiVersion, packet.TimezoneOffset, Config.Currency, Config.RegionKey, Version);
    }
    public async Task LoadAsync(CancellationToken token)
    {
        if (_earlyLoadTask != null && !_earlyLoadTask.IsCompleted)
        {
            await _earlyLoadTask.ConfigureAwait(false);
            await ToUpdate(token);
            _earlyLoadTask = null;
        }
        else await ToUpdate(token);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EventDispatcher.SubscribeToAll();

        Zone.OnLevelLoaded();

        try
        {
            /* DATA CONSTRUCTION */
            await Data.LoadVariables(token);
        }
        catch (Exception ex)
        {
            L.LogError("Startup error");
            L.LogError(ex);
            throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
        }
        await ToUpdate(token);

        /* START STATS COROUTINE */
        StatsRoutine = StartCoroutine(StatsCoroutine.StatsRoutine());

        L.Log("Subscribing to events...", ConsoleColor.Magenta);
        SubscribeToEvents();

        F.CheckDir(Data.Paths.FlagStorage, out _, true);
        F.CheckDir(Data.Paths.StructureStorage, out _, true);
        F.CheckDir(Data.Paths.VehicleStorage, out _, true);
        ZonePlayerComponent.UIInit();

        Solver = gameObject.AddComponent<Projectiles.ProjectileSolver>();

        Announcer = await Data.Singletons.LoadSingletonAsync<UCAnnouncer>(token: token);
        await ToUpdate(token);

        Data.ExtraPoints = JSONMethods.LoadExtraPoints();
        //L.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);
        if (Data.Gamemode != null)
        {
            await Data.Gamemode.OnLevelReady(token).ConfigureAwait(false);
            await ToUpdate(token);
        }

#if DEBUG
        if (Config.Debug && File.Exists(@"C:\orb.wav"))
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\orb.wav");
            player.Load();
            player.Play();
        }
#endif

        Debugger.Reset();

        UCPlayerData.ReloadToastIDs();

        /* BASIC CONFIGS */
        Provider.modeConfigData.Players.Lose_Items_PvP = 0;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Barricades.Decay_Time = 0;
        Provider.modeConfigData.Structures.Decay_Time = 0;

        if (!Level.info.configData.Has_Global_Electricity)
        {
            L.LogWarning("Level does not have global electricity enabled, electrical grid effects will not work!");
            Data.UseElectricalGrid = false;
        }

        UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
        FullyLoaded = true;
        PlayerJoinLock.Release();
    }
    private IEnumerator<WaitForSecondsRealtime> RestartIn(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        ShutdownCommand.ShutdownAfterGameDaily();
    }
    private void SubscribeToEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Subscribe();
        StatsManager.LoadEvents();

        GameUpdateMonitor.OnGameUpdateDetected += EventFunctions.OnGameUpdateDetected;
        EventDispatcher.PlayerJoined += EventFunctions.OnPostPlayerConnected;
        EventDispatcher.PlayerLeaving += EventFunctions.OnPlayerDisconnected;
        Provider.onCheckValidWithExplanation += EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
        ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
        BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
        UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
        UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
        PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
        Provider.onLoginSpawning += EventFunctions.OnCalculateSpawnDuringJoin;
        BarricadeManager.onBarricadeSpawned += EventFunctions.OnBarricadePlaced;
        StructureManager.onStructureSpawned += EventFunctions.OnStructurePlaced;
        Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
        Patches.OnPlayerSetsCosmetics_Global += EventFunctions.StopCosmeticsSetStateEvent;
        Patches.OnBatterySteal_Global += EventFunctions.BatteryStolen;
        Patches.OnPlayerTriedStoreItem_Global += EventFunctions.OnTryStoreItem;
        Patches.OnPlayerGesture_Global += EventFunctions.OnPlayerGestureRequested;
        Patches.OnPlayerMarker_Global += EventFunctions.OnPlayerMarkedPosOnMap;
        DamageTool.damagePlayerRequested += EventFunctions.OnPlayerDamageRequested;
        BarricadeManager.onTransformRequested += EventFunctions.BarricadeMovedInWorkzone;
        BarricadeManager.onDamageBarricadeRequested += EventFunctions.OnBarricadeDamaged;
        StructureManager.onTransformRequested += EventFunctions.StructureMovedInWorkzone;
        StructureManager.onDamageStructureRequested += EventFunctions.OnStructureDamaged;
        BarricadeManager.onOpenStorageRequested += EventFunctions.OnEnterStorage;
        EventDispatcher.EnterVehicle += EventFunctions.OnEnterVehicle;
        EventDispatcher.VehicleSwapSeat += EventFunctions.OnVehicleSwapSeat;
        EventDispatcher.ExitVehicle += EventFunctions.OnPlayerLeavesVehicle;
        EventDispatcher.LandmineExploding += EventFunctions.OnLandmineExploding;
        EventDispatcher.ItemDropRequested += EventFunctions.OnItemDropRequested;
        EventDispatcher.CraftRequested += EventFunctions.OnCraftRequested;
        VehicleManager.onDamageVehicleRequested += EventFunctions.OnPreVehicleDamage;
        ItemManager.onServerSpawningItemDrop += EventFunctions.OnDropItemFinal;
        UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
        UseableConsumeable.onConsumePerformed += EventFunctions.OnConsume;
        EventDispatcher.BarricadeDestroyed += EventFunctions.OnBarricadeDestroyed;
        EventDispatcher.StructureDestroyed += EventFunctions.OnStructureDestroyed;
        PlayerVoice.onRelayVoice += EventFunctions.OnRelayVoice2;
    }
    private void UnsubscribeFromEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Unsubscribe();
        EventDispatcher.UnsubscribeFromAll();

        GameUpdateMonitor.OnGameUpdateDetected -= EventFunctions.OnGameUpdateDetected;
        ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
        EventDispatcher.PlayerJoined -= EventFunctions.OnPostPlayerConnected;
        EventDispatcher.PlayerLeaving -= EventFunctions.OnPlayerDisconnected;
        Provider.onCheckValidWithExplanation -= EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
        BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
        UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
        UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
        PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
        Provider.onLoginSpawning -= EventFunctions.OnCalculateSpawnDuringJoin;
        BarricadeManager.onBarricadeSpawned -= EventFunctions.OnBarricadePlaced;
        StructureManager.onStructureSpawned -= EventFunctions.OnStructurePlaced;
        Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
        Patches.OnPlayerSetsCosmetics_Global -= EventFunctions.StopCosmeticsSetStateEvent;
        Patches.OnBatterySteal_Global -= EventFunctions.BatteryStolen;
        Patches.OnPlayerTriedStoreItem_Global -= EventFunctions.OnTryStoreItem;
        Patches.OnPlayerGesture_Global -= EventFunctions.OnPlayerGestureRequested;
        Patches.OnPlayerMarker_Global -= EventFunctions.OnPlayerMarkedPosOnMap;
        DamageTool.damagePlayerRequested -= EventFunctions.OnPlayerDamageRequested;
        BarricadeManager.onTransformRequested -= EventFunctions.BarricadeMovedInWorkzone;
        BarricadeManager.onDamageBarricadeRequested -= EventFunctions.OnBarricadeDamaged;
        StructureManager.onTransformRequested -= EventFunctions.StructureMovedInWorkzone;
        BarricadeManager.onOpenStorageRequested -= EventFunctions.OnEnterStorage;
        StructureManager.onDamageStructureRequested -= EventFunctions.OnStructureDamaged;
        EventDispatcher.ItemDropRequested -= EventFunctions.OnItemDropRequested;
        EventDispatcher.LandmineExploding -= EventFunctions.OnLandmineExploding;
        EventDispatcher.EnterVehicle -= EventFunctions.OnEnterVehicle;
        EventDispatcher.VehicleSwapSeat -= EventFunctions.OnVehicleSwapSeat;
        EventDispatcher.ExitVehicle -= EventFunctions.OnPlayerLeavesVehicle;
        VehicleManager.onDamageVehicleRequested -= EventFunctions.OnPreVehicleDamage;
        ItemManager.onServerSpawningItemDrop -= EventFunctions.OnDropItemFinal;
        UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
        UseableConsumeable.onConsumePerformed -= EventFunctions.OnConsume;
        EventDispatcher.BarricadeDestroyed -= EventFunctions.OnBarricadeDestroyed;
        EventDispatcher.StructureDestroyed -= EventFunctions.OnStructureDestroyed;
        PlayerVoice.onRelayVoice -= EventFunctions.OnRelayVoice2;
        StatsManager.UnloadEvents();
    }
    internal void UpdateLangs(UCPlayer player, bool uiOnly)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!uiOnly)
            player.OnLanguageChanged();

        EventDispatcher.InvokeUIRefreshRequest(player);
        if (!uiOnly) Signs.UpdateAllSigns(player);

        if (Data.Gamemode != null)
        {
            if (!uiOnly)
                Data.Gamemode.InvokeLanguageChanged(player);
        }

        Data.UpdateAllUI(player);
    }

    private static Queue<MainThreadTask.MainThreadResult>? _threadActionRequests;
    private static Queue<LevelLoadTask.LevelLoadResult>? _levelLoadRequests;
    internal static Queue<MainThreadTask.MainThreadResult> ThreadActionRequests => _threadActionRequests ??= new Queue<MainThreadTask.MainThreadResult>(4);
    internal static Queue<LevelLoadTask.LevelLoadResult> LevelLoadRequests => _levelLoadRequests ??= new Queue<LevelLoadTask.LevelLoadResult>(4);
    public static MainThreadTask ToUpdate(CancellationToken token = default) => IsMainThread ? MainThreadTask.CompletedNoSkip : new MainThreadTask(false, token);
    public static MainThreadTask SkipFrame(CancellationToken token = default) => IsMainThread ? MainThreadTask.CompletedSkip : new MainThreadTask(true, token);
    public static LevelLoadTask ToLevelLoad(CancellationToken token = default) => new LevelLoadTask(token);

    // 'fire and forget' functions that will report errors once the task completes.

    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2, T3>(Func<T1, T2, T3, CancellationToken, Task> task, T1 arg1, T2 arg2, T3 arg3, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2, arg3, token);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2, T3>(Func<T1, T2, T3, Task> task, T1 arg1, T2 arg2, T3 arg3, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2, arg3);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2>(Func<T1, T2, CancellationToken, Task> task, T1 arg1, T2 arg2, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2, token);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2>(Func<T1, T2, Task> task, T1 arg1, T2 arg2, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T>(Func<T, CancellationToken, Task> task, T arg1, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, token);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T>(Func<T, Task> task, T arg1, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask(Func<CancellationToken, Task> task, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(default);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask(Func<Task> task, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task();
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask(Task task, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        if (!IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));

        member = fp + " :: " + member;

        if (string.IsNullOrEmpty(ctx))
            ctx = member;
        else
            ctx += " Member: " + member;
        if (task.IsCanceled)
        {
            L.LogDebug("Task cancelled: \"" + ctx + "\".");
            return;
        }
        if (task.IsFaulted)
        {
            RegisterErroredTask(task, ctx);
            return;
        }
        if (task.IsCompleted)
        {
            L.LogDebug("Task completed without awaiting: \"" + ctx + "\".");
            return;
        }
        L.LogDebug("Adding task \"" + ctx + "\".");
        I._tasks.Add(new UCTask(task, ctx, awaitOnUnload
#if DEBUG
            , timeout
#endif
        ));
    }
    private static void RegisterErroredTask(Task task, string? ctx)
    {
        AggregateException? ex = task.Exception;
        if (ex is null)
        {
            L.LogError("A registered task has failed without exception!" + (string.IsNullOrEmpty(ctx) ? string.Empty : (" Context: " + ctx)));
        }
        else
        {
            if (ex.InnerExceptions.All(x => x is OperationCanceledException))
            {
                L.LogDebug("A registered task was cancelled." + (string.IsNullOrEmpty(ctx) ? string.Empty : (" Context: " + ctx)));
                return;
            }
            L.LogError("A registered task has failed!" + (string.IsNullOrEmpty(ctx) ? string.Empty : (" Context: " + ctx)));
            L.LogError(ex);
        }
    }
    public static bool IsMainThread => Thread.CurrentThread.IsGameThread();
    public static void RunOnMainThread(System.Action action) => RunOnMainThread(action, false, default);
    public static void RunOnMainThread(System.Action action, CancellationToken token) => RunOnMainThread(action, false, token);
    /// <param name="action">Method to be ran on the main thread in an update dequeue loop.</param>
    /// <param name="skipFrame">If this is called on the main thread it will queue it to be called next update or at the end of the current frame.</param>
    public static void RunOnMainThread(System.Action action, bool skipFrame, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (IsMainThread)
            action();
        else
        {
            MainThreadTask.MainThreadResult res = new MainThreadTask.MainThreadResult(new MainThreadTask(skipFrame, token));
            res.OnCompleted(action);
        }
    }
    /// <summary>Continues to run main thread operations in between spins so that calls to <see cref="ToUpdate"/> are not blocked.</summary>
    public static bool SpinWaitUntil(Func<bool> condition, int millisecondsTimeout = -1, CancellationToken token = default)
    {
        if (!IsMainThread)
            return SpinWait.SpinUntil(condition, millisecondsTimeout);

        uint stTime = 0;
        if (millisecondsTimeout != 0 && millisecondsTimeout != -1)
            stTime = (uint)Environment.TickCount;
        SpinWait spinWait = new SpinWait();
        while (!condition())
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);
            if (millisecondsTimeout == 0)
                return false;
            spinWait.SpinOnce();
            ProcessQueues();
            if (millisecondsTimeout != -1 && spinWait.NextSpinWillYield && millisecondsTimeout <= Environment.TickCount - stTime)
                return false;
        }
        return true;
    }
    [UsedImplicitly]
    private void Update()
    {
        ProcessQueues();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            PlayerManager.OnlinePlayers[i].Update();
        if (ProcessTasks)
        {
#if DEBUG
            DateTime now = _tasks.Count > 0 ? DateTime.UtcNow : default;
#endif
            for (int i = _tasks.Count - 1; i >= 0; --i)
            {
                UCTask task = _tasks[i];
#if DEBUG
                double sec = (now - task.StartTime).TotalSeconds;
#endif
                if (!task.Task.IsCompleted)
                {
#if DEBUG
                    if (task.TimeoutMs >= 0 && sec > task.TimeoutMs)
                    {
                        L.LogDebug($"Task not completed after a long time ({sec} seconds)." + (string.IsNullOrEmpty(task.Context) ? string.Empty : (" Context: " + task.Context)));
                    }
#endif
                    continue;
                }
                if (task.Task.IsCanceled)
                {
                    L.LogDebug("Task cancelled." + (string.IsNullOrEmpty(task.Context) ? string.Empty : (" Context: " + task.Context)));
                }
                else if (task.Task.IsFaulted)
                {
                    _tasks.RemoveAtFast(i);
                    RegisterErroredTask(task.Task, task.Context);
                    return;
                }
                if (task.Task.IsCompleted)
                {
                    _tasks.RemoveAtFast(i);
#if DEBUG
                    L.LogDebug("Task complete in " + sec.ToString("0.#", Data.AdminLocale) + " seconds." + (string.IsNullOrEmpty(task.Context) ? string.Empty : (" Context: " + task.Context)));
#endif
                    return;
                }
            }
        }
    }
    private static void ProcessQueues()
    {
        if (_threadActionRequests != null)
        {
            while (_threadActionRequests.Count > 0)
            {
                MainThreadTask.MainThreadResult? res = null;
                try
                {
                    res = _threadActionRequests.Dequeue();
                    res.Task.Token.ThrowIfCancellationRequested();
                    res.Continuation();
                }
                catch (OperationCanceledException) { L.LogDebug("Execution on update cancelled."); }
                catch (Exception ex)
                {
                    L.LogError("Error executing main thread operation.");
                    L.LogError(ex);
                }
                finally
                {
                    res?.Complete();
                }
            }
        }
        if (_levelLoadRequests != null && Level.isLoaded)
        {
            while (_levelLoadRequests.Count > 0)
            {
                LevelLoadTask.LevelLoadResult? res = null;
                try
                {
                    res = _levelLoadRequests.Dequeue();
                    res.Task.Token.ThrowIfCancellationRequested();
                    res.continuation();
                }
                catch (OperationCanceledException) { L.LogDebug("Execution on level load cancelled."); }
                catch (Exception ex)
                {
                    L.LogError("Error executing level load operation.");
                    L.LogError(ex);
                }
                finally
                {
                    res?.Complete();
                }
            }
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal static void ForceUnload()
    {
        Nexus.UnloadNow();
        throw new SingletonUnloadedException(typeof(UCWarfare));
    }
    internal async Task LetTasksUnload(CancellationToken token)
    {
        while (_tasks.Count > 0)
        {
            UCTask task = _tasks[0];
            if (task.AwaitOnUnload && !task.Task.IsCompleted)
            {
                L.LogDebug("Letting task \"" + (task.Context ?? "null") + "\" finish for up to 10 seconds before unloading...");
                try
                {
                    await Task.WhenAny(task.Task, Task.Delay(10000, token));
                }
                catch
                {
                    RegisterErroredTask(task.Task, task.Context);
                    continue;
                }
                if (!task.Task.IsCompleted)
                {
                    L.LogWarning("Task \"" + (task.Context ?? "null") + "\" did not complete after 10 seconds of waiting.");
                }
                else L.LogDebug("  ... Done");
            }
            _tasks.RemoveAt(0);
        }
    }

    public async Task UnloadAsync(CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FullyLoaded = false;
        try
        {
            ProcessTasks = false;
            if (StatsRoutine != null)
            {
                StopCoroutine(StatsRoutine);
                StatsRoutine = null;
            }
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            L.Log("Unloading Uncreated Warfare", ConsoleColor.Magenta);
            await LetTasksUnload(token).ConfigureAwait(false);
            if (Data.Singletons is not null)
            {
                await Data.Singletons.UnloadSingletonAsync(Data.DeathTracker, false, token: token);
                Data.DeathTracker = null!;
                if (Announcer != null)
                {
                    await Data.Singletons.UnloadSingletonAsync(Announcer, token: token);
                    Announcer = null!;
                }
                if (Data.Gamemode != null)
                {
                    Data.Gamemode.IsPendingCancel = true;
                    await Data.Singletons.UnloadSingletonAsync(Data.Gamemode, token: token);
                    Data.Gamemode = null!;
                }
            }

            await LetTasksUnload(token).ConfigureAwait(false);
            await ToUpdate(token);

            ThreadUtil.assertIsGameThread();
            if (Solver != null)
            {
                Destroy(Solver);
            }
            if (Maps.MapScheduler.Instance != null)
            {
                Destroy(Maps.MapScheduler.Instance);
                Maps.MapScheduler.Instance = null!;
            }

            if (Debugger != null)
                Destroy(Debugger);
            OffenseManager.Deinit();
            if (Data.DatabaseManager != null)
            {
                try
                {
                    await Data.DatabaseManager.CloseAsync(token);
                    Data.DatabaseManager.Dispose();
                }
                finally
                {
                    Data.DatabaseManager = null!;
                }
            }
            if (Data.RemoteSQL != null)
            {
                try
                {
                    await Data.RemoteSQL.CloseAsync(token);
                    Data.RemoteSQL.Dispose();
                }
                finally
                {
                    Data.RemoteSQL = null!;
                }
            }
            await LetTasksUnload(token).ConfigureAwait(false);
            await ToUpdate(token);
            ThreadUtil.assertIsGameThread();
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            if (NetClient != null)
            {
                Destroy(NetClient);
                NetClient = null;
            }
            Logging.OnLogInfo -= L.NetLogInfo;
            Logging.OnLogWarning -= L.NetLogWarning;
            Logging.OnLogError -= L.NetLogError;
            Logging.OnLogException -= L.NetLogException;
            ConfigSync.UnpatchAll();
            try
            {
                LoadingQueueBlockerPatches.Unpatch();
                Patches.Unpatch();
            }
            catch (Exception ex)
            {
                L.LogError("Unpatching Error, perhaps Nelson changed something:");
                L.LogError(ex);
            }
            for (int i = 0; i < StatsManager.OnlinePlayers.Count; i++)
            {
                WarfareStats.IO.WriteTo(StatsManager.OnlinePlayers[i], StatsManager.StatsDirectory + StatsManager.OnlinePlayers[i].Steam64.ToString(Data.AdminLocale) + ".dat");
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error unloading: ");
            L.LogError(ex);
        }

        if (Data.Singletons != null)
        {
            await Data.Singletons.UnloadAllAsync(token);
            await ToUpdate(token);
            ThreadUtil.assertIsGameThread();
        }
        L.Log("Warfare unload complete", ConsoleColor.Blue);
#if DEBUG
        profiler.Dispose();
        F.SaveProfilingData();
#endif
        await Task.Delay(1000, token);
    }
    public static Color GetColor(string key)
    {
        if (Data.Colors == null) return Color.white;
        if (Data.Colors.TryGetValue(key, out Color color)) return color;
        else if (Data.Colors.TryGetValue("default", out color)) return color;
        else return Color.white;
    }
    public static string GetColorHex(string key)
    {
        if (Data.ColorsHex == null) return @"ffffff";
        if (Data.ColorsHex.TryGetValue(key, out string color)) return color;
        else if (Data.ColorsHex.TryGetValue("default", out color)) return color;
        else return @"ffffff";
    }
    public static void ShutdownIn(string reason, ulong instigator, int seconds)
    {
        I.StartCoroutine(ShutdownIn2(reason, instigator, seconds));
    }
    public static void ShutdownNow(string reason, ulong instigator)
    {
        for (int i = 0; i < Provider.clients.Count; ++i)
            Provider.kick(Provider.clients[i].playerID.steamID, "Intentional Shutdown: " + reason);

        VehicleSpawner? bay = Data.Singletons.GetSingleton<VehicleSpawner>();
        if (bay != null && bay.IsLoaded)
        {
            bay.AbandonAllVehicles(false);
        }

        if (CanUseNetCall)
        {
            ShutdownCommand.NetCalls.SendShuttingDownInstant.NetInvoke(instigator, reason);
            I.StartCoroutine(ShutdownIn(reason, 4));
        }
        else
        {
            ShutdownInAwaitUnload(2, reason);
        }
    }
    private static IEnumerator<WaitForSeconds> ShutdownIn(string reason, float seconds)
    {
        yield return new WaitForSeconds(seconds / 2f);
        ShutdownInAwaitUnload(Mathf.RoundToInt(seconds / 2f), reason);
    }
    private static void ShutdownInAwaitUnload(int seconds, string reason)
    {
        Task.Run(async () =>
        {
            await ToUpdate();
            await Nexus.Unload();
            Provider.shutdown(seconds, reason);
        });
    }
    private static IEnumerator<WaitForSeconds> ShutdownIn2(string reason, ulong instigator, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ShutdownCommand.NetCalls.SendShuttingDownInstant.NetInvoke(instigator, reason);
        yield return new WaitForSeconds(1f);
        ShutdownInAwaitUnload(2, reason);
    }
    private readonly struct UCTask
    {
        public readonly Task Task;
        public readonly string? Context;
        public readonly bool AwaitOnUnload;
#if DEBUG
        public readonly int TimeoutMs;
        public readonly DateTime StartTime;
#endif
        public UCTask(Task task, string context, bool awaitOnUnload
#if DEBUG
            , int timeout
#endif
            )
        {
            Task = task;
            Context = context;
            AwaitOnUnload = awaitOnUnload;
#if DEBUG
            TimeoutMs = timeout;
            StartTime = DateTime.UtcNow;
#endif
        }
    }

    public static bool IsNerd(ulong s64)
    {
        return Config.Nerds != null && Config.Nerds.Contains(s64);
    }
}

public class UCWarfareNexus : IModuleNexus
{
    public bool Loaded { get; private set; }

    void IModuleNexus.initialize()
    {
        CommandWindow.Log("Initializing UCWarfareNexus...");
        try
        {
            L.Init();
        }
        catch (Exception ex)
        {
            Logging.LogException(ex);
        }
        Level.onPostLevelLoaded += OnLevelLoaded;
        UCWarfare.Nexus = this;
        GameObject go = new GameObject("UCWarfare " + UCWarfare.Version);
        go.AddComponent<Maps.MapScheduler>();
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<UCWarfare>();
    }

    private void Load()
    {
        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        try
        {
            await UCWarfare.I.LoadAsync(UCWarfare.UnloadCancel).ConfigureAwait(false);
            await UCWarfare.ToUpdate(UCWarfare.UnloadCancel);
            Loaded = true;
        }
        catch (Exception ex)
        {
            if (UCWarfare.I != null)
                await UCWarfare.ToUpdate();
            L.LogError(ex);
            Loaded = false;
            if (UCWarfare.I != null)
            {
                try
                {
                    await UCWarfare.I.UnloadAsync(CancellationToken.None).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(CancellationToken.None);
                }
                catch (Exception e)
                {
                    L.LogError("Unload error: ");
                    L.LogError(e);
                }

                UnityEngine.Object.Destroy(UCWarfare.I);
                UCWarfare.I = null!;
            }

            ShutdownCommand.ShutdownIn(10, "Uncreated Warfare failed to load: " + ex.GetType().Name);
            if (ex is SingletonLoadException)
                throw;
            else
                throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
        }
    }

    private IEnumerator Coroutine()
    {
        while (!Level.isLoaded)
            yield return null;
        Load();
    }

    private void OnLevelLoaded(int level)
    {
        if (level == Level.BUILD_INDEX_GAME)
        {
            UCWarfare.I.StartCoroutine(Coroutine());
        }
    }

    public void UnloadNow()
    {
        Task.Run(async () =>
        {
            await UCWarfare.ToUpdate();
            await Unload().ConfigureAwait(false);
            ShutdownCommand.ShutdownIn(10, "Uncreated Warfare unloading.");
        });
    }
    public async Task Unload()
    {
        try
        {
            await UCWarfare.I.UnloadAsync(CancellationToken.None).ConfigureAwait(false);
            await UCWarfare.ToUpdate(CancellationToken.None);
            if (UCWarfare.I.gameObject != null)
            {
                UnityEngine.Object.Destroy(UCWarfare.I.gameObject);
            }
            UCWarfare.I = null!;
        }
        catch (Exception ex)
        {
            L.LogError(ex);
            if (ex is SingletonLoadException)
                throw;
            else
                throw new SingletonLoadException(SingletonLoadType.Unload, null, ex);
        }
    }
    void IModuleNexus.shutdown()
    {
        Level.onPostLevelLoaded -= OnLevelLoaded;
        if (!UCWarfare.IsLoaded) return;
        Unload().Wait();
    }
}

[Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
internal sealed class OperationTestAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public float? ArgumentSingle { get; }
    public double? ArgumentDouble { get; }
    public decimal? ArgumentDecimal { get; }
    public long? ArgumentInt64 { get; }
    public ulong? ArgumentUInt64 { get; }
    public int? ArgumentInt32 { get; }
    public uint? ArgumentUInt32 { get; }
    public short? ArgumentInt16 { get; }
    public ushort? ArgumentUInt16 { get; }
    public sbyte? ArgumentInt8 { get; }
    public byte? ArgumentUInt8 { get; }
    public bool? ArgumentBoolean { get; }
    public string? ArgumentString { get; }
    public Type? ArgumentType { get; }
    public Type[]? IgnoreExceptions { get; set; }
    /// <summary>Just run it, check exceptions only.</summary>
    public OperationTestAttribute() { }
    public OperationTestAttribute(long arg) { ArgumentInt64 = arg; }
    public OperationTestAttribute(ulong arg) { ArgumentUInt64 = arg; }
    public OperationTestAttribute(int arg) { ArgumentInt32 = arg; }
    public OperationTestAttribute(uint arg) { ArgumentUInt32 = arg; }
    public OperationTestAttribute(short arg) { ArgumentInt16 = arg; }
    public OperationTestAttribute(ushort arg) { ArgumentUInt16 = arg; }
    public OperationTestAttribute(sbyte arg) { ArgumentInt8 = arg; }
    public OperationTestAttribute(byte arg) { ArgumentUInt8 = arg; }
    public OperationTestAttribute(bool arg) { ArgumentBoolean = arg; }
    public OperationTestAttribute(float arg) { ArgumentSingle = arg; }
    public OperationTestAttribute(double arg) { ArgumentDouble = arg; }
    public OperationTestAttribute(decimal arg) { ArgumentDecimal = arg; }
    public OperationTestAttribute(string arg) { ArgumentString = arg; }
    public OperationTestAttribute(Type arg) { ArgumentType = arg; }
}