using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BigMixer
{
    // Renders the Mk3 shop/inventory icon with the same preview renderer the game's own
    // IconGenerator uses, so it matches the style of the other item icons.
    internal static class Mk3Icon
    {
        private const int Size = 512;

        public static Sprite Render(Transform template, Sprite fallback)
        {
            GameObject proxy = null;
            AmbientMode oldMode = RenderSettings.ambientMode;
            Color oldAmbient = RenderSettings.ambientLight;
            try
            {
                proxy = BuildVisualProxy(template);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white;
                RuntimePreviewGenerator.PreviewDirection = new Vector3(-0.7f, -0.45f, 1f);
                RuntimePreviewGenerator.Padding = 0.04f;
                RuntimePreviewGenerator.BackgroundColor = new Color(0f, 0f, 0f, 0f);
                RuntimePreviewGenerator.OrthographicMode = false;
                RuntimePreviewGenerator.MarkTextureNonReadable = false;
                Texture2D tex = RuntimePreviewGenerator.GenerateModelPreview(proxy.transform, Size, Size, false, true);
                if (tex == null)
                    return fallback;
                tex.name = "MixingStationMk3 Icon";
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                s.name = "MixingStationMk3 Icon";
                Object.DontDestroyOnLoad(tex);
                return s;
            }
            catch (Exception ex)
            {
                BigMixerMod.Log.Warning("Icon render failed, using the Mk2 icon: " + ex.Message);
                return fallback;
            }
            finally
            {
                RenderSettings.ambientMode = oldMode;
                RenderSettings.ambientLight = oldAmbient;
                if (proxy != null)
                    Object.Destroy(proxy);
            }
        }

        // A script-free copy of the template's visible meshes (the real template is inactive and
        // full of network scripts, so it can't be rendered or cloned directly).
        private static GameObject BuildVisualProxy(Transform template)
        {
            GameObject root = new GameObject("Mk3IconProxy");
            root.transform.position = new Vector3(0f, -500f, 0f);
            foreach (MeshRenderer mr in template.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!IsVisibleInTemplate(mr.transform, template))
                    continue;
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                TextMeshPro tmp = mr.GetComponent<TextMeshPro>();
                if ((mf == null || mf.sharedMesh == null) && tmp == null)
                    continue;

                GameObject part = new GameObject(mr.name);
                part.transform.SetParent(root.transform, false);
                Matrix4x4 m = template.worldToLocalMatrix * mr.transform.localToWorldMatrix;
                part.transform.localPosition = m.MultiplyPoint3x4(Vector3.zero);
                part.transform.localRotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
                part.transform.localScale = new Vector3(m.GetColumn(0).magnitude, m.GetColumn(1).magnitude, m.GetColumn(2).magnitude);

                if (tmp != null)
                {
                    TextMeshPro t = part.AddComponent<TextMeshPro>();
                    t.font = tmp.font;
                    t.text = tmp.text;
                    t.fontStyle = tmp.fontStyle;
                    t.alignment = tmp.alignment;
                    t.color = tmp.color;
                    t.fontSize = tmp.fontSize;
                    t.rectTransform.sizeDelta = tmp.rectTransform.sizeDelta;
                    t.ForceMeshUpdate();
                    continue;
                }
                part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                MeshRenderer r = part.AddComponent<MeshRenderer>();
                r.sharedMaterials = mr.sharedMaterials;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            return root;
        }

        private static bool IsVisibleInTemplate(Transform t, Transform template)
        {
            for (Transform p = t; p != null && p != template; p = p.parent)
                if (!p.gameObject.activeSelf)
                    return false;
            Renderer r = t.GetComponent<Renderer>();
            return r != null && r.enabled;
        }
    }
}
