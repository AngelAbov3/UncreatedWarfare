﻿using Cysharp.Threading.Tasks;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using UnityEngine;
using Module = SDG.Framework.Modules.Module;

namespace Uncreated.Warfare;
public sealed class WarfareModule : IModuleNexus
{
    internal static int GameThreadId = -1;

    private bool _unloadedHostedServices;
    private IServiceScope? _activeScope;
    private CancellationTokenSource _cancellationTokenSource;
    private GameSession? _activeGameSession;
    private GameObject _gameObjectHost;

    /// <summary>
    /// A path to the top-level 'Warfare' folder.
    /// </summary>
    public string HomeDirectory { get; private set; }

    /// <summary>
    /// System Config.yml. Stores information not directly related to gameplay.
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Global service provider. Gamemodes have their own scoped service providers and should be used instead.
    /// </summary>
    public ServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Game-specific service provider. If a game is not active, this will return <see cref="ServiceProvider"/>.
    /// </summary>
    public IServiceProvider ScopedProvider => _activeScope?.ServiceProvider ?? ServiceProvider;

    /// <summary>
    /// Token that will cancel when the module shuts down.
    /// </summary>
    public CancellationToken UnloadToken
    {
        get
        {
            try
            {
                return _cancellationTokenSource.Token;
            }
            catch (ObjectDisposedException)
            {
                return new CancellationToken(true);
            }
        }
    }

    void IModuleNexus.initialize()
    {
        GameThreadId = ThreadUtil.gameThread.ManagedThreadId;
        _gameObjectHost = new GameObject("Uncreated.Warfare");
        _cancellationTokenSource = new CancellationTokenSource();

        ConfigurationSettings.SetupTypeConverters();

        // Set the environment directory to the folder now at U3DS/Servers/ServerId/Warfare/
        HomeDirectory = Path.Combine(UnturnedPaths.RootDirectory.Name, "Servers", Provider.serverID, "Warfare");
        Directory.CreateDirectory(HomeDirectory);

        // Add system configuration provider.
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, Path.Join(HomeDirectory, "System Config.yml"));
        Configuration = configBuilder.Build();

        IServiceCollection serviceCollection = new ServiceCollection();

        // todo rewrite logging
        serviceCollection.AddSingleton<ILoggerFactory, L.UCLoggerFactory>();

        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });


        UniTask.Create(HostAsync);
    }

    void IModuleNexus.shutdown()
    {
        if (Configuration is IDisposable disposableConfig)
            disposableConfig.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            L.LogError("Error(s) while canceling module cancellation token source.");
            L.LogError(ex);
        }

        _cancellationTokenSource.Dispose();

        if (!_unloadedHostedServices)
        {
            Unhost();
        }
        
        ServiceProvider.Dispose();
    }

    private void ConfigureServices(IServiceCollection serviceCollection)
    {
        Assembly thisAsm = Assembly.GetExecutingAssembly();

        serviceCollection.AddDbContext<WarfareDbContext>(contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Singleton);

        serviceCollection.AddReflectionTools();
        serviceCollection.AddModularRpcs(isServer: false, searchedAssemblies: [Assembly.GetExecutingAssembly()]);

        serviceCollection.AddSingleton(this);
        serviceCollection.AddSingleton(ModuleHook.modules.First(x => x.config.Name.Equals("Uncreated.Warfare", StringComparison.Ordinal) && x.assemblies.Contains(thisAsm)));

        serviceCollection.AddSingleton<GameSessionFactory>();
        serviceCollection.AddSingleton<ActionManager>();
        serviceCollection.AddSingleton<CommandDispatcher>();
        serviceCollection.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<CommandDispatcher>().Parser);
        serviceCollection.AddRpcSingleton<UserPermissionStore>();
        serviceCollection.AddSingleton(_gameObjectHost);
        serviceCollection.AddTransient<IManualMySqlProvider, ManualMySqlProvider>(_ =>
        {
            string connectionString = UCWarfare.Config.SqlConnectionString ??
                                      (UCWarfare.Config.RemoteSQL ?? UCWarfare.Config.SQL).GetConnectionString("UCWarfare", true, true);

            return new ManualMySqlProvider(connectionString);
        });

        serviceCollection.AddScoped<BuildableSaver>();

        serviceCollection.AddTransient<ILoopTickerFactory, UnityLoopTickerFactory>();

        serviceCollection.AddTransient(serviceProvider => serviceProvider.GetRequiredService<WarfareModule>().GetActiveGameSession());
        serviceCollection.AddTransient(serviceProvider => serviceProvider.GetRequiredService<WarfareModule>().GetActiveGameSession().TeamManager);

        // add all game session types
        foreach (Type type in Accessor.GetTypesSafe(thisAsm).Where(x => x.IsSubclassOf(typeof(GameSession))))
        {
            serviceCollection.Add(new ServiceDescriptor(type, _ =>
            {
                GameSession session = GetActiveGameSession();
                if (!type.IsInstanceOfType(session))
                {
                    throw new InvalidOperationException($"The current game session type is not {Accessor.ExceptionFormatter.Format(type)}.");
                }

                return session;
            }, ServiceLifetime.Transient));
        }
    }

    public async UniTask ShutdownAsync(CancellationToken token = default)
    {
        if (!_unloadedHostedServices)
        {
            await UnhostAsync(token);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();
    }

    /// <summary>
    /// Start all hosted services.
    /// </summary>
    private async UniTask HostAsync()
    {
        CancellationToken token = UnloadToken;
        List<IHostedService> hostedServices = ServiceProvider.GetServices<IHostedService>().ToList();
        int errIndex = -1;
        for (int i = 0; i < hostedServices.Count; i++)
        {
            IHostedService hostedService = hostedServices[i];
            try
            {
                await UniTask.SwitchToMainThread(token);
                await hostedService.StartAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                L.LogError($"Error hosting service {Accessor.Formatter.Format(hostedService.GetType())}.");
                L.LogError(ex);
                errIndex = i;
                break;
            }
        }

        // one of the hosted services errored, unhost all that were hosted and shut down.
        if (errIndex == -1)
            return;

        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;

        _unloadedHostedServices = true;
        UniTask[] tasks = new UniTask[errIndex];
        for (int i = errIndex - 1; i >= 0; --i)
        {
            IHostedService hostedService = hostedServices[i];
            try
            {
                tasks[i] = hostedService.StopAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                L.LogError($"Error stopping service {Accessor.Formatter.Format(hostedService.GetType())}.");
                L.LogError(ex);
            }
        }

        _unloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            await UniTask.SwitchToMainThread();
            L.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();
    }

    private async UniTask UnhostAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;

        List<IHostedService> hostedServices = ServiceProvider.GetServices<IHostedService>().ToList();

        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                tasks[i] = hostedServices[i].StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
        }

        _unloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            await UniTask.SwitchToMainThread();
            L.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
        }
    }

    /// <summary>
    /// Synchronously unhost so <see cref="IModuleNexus.shutdown"/> can wait on unhost.
    /// </summary>
    private void Unhost()
    {
        _unloadedHostedServices = true;
        using CancellationTokenSource timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3d));

        List<IHostedService> hostedServices = ServiceProvider.GetServices<IHostedService>().ToList();

        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                tasks[i] = hostedServices[i].StopAsync(timeoutSource.Token);
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
        }

        bool canceled = false;
        try
        {
            UniTask.WhenAll(tasks).AsTask().Wait(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            canceled = true;
        }
        catch
        {
            L.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
            Thread.Sleep(500);
            return;
        }

        if (!canceled)
            return;

        L.LogError("Unloading timed out:");
        for (int i = 0; i < tasks.Length; ++i)
        {
            if (tasks[i].Status is not UniTaskStatus.Canceled and not UniTaskStatus.Pending)
                continue;

            L.LogError(Accessor.Formatter.Format(hostedServices[i].GetType()) + $" - {tasks[i].Status}.");
        }
        Thread.Sleep(500);
    }

    /// <summary>
    /// Start a new scope, used for each game.
    /// </summary>
    /// <returns>The newly created scope.</returns>
    internal async UniTask<IServiceProvider> CreateScopeAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _activeGameSession = null;
        if (_activeScope is IAsyncDisposable asyncDisposableScope)
        {
            ValueTask vt = asyncDisposableScope.DisposeAsync();
            _activeScope = null;
            await vt.ConfigureAwait(false);
            await UniTask.SwitchToMainThread();
        }
        else if (_activeScope is IDisposable disposableScope)
        {
            disposableScope.Dispose();
            _activeScope = null;
        }

        IServiceScope scope = ServiceProvider.CreateScope();
        _activeScope = scope;
        return scope.ServiceProvider;
    }

    /// <summary>
    /// Set the new game after calling <see cref="CreateScopeAsync"/>.
    /// </summary>
    internal void SetActiveGameSession(GameSession? gameSession)
    {
        GameSession? oldSession = Interlocked.Exchange(ref _activeGameSession, gameSession);
        if (oldSession == null || gameSession == null)
            return;

        L.LogError("A session was started while one was already active.");
        oldSession.Dispose();
    }

    /// <summary>
    /// Check if there is an active game session.
    /// </summary>
    public bool IsGameSessionActive() => _activeGameSession != null;

    /// <summary>
    /// Get the active game session.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is not an active game session.</exception>
    public GameSession GetActiveGameSession()
    {
        return _activeGameSession ?? throw new InvalidOperationException("There is not an active game session.");
    }

    private void UnloadModule()
    {
        ServiceProvider.GetRequiredService<Module>().isEnabled = false;
    }
}
