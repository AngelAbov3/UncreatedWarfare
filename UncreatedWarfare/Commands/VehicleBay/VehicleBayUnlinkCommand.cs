using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("unlink"), SubCommandOf(typeof(VehicleBayCommand))]
internal sealed class VehicleBayUnlinkCommand : IExecutableCommand
{
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleBayUnlinkCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        VehicleSpawnerService spawnerStore)
    {
        _spawnerStore = spawnerStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? barricade) || barricade.interactable is not InteractableSign sign)
        {
            throw Context.Reply(_translations.NoTarget);
        }

        await UniTask.SwitchToMainThread(token);

        IBuildable buildable = new BuildableBarricade(barricade);

        if (!_spawnerStore.TryGetSpawner(buildable.InstanceId, out VehicleSpawner? existing))
        {
            throw Context.Reply(_translations.SignNotLinked);
        }

        existing.Signs.RemoveAll(s => s.Equals(buildable));
        existing.SpawnInfo.SignInstanceIds.Remove(buildable.InstanceId);

        BarricadeManager.ServerSetSignText(sign, string.Empty);
        await _spawnerStore.SpawnerStore.AddOrUpdateSpawnAsync(existing.SpawnInfo, token);
        Context.Reply(_translations.VehicleBayUnlinked, existing.VehicleInfo.VehicleAsset.GetAssetOrFail());
    }
}