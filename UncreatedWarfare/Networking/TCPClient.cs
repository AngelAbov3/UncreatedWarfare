﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Uncreated.Warfare.Stats;

namespace Uncreated.Networking
{
    public static class Server
    {
        public static void SendPlayerJoined(Players.FPlayerName player)
        {
            //TCPClient.I.SendMessageAsync(player.GetBytes().Callify(ECall.PLAYER_JOINED));
        }
        public static void SendPlayerLeft(Players.FPlayerName player)
        {
            //TCPClient.I.SendMessageAsync(player.GetBytes().Callify(ECall.PLAYER_LEFT));
        }
        public static void SendPlayerList(List<Players.FPlayerName> players)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < players.Count; i++)
            {
                bytes.AddRange(players[i].GetBytes());
            }
            TCPClient.I.SendMessageAsync(bytes.ToArray().Callify(ECall.PLAYER_LIST));
        }
        public static void SendPlayerOnDuty(ulong player, bool intern)
        {
            byte[] rtn = new byte[sizeof(ulong) + 1];
            rtn[0] = intern.ToByte();
            Array.Copy(BitConverter.GetBytes(player), 0, rtn, 1, sizeof(ulong));
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.ON_DUTY));
        }
        public static void SendPlayerOffDuty(ulong player, bool intern)
        {
            byte[] rtn = new byte[sizeof(ulong) + 1];
            rtn[0] = intern.ToByte();
            Array.Copy(BitConverter.GetBytes(player), 0, rtn, 1, sizeof(ulong));
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.OFF_DUTY));
        }
        public static void LogPlayerBanned(ulong violator, ulong admin_id, string reason, uint duration, DateTime time)
        {
            byte[] r = Encoding.UTF8.GetBytes(reason);
            byte[] rl = BitConverter.GetBytes((ushort)r.Length);
            byte[] d = BitConverter.GetBytes(duration);
            byte[] v = BitConverter.GetBytes(violator);
            byte[] a = BitConverter.GetBytes(admin_id);
            byte[] t = BitConverter.GetBytes(time.Ticks);
            byte[] rtn = new byte[v.Length + a.Length + d.Length + t.Length + rl.Length + r.Length];
            Array.Copy(v, 0, rtn, 0, v.Length);
            int i = v.Length;
            Array.Copy(a, 0, rtn, i, a.Length);
            i += a.Length;
            Array.Copy(d, 0, rtn, i, d.Length);
            i += d.Length;
            Array.Copy(t, 0, rtn, i, t.Length);
            i += t.Length;
            Array.Copy(rl, 0, rtn, i, rl.Length);
            i += rl.Length;
            Array.Copy(r, 0, rtn, i, r.Length);
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.LOG_BAN));
        }
        public static void LogPlayerKicked(ulong violator, ulong admin_id, string reason, DateTime time)
        {
            byte[] r = Encoding.UTF8.GetBytes(reason);
            byte[] rl = BitConverter.GetBytes((ushort)r.Length);
            byte[] v = BitConverter.GetBytes(violator);
            byte[] a = BitConverter.GetBytes(admin_id);
            byte[] t = BitConverter.GetBytes(time.Ticks);
            byte[] rtn = new byte[v.Length + a.Length + t.Length + rl.Length + r.Length];
            Array.Copy(v, 0, rtn, 0, v.Length);
            int i = v.Length;
            Array.Copy(a, 0, rtn, i, a.Length);
            i += a.Length;
            Array.Copy(t, 0, rtn, i, t.Length);
            i += t.Length;
            Array.Copy(rl, 0, rtn, i, rl.Length);
            i += rl.Length;
            Array.Copy(r, 0, rtn, i, r.Length);
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.LOG_KICK));
        }
        public static void LogPlayerBattleyeKicked(ulong violator, string reason, DateTime time)
        {
            byte[] r = Encoding.UTF8.GetBytes(reason);
            byte[] rl = BitConverter.GetBytes((ushort)r.Length);
            byte[] v = BitConverter.GetBytes(violator);
            byte[] t = BitConverter.GetBytes(time.Ticks);
            byte[] rtn = new byte[v.Length + t.Length + rl.Length + r.Length];
            Array.Copy(v, 0, rtn, 0, v.Length);
            int i = v.Length;
            Array.Copy(t, 0, rtn, i, t.Length);
            i += t.Length;
            Array.Copy(rl, 0, rtn, i, rl.Length);
            i += rl.Length;
            Array.Copy(r, 0, rtn, i, r.Length);
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.LOG_BATTLEYEKICK));
        }
        public static void LogPlayerTeamkilled(ulong violator, ulong dead, ulong landmine_assoc, string death_cause, DateTime time)
        {
            byte[] r = Encoding.UTF8.GetBytes(death_cause);
            byte[] rl = BitConverter.GetBytes((ushort)r.Length);
            byte[] v = BitConverter.GetBytes(violator);
            byte[] t = BitConverter.GetBytes(time.Ticks);
            byte[] d = BitConverter.GetBytes(dead);
            byte[] l = BitConverter.GetBytes(landmine_assoc);
            byte[] rtn = new byte[v.Length + d.Length + l.Length + t.Length + rl.Length + r.Length];
            Array.Copy(v, 0, rtn, 0, v.Length);
            int i = v.Length;
            Array.Copy(d, 0, rtn, 0, d.Length);
            i += d.Length;
            Array.Copy(l, 0, rtn, 0, l.Length);
            i += l.Length;
            Array.Copy(t, 0, rtn, i, t.Length);
            i += t.Length;
            Array.Copy(rl, 0, rtn, i, rl.Length);
            i += rl.Length;
            Array.Copy(r, 0, rtn, i, r.Length);
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.LOG_TEAMKILL));
        }
        internal static void ProcessResponse(object sender, ReceivedServerMessageArgs e)
        {
            throw new NotImplementedException();
        }

        public static void LogPlayerUnbanned(ulong pardoned, ulong admin_id, DateTime time)
        {
            byte[] p = BitConverter.GetBytes(pardoned);
            byte[] a = BitConverter.GetBytes(admin_id);
            byte[] t = BitConverter.GetBytes(time.Ticks);
            byte[] rtn = new byte[p.Length + a.Length + t.Length];
            Array.Copy(p, 0, rtn, 0, p.Length);
            int i = p.Length;
            Array.Copy(a, 0, rtn, i, a.Length);
            i += a.Length;
            Array.Copy(t, 0, rtn, i, t.Length);
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.LOG_UNBAN));
        }
        public static void LogPlayerWarned(ulong violator, ulong admin_id, string reason, DateTime time)
        {
            byte[] r = Encoding.UTF8.GetBytes(reason);
            byte[] rl = BitConverter.GetBytes((ushort)r.Length);
            byte[] v = BitConverter.GetBytes(violator);
            byte[] a = BitConverter.GetBytes(admin_id);
            byte[] t = BitConverter.GetBytes(time.Ticks);
            byte[] rtn = new byte[v.Length + a.Length + t.Length + rl.Length + r.Length];
            Array.Copy(v, 0, rtn, 0, v.Length);
            int i = v.Length;
            Array.Copy(a, 0, rtn, i, a.Length);
            i += a.Length;
            Array.Copy(t, 0, rtn, i, t.Length);
            i += t.Length;
            Array.Copy(rl, 0, rtn, i, rl.Length);
            i += rl.Length;
            Array.Copy(r, 0, rtn, i, r.Length);
            TCPClient.I.SendMessageAsync(rtn.Callify(ECall.LOG_WARNING));
        }
        public static void SendStartupStep(EStartupStep step) =>
            TCPClient.I.SendMessageAsync(new byte[1] { (byte)step }.Callify(ECall.SERVER_STARTING_UP));
        public static void SendGracefulShutdown() => TCPClient.I.SendMessageAsync(new byte[1] { (byte)ECall.SERVER_SHUTTING_DOWN });
    }
    public class ReceivedServerMessageArgs : EventArgs
    {
        public byte[] message;
        public ReceivedServerMessageArgs(byte[] message)
        {
            this.message = message;
        }
    }
    public class TCPClient : IDisposable
    {
        public static TCPClient I;
        public const int BufferSize = 4096;
        public string IP = "127.0.0.1";
        public int LocalID = 0;
        public ushort Port = 31902;
        public event EventHandler<ReceivedServerMessageArgs> OnReceivedData;
        public ClientConnection connection;
        public TCPClient(string ip, ushort port)
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Warfare.F.LogWarning("Connection already established, resetting.", ConsoleColor.DarkYellow);
                I.Shutdown();
                GC.SuppressFinalize(I);
                I = this;
            }
            this.IP = ip;
            this.Port = port;
            this.connection = new ClientConnection(this);
        }
        public bool Connect()
        {
            if (connection != null)
            {
                connection.Connect();
                return true;
            }
            else return false;
        }
        public void Shutdown()
        {
            Warfare.F.Log("Shutting down", ConsoleColor.Magenta);
            connection.socket.Close();
            connection.socket.Dispose();
        }
        public void SendMessageAsync(byte[] message) => connection?.SendMessage(message);
        internal void ReceiveData(byte[] data) => OnReceivedData?.Invoke(this, new ReceivedServerMessageArgs(data));
        public class ClientConnection
        {
            public TcpClient socket;
            private TCPClient _owner;
            private NetworkStream stream;
            private byte[] _buffer;
            public ClientConnection(TCPClient owner)
            {
                this._owner = owner;
            }
            int connection_tries = 0;
            const int max_connection_tries = 10;
            public void Connect(bool first = true)
            {
                if (first) connection_tries = 0;
                if (socket != null)
                {
                    try
                    {
                        socket.Close();
                        socket.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Warfare.F.LogError(ex);
                    }
                }
                socket = new TcpClient()
                {
                    ReceiveBufferSize = BufferSize,
                    SendBufferSize = BufferSize
                };
                _buffer = new byte[BufferSize];
                socket.BeginConnect(_owner.IP, _owner.Port, Connected, socket);
            }

            private void Connected(IAsyncResult ar)
            {
                connection_tries++;
                try
                {
                    socket.EndConnect(ar);
                    Warfare.F.Log($"Connected to {socket.Client.RemoteEndPoint}.", ConsoleColor.DarkYellow);
                }
                catch (SocketException)
                {
                    if (connection_tries <= max_connection_tries)
                    {
                        Warfare.F.LogWarning($"Unable to connect, retrying ({connection_tries}/{max_connection_tries})", ConsoleColor.DarkYellow);
                        Connect(false);
                    }
                    else Warfare.F.LogError($"Unable to connect after {max_connection_tries} tries.", ConsoleColor.Red);

                }
                if (!socket.Connected) return;
                stream = socket.GetStream();
                stream.BeginRead(_buffer, 0, BufferSize, AsyncReceivedData, socket);
            }
            public void SendMessage(byte[] message)
            {
                try
                {
                    if (stream == null) stream = socket.GetStream();
                    stream.BeginWrite(message, 0, message.Length, WriteComplete, socket);
                }
                catch (SocketException)
                {
                    Warfare.F.LogError("Unable to write message.", ConsoleColor.Red);
                }
            }
            protected virtual void WriteComplete(IAsyncResult ar)
            {
                try
                {
                    stream.EndWrite(ar);
                }
                catch (SocketException ex)
                {
                    Warfare.F.LogError(ex, ConsoleColor.Red);
                }
                ar.AsyncWaitHandle.Dispose();
            }

            private void AsyncReceivedData(IAsyncResult ar)
            {
                try
                {
                    int received_bytes_count = stream.EndRead(ar);
                    if (received_bytes_count <= 0) _owner.Shutdown();
                    byte[] received = new byte[received_bytes_count];
                    Array.Copy(_buffer, 0, received, 0, received_bytes_count);
                    _owner.ReceiveData(received);
                    stream.BeginRead(_buffer, 0, BufferSize, AsyncReceivedData, socket);
                }
                catch (SocketException)
                {
                    _owner.Shutdown();
                }
                catch (IOException)
                {
                    _owner.Shutdown();
                }
            }
        }
        public void Dispose()
        {
            this.Shutdown();
            I = null;
            GC.SuppressFinalize(this);
        }
    }
    public enum ECall : ushort
    {
        SERVER_SHUTTING_DOWN = 0,
        SERVER_STARTING_UP = 1,
        PLAYER_LIST = 2,
        PLAYER_JOINED = 3,
        PLAYER_LEFT = 4,
        USERNAME_UPDATED = 5,
        LOG_BAN = 6,
        LOG_KICK = 7,
        LOG_BATTLEYEKICK = 8,
        LOG_TEAMKILL = 9,
        LOG_UNBAN = 10,
        LOG_WARNING = 11,
        ON_DUTY = 12,
        OFF_DUTY = 13,
        INVOKE_BAN = 14,
        INVOKE_KICK = 15,
        INVOKE_WARN = 16,
        INVOKE_UNBAN = 17,
        INVOKE_GIVE_KIT = 18,
        INVOKE_REVOKE_KIT = 19,
        INVOKE_SHUTDOWN = 20,
        INVOKE_SHUTDOWN_AFTER_GAME = 21,
        INVOKE_PROMOTE_OFFICER = 22,
        INVOKE_DEMOTE_OFFICER = 23
    }
    public enum EStartupStep : byte
    {
        LOADING_PLUGIN = 0,
        PLUGINS_LOADED = 1,
        LEVEL_LOADED = 2
    }
}
