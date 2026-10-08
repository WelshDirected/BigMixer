using System;
using System.Text;
using HarmonyLib;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Stations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BigMixer
{
    // Adds a second product, ingredient and output slot to the mixing station screen, cloned from
    // the game's own slot widgets and placed under the originals. Hidden for Mk1/Mk2 stations.
    internal static class Mk3Ui
    {
        private const float Gap = 10f;

        private static MixingStationInterface ui;
        private static ItemSlotUI product2, ingredient2, output2;
        private static Mk3Station current;
        private static Action contentsChanged;
        private static bool dumped;

        public static void Opened(MixingStationInterface face, MixingStation station)
        {
            EnsureClones(face);
            current = Mk3Station.Of(station);
            bool show = current != null;
            product2.gameObject.SetActive(show);
            ingredient2.gameObject.SetActive(show);
            output2.gameObject.SetActive(false);
            if (!show)
                return;

            if (BigMixerMod.DevDump.Value && !dumped)
            {
                dumped = true;
                DumpUi(face.transform);
            }
            product2.AssignSlot(current.ProductSlot2);
            ingredient2.AssignSlot(current.MixerSlot2);
            output2.AssignSlot(current.OutputSlot2);
            Subscribe(true);
            UpdateOutput(face);
        }

        public static void Closed()
        {
            if (current == null)
                return;
            Subscribe(false);
            product2.ClearSlot();
            ingredient2.ClearSlot();
            output2.ClearSlot();
            current = null;
        }

        // The output view shows while the first output slot has items (the second slides into it).
        public static void UpdateOutput(MixingStationInterface face)
        {
            if (output2 == null)
                return;
            output2.gameObject.SetActive(current != null && face.OutputSlotUI.gameObject.activeSelf);
        }

        private static void Subscribe(bool on)
        {
            foreach (ItemSlot s in new[] { current.ProductSlot2, current.MixerSlot2, current.OutputSlot2 })
            {
                s.onItemDataChanged = on
                    ? (Action)Delegate.Combine(s.onItemDataChanged, contentsChanged)
                    : (Action)Delegate.Remove(s.onItemDataChanged, contentsChanged);
            }
        }

        private static void EnsureClones(MixingStationInterface face)
        {
            if (ui == face && product2 != null)
                return;
            ui = face;
            product2 = CloneBelow(face.ProductSlotUI, "ProductSlot2");
            ingredient2 = CloneBelow(face.IngredientSlotUI, "IngredientSlot2");
            output2 = CloneBelow(face.OutputSlotUI, "OutputSlot2");
            contentsChanged = (Action)Delegate.CreateDelegate(typeof(Action), face,
                AccessTools.Method(typeof(MixingStationInterface), "StationContentsChanged"));
        }

        private static ItemSlotUI CloneBelow(ItemSlotUI original, string name)
        {
            RectTransform src = (RectTransform)original.transform;
            GameObject go = Object.Instantiate(original.gameObject, src.parent);
            go.name = name;
            go.GetComponent<ItemSlotUI>().ClearSlot();
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.sizeDelta = src.sizeDelta;
            rt.anchoredPosition = src.anchoredPosition - new Vector2(0f, src.rect.height + Gap);
            go.SetActive(false);
            return go.GetComponent<ItemSlotUI>();
        }

        private static void DumpUi(Transform root)
        {
            StringBuilder sb = new StringBuilder("MixingStationInterface dump:\n");
            Dump(root, 0, sb);
            BigMixerMod.Log.Msg(sb.ToString());
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            if (depth > 7) return;
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf) sb.Append(" [off]");
            RectTransform rt = t as RectTransform;
            if (rt != null)
                sb.Append(" pos=").Append(rt.anchoredPosition.ToString("F0")).Append(" size=").Append(rt.rect.size.ToString("F0"));
            if (t.GetComponent<LayoutGroup>() != null) sb.Append(" [layout]");
            sb.Append('\n');
            for (int i = 0; i < t.childCount; i++)
                Dump(t.GetChild(i), depth + 1, sb);
        }
    }

    [HarmonyPatch(typeof(MixingStationInterface), nameof(MixingStationInterface.Open))]
    internal static class Patch_UiOpen
    {
        private static void Postfix(MixingStationInterface __instance, MixingStation station)
        {
            Mk3Ui.Opened(__instance, station);
        }
    }

    [HarmonyPatch(typeof(MixingStationInterface), nameof(MixingStationInterface.Close))]
    internal static class Patch_UiClose
    {
        private static void Prefix()
        {
            Mk3Ui.Closed();
        }
    }

    [HarmonyPatch(typeof(MixingStationInterface), "UpdateDisplayMode")]
    internal static class Patch_UiDisplayMode
    {
        private static void Postfix(MixingStationInterface __instance)
        {
            Mk3Ui.UpdateOutput(__instance);
        }
    }
}
