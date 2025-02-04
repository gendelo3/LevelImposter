using HarmonyLib;
using LevelImposter.DB;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace LevelImposter.Core
{
    class AmbientSoundBuilder : IElemBuilder
    {
        public void Build(LIElement elem, GameObject obj)
        {
            if (elem.type != "util-sound1" && elem.type != "util-triggersound")
                return;

            // Colliders
            Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                collider.isTrigger = true;
            }

            // AudioClip
            if (elem.properties.sounds == null)
            {
                LILogger.Warn($"{elem.name} missing audio listing");
                return;
            }

            if (elem.properties.sounds.Length <= 0)
            {
                LILogger.Warn($"{elem.name} missing audio elements");
                return;
            }

            // Sound Data
            LISound soundData = elem.properties.sounds[0];
            if (soundData.data == null)
            {
                LILogger.Warn($"{elem.name} missing audio data");
                return;
            }

            // Sound Player
            AmbientSoundPlayer ambientPlayer = obj.AddComponent<AmbientSoundPlayer>();
            ambientPlayer.HitAreas = colliders;
            ambientPlayer.MaxVolume = soundData.volume;
            obj.SetActive(false);

            // WAVLoader
            WAVLoader.Instance?.LoadWAV(elem, soundData, (AudioClip audioClip) =>
            {
                ambientPlayer.AmbientSound = audioClip;
                if (elem.type != "util-triggersound")
                    obj.SetActive(true);
            });
        }

        public void PostBuild() { }
    }
}