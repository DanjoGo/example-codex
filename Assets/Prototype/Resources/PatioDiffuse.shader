Shader "Patio/Diffuse"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            fixed4 _Color;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct Output
            {
                float4 position : SV_POSITION;
                fixed3 lighting : COLOR;
                UNITY_FOG_COORDS(0)
            };
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float diffuse = saturate(dot(normalize(normal), normalize(_WorldSpaceLightPos0.xyz)));
                o.lighting = UNITY_LIGHTMODEL_AMBIENT.xyz + _LightColor0.rgb * diffuse;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 color = fixed4(_Color.rgb * i.lighting, 1);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
