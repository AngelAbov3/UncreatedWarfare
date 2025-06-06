using System;
using Uncreated.Warfare.Events.Models;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Optional configuration for event listener handle methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventListenerAttribute : Attribute
{
    internal bool HasRequiredMainThread;

    /// <summary>
    /// Highest priority possible, ensures the event listener runs without switching contexts from the original event.
    /// </summary>
    /// <remarks>Not supported on <see cref="IAsyncEventListener{TEventArgs}"/>. An error will be thrown.</remarks>
    public bool MustRunInstantly { get; set; }

    /// <summary>
    /// Lowest priority possible, ensures the event listener runs after any events could have cancelled the event. If an event with this flag cancels the event an exception will be thrown.
    /// </summary>
    public bool MustRunLast { get; set; }

    /// <summary>
    /// Positive values run before negative values. 0 is neutral.
    /// </summary>
    /// <remarks>Defaults to 0.</remarks>
    public int Priority { get; set; }

    /// <summary>
    /// Requires that a session has started before invoking this event.
    /// </summary>
    public bool RequireActiveLayout { get; set; }

    /// <summary>
    /// Requires that this event listener wait until the next frame has completed (<see cref="UniTask.NextFrame(CancellationToken, bool)"/>).
    /// </summary>
    public bool RequireNextFrame { get; set; }

    /// <summary>
    /// If this listener must be invoked on the main thread.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/> for <see cref="IEventListener{TEventArgs}"/>'s and <see langword="false"/> for <see cref="IAsyncEventListener{TEventArgs}"/>'s.</remarks>
    public bool RequiresMainThread
    {
        get;
        set
        {
            field = value;
            HasRequiredMainThread = true;
        }
    }
}

/// <summary>
/// Optional configuration for event models.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EventModelAttribute : Attribute
{
    /// <summary>
    /// Describes how other events of the same type can run while this one is running.
    /// </summary>
    public EventSynchronizationContext SynchronizationContext { get; set; }

    /// <summary>
    /// List of tags that this model is synchronized with. Other models must also share at least one of these tags to be synchronized.
    /// </summary>
    public string[]? SynchronizedModelTags { get; set; }
    
    public EventModelAttribute(EventSynchronizationContext sync)
    {
        SynchronizationContext = sync;
    }
}

/// <summary>
/// Describes how other events of the same type can run while this one is running.
/// </summary>
public enum EventSynchronizationContext
{
    /// <summary>
    /// Events are not synchronized.
    /// </summary>
    None,

    /// <summary>
    /// Events are synchronized per-player per-event.
    /// </summary>
    PerPlayer,

    /// <summary>
    /// Events are synchronized per-event.
    /// </summary>
    Global,

    /// <summary>
    /// Events are grouped by priority and each group is ran simultaneously (for async handlers).
    /// This should be used on events where the handlers for this event wouldn't effect each other.
    /// </summary>
    Pure
}