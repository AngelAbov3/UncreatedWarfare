﻿using Cysharp.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.NetPak;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;
using Uncreated.Warfare.Networking;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Uses Senior S's audio converter web API for converting Steam Voice to .wav files.
/// </summary>
/// <remarks><see href="https://github.com/Senior-S/SVD-Example-Use/"/></remarks>
[HarmonyPatch]
public class AudioRecordManager
{
    public static AudioRecordManager Instance { get; } = new AudioRecordManager();

    static AudioRecordManager() { }

    private DateTime _lastAuthenticate = DateTime.MinValue;
    private string? _tokenStr;

    private readonly Uri? _authenticateUri;
    private readonly Uri? _decodeUri;
    private readonly Random _boundaryGenerator = new Random();

    public AudioRecordManager()
    {
        SystemConfigData.AudioRecordConfiguration? config = UCWarfare.Config.AudioRecordConfig;

        if (config?.AudioRecordBaseUri == null || config.AudioRecordUsername == null || config.AudioRecordPassword == null)
        {
            return;
        }

        string username = Uri.EscapeDataString(config.AudioRecordUsername);
        string password = Uri.EscapeDataString(config.AudioRecordPassword);
        _authenticateUri = new Uri(config.AudioRecordBaseUri, $"authentication?username={username}&key={password}");
        _decodeUri = new Uri(config.AudioRecordBaseUri, "decoder/form");
    }
    public async UniTask<AudioConvertResult> TryWriteWavAsync(byte[] multipartData, Stream writeTo, bool leaveOpen = true, CancellationToken token = default)
    {
        if (!UCWarfare.IsMainThread)
        {
            await UniTask.SwitchToMainThread(token);
        }

        try
        {
            int authTries = 0;
            while (true)
            {
                if (_tokenStr == null || (DateTime.UtcNow - _lastAuthenticate).TotalMinutes > 23d)
                {
                    AudioConvertResult result = await TryAuthenticateAsync(token);
                    if (result != AudioConvertResult.Success)
                        return result;
                    ++authTries;
                }

                byte[] boundary = new byte[40];

                Buffer.BlockCopy(multipartData, multipartData.Length - 42, boundary, 0, 40);

                using UnityWebRequest webRequest = new UnityWebRequest(_decodeUri, "POST");

                webRequest.SetRequestHeader("Authorization", _tokenStr);
                webRequest.uploadHandler = new UploadHandlerRaw(multipartData)
                {
                    contentType = "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(boundary)
                };

                webRequest.downloadHandler = new UnityStreamDownloadHandler(writeTo, leaveOpen: true);

                L.Log(webRequest.url);
                L.Log("Authorization: " + _tokenStr!);

                try
                {
                    await webRequest.SendWebRequest();
                }
                catch (UnityWebRequestException)
                {
                    // ignore
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    return AudioConvertResult.ConnectionError;
                }

                switch (webRequest.responseCode)
                {
                    case 400: // Bad Request
                        return AudioConvertResult.InvalidFormat;

                    case 401: // Unauthorized
                        _tokenStr = null;
                        if (authTries > 1)
                            return AudioConvertResult.Unauthorized;
                        continue;

                    case 200: // Success
                        return AudioConvertResult.Success;

                    default:
                        L.LogError($"Unrecognized API response: {webRequest.responseCode}, {webRequest.result}, \"{webRequest.error}\".");
                        return AudioConvertResult.UnknownError;
                }
            }
        }
        finally
        {
            if (!leaveOpen)
            {
                try
                {
                    await writeTo.FlushAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                writeTo.Dispose();
            }
        }
    }
    private static string ReadJwt(byte[] utf8)
    {
        if (utf8[0] != (byte)'{')
            return System.Text.Encoding.UTF8.GetString(utf8);

        Utf8JsonReader reader = new Utf8JsonReader(utf8);
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName
                || !reader.GetString()!.Equals("token", StringComparison.InvariantCultureIgnoreCase)
                || !reader.Read())
            {
                continue;
            }

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Invalid token type for token: {reader.TokenType}.");

            return reader.GetString()!;
        }

        throw new JsonException("Missing property \"token\" from json.");
    }
    public async UniTask<AudioConvertResult> TryAuthenticateAsync(CancellationToken token = default)
    {
        const int tries = 3;

        for (int i = 0; i < tries; ++i)
        {
            using UnityWebRequest authWebReq = UnityWebRequest.Get(_authenticateUri);
            authWebReq.downloadHandler = new DownloadHandlerBuffer();

            _tokenStr = null;

            L.Log(authWebReq.url);

            try
            {
                await authWebReq.SendWebRequest();
            }
            catch (UnityWebRequestException)
            {
                // ignore
            }

            switch (authWebReq.result)
            {
                case UnityWebRequest.Result.Success:
                    string jwt;

                    L.Log(authWebReq.downloadHandler.text);

                    try
                    {
                        jwt = ReadJwt(authWebReq.downloadHandler.data);
                    }
                    catch (JsonException ex)
                    {
                        L.LogError($"Invalid JWT json{Environment.NewLine}{authWebReq.downloadHandler.text}");
                        L.LogError(ex);
                        return AudioConvertResult.Unauthorized;
                    }
                    catch (ArgumentException ex) // includes SecurityTokenMalformedException
                    {
                        L.LogError($"Invalid JWT{Environment.NewLine}{authWebReq.downloadHandler.text}");
                        L.LogError(ex);
                        return AudioConvertResult.Unauthorized;
                    }

                    _tokenStr = "Bearer " + jwt;
                    _lastAuthenticate = DateTime.UtcNow;
                    return AudioConvertResult.Success;

                case UnityWebRequest.Result.ConnectionError:
                    L.LogWarning($"Failed to get auth token for audio conversion API. Try {i + 1}/{tries}.");
                    await UniTask.Delay(1000, cancellationToken: token);
                    break;

                default:
                    L.LogWarning($"Failed to get auth token for audio conversion API. Result: {authWebReq.result}, {authWebReq.responseCode}, \"{authWebReq.error}\".");
                    return AudioConvertResult.Unauthorized;
            }
        }

        return AudioConvertResult.ConnectionError;
    }
    public byte[] CreateMultipartPacket(AudioRecordPlayerComponent component)
    {
        if (component.PacketCount == 0)
            return Array.Empty<byte>();

        AudioBatchSectionInfo[] sections =
        [
            new AudioBatchSectionInfo(0, component)
        ];

        return CreateMultipartPacket(component.Packets, sections);
    }
    public byte[] CreateMultipartPacket(IReadOnlyList<PacketInfo> segments, ReadOnlySpan<AudioBatchSectionInfo> sections)
    {
        if (segments.Count == 0 || sections.Length == 0)
            return Array.Empty<byte>();

        ReadOnlySpan<byte> contentSize = "Content-Length: "u8;
        ReadOnlySpan<byte> newLine = "\r\n"u8;
        ReadOnlySpan<byte> doubleDash = "--"u8;
        ReadOnlySpan<byte> contentDisp = "Content-Disposition: form-data; name=\"packet_"u8;
        ReadOnlySpan<byte> fileName = "\"; filename=\""u8;
        ReadOnlySpan<byte> contentType = ".bin\"\r\nContent-Type: application/octet-stream\r\n"u8;

        Span<byte> boundary = stackalloc byte[40];
        for (int index = 0; index < 40; ++index)
        {
            int num = _boundaryGenerator.Next(48, 110);
            if (num > 57) num += 7;
            if (num > 90) num += 6;
            boundary[index] = (byte)num;
        }

        int size = (2 + 2 + boundary.Length + 2 + 45 + 13 + 47 + 2 + 16 + 2) * segments.Count + /* end */ 4 + 2 + boundary.Length;
        int section = 0;
        int maxSize = 0;
        for (int i = 0; i < segments.Count; ++i)
        {
            while (section != sections.Length - 1 && sections[section + 1].StartIndex <= i)
            {
                ++section;
            }

            AudioRecordPlayerComponent comp = sections[section].Component;
            int stInd = segments[i].StartIndex;
            int endInd = i == segments.Count - 1 ? (comp.StartIndex + comp.ByteCount) % comp.InternalBuffer.Length : segments[i + 1].StartIndex;
            int packetSize = endInd < stInd ? comp.InternalBuffer.Length - stInd + endInd : endInd - stInd;
            size += packetSize + F.CountDigits(i) * 2 + F.CountDigits(packetSize);
            if (maxSize < packetSize)
                maxSize = packetSize;
        }

        byte[] resArr = new byte[size];
        Span<byte> result = resArr;
        int pos = 0;
        section = 0;
        Span<byte> numUtf8 = stackalloc byte[Math.Max(F.CountDigits(segments.Count), F.CountDigits(maxSize))];
        for (int i = 0; i < segments.Count; ++i)
        {
            ReadOnlySpan<char> numStr = i.ToString(CultureInfo.InvariantCulture).AsSpan();

            for (int c = 0; c < numStr.Length; ++c)
                numUtf8[c] = (byte)numStr[c];

            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;
            doubleDash.CopyTo(result.Slice(pos));
            pos += doubleDash.Length;
            boundary.CopyTo(result.Slice(pos));
            pos += boundary.Length;
            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;
            contentDisp.CopyTo(result.Slice(pos));
            pos += contentDisp.Length;
            numUtf8.Slice(0, numStr.Length).CopyTo(result.Slice(pos));
            pos += numStr.Length;
            fileName.CopyTo(result.Slice(pos));
            pos += fileName.Length;
            numUtf8.Slice(0, numStr.Length).CopyTo(result.Slice(pos));
            pos += numStr.Length;
            contentType.CopyTo(result.Slice(pos));
            pos += contentType.Length;
            contentSize.CopyTo(result.Slice(pos));
            pos += contentSize.Length;

            while (section != sections.Length - 1 && sections[section + 1].StartIndex <= i)
            {
                ++section;
            }

            byte[] buffer = sections[section].Component.InternalBuffer;

            AudioRecordPlayerComponent comp = sections[section].Component;
            int stInd = segments[i].StartIndex;
            int endInd = i == segments.Count - 1 ? (comp.StartIndex + comp.ByteCount) % comp.InternalBuffer.Length : segments[i + 1].StartIndex;
            int packetSize = endInd < stInd ? comp.InternalBuffer.Length - stInd + endInd : endInd - stInd;

            numStr = packetSize.ToString(CultureInfo.InvariantCulture).AsSpan();

            for (int c = 0; c < numStr.Length; ++c)
                numUtf8[c] = (byte)numStr[c];

            numUtf8.Slice(0, numStr.Length).CopyTo(result.Slice(pos));
            pos += numStr.Length;
            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;
            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;

            if (stInd > endInd)
            {
                buffer.AsSpan(stInd).CopyTo(result.Slice(pos));
                pos += buffer.Length - stInd;
                buffer.AsSpan(0, endInd).CopyTo(result.Slice(pos));
                pos += endInd;
            }
            else
            {
                buffer.AsSpan(stInd, packetSize).CopyTo(result.Slice(pos));
                pos += packetSize;
            }
        }

        newLine.CopyTo(result.Slice(pos));
        pos += newLine.Length;
        doubleDash.CopyTo(result.Slice(pos));
        pos += doubleDash.Length;
        boundary.CopyTo(result.Slice(pos));
        pos += boundary.Length;
        doubleDash.CopyTo(result.Slice(pos));
#if DEBUG
        if (pos + doubleDash.Length != resArr.Length)
            L.LogWarning($"Multipart data not equal lengths: {pos} instead of {resArr.Length}.");
#endif
        return resArr;
    }

    private static void OnVoiceActivity(PlayerVoice voice, ArraySegment<byte> data)
    {
        L.LogDebug(data.Count + " B received.");

        UCPlayer? player = UCPlayer.FromPlayer(voice.player);
        if (player == null)
            return;

        AudioRecordPlayerComponent.Get(player)?.AppendPacket(data);
    }

    [UsedImplicitly]
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerVoice), "ReceiveVoiceChatRelay")]
    private static IEnumerable<CodeInstruction> TranspileReceiveVoiceChatRelay(IEnumerable<CodeInstruction> instructions, MethodBase method)
    {
        MethodInfo? readbytesPtr = typeof(NetPakReader)
                                   .GetMethod(nameof(NetPakReader.ReadBytesPtr), BindingFlags.Instance | BindingFlags.Public);
        if (readbytesPtr == null)
        {
            L.LogWarning($"{method.FullDescription()} - Failed to find NetPakReader.ReadBytesPtr(int, out byte[], out int).");
            return instructions;
        }

        ConstructorInfo? arrSegmentCtor = typeof(ArraySegment<byte>)
                                         .GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [typeof(byte[]), typeof(int), typeof(int)], null);
        if (arrSegmentCtor == null)
        {
            L.LogWarning($"{method.FullDescription()} - Failed to find ArraySegment<byte>(byte[], int, int).");
            return instructions;
        }

        FieldInfo? rpcField = typeof(PlayerVoice).GetField("SendPlayVoiceChat", BindingFlags.NonPublic | BindingFlags.Static);
        if (readbytesPtr == null)
        {
            L.LogWarning($"{method.FullDescription()} - Failed to find PlayerVoice.SendPlayVoiceChat.");
            return instructions;
        }

        MethodInfo invokeTarget = new Action<PlayerVoice, ArraySegment<byte>>(OnVoiceActivity).Method;

        List<CodeInstruction> ins = [.. instructions];

        int readCallPos = -1;
        bool success = false;
        for (int i = 0; i < ins.Count; ++i)
        {
            CodeInstruction opcode = ins[i];
            // find ReadBytesPtr call
            if (readCallPos == -1 && opcode.Calls(readbytesPtr))
            {
                readCallPos = i;
                if (i == ins.Count - 1)
                    break;
            }
            // find target of branch statement
            else if (readCallPos != -1 && opcode.LoadsField(rpcField))
            {
                // locals are stored in display class since they're used in a lambda function.

                // this can either be int or LocalBuilder
                object? displayClassLocal = null;

                FieldInfo? byteArrLocal = null,
                           offsetLocal = null,
                           lengthLocal = null;

                // find display class local and fields in the display class
                for (int j = readCallPos - 6; j < readCallPos; ++j)
                {
                    CodeInstruction backtrackOpcode = ins[j];
                    if (backtrackOpcode.IsLdloc())
                    {
                        if (backtrackOpcode.opcode == OpCodes.Ldloc_0)
                            displayClassLocal = 0;
                        else if (backtrackOpcode.opcode == OpCodes.Ldloc_1)
                            displayClassLocal = 1;
                        else if (backtrackOpcode.opcode == OpCodes.Ldloc_2)
                            displayClassLocal = 2;
                        else if (backtrackOpcode.opcode == OpCodes.Ldloc_3)
                            displayClassLocal = 3;
                        else
                            displayClassLocal = (LocalBuilder)backtrackOpcode.operand;
                    }
                    else if (backtrackOpcode.opcode == OpCodes.Ldfld || backtrackOpcode.opcode == OpCodes.Ldflda)
                    {
                        FieldInfo loadedField = (FieldInfo)backtrackOpcode.operand;
                        if (lengthLocal == null)
                            lengthLocal = loadedField;
                        else if (byteArrLocal == null)
                            byteArrLocal = loadedField;
                        else if (offsetLocal == null)
                            offsetLocal = loadedField;
                    }
                }

                if (displayClassLocal == null || byteArrLocal == null || offsetLocal == null || lengthLocal == null)
                {
                    L.LogWarning($"{method.FullDescription()} - Failed to discover local fields or display class local." +
                                                   $"disp class: {displayClassLocal != null}, byte arr: {byteArrLocal != null}," +
                                                   $"offset: {offsetLocal != null}, length: {lengthLocal != null}.");
                    break;
                }

                CodeInstruction startInstruction = new CodeInstruction(
                    displayClassLocal switch
                    {
                        int index => index switch
                        {
                            0 => OpCodes.Ldloc_0,
                            1 => OpCodes.Ldloc_1,
                            2 => OpCodes.Ldloc_2,
                            _ => OpCodes.Ldloc_3
                        },
                        _ => OpCodes.Ldloc
                    }, displayClassLocal is LocalBuilder ? displayClassLocal : null);
                ins.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                ins.Insert(i + 1, startInstruction);
                ins.Insert(i + 2, new CodeInstruction(OpCodes.Ldfld, byteArrLocal));
                ins.Insert(i + 3, new CodeInstruction(startInstruction.opcode, startInstruction.operand));
                ins.Insert(i + 4, new CodeInstruction(OpCodes.Ldfld, offsetLocal));
                ins.Insert(i + 5, new CodeInstruction(startInstruction.opcode, startInstruction.operand));
                ins.Insert(i + 6, new CodeInstruction(OpCodes.Ldfld, lengthLocal));

                ins.Insert(i + 7, new CodeInstruction(OpCodes.Newobj, arrSegmentCtor));
                ins.Insert(i + 8, new CodeInstruction(OpCodes.Call, invokeTarget));
                success = true;
                break;
            }
        }

        L.Log($"{method.FullDescription()} - Patched incoming voice data.");

        if (success)
            return ins;

        L.LogWarning($"{method.FullDescription()} - Failed to patch voice to copy voice data for recording.");
        return instructions;
    }

    public enum AudioConvertResult
    {
        Success,
        InvalidFormat,
        ConnectionError,
        Unauthorized,
        UnknownError,
        NoData
    }
}

public struct AudioBatchSectionInfo
{
    public readonly int StartIndex;
    public readonly AudioRecordPlayerComponent Component;
    public AudioBatchSectionInfo(int startIndex, AudioRecordPlayerComponent component)
    {
        StartIndex = startIndex;
        Component = component;
    }
}