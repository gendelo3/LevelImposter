using HarmonyLib;
using UnityEngine;
using LevelImposter.Core;
using LevelImposter.Shop;

namespace LevelImposter.Core
{
    /*
     *      Gives credit to map makers
     *      through the Ping Tracker in
     *      the top right corner.
     */
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    public static class PingPatch
    {
        public static void Postfix(PingTracker __instance)
        {
            LIMap currentMap = MapLoader.CurrentMap;
            if (currentMap != null)
            {
                if (currentMap.properties.showPingIndicator == false)
                    return;
                if (!__instance.gameObject.active)
                    __instance.gameObject.SetActive(true);

                __instance.text.text += "\n<color=#1a95d8>" + currentMap.name + " \n";
                if (!string.IsNullOrEmpty(currentMap.authorID))
                    __instance.text.text += "<size=2>by " + currentMap.authorName + "</size>";
                else
                    __instance.text.text += "<size=2><i>(Freeplay Only)</i></size></color>";
            }
            
        }
    }
}
