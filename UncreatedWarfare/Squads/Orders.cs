﻿using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using UnityEngine;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Point;
using SDG.Unturned;
using Uncreated.Warfare.Gamemodes;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using Uncreated.Players;

namespace Uncreated.Warfare.Squads
{
    public static class Orders
    {
        public static List<Order> orders = new List<Order>(16);

        public static Order GiveOrder(Squad squad, UCPlayer commander, EOrder type, Vector3 marker, string message)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            Order order = squad.Leader.Player.gameObject.AddComponent<Order>();
            order.Initialize(squad, commander, type, marker, message);
            orders.Add(order);

            commander.Message("order_s_sent", squad.Name, message);
            foreach (UCPlayer player in squad.Members)
            {
                order.SendUI(player);
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("order_s_received", player, commander.CharacterName, message), EToastMessageSeverity.MEDIUM));
            }

            commander.Player.quests.sendSetMarker(false, marker);

            return order;
        }
        public static bool HasOrder(Squad squad, out Order order)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            order = null;
            if (squad is null) return false;
            return (bool)(squad.Leader.Player.TryGetComponent(out order));
        }
        public static bool CancelOrder(Order order)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            bool success = orders.Remove(order);
            order.Cancel();
            return success;
        }
        public static void OnFOBBunkerBuilt(FOB fob, BuildableComponent buildable)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            foreach (KeyValuePair<ulong, int> pair in buildable.PlayerHits)
            {
                UCPlayer player = UCPlayer.FromID(pair.Key);
                if (player != null &&
                    (float)pair.Value / buildable.Buildable.requiredHits >= 0.1F &&
                    HasOrder(player.Squad, out var order) &&
                    order.Type == EOrder.BUILDFOB &&
                    (fob.Position - order.Marker).sqrMagnitude <= Math.Pow(80, 2)
                )
                {
                    order.Fulfill();
                }
            }
        }
    }

    public class Order : MonoBehaviour
    {
        public UCPlayer Commander { get; private set; }
        public Squad Squad { get; private set; }
        public EOrder Type { get; private set; }
        public Vector3 Marker { get; private set; }
        public string Message { get; private set; }
        public int TimeLeft { get; private set; }
        public string MinutesLeft
        {
            get
            {
                return ((int)Math.Ceiling(TimeLeft / 60F)).ToString();
            }
        }
        public string RewardLevel { get; private set; }
        public int RewardXP { get; private set; }
        public int RewardTW { get; private set; }
        public bool IsActive { get; private set; }
        public Flag Flag { get; private set; }

        private OrderCondition Condition;

        private Coroutine loop;

        internal const short orderKey = 12004;

        public void Initialize(Squad squad, UCPlayer commander, EOrder type, Vector3 marker, string message, Flag flag = null)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            Squad = squad;
            Commander = commander;
            Type = type;
            Marker = marker;
            Message = message;
            Flag = flag;

            switch (Type)
            {
                case EOrder.ATTACK:
                    TimeLeft = 300;
                    RewardXP = 0;
                    RewardTW = 0;
                    break;
                case EOrder.DEFEND:
                    TimeLeft = 420;
                    RewardXP = 0;
                    RewardTW = 0;
                    break;
                case EOrder.BUILDFOB:
                    TimeLeft = 420;
                    RewardXP = 150;
                    RewardTW = 100;
                    break;
                case EOrder.MOVE:
                    TimeLeft = 240;
                    RewardXP = 150;
                    RewardTW = 100;

                    Vector3 avgMemberPoint = Vector3.zero;
                    foreach (var player in Squad.Members)
                        avgMemberPoint += player.Position;

                    avgMemberPoint /= squad.Members.Count;
                    float distanceToMarker = (avgMemberPoint - Marker).magnitude;

                    L.Log("distance to marker: " + distanceToMarker);
                    
                    if (distanceToMarker < 100) { RewardXP = 0; RewardTW = 0;  }
                    if (distanceToMarker >= 100 && distanceToMarker < 200) { RewardXP = 15; RewardTW = 15;  }
                    if (distanceToMarker >= 200 && distanceToMarker < 400) { RewardXP = 50; RewardTW = 50;  }
                    if (distanceToMarker >= 600 && distanceToMarker < 1000) { RewardXP = 70; RewardTW = 70;  }
                    if (distanceToMarker >= 1000) { RewardXP = 90; RewardTW = 90;  }

                    break;
            }

            if (RewardTW < 50) RewardLevel = "Low".Colorize("999999");
            else if (RewardTW >= 50 && RewardTW < 90) RewardLevel = "Medium".Colorize("e0b4a2");
            else if (RewardTW >= 90 && RewardTW < 120) RewardLevel = "High".Colorize("f5dfa6");
            else if (RewardTW >= 120) RewardLevel = "Very High".Colorize("ffe4b3");

            Condition = new OrderCondition(type, squad, marker);
            IsActive = true;

            loop = StartCoroutine(Tick());

        }
        public void Fulfill()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!IsActive) return;

            switch (Type)
            {
                case EOrder.ATTACK:
                    break;
                case EOrder.DEFEND:
                    break;
                case EOrder.BUILDFOB:
                    foreach (var player in Squad.Members)
                    {
                        GiveReward(player);
                        HideUI(player);
                    }
                    break;
                case EOrder.MOVE:

                    foreach (var player in Condition.FullfilledPlayers)
                    {
                        if (player.IsOnline)
                        {
                            GiveReward(player);
                            HideUI(player);
                        }
                    }

                    break;
            }

            if (Commander.IsOnline)
            {
                GiveReward(Commander);
            }

            IsActive = false;
            StartCoroutine(Delete());
        }
        private void GiveReward(UCPlayer player)
        {
            // TODO: colorize toast message
            Points.AwardXP(player, RewardXP, "ORDER FULFILLED".Colorize("a6f5b8"));
            Points.AwardTW(player, RewardTW, "");
        }

        public void Cancel()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!IsActive) return;

            ToastMessage toast = new ToastMessage("ORDER CANCELLED".Colorize("c7b3a5"), EToastMessageSeverity.MINI);

            foreach (UCPlayer player in Squad.Members)
            {
                HideUI(player);
                ToastMessage.QueueMessage(player, toast);
            }

            ToastMessage.QueueMessage(Commander, toast);

            IsActive = false;
            Destroy(this);
        }
        public void TimeOut()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!IsActive) return;

            ToastMessage toast = new ToastMessage("ORDER TIMED OUT".Colorize("c7b3a5"), EToastMessageSeverity.MINI);

            foreach (UCPlayer player in Squad.Members)
            {
                HideUI(player);
                ToastMessage.QueueMessage(player, toast);
            }

            ToastMessage.QueueMessage(Commander, toast);

            IsActive = false;
            Destroy(this);
        }
        public void SendUI(UCPlayer player)
        {
            EffectManager.sendUIEffect(SquadManager.orderID, orderKey, true);
            UpdateUI(player);
        }
        public void UpdateUI(UCPlayer player)
        {
            L.Log("Order UI updated");
            EffectManager.sendUIEffectText(orderKey, player.connection, true, "OrderInfo", $"Orders from <color=#a7becf>{Commander.CharacterName}</color>:");
            EffectManager.sendUIEffectText(orderKey, player.connection, true, "Order", Message);
            EffectManager.sendUIEffectText(orderKey, player.connection, true, "Time", $"- {MinutesLeft}m left");
            EffectManager.sendUIEffectText(orderKey, player.connection, true, "Reward", $"- Reward: {RewardLevel}");
        }
        public void HideUI(UCPlayer player)
        {
             EffectManager.askEffectClearByID(SquadManager.orderID, player.connection);
        }

        public IEnumerator<WaitForSeconds> Tick()
        {
            int counter = 0;
            float tickFrequency = 1;

            while (true)
            {
                IDisposable profiler = ProfilingUtils.StartTracking();
                // every 1 second

                TimeLeft--;

                if (counter % (5 / tickFrequency) == 0) // every 5 seconds
                {
                    if (Type == EOrder.MOVE)
                    {
                        Condition.UpdateData();
                        if (Condition.Check())
                            Fulfill();
                        yield break;
                    }
                }

                if (counter % (30 / tickFrequency) == 0) // every 30 seconds
                {

                }
                if (counter % (60 / tickFrequency) == 0) // every 60 seconds
                {
                    foreach (var player in Squad.Members)
                        UpdateUI(player);
                }


                if (TimeLeft <= 0)
                {
                    TimeOut();
                }

                counter++;
                if (counter >= 60 / tickFrequency)
                    counter = 0;
                profiler.Dispose();
                yield return new WaitForSeconds(tickFrequency);
            }
        }
        public IEnumerator<WaitForSeconds> Delete()
        {
            yield return new WaitForSeconds(20);
            using IDisposable profiler = ProfilingUtils.StartTracking();
            // TODO: Clear UI
            StopCoroutine(loop);
            Destroy(this);
        }
    }


    public struct OrderCondition
    {
        public readonly EOrder Type;
        public readonly Squad Squad;
        public readonly Vector3 Marker;
        public List<UCPlayer> FullfilledPlayers;


        public OrderCondition(EOrder type, Squad squad, Vector3 marker)
        {
            Type = type;
            Squad = squad;
            Marker = marker;
            FullfilledPlayers = new List<UCPlayer>(12);
        }
        public bool Check()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Type == EOrder.MOVE)
            {
                return FullfilledPlayers.Count >= 0.75F * Squad.Members.Count;
            }
            return false;
        }
        public void UpdateData()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Type == EOrder.MOVE)
            {
                foreach (UCPlayer player in Squad.Members)
                {
                    if ((player.Position - Marker).sqrMagnitude <= Math.Pow(40, 2))
                    {
                        if (!FullfilledPlayers.Contains(player))
                            FullfilledPlayers.Add(player);
                    }
                }
            }
        }
    }

    public enum EOrder
    {
        ATTACK,
        DEFEND,
        BUILDFOB,
        MOVE
    }
}
