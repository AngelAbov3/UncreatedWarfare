using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uncreated.Warfare;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Logging.Formatting;

// ReSharper disable once CheckNamespace
namespace Uncreated;

// this class is mostly copied and expanded from
// https://github.com/dotnet/extensions/blob/v3.1.0/src/Logging/Logging.Abstractions/src/LoggerExtensions.cs
//  - Microsoft.Extensions.Logging.Abstractions v3.1.0 source
public static class WarfareLoggingExtensions
{
    //------------------------------------------DEBUG------------------------------------------//

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogDebug(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogDebug(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogDebug(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Debug, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------DEBUG------------------------------------------//
    // conditional debug

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareDebugLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Debug, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Debug, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------TRACE------------------------------------------//

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareTraceLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Trace, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogTrace(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareTraceLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Trace, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogTrace(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareTraceLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Trace, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogTrace(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareTraceLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Trace, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Trace, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Trace, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Trace, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    /// <summary>
    /// Formats and writes a informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareInformationLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Information, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogInformation(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareInformationLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Information, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogInformation(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareInformationLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Information, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogInformation(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareInformationLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Information, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Information, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Information, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Information, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------WARNING------------------------------------------//

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareWarningLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Warning, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogWarning(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareWarningLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Warning, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogWarning(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareWarningLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Warning, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogWarning(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareWarningLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Warning, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Warning, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Warning, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Warning, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------ERROR------------------------------------------//

    /// <summary>
    /// Formats and writes a error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareErrorLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Error, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogError(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareErrorLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Error, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogError(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareErrorLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Error, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogError(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareErrorLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Error, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Error, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Error, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Error, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareCriticalLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Critical, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogCritical(this ILogger logger, EventId eventId, [InterpolatedStringHandlerArgument("logger")] WarfareCriticalLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Critical, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogCritical(this ILogger logger, Exception? exception, [InterpolatedStringHandlerArgument("logger")] WarfareCriticalLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Critical, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void LogCritical(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] WarfareCriticalLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(LogLevel.Critical, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Critical, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Critical, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Critical, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, [InterpolatedStringHandlerArgument("logger", "logLevel")] WarfareLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(logLevel, eventId, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, [InterpolatedStringHandlerArgument("logger", "logLevel")] WarfareLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(logLevel, eventId, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, [InterpolatedStringHandlerArgument("logger", "logLevel")] WarfareLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(logLevel, 0, exception, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="builder">Format string of the log message in message template format. Example: <code>$"User {userName} logged in from {ipAddress}"</code></param>
    public static void Log(this ILogger logger, LogLevel logLevel, [InterpolatedStringHandlerArgument("logger", "logLevel")] WarfareLoggerInterpolatedStringHandler builder)
    {
        builder.GetResult(out StringParameterList parameterList, out string literal);
        logger.Log(logLevel, 0, null, literal, parameterList);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, params object?[]? args)
    {
        logger.Log(logLevel, 0, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(logLevel, eventId, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(logLevel, 0, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        args ??= Array.Empty<object?>();
        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, args), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, args);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message)
    {
        logger.Log(logLevel, 0, null, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message)
    {
        logger.Log(logLevel, eventId, null, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message)
    {
        logger.Log(logLevel, 0, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, Array.Empty<object>()), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, Array.Empty<object>());
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1)
    {
        logger.Log(logLevel, 0, null, message, arg1);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1)
    {
        logger.Log(logLevel, eventId, null, message, arg1);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1)
    {
        logger.Log(logLevel, 0, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, arg1), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1 ]);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    private static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, StringParameterList parameterList)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, parameterList), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, parameterList.ToArray());
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1, object? arg2)
    {
        logger.Log(logLevel, 0, null, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(logLevel, eventId, null, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(logLevel, 0, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, arg1, arg2), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1, arg2 ]);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(logLevel, 0, null, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(logLevel, eventId, null, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(logLevel, 0, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, arg1, arg2, arg3), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1, arg2, arg3 ]);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(logLevel, 0, null, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(logLevel, eventId, null, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(logLevel, 0, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }


        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, logLevel, arg1, arg2, arg3, arg4), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1, arg2, arg3, arg4 ]);
        }
    }

    private static readonly Func<WarfareFormattedLogValues, Exception?, string> MessageFormatter = MessageFormatterMtd;
    private static string MessageFormatterMtd(WarfareFormattedLogValues state, Exception? error)
    {
        return state.ToString();
    }
}