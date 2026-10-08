using System;
using System.Collections;
using HarmonyLib;
using MelonLoader;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using UnityEngine;

[assembly: MelonInfo(typeof(BigMixer.BigMixerMod), "Mixing Station Mk3", "1.0.0", "Ashton")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BigMixer
{
    // Adds the Mixing Station Mk3: a restyled Mk2 that mixes 40 product with 40 ingredients,
    // sold only by Oscar at the warehouse.
    public class BigMixerMod : MelonMod
    {
        public const string ItemId = "mixingstationmk3";
        public const string ItemName = "Mixing Station Mk3";
        public const int Capacity = 40;

        internal static MelonLogger.Instance Log;
        internal static MelonPreferences_Entry<float> Price;
        internal static MelonPreferences_Entry<bool> DevDump;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            MelonPreferences_Category cat = MelonPreferences.CreateCategory("MixingStationMk3", "Mixing Station Mk3");
            Price = cat.CreateEntry("Price", 8000f, "Price", "Purchase price at Oscar's warehouse.");
            DevDump = cat.CreateEntry("DevDump", false, "Dev dump", "Log the Mk2 prefab hierarchy and materials on load.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Main")
                return;
            try
            {
                // Registry drops runtime items on every scene change, so re-register each time
                // Main loads. This runs before the save's loaders spawn placed stations.
                Mk3Factory.EnsureRegistered();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to register Mixing Station Mk3: " + ex);
                return;
            }
            MelonCoroutines.Start(AddToOscarWhenReady());
        }

        private static IEnumerator AddToOscarWhenReady()
        {
            float timeout = Time.realtimeSinceStartup + 300f;
            while (Time.realtimeSinceStartup < timeout)
            {
                bool loading = Singleton<LoadManager>.InstanceExists && Singleton<LoadManager>.Instance.IsLoading;
                if (!loading && OscarShop.TryAddListing())
                    yield break;
                yield return new WaitForSeconds(1f);
            }
            Log.Warning("Gave up waiting for Oscar's shop; Mk3 not listed this session.");
        }
    }
}


