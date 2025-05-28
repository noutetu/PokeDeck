using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIGradientPro
{
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public class UIDynamicGradientPro : MonoBehaviour
    {
        // ────────────────────────────────
        public enum GradientType { Linear, Radial, Angle, Diamond, Reflected }

        [Header("基本")]
        public Gradient gradient = new Gradient();
        public GradientType type = GradientType.Linear;

        [Header("共通パラメータ")]
        [Tooltip("解像度 (8-512)")][Range(8, 512)] public int resolution = 128;
        [Tooltip("回転 (deg) ※Linear/Angle/Diamond")]
        [Range(0, 360)] public float rotation = 0;
        [Tooltip("中心 (0-1) ※Radial/Angle/Diamond")]
        public Vector2 center = new Vector2(0.5f, 0.5f);

        [Header("グラデーション幅")]
        [Tooltip("1.0=等倍。大きいほど勾配がゆるやか／小さいほど急")]
        [Range(0.1f, 10f)] public float width = 1f;

        [Header("ループ & ミラー")]
        public bool repeat = false;
        public bool mirror = false;

        [Header("アニメーション")]
        public bool animateOffset = false;
        public float offsetSpeed = 0.2f;

        [Header("ディザリング (2次元バイヤー)")]
        public bool dithering = false;

        // ─────────────────────────────── internal
        Texture2D gradTex;
        Material runtimeMat;
        Image img;

        // 変更検知キャッシュ
        Gradient gCache;
        int rCache; float rotCache; Vector2 cenCache;
        GradientType tCache; bool repCache, mirCache, dithCache;
        float wCache;

        const string shaderName = "UI/DynamicGradientPro";

        // ─────────────────────────────── Unity
        void OnEnable()
        {
            img = GetComponent<Image>();

            Shader s = Shader.Find(shaderName);
            if (s)
            {
                // 常に runtimeMat を初期化
                runtimeMat = new Material(s);
                img.material = runtimeMat;

                Apply(force: true);
            }
        }

        public void OnDisable()
        {
            if (Application.isPlaying && runtimeMat) Destroy(runtimeMat);
            if (gradTex) DestroyImmediate(gradTex);
        }
        void Update()
        {
            Apply();

            if (animateOffset && img != null && img.material != null)
            {
                float ofs;
                if (Application.isPlaying)
                {
                    ofs = Time.time * offsetSpeed;
                }
                else
                {
#if UNITY_EDITOR
                    ofs = (float)UnityEditor.EditorApplication.timeSinceStartup * offsetSpeed;
#else
            ofs = Time.realtimeSinceStartup * offsetSpeed;
#endif
                }
                img.material.SetFloat("_AnimOffset", ofs);
            }
        }


        // ─────────────────────────────── core
        public void Apply(bool force = false)
        {
            bool changed =
                force ||
                !GradientEquals(gCache, gradient) ||
                rCache != resolution || rotCache != rotation ||
                cenCache != center || tCache != type ||
                repCache != repeat || mirCache != mirror ||
                dithCache != dithering || Mathf.Abs(wCache - width) > 0.0001f;

            if (!changed) return;

            // ★ テクスチャ生成
            if (gradTex == null || gradTex.width != resolution)
            {
                if (gradTex) DestroyImmediate(gradTex);
                gradTex = new Texture2D(resolution, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    name = "UIDynamicGradientProTex"
                };
            }
            for (int x = 0; x < resolution; x++)
            {
                float t = (float)x / (resolution - 1);
                gradTex.SetPixel(x, 0, gradient.Evaluate(t));
            }
            gradTex.Apply();

            // ★ マテリアル確保
            if (img.material == null || img.material.shader == null || img.material.shader.name != shaderName)
            {
                runtimeMat = new Material(Shader.Find(shaderName));
                img.material = runtimeMat;
            }
            else
            {
                runtimeMat = Application.isPlaying ? new Material(img.material) : img.material;
                img.material = runtimeMat;
            }

            // ★ 値転送
            runtimeMat.SetTexture("_GradientTex", gradTex);
            runtimeMat.SetInt("_Type", (int)type);
            runtimeMat.SetFloat("_Rotation", rotation * Mathf.Deg2Rad);
            runtimeMat.SetVector("_Center", center);
            runtimeMat.SetFloat("_Repeat", repeat ? 1 : 0);
            runtimeMat.SetFloat("_Mirror", mirror ? 1 : 0);
            runtimeMat.SetFloat("_Dither", dithering ? 1 : 0);
            runtimeMat.SetFloat("_Width", Mathf.Max(0.001f, width));   // new!

            // ★ キャッシュ
            gCache = CloneGradient(gradient);
            rCache = resolution; rotCache = rotation; cenCache = center;
            tCache = type; repCache = repeat; mirCache = mirror; dithCache = dithering;
            wCache = width;
        }

        // ─────────────────────────────── helper
        static bool GradientEquals(Gradient a, Gradient b)
            => a.colorKeys.Length == b.colorKeys.Length && a.alphaKeys.Length == b.alphaKeys.Length;

        static Gradient CloneGradient(Gradient g)
        {
            var ng = new Gradient(); ng.SetKeys(g.colorKeys, g.alphaKeys); return ng;
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(UIDynamicGradientPro))]
    public class EditorImpl : Editor
    {
        bool usePastelTheme = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var comp = target as UIDynamicGradientPro;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("▼ カスタム操作", EditorStyles.boldLabel);

            if (GUILayout.Button("Force Update"))
                comp.Apply(force: true);

            usePastelTheme = EditorGUILayout.Toggle("パステルテーマで生成", usePastelTheme);

            if (GUILayout.Button("ランダム生成"))
            {
                Undo.RecordObject(comp, "ランダムグラデーション生成");

                Gradient gradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

                for (int i = 0; i < colorKeys.Length; i++)
                {
                    Color randomColor = usePastelTheme ?
                        Random.ColorHSV(0f, 1f, 0f, 1f, 0.9f, 1f) : // パステル調
                        Random.ColorHSV(); // フルランダム

                    colorKeys[i] = new GradientColorKey(
                        randomColor,
                        i / (float)(colorKeys.Length - 1)
                    );
                }

                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                gradient.SetKeys(colorKeys, alphaKeys);
                comp.gradient = gradient;

                comp.type = (UIDynamicGradientPro.GradientType)Random.Range(0, 5);
                comp.rotation = Random.Range(0f, 360f);
                comp.center = new Vector2(Random.Range(0.3f, 0.7f), Random.Range(0.3f, 0.7f));
                comp.width = Random.Range(0.3f, 3f);
                comp.repeat = Random.value > 0.5f;
                comp.mirror = Random.value > 0.5f;
                comp.dithering = Random.value > 0.5f;

                comp.Apply(force: true);
                EditorUtility.SetDirty(comp);
            }

            EditorGUILayout.HelpBox(
                "Width を大きくすると勾配がゆるやか、小さくすると急になります。\n" +
                "Repeat を ON にしておくと幅変更後も無限タイルされます。", MessageType.Info);


        }
    }
#endif
}



