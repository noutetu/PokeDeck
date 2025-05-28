Shader "UI/DynamicGradientPro"
{
    Properties
    {
        [HideInInspector]_MainTex ("Sprite", 2D) = "white" {}
        _GradientTex ("Gradient", 2D) = "white" {}

        _Type      ("Type 0=Linear 1=Radial 2=Angle 3=Diamond 4=Reflected", Int) = 0
        _Rotation  ("Rotation (rad)", Float) = 0
        _Center    ("Center", Vector) = (0.5,0.5,0,0)
        _Width     ("Width Scale", Float) = 1             // ★追加

        _Repeat    ("Repeat 0/1", Float) = 0
        _Mirror    ("Mirror 0/1", Float) = 0
        _AnimOffset("Anim Offset", Float) = 0
        _Dither    ("Dither 0/1", Float) = 0

        // --- ステンシル省略（そのまま） ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _GradientTex;
            int   _Type;
            float _Rotation;
            float2 _Center;
            float _Repeat, _Mirror, _AnimOffset, _Dither;
            float _Width;                      // ★追加

            // --- 省略: Bayer 配列そのまま ---

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };

            v2f vert(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.col; return o;
            }

            float2 rotate(float2 p, float rad){
                float s = sin(rad), c = cos(rad);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            fixed4 sampleGrad(float t){
                if (_Repeat > 0.5) { t = frac(t + _AnimOffset); }
                else               { t = saturate(t); }
                if (_Mirror > 0.5){
                    t = (fmod(floor(t*2), 2) == 0) ? frac(t*2) : 1 - frac(t*2);
                }
                return tex2D(_GradientTex, float2(t, 0));
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv = i.uv;
                float2 p  = rotate(uv - _Center, _Rotation);

                // 幅スケールは X 方向距離 or その絶対値合算に対して割るだけ
                float t = 0;
                if      (_Type == 0) t =  p.x               / _Width + 0.5;          // LINEAR
                else if (_Type == 1) t =  length(p)         / _Width * 1.4142136;    // RADIAL
                else if (_Type == 2) t = (atan2(p.y,p.x)/UNITY_PI + 1)*0.5;          // ANGLE (幅≒角度なので無スケール)
                else if (_Type == 3) t = (abs(p.x)+abs(p.y))/ _Width * 1.4142136;    // DIAMOND
                else                 t =  abs(p.x*2)        / _Width;                // REFLECTED

                fixed4 grad   = sampleGrad(t);
                fixed4 sprite = tex2D(_MainTex, uv) * i.col;

                // バイヤーディザ α
                if (_Dither > 0.5){
                    int2 px = int2(fmod(i.pos.x,8), fmod(i.pos.y,8));
                    int idx = px.y*8 + px.x;
                    float thr = (idx + 0.5)/64.0;
                    grad.a = grad.a < thr ? 0 : grad.a;
                }
                return grad * sprite.a;
            }
            ENDCG
        }
    }
}
