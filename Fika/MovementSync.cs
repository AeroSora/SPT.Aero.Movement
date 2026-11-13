using System;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Components;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using UnityEngine;

namespace Aero.Movement.Fika
{
    public static class MovementSync
    {
        public static void Init()
        {
            FikaEventDispatcher.SubscribeEvent(new Action<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated));
            Plugin.FikaKick = BroadcastKick;
            Plugin.FikaVault = BroadcastVault;
            Plugin.FikaDoor = BroadcastDoor;
            Diag.Log("[Fika] movement sync initialized");
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent e)
        {
            e.Manager.RegisterPacket<MovementSyncPacket, NetPeer>(new Action<MovementSyncPacket, NetPeer>(OnReceive));
            Diag.Log("[Fika] movement packet registered");
        }

        public static void BroadcastKick()
        {
            try
            {
                var nm = Singleton<IFikaNetworkManager>.Instance;
                if (nm == null) return;
                if (!CoopHandler.TryGetCoopHandler(out var ch) || ch.MyPlayer == null) return;

                var pkt = new MovementSyncPacket { NetId = ch.MyPlayer.NetId, Type = MovementSyncPacket.TypeKick };
                nm.SendData(ref pkt, DeliveryMethod.ReliableOrdered, true);
            }
            catch (Exception ex) { Plugin.Log.LogError("[Fika] BroadcastKick failed: " + ex.Message); }
        }

        public static void BroadcastVault(bool isVault)
        {
            try
            {
                var nm = Singleton<IFikaNetworkManager>.Instance;
                if (nm == null) return;
                if (!CoopHandler.TryGetCoopHandler(out var ch) || ch.MyPlayer == null) return;

                var pkt = new MovementSyncPacket { NetId = ch.MyPlayer.NetId, Type = MovementSyncPacket.TypeVault, Reverse = isVault };
                nm.SendData(ref pkt, DeliveryMethod.ReliableOrdered, true);
            }
            catch (Exception ex) { Plugin.Log.LogError("[Fika] BroadcastVault failed: " + ex.Message); }
        }

        public static void BroadcastDoor(string doorId, Vector3 kickerPosition, bool reverse)
        {
            try
            {
                var nm = Singleton<IFikaNetworkManager>.Instance;
                if (nm == null) return;
                if (!CoopHandler.TryGetCoopHandler(out var ch) || ch.MyPlayer == null) return;

                var pkt = new MovementSyncPacket
                {
                    NetId = ch.MyPlayer.NetId,
                    Type = MovementSyncPacket.TypeDoor,
                    DoorId = doorId,
                    KickerPosition = kickerPosition,
                    Reverse = reverse,
                };
                nm.SendData(ref pkt, DeliveryMethod.ReliableOrdered, true);
                Diag.Log($"[Fika] TX door '{doorId}' reverse={reverse}");
            }
            catch (Exception ex) { Plugin.Log.LogError("[Fika] BroadcastDoor failed: " + ex.Message); }
        }

        private static void OnReceive(MovementSyncPacket pkt, NetPeer peer)
        {
            try
            {
                if (!CoopHandler.TryGetCoopHandler(out var ch)) return;
                if (ch.MyPlayer != null && pkt.NetId == ch.MyPlayer.NetId) return;

                if (pkt.Type == MovementSyncPacket.TypeKick)
                {
                    if (!ch.Players.TryGetValue(pkt.NetId, out var player) || player == null) return;
                    Doors.Kick.PlayOnObserved(player);
                    Diag.Log($"[Fika] RX kick from '{player.Profile?.Nickname}'");
                }
                else if (pkt.Type == MovementSyncPacket.TypeVault)
                {
                    if (!ch.Players.TryGetValue(pkt.NetId, out var player) || player == null) return;
                    Climb.MidairVault.PlayOnObserved(player, pkt.Reverse);
                    Diag.Log($"[Fika] RX vault (isVault={pkt.Reverse}) from '{player.Profile?.Nickname}'");
                }
                else if (pkt.Type == MovementSyncPacket.TypeDoor)
                {
                    Door door = Singleton<GameWorld>.Instance?.FindDoor(pkt.DoorId) as Door;
                    if (door == null)
                    {
                        Diag.Warn($"[Fika] RX door '{pkt.DoorId}' — not found");
                        return;
                    }
                    if (pkt.Reverse)
                        Doors.DoorBreacher.ReverseKickOpen(door, pkt.KickerPosition);
                    else
                        door.KickOpen(pkt.KickerPosition);
                    Diag.Log($"[Fika] RX door '{pkt.DoorId}' reverse={pkt.Reverse} — replayed");
                }
            }
            catch (Exception ex) { Plugin.Log.LogError("[Fika] OnReceive failed: " + ex.Message); }
        }
    }
}
