using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using LevelImposter.Core;
using System.Text.Json;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Attributes;

namespace LevelImposter.DB
{
    /// <summary>
    /// A Singleton GameObject that stores references to
    /// sprites, minigames, and prefabs from other locations
    /// within the Among Us game.
    /// </summary>
    class AssetDB : MonoBehaviour
    {
        public static AssetDB? Instance { get; private set; }

        public static bool IsReady = false;
        public static Dictionary<string, TaskData> Tasks;
        public static Dictionary<string, UtilData> Utils;
        public static Dictionary<string, SabData> Sabs;
        public static Dictionary<string, DecData> Decor;
        public static Dictionary<string, RoomData> Room;
        public static Dictionary<string, SSData> Ships;
        public static Dictionary<string, SoundData> Sounds;

        private string _status = "Initializing AssetDB...";
        public string Status => _status;

        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        public void Start()
        {

            AssetDBTemplate? tempDB = MapUtils.LoadJsonResource<AssetDBTemplate>("AssetDB.json");
            if (tempDB == null)
            {
                LILogger.Warn("Serialized AssetDB was not found in Assembly resources");
                return;
            }

            Tasks = tempDB.tasks;
            Utils = tempDB.utils;
            Sabs = tempDB.sabs;
            Decor = tempDB.dec;
            Room = tempDB.room;
            Ships = tempDB.ss;
            Sounds = tempDB.sounds;
            StartCoroutine(CoLoadAssets().WrapToIl2Cpp());
        }

        [HideFromIl2Cpp]
        private IEnumerator CoLoadAssets()
        {
            {
                _status = "Loading ship references";
                LILogger.Info("Loading AssetDB...");
                for (int i = 0; i < AmongUsClient.Instance.ShipPrefabs.Count; i++)
                {
                    AssetReference shipRef = AmongUsClient.Instance.ShipPrefabs[i];
                    while (true)
                    {
                        if (shipRef.Asset == null)
                        {
                            AsyncOperationHandle op = shipRef.LoadAssetAsync<GameObject>();
                            if (!op.IsValid())
                            {
                                LILogger.Warn($"Could not import [{shipRef.AssetGUID}] due to invalid Async Operation. Trying again in 5 seconds...");
                                yield return new WaitForSeconds(5);
                                continue;
                            }
                            yield return op;
                            if (op.Status != AsyncOperationStatus.Succeeded)
                                LILogger.Warn($"Could not import [{shipRef.AssetGUID}] due to failed Async Operation. Ignoring...");
                        }
                        break;
                    }

                    if (shipRef.Asset != null)
                    {
                        GameObject shipPrefab = shipRef.Asset.Cast<GameObject>();
                        yield return _importPrefab(shipPrefab);
                    }
                    else
                    {
                        LILogger.Warn($"Could not import [{shipRef.AssetGUID}] due to missing Assets. Ignoring...");
                    }
                }
                IsReady = true;
            }
        }

        [HideFromIl2Cpp]
        private IEnumerator _importPrefab(GameObject prefab)
        {
            {
                _status = $"Loading \"{prefab.name}\"...";
                ShipStatus shipStatus = prefab.GetComponent<ShipStatus>();
                MapType mapType = MapType.Skeld;
                if (prefab.name == "AprilShip")
                    yield break;
                if (prefab.name == "MiraShip")
                    mapType = MapType.Mira;
                if (prefab.name == "PolusShip")
                    mapType = MapType.Polus;
                if (prefab.name == "Airship")
                    mapType = MapType.Airship;

                yield return _importAssets(prefab, shipStatus, mapType, Tasks);
                yield return _importAssets(prefab, shipStatus, mapType, Utils);
                yield return _importAssets(prefab, shipStatus, mapType, Sabs);
                yield return _importAssets(prefab, shipStatus, mapType, Decor);
                yield return _importAssets(prefab, shipStatus, mapType, Room);
                yield return _importAssets(prefab, shipStatus, mapType, Ships);
                yield return _importAssets(prefab, shipStatus, mapType, Sounds);

                LILogger.Info("..." + prefab.name + " Loaded");
            }
        }

        [HideFromIl2Cpp]
        private IEnumerator _importAssets<T>(GameObject map, ShipStatus shipStatus, MapType mapType, Dictionary<string, T> list) where T : AssetData
        {
            {
                foreach (var elem in list)
                {
                    if (elem.Value.MapType == mapType)
                    {
                        elem.Value.ImportMap(map, shipStatus);
                        yield return null;
                    }
                }
            }
        }
    }
}