using HarmonyLib;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BrightPlugin.Utils
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
                    Debug.LogWarning((object) "NetworkClient: failed to add batch, disconnecting.");
                    if (__instance.identity != null && __instance.identity.isServer)
                    {
                        Log.Debug("NetworkClient ERROR (unbatcher.AddBatch): " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                        return false;
                    }
                    NetworkClient.connection.Disconnect();
                }
                else
                {
                    NetworkReader message;
                    double remoteTimeStamp;
                    while (!NetworkClient.isLoadingScene && NetworkClient.unbatcher.GetNextMessage(out message, out remoteTimeStamp))
                    {
                        if (message.Remaining >= 2)
                        {
                            NetworkClient.connection.remoteTimeStamp = remoteTimeStamp;
                            if (!NetworkClient.UnpackAndInvoke(message, channelId))
                            {
                                Debug.LogWarning((object) "NetworkClient: failed to unpack and invoke message. Disconnecting.");
                                if (__instance.identity != null && __instance.identity.isServer)
                                {
                                    Log.Debug("NetworkClient ERROR (UnpackAndInvoke): " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                                    return false;
                                }
                                NetworkClient.connection.Disconnect();
                                return false;
                            }
                        }
                        else
                        {
                            if (__instance.identity != null && __instance.identity.isServer) return false;
                            Debug.LogWarning((object) "NetworkClient: received Message was too short (messages should start with message id)");
                            if (__instance.identity != null && __instance.identity.isServer)
                            {
                                Log.Debug("NetworkClient ERROR (message.Remaining): " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                                return false;
                            }
                            NetworkClient.connection.Disconnect();
                            return false;
                        }
                    }
                    if (NetworkClient.isLoadingScene || NetworkClient.unbatcher.BatchesCount <= 0) return false;
                    Debug.LogError((object) string.Format("Still had {0} batches remaining after processing, even though processing was not interrupted by a scene change. This should never happen, as it would cause ever growing batches.\nPossible reasons:\n* A message didn't deserialize as much as it serialized\n*There was no message handler for a message id, so the reader wasn't read until the end.", (object) NetworkClient.unbatcher.BatchesCount));
                }
            }
            else
                Debug.LogError((object) "Skipped Data message handling because connection is null.");

            return false;
        }
    }
}