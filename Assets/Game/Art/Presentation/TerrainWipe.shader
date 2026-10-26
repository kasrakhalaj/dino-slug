Shader "DesertDash/Terrain Wipe"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture",2D)="white" {}
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
        [PerRendererData] _WipeSide("Wipe Side",Float)=0
        [PerRendererData] _BoundaryX("Boundary X",Float)=100
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;half4 color:COLOR; };
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WipeSide;
                float _BoundaryX;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;float3 world=TransformObjectToWorld(v.positionOS);
                o.positionCS=TransformWorldToHClip(world);o.world=world.xy;o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                if(abs(_WipeSide)>.5)
                {
                    // Complementary masks leave exactly one map at every pixel.
                    // A small ordered-dither band softens the moving world boundary.
                    const float bayer[16]={0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                    int2 cell=(int2)floor(i.world*16);
                    float threshold=(bayer[(cell.y&3)*4+(cell.x&3)]+.5)/16;
                    float incoming=saturate((i.world.x-_BoundaryX)/1.5+.5);
                    clip(_WipeSide>0 ? incoming-threshold : threshold-incoming);
                }
                return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
            }
            ENDHLSL
        }
    }
}
