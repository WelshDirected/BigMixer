using System;
using System.Collections.Generic;
using FishNet;
using HarmonyLib;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.Product;
using UnityEngine;

namespace BigMixer
{
    // Items can't stack past 20, so the Mk3 gets a second product, ingredient and output slot.
    // The game's own code only knows the first slot of each pair, so:
    //  - the second slot slides into the first whenever the first empties (server side), which
    //    keeps GetProduct/GetMixer, the output checks and chemists' collection working unchanged;
    //  - mix quantity counts both slots, and taking more than the first slot holds takes the rest
    //    from the second (covers both the player's Begin button and chemists);
    //  - output of a 40 batch is split across both output slots.
    public class Mk3Station : MonoBehaviour
    {
        public MixingStation Station;
        public ItemSlot ProductSlot2;
        public ItemSlot MixerSlot2;
        public ItemSlot OutputSlot2;
        private bool compacting;

        public static Mk3Station Of(MixingStation station)
        {
            return station == null ? null : station.GetComponent<Mk3Station>();
        }

        public static bool IsMk3Object(MixingStation station)
        {
            return station.gameObject.name.StartsWith("MixingStationMk3", StringComparison.Ordinal);
        }

        public ItemSlot SecondOf(ItemSlot first)
        {
            if (first == Station.ProductSlot) return ProductSlot2;
            if (first == Station.MixerSlot) return MixerSlot2;
            if (first == Station.OutputSlot) return OutputSlot2;
            return null;
        }

        public void Setup(MixingStation station)
        {
            Station = station;
            ProductSlot2 = NewSlot(new ItemFilter_UnpackagedProduct(), station.InputVisuals);
            MixerSlot2 = NewSlot(new ItemFilter_MixingIngredient(), station.InputVisuals);
            OutputSlot2 = NewSlot(null, station.OutputVisuals);
            OutputSlot2.SetIsAddLocked(true);
            station.InputSlots.Add(ProductSlot2);
            station.InputSlots.Add(MixerSlot2);
            station.OutputSlots.Add(OutputSlot2);

            Hook(station.ProductSlot, ProductSlot2);
            Hook(station.MixerSlot, MixerSlot2);
            Hook(station.OutputSlot, OutputSlot2);
        }

        private ItemSlot NewSlot(ItemFilter filter, ScheduleOne.Storage.StorageVisualizer visuals)
        {
            ItemSlot slot = new ItemSlot(true);
            if (filter != null)
                slot.AddFilter(filter);
            slot.SetSlotOwner(Station); // also appends it to Station.ItemSlots (network index)
            if (visuals != null)
                visuals.AddSlot(slot, false);
            slot.onItemDataChanged = (Action)Delegate.Combine(slot.onItemDataChanged, new Action(MarkChanged));
            return slot;
        }

        private void MarkChanged()
        {
            Station.HasChanged = true;
        }

        private void Hook(ItemSlot first, ItemSlot second)
        {
            Action compact = delegate { Compact(first, second); };
            first.onItemDataChanged = (Action)Delegate.Combine(first.onItemDataChanged, compact);
            second.onItemDataChanged = (Action)Delegate.Combine(second.onItemDataChanged, compact);
        }

        // Slide the second slot's stack into the first slot when the first is empty.
        private void Compact(ItemSlot first, ItemSlot second)
        {
            if (compacting || !InstanceFinder.IsServer || first.ItemInstance != null || second.ItemInstance == null)
                return;
            if (first.IsLocked || second.IsLocked)
                return;
            compacting = true;
            try
            {
                ItemInstance moved = second.ItemInstance.GetCopy(second.Quantity);
                second.ClearStoredInstance(false);
                first.SetStoredItem(moved, false);
            }
            finally
            {
                compacting = false;
            }
        }

        // How many of `first`'s item the pair holds (the second only counts if it stacks with it).
        public int PairQuantity(ItemSlot first)
        {
            ItemSlot second = SecondOf(first);
            int q = first.Quantity;
            if (second != null && first.ItemInstance != null && second.ItemInstance != null &&
                first.ItemInstance.CanStackWith(second.ItemInstance, false))
                q += second.Quantity;
            return q;
        }
    }

    [HarmonyPatch(typeof(MixingStationMk2), nameof(MixingStationMk2.Awake))]
    internal static class Patch_Mk2Awake
    {
        private static void Postfix(MixingStationMk2 __instance)
        {
            if (__instance.isGhost || !Mk3Station.IsMk3Object(__instance) || Mk3Station.Of(__instance) != null)
                return;
            __instance.gameObject.AddComponent<Mk3Station>().Setup(__instance);
        }
    }

    [HarmonyPatch(typeof(MixingStation), nameof(MixingStation.GetMixQuantity))]
    internal static class Patch_GetMixQuantity
    {
        private static void Postfix(MixingStation __instance, ref int __result)
        {
            Mk3Station mk3 = Mk3Station.Of(__instance);
            if (mk3 == null || __instance.GetProduct() == null || __instance.GetMixer() == null)
                return;
            __result = Mathf.Min(Mathf.Min(mk3.PairQuantity(__instance.ProductSlot), mk3.PairQuantity(__instance.MixerSlot)),
                __instance.MaxMixQuantity);
        }
    }

    [HarmonyPatch(typeof(MixingStation), nameof(MixingStation.CanStartMix))]
    internal static class Patch_CanStartMix
    {
        private static void Postfix(MixingStation __instance, ref bool __result)
        {
            Mk3Station mk3 = Mk3Station.Of(__instance);
            if (mk3 != null && mk3.OutputSlot2.Quantity > 0)
                __result = false;
        }
    }

    // Starting a mix takes the batch out of the first slot; take any overflow from the second.
    [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.ChangeQuantity))]
    internal static class Patch_ChangeQuantity
    {
        private static void Prefix(ItemSlot __instance, ref int change, bool _internal)
        {
            if (change >= 0 || -change <= __instance.Quantity)
                return;
            MixingStation station = __instance.SlotOwner as MixingStation;
            Mk3Station mk3 = Mk3Station.Of(station);
            if (mk3 == null || (__instance != station.ProductSlot && __instance != station.MixerSlot))
                return;
            ItemSlot second = mk3.SecondOf(__instance);
            int overflow = -change - __instance.Quantity;
            if (second.ItemInstance == null || __instance.ItemInstance == null ||
                !__instance.ItemInstance.CanStackWith(second.ItemInstance, false) || second.Quantity < overflow)
                return; // let vanilla report the error
            second.ChangeQuantity(-overflow, _internal);
            change = -__instance.Quantity;
        }
    }

    // A finished batch bigger than one stack is split across both output slots.
    [HarmonyPatch(typeof(MixingStation), "RpcLogic___TryCreateOutputItems_2166136261")]
    internal static class Patch_TryCreateOutputItems
    {
        private static bool Prefix(MixingStation __instance)
        {
            Mk3Station mk3 = Mk3Station.Of(__instance);
            MixOperation op = __instance.CurrentMixOperation;
            if (mk3 == null || op == null)
                return true;
            ProductDefinition product;
            if (!op.IsOutputKnown(out product))
                return false;

            int remaining = op.Quantity;
            string outputId = null;
            foreach (ItemSlot slot in new[] { __instance.OutputSlot, mk3.OutputSlot2 })
            {
                if (remaining <= 0)
                    break;
                int n = Mathf.Min(remaining, product.StackLimit);
                QualityItemInstance item = product.GetDefaultInstance(n) as QualityItemInstance;
                item.SetQuality(op.ProductQuality);
                slot.AddItem(item, false);
                outputId = item.ID;
                remaining -= n;
            }
            if (remaining > 0)
                BigMixerMod.Log.Warning("Mk3 output overflow: " + remaining + " item(s) didn't fit.");
            if (NetworkSingleton<ProductManager>.Instance.GetRecipe(op.ProductID, op.IngredientID) == null)
                NetworkSingleton<ProductManager>.Instance.SendMixRecipe(op.ProductID, op.IngredientID, outputId);
            __instance.SetMixOperation(null, null, 0);
            return false;
        }
    }

    // Saving: write both slots of each pair into the station's existing item sets.
    [HarmonyPatch(typeof(MixingStation), nameof(MixingStation.GetBaseData))]
    internal static class Patch_GetBaseData
    {
        private static void Postfix(MixingStation __instance, BuildableItemData __result)
        {
            Mk3Station mk3 = Mk3Station.Of(__instance);
            MixingStationData data = __result as MixingStationData;
            if (mk3 == null || data == null)
                return;
            data.ProductContents = new ItemSet(new List<ItemSlot> { __instance.ProductSlot, mk3.ProductSlot2 });
            data.MixerContents = new ItemSet(new List<ItemSlot> { __instance.MixerSlot, mk3.MixerSlot2 });
            data.OutputContents = new ItemSet(new List<ItemSlot> { __instance.OutputSlot, mk3.OutputSlot2 });
        }
    }

    // Loading: the vanilla loader fills index 0 of each set; fill the second slots from index 1.
    [HarmonyPatch(typeof(MixingStationLoader), nameof(MixingStationLoader.Load), new[] { typeof(DynamicSaveData) })]
    internal static class Patch_LoadDynamic
    {
        private static void Postfix(DynamicSaveData data)
        {
            MixingStationData d;
            if (data.TryExtractBaseData<MixingStationData>(out d))
                Mk3Load.LoadSeconds(d);
        }
    }

    [HarmonyPatch(typeof(MixingStationLoader), nameof(MixingStationLoader.Load), new[] { typeof(string) })]
    internal static class Patch_LoadPath
    {
        private static void Postfix(MixingStationLoader __instance, string mainPath)
        {
            MixingStationData d = AccessTools.Method(typeof(MixingStationLoader), "GetData")
                .MakeGenericMethod(typeof(MixingStationData)).Invoke(__instance, new object[] { mainPath }) as MixingStationData;
            if (d != null)
                Mk3Load.LoadSeconds(d);
        }
    }

    internal static class Mk3Load
    {
        public static void LoadSeconds(MixingStationData d)
        {
            Guid guid;
            try { guid = new Guid(d.GUID); } catch { return; }
            MixingStation station = GUIDManager.GetObject<MixingStation>(guid);
            Mk3Station mk3 = Mk3Station.Of(station);
            if (mk3 == null)
                return;
            if (d.ProductContents != null) d.ProductContents.LoadTo(mk3.ProductSlot2, 1);
            if (d.MixerContents != null) d.MixerContents.LoadTo(mk3.MixerSlot2, 1);
            if (d.OutputContents != null) d.OutputContents.LoadTo(mk3.OutputSlot2, 1);
        }
    }
}
