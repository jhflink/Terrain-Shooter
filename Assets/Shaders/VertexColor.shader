Shader "Custom/VertexColorBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MinDarkness ("Minimum Darkness Value", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform StructuredBuffer<float4> _vertexPositions;
            uniform StructuredBuffer<float3> _vertexColors;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                uint vertexId : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldNormal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _MinDarkness;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

                // load the vertex position from the vertex position array  
                o.vertex = UnityObjectToClipPos(float4(_vertexPositions[v.vertexId]));
                
                // discard vertex if w is less than 1
                if(_vertexPositions[v.vertexId].w<1)
                    o.vertex.w = 0.0 / 0.0;

                // load the color from the color array 
                o.color = float4(_vertexColors[v.vertexId], 1);

                // get uv  
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            fixed3 frag (v2f i) : SV_Target
            {
                // get main texture
                fixed3 tex = tex2D(_MainTex, i.uv).aaa;

                // since its a grayscale texture flip the colors to have it brighter
                tex = 1.0 - tex;

                // calculate lightning but restrict its lowest value so things doesnt get too dark 
                fixed3 lighting = max(dot(i.worldNormal, _WorldSpaceLightPos0.xyz), _MinDarkness);

                // multiply the texture with the vertex color
                tex *= i.color * lighting;

                return tex;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
