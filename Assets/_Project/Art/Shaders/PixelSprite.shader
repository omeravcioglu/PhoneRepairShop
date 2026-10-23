// Unlit sprite shader for the pixel-art game (URP 2D Renderer).
// Adds a 1-pixel hover outline (_OutlineColor, alpha 0 = off) and a colour flash (_Flash, alpha = amount),
// both set per renderer through a MaterialPropertyBlock by SpriteHighlighter.
// The outline is drawn on transparent texels next to opaque ones, so outlined sprites need
// at least 1 px of transparent padding and Full Rect meshes (the importer sets both).
Shader "PhoneRepair/Pixel Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Colour", Color) = (1, 1, 1, 0)
        _Flash ("Flash Colour (alpha = amount)", Color) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half4 _Flash;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 colour = texel * i.color;

                if (_OutlineColor.a > 0.0h && texel.a < 0.5h)
                {
                    float2 texelStep = _MainTex_TexelSize.xy;
                    half neighbours = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(texelStep.x, 0)).a;
                    neighbours = max(neighbours, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(texelStep.x, 0)).a);
                    neighbours = max(neighbours, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, texelStep.y)).a);
                    neighbours = max(neighbours, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(0, texelStep.y)).a);
                    if (neighbours >= 0.5h)
                    {
                        return half4(_OutlineColor.rgb, _OutlineColor.a * i.color.a);
                    }
                }

                colour.rgb = lerp(colour.rgb, _Flash.rgb, _Flash.a * step(0.01h, colour.a));
                return colour;
            }
            ENDHLSL
        }
    }
}
