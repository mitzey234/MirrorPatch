using HarmonyLib;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MirrorPatch.Utils
{
    //For some reason its possible to make the server disconnect itself with invalid data, this patch stops that from being a thing
    [HarmonyPatch(typeof(NetworkClient), nameof(NetworkClient.OnTransportData))]
    internal class TransportReceivePatch
    {
        public static bool Prefix(ArraySegment<byte> data, int channelId, NetworkConnection __instance)
        {
            if (NetworkClient.connection != null)
            {
                if (!NetworkClient.unbatcher.AddBatch(data))
                {
                    Debug.LogWarning("NetworkClient: failed to add batch, disconnecting.");
                    if (__instance.identity != null && __instance.identity.isServer)
                    {
                        Log.Debug("NetworkClient ERROR (unbatcher.AddBatch): " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                        return false;
                    }
                    NetworkClient.connection.Disconnect();
                    return false;
                }
                NetworkReader networkReader;
                double num;
                while (!NetworkClient.isLoadingScene && NetworkClient.unbatcher.GetNextMessage(out networkReader, out num))
                {
                    if (networkReader.Remaining < 2)
                    {
                        Debug.LogWarning("NetworkClient: received Message was too short (messages should start with message id)");
                        if (__instance.identity != null && __instance.identity.isServer)
                        {
                            Log.Debug("NetworkClient ERROR (unbatcher.GetNextMessage): " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                            return false;
                        }
                        NetworkClient.connection.Disconnect();
                        return false;
                    }
                    NetworkClient.connection.remoteTimeStamp = num;
                    if (!NetworkClient.UnpackAndInvoke(networkReader, channelId))
                    {
                        Debug.LogWarning("NetworkClient: failed to unpack and invoke message. Disconnecting.");
                        if (__instance.identity != null && __instance.identity.isServer)
                        {
                            Log.Debug("NetworkClient ERROR (unbatcher.UnpackAndInvoke): " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                            return false;
                        }
                        NetworkClient.connection.Disconnect();
                        return false;
                    }
                }
                if (!NetworkClient.isLoadingScene && NetworkClient.unbatcher.BatchesCount > 0)
                {
                    Debug.LogError(string.Format("Still had {0} batches remaining after processing, even though processing was not interrupted by a scene change. This should never happen, as it would cause ever growing batches.\nPossible reasons:\n* A message didn't deserialize as much as it serialized\n*There was no message handler for a message id, so the reader wasn't read until the end.", NetworkClient.unbatcher.BatchesCount));
                    return false;
                }
            }
            else
            {
                Debug.LogError("Skipped Data message handling because connection is null.");
            }
            return false;
        }
    }
}