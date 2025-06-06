using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Commands;

[Command("request", "req", "r"), SynchronizedCommand, MetadataFile]
internal sealed class RequestCommand : ICompoundingCooldownCommand
{
    private readonly SignInstancer _signInstancer;
    private readonly WarfareModule _module;
    private readonly RequestTranslations _translations;
    public float CompoundMultiplier => 2f;
    public float MaxCooldown => 900f; // 15 mins

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public RequestCommand(
        TranslationInjection<RequestTranslations> translations,
        SignInstancer signInstancer,
        WarfareModule module)
    {
        _signInstancer = signInstancer;
        _module = module;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();
        
        IRequestable<object>? requestable = null;
        if (Context.TryGetTargetRootTransform(out Transform? transform))
            requestable = RequestHelper.GetRequestable(transform, _signInstancer);

        if (requestable == null)
        {
            throw Context.Reply(_translations.RequestNoTarget);
        }

        await RequestHelper.RequestAsync(
            Context.Player,
            requestable,
            Context.Logger,
            _module.ScopedProvider.Resolve<IServiceProvider>(),
            typeof(RequestCommandResultHandler),
            token
        );

        Context.Defer();
    }
}