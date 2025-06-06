using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;

public class ThrownAmmoBag : ThrownSupplyCrate
{
    private static int MidairCheckLayerMask = (1 << LayerMasks.LARGE) | (1 << LayerMasks.MEDIUM) | (1 << LayerMasks.BARRICADE) | (1 << LayerMasks.STRUCTURE) |
                                   (1 << LayerMasks.GROUND) | (1 << LayerMasks.GROUND2);
    private static readonly Collider?[] TempHitColliders = new Collider?[1];
    private readonly ItemBarricadeAsset _completedAmmoCrateAsset;
    private readonly float _startingAmmo;
    private readonly bool _isInMain;
    private readonly ThrownComponent _thrownComponent;
    private readonly Team _team;
    private readonly IServiceProvider _serviceProvider;

    private Layout? _layout;

    public ThrownAmmoBag(
        GameObject throwable,
        WarfarePlayer thrower,
        ItemThrowableAsset thrownAsset,
        ItemBarricadeAsset completedAmmoCrateAsset,
        IServiceProvider serviceProvider,
        float startingAmmo,
        bool isInMain)
        : base(throwable, thrownAsset, thrower)
    {
        _completedAmmoCrateAsset = completedAmmoCrateAsset;
        _startingAmmo = startingAmmo;
        _isInMain = isInMain;
        _team = thrower.Team;
        _serviceProvider = serviceProvider;
        _thrownComponent = Throwable.AddComponent<ThrownComponent>();
        _thrownComponent.OnThrowableDestroyed = OnThrowableDestroyed;

        // check to make sure the layout didn't expire
        _layout = _serviceProvider.GetService<Layout>();
    }

    private void OnThrowableDestroyed()
    {
        if (_layout is { IsActive: false } || _isInMain)
        {
            RespawnThrowableItem();
            return;
        }
        
        int resultsCount = Physics.OverlapSphereNonAlloc(Throwable.transform.position, 0.5f, TempHitColliders, 
            MidairCheckLayerMask);
        TempHitColliders[0] = null;
        if (resultsCount <= 0) // check that this ammo bag isn't being destroyed while midair
        {
            RespawnThrowableItem();
            return;
        }
            
        IBuildable buildable = BuildableExtensions.DropBuildable(
            _completedAmmoCrateAsset,
            Throwable.transform.position,
            Quaternion.Euler(-90, Throwable.transform.eulerAngles.y, 0),
            Thrower.Steam64,
            Thrower.GroupId
        );
        buildable.Model.gameObject.AddComponent<PlacedAmmoBagComponent>().Init(Thrower, buildable, _startingAmmo, _team, _serviceProvider);
    }
}