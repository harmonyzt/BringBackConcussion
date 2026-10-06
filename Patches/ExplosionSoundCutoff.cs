using System.Collections;
using System.Reflection;
using Comfort.Common;
using HarmonyLib;
using UnityEngine;

namespace BringBackConcussion.Patches
{
    /// <summary>
    /// Cuts off world sound based on explosion proximity and fades it back in afterwards.
    /// We use a tinnitus here to reroute to Master groupe (coroutine), because if we don't do that, our sound will get fucked after tinnitus
    /// </summary>
    internal static class ExplosionSoundCutoff
    {
        private static Plugin _host;
        private static Coroutine _activeRoutine;
        private static FieldInfo _tinnitusEndTimeField;

        // Captured before our cut, restored on new explosion
        private static string[] _lastKeys;
        private static float[] _lastStartDb;

        // 0 = edge of the radius
        // 1 = epicenter
        internal static void Trigger(float closeness)
        {
            if (closeness <= 0.05f)
                return;

            if (Plugin.Instance == null)
                return;

            if (!Singleton<BetterAudio>.Instantiated)
                return;

            BetterAudio betterAudio = Singleton<BetterAudio>.Instance;
            if (betterAudio == null || betterAudio.Master == null || betterAudio.AudioMixerData == null)
                return;

            // A new explosion already?
            // hand the mixer back the levels it had, then recapture
            if (_activeRoutine != null && _host != null)
            {
                _host.StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }

            RestoreLastStart(betterAudio);

            _host = Plugin.Instance;
            _activeRoutine = _host.StartCoroutine(CutoffRoutine(betterAudio, closeness));
        }

        private static IEnumerator CutoffRoutine(BetterAudio betterAudio, float closeness)
        {
            Audio.Data.AudioMixerDataContainer data = betterAudio.AudioMixerData;
            string[] keys =
            {
                data.MainMixerVolume,
                data.GunsMixerVolume,
                data.WorldMixerVolume,
                data.AmbientInMixerVolume,
                data.AmbientOutMixerVolume
            };
            
            float endVolumeDb = Mathf.Lerp(0f, -80f, closeness);
            float cutTime = Mathf.Lerp(0.3f, 0.05f, closeness);
            float holdTime = Mathf.Lerp(0.2f, 1.5f, closeness);
            float fadeTime = Plugin.ExplosionSoundFadeTime.Value;
            
            // Values at -80 dB is our work and nothing else writes them so restore them back to normal
            float[] startDb = new float[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                if (betterAudio.Master == null || !betterAudio.Master.GetFloat(keys[i], out startDb[i]))
                {
                    _activeRoutine = null;
                    yield break;
                }

                if (startDb[i] <= -75f)
                    startDb[i] = 0f;
            }

            _lastKeys = keys;
            _lastStartDb = startDb;

            // Cut is here
            float elapsed = 0f;
            while (elapsed < cutTime)
            {
                if (betterAudio == null)
                {
                    _activeRoutine = null;
                    yield break;
                }

                ApplyLevels(betterAudio, keys, startDb, endVolumeDb, cutTime > 0f ? elapsed / cutTime : 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyLevels(betterAudio, keys, startDb, endVolumeDb, 1f);

            // Holding the mute
            // Can be triggered again so we easily reset
            yield return new WaitForSeconds(holdTime);

            // Fade back to levels we captured
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                if (betterAudio == null)
                {
                    _activeRoutine = null;
                    yield break;
                }

                ApplyLevels(betterAudio, keys, startDb, endVolumeDb, fadeTime > 0f ? 1f - elapsed / fadeTime : 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // progress 0 = captured start levels, restore
            ApplyLevels(betterAudio, keys, startDb, endVolumeDb, 0f);

            _lastKeys = null;
            _lastStartDb = null;
            _activeRoutine = null;
        }

        private static void ApplyLevels(BetterAudio betterAudio, string[] keys, float[] startDb, float endVolumeDb, float remaining)
        {
            if (betterAudio.Master == null)
                return;

            bool ringActive = IsTinnitusActive(betterAudio);

            for (int i = 0; i < keys.Length; i++)
            {
                // While the tinnitus is playing, EFT rewrites MainVolume/GunsVolume every frame
                // So we hand them over to our coroutine
                if (ringActive && (i == 0 || i == 1))
                    continue;

                betterAudio.Master.SetFloat(keys[i], Mathf.Lerp(startDb[i], endVolumeDb, remaining));
            }
        }

        private static bool IsTinnitusActive(BetterAudio betterAudio)
        {
            try
            {
                if (_tinnitusEndTimeField == null)
                    _tinnitusEndTimeField = AccessTools.Field(typeof(BetterAudio), "float_2");

                if (_tinnitusEndTimeField == null)
                    return false;

                return (float)_tinnitusEndTimeField.GetValue(betterAudio) > Time.time;
            }
            catch
            {
                return false;
            }
        }

        private static void RestoreLastStart(BetterAudio betterAudio)
        {
            if (_lastKeys == null || _lastStartDb == null || betterAudio == null || betterAudio.Master == null)
                return;

            for (int i = 0; i < _lastKeys.Length && i < _lastStartDb.Length; i++)
            {
                betterAudio.Master.SetFloat(_lastKeys[i], _lastStartDb[i]);
            }

            _lastKeys = null;
            _lastStartDb = null;
        }
    }
}