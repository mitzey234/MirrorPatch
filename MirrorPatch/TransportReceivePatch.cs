using HarmonyLib;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightPlugin.Utils
{
    //For some reaosn its possible to make the server disconnect itself with invalid data, this patch stops that from being a thing
    [HarmonyPatch(typeof(NetworkConnection), nameof(NetworkConnection.TransportReceive))]
    internal class TransportReceivePatch
    {
        public static bool Prefix(ArraySegment<byte> buffer, int channelId, NetworkConnection __instance)
        {
            if (buffer.Count < 2)
            {
                if (__instance.identity != null && __instance.identity.isServer) return false;
                UnityEngine.Debug.LogError(string.Format("ConnectionRecv {0} Message was too short (messages should start with message id)", __instance));
                Log.Debug("Caught dumb shit: " + __instance.address + " - " + (__instance.identity != null ? __instance.identity.name + " - " + __instance.identity.isServer : "(null)") + __instance.connectionId + " - " + __instance.isAuthenticated);
                __instance.Disconnect();
                return false;
            }
            using (PooledNetworkReader reader = NetworkReaderPool.GetReader(buffer))
            {
                while (reader.Position < reader.Length)
                {
                    if (!__instance.UnpackAndInvoke(reader, channelId))
                    {
                        break;
                    }
                }
            }
            return false;
        }
    }
}
