using Microsoft.Extensions.Configuration;

namespace Uncreated.Warfare.Layouts.Phases;

/// <summary>
/// Represents a phase of a <see cref="Layout"/>.
/// </summary>
public interface ILayoutPhase
{
    /// <summary>
    /// If this phase is currently activated.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Display name of the phase.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The configuration section used to create this phase.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Invoked before the layout starts. Meant to be used for loading extra configuration information.
    /// </summary>
    UniTask InitializePhaseAsync(CancellationToken token = default);

    /// <summary>
    /// Activates the phase. This should happen just after the old phase ended if there was one.
    /// </summary>
    /// <remarks>Must set <see cref="IsActive"/> to <see langword="true"/> or the phase will be skipped.</remarks>
    UniTask BeginPhaseAsync(CancellationToken token = default);

    /// <summary>
    /// Deactivates the phase.
    /// </summary>
    UniTask EndPhaseAsync(CancellationToken token = default);
}
