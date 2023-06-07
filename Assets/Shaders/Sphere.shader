Shader "Custom/Sphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MinDarkness ("Minimum Darkness Value", Float) = 0.2
        _Amplitude ("Amplitude", Range(0, 1)) = 0.1
        _Frequency ("Frequency", Range(0, 10)) = 1
        _Speed ("Speed", Range(0, 50)) = 1
        _Active ("Active", Int) = 1.0
    }

    SubShader
    {
        Tags {"Queue"="Geometry" "RenderType"="Opaque"}
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform StructuredBuffer<float3> _vertexPositions;

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
            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _MinDarkness;
            float4 _MainTex_ST;
            uint _Active;

            v2f vert (appdata v)
            {
                v2f o;

                // load the vertex position from the vertex position array  
                //o.vertex = UnityObjectToClipPos(float4(_vertexPositions[v.vertexId]));
                o.vertex = UnityObjectToClipPos(v.vertex);
                // discard vertex if w is less than 1
                //if(_vertexPositions[v.vertexId].w<1)
                 //   o.vertex.w = 0.0 / 0.0;

                // load the color from the color array 
                o.color = float4(0.66, 0.58, 0.5, 1);

                // get uv   
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                if(_Active>0)
                {
                    //float displacement = _Amplitude * sin(_Frequency * v.vertex.xyz + _Time.y * _Speed);
                    //o.vertex.xyz += v.normal * displacement;
                }

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
