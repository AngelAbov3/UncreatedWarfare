using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Kits.Tweaks;

internal sealed class KitGiveDefaultOnLeaveSquadKit : IAsyncEventListener<SquadMemberLeft>
{
    private readonly KitRequestService _kitRequestService;

    public KitGiveDefaultOnLeaveSquadKit(KitRequestService kitRequestService)
    {
        _kitRequestService = kitRequestService;
    }
    public async UniTask HandleEventAsync(SquadMemberLeft e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Player.Team == Team.NoTeam)
            return;
        
        KitPlayerComponent kitPlayerComponent = e.Player.Component<KitPlayerComponent>();
        if (!kitPlayerComponent.HasKit || kitPlayerComponent.ActiveClass == Class.Unarmed)
            return;

        await _kitRequestService.GiveAvailableFreeKitAsync(e.Player, silent: true, isLowAmmo: true, token: token);
    }
}