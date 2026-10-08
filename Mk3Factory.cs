using System;
using System.Collections.Generic;
using System.Text;
using FishNet;
using FishNet.Object;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BigMixer
{
    // Builds the Mk3 at runtime from the game's own Mk2: the definition, the placeable prefab
    // (kept as an inactive template so its scripts never run) and the shop icon.
    internal static class Mk3Factory
    {
        private static GameObject holder;
        private static MixingStationMk2 template;
        private static BuildableItemDefinition definition;

        public static BuildableItemDefinition Definition { get { return definition; } }

        public static void EnsureRegistered()
        {
            if (definition == null)
                Build();

            if (Registry.GetItem(BigMixerMod.ItemId) == null)
                Singleton<Registry>.Instance.AddToRegistry(definition);

            // Register the prefab with FishNet so the object spawns as a Mk3 on every client.
            // Every peer runs this in the same order, so the prefab IDs line up.
            NetworkObject nob = template.GetComponent<NetworkObject>();
            InstanceFinder.NetworkManager.SpawnablePrefabs.AddObject(nob, true);
            BigMixerMod.Log.Msg("Mixing Station Mk3 registered (prefab id " + nob.PrefabId + ").");
        }

        private static void Build()
        {
            BuildableItemDefinition mk2Def = FindMk2Definition();
            if (mk2Def == null)
                throw new InvalidOperationException("Could not find the Mixing Station Mk2 definition.");
            MixingStationMk2 mk2Prefab = (MixingStationMk2)mk2Def.BuiltItem;

            if (BigMixerMod.DevDump.Value)
                MelonDump(mk2Prefab.transform);

            holder = new GameObject("BigMixer_Templates");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);

            GameObject go = Object.Instantiate(mk2Prefab.gameObject, holder.transform);
            go.name = "MixingStationMk3";
            template = go.GetComponent<MixingStationMk2>();
            template.MaxMixQuantity = BigMixerMod.Capacity;
            // Keep a full Mk3 batch as quick as a full Mk2 batch.
            int perItem = mk2Prefab.MixTimePerItem * mk2Prefab.MaxMixQuantity / BigMixerMod.Capacity;
            template.MixTimePerItem = Mathf.Max(1, perItem);
            Mk3Style.Apply(template);

            definition = Object.Instantiate(mk2Def);
            definition.name = "MixingStationMk3";
            definition.ID = BigMixerMod.ItemId;
            definition.Name = BigMixerMod.ItemName;
            definition.Description = "Industrial-grade mixing station. Mixes up to " + BigMixerMod.Capacity +
                " product with " + BigMixerMod.Capacity + " ingredients in a single batch.";
            definition.BasePurchasePrice = BigMixerMod.Price.Value;
            definition.RequiresLevelToPurchase = false;
            definition.BuiltItem = template;
            definition.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Sprite icon = Mk3Icon.Render(template.transform, mk2Def.Icon);
            if (icon != null)
                definition.Icon = icon;

            BigMixerMod.Log.Msg(string.Format("Built Mk3 from {0}: {1}x{1}, {2} min/item (Mk2 {3}x, {4} min/item).",
                mk2Def.ID, template.MaxMixQuantity, template.MixTimePerItem, mk2Prefab.MaxMixQuantity, mk2Prefab.MixTimePerItem));
        }

        private static BuildableItemDefinition FindMk2Definition()
        {
            foreach (ItemDefinition def in Singleton<Registry>.Instance.GetAllItems())
            {
                BuildableItemDefinition b = def as BuildableItemDefinition;
                if (b != null && b.BuiltItem is MixingStationMk2 && b.ID != BigMixerMod.ItemId)
                    return b;
            }
            return null;
        }

        // Writes the prefab hierarchy, renderers and material colours to the log (DevDump preference).
        private static void MelonDump(Transform root)
        {
            StringBuilder sb = new StringBuilder("Mk2 prefab dump:\n");
            DumpRecursive(root, 0, sb);
            BigMixerMod.Log.Msg(sb.ToString());
        }

        private static void DumpRecursive(Transform t, int depth, StringBuilder sb)
        {
            sb.Append(' ', depth * 2).Append(t.name)
              .Append(" pos=").Append(t.localPosition.ToString("F3"))
              .Append(" rot=").Append(t.localEulerAngles.ToString("F0"))
              .Append(" scl=").Append(t.localScale.ToString("F2"));
            if (!t.gameObject.activeSelf) sb.Append(" [inactive]");
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                sb.Append(" mesh=").Append(mf.sharedMesh.name).Append(" bounds=").Append(mf.sharedMesh.bounds.size.ToString("F3"));
            Renderer r = t.GetComponent<Renderer>();
            if (r != null)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    sb.Append(" mat=").Append(m.name).Append('(').Append(m.shader.name);
                    if (m.HasProperty("_BaseColor")) sb.Append(' ').Append(m.GetColor("_BaseColor").ToString("F2"));
                    if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) sb.Append(" tex=").Append(m.GetTexture("_BaseMap").name);
                    sb.Append(')');
                }
            }
            sb.Append('\n');
            for (int i = 0; i < t.childCount; i++)
                DumpRecursive(t.GetChild(i), depth + 1, sb);
        }
    }
}
