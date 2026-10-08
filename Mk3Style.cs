using System.Collections.Generic;
using ScheduleOne.ObjectScripts;
using TMPro;
using UnityEngine;

namespace BigMixer
{
    // Gives the cloned Mk2 the Mk3 look using only the game's own meshes and materials:
    // a new paint colour on the painted parts and an "MK3" badge in the game's own font.
    internal static class Mk3Style
    {
        // Paint for the Mk3 body (replaces the hue of the Mk2's painted parts).
        private static readonly Color Paint = new Color(0.55f, 0.08f, 0.08f);
        private static readonly Color BadgeColor = new Color(0.95f, 0.78f, 0.25f);

        public static void Apply(MixingStationMk2 station)
        {
            Repaint(station.gameObject);
            AddBadge(station);
        }

        private static void Repaint(GameObject root)
        {
            Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
            float paintH, paintS, paintV;
            Color.RGBToHSV(Paint, out paintH, out paintS, out paintV);

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r.GetComponent<TMP_Text>() != null)
                    continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || !m.HasProperty("_BaseColor"))
                        continue;
                    Color c = m.GetColor("_BaseColor");
                    float h, s, v;
                    Color.RGBToHSV(c, out h, out s, out v);
                    if (s < 0.25f || v < 0.12f)
                        continue; // greys, steel, black rubber and glass stay as they are

                    Material copy;
                    if (!copies.TryGetValue(m, out copy))
                    {
                        copy = new Material(m);
                        copy.name = m.name + " (Mk3)";
                        Color nc = Color.HSVToRGB(paintH, Mathf.Max(s, paintS), Mathf.Min(v, paintV + 0.15f));
                        nc.a = c.a;
                        copy.SetColor("_BaseColor", nc);
                        if (copy.HasProperty("_Color")) copy.SetColor("_Color", nc);
                        copies[m] = copy;
                    }
                    mats[i] = copy;
                    changed = true;
                }
                if (changed)
                    r.sharedMaterials = mats;
            }
            BigMixerMod.Log.Msg("Mk3 repaint: " + copies.Count + " material(s) recoloured.");
        }

        private static void AddBadge(MixingStationMk2 station)
        {
            Transform root = station.transform;
            Bounds b = LocalBounds(root);

            GameObject go = new GameObject("Mk3Badge");
            go.layer = station.gameObject.layer;
            go.transform.SetParent(root, false);
            // Front face (-Z, the side the player faces when placing), near the top edge.
            go.transform.localPosition = new Vector3(b.center.x, b.center.y + b.extents.y * 0.55f, b.min.z - 0.004f);
            go.transform.localRotation = Quaternion.identity;

            TextMeshPro text = go.AddComponent<TextMeshPro>();
            if (station.QuantityLabel != null && station.QuantityLabel.font != null)
                text.font = station.QuantityLabel.font;
            text.text = "MK3";
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = BadgeColor;
            text.enableAutoSizing = false;
            text.fontSize = 1.2f;
            text.rectTransform.sizeDelta = new Vector2(b.size.x * 0.8f, 0.2f);
        }

        // Bounds of all mesh renderers in the prefab's own space (the template is inactive, so
        // Renderer.bounds is unreliable; use mesh bounds through the transforms instead).
        internal static Bounds LocalBounds(Transform root)
        {
            bool any = false;
            Bounds result = new Bounds();
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null)
                    continue;
                Bounds mb = mf.sharedMesh.bounds;
                Matrix4x4 toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mb.center + Vector3.Scale(mb.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = toRoot.MultiplyPoint3x4(corner);
                    if (!any) { result = new Bounds(p, Vector3.zero); any = true; }
                    else result.Encapsulate(p);
                }
            }
            return result;
        }
    }
}
