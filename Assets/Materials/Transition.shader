Shader "Unlit/Transition"
{
    Properties
    {
        _TransitionTime ("Transition Time", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha 
        ZTest Always
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Built-in properties
            float _TransitionTime;

            // Global access to uv data
            static v2f vertex_output;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv =  v.uv;
                return o;
            }

            static const float PI = 3.1415927;
            float easeOutSine(float n)
            {
                return sin(n*PI/2.);
            }


            float4 frag (v2f __vertex_output) : SV_Target
            {
                vertex_output = __vertex_output;
                float2 uv = vertex_output.uv;

                float xPos = uv.x;
                xPos += uv.y / 5;

                float alpha = -1 + xPos + _TransitionTime * 4;
                if (_TransitionTime > 0.5) {
                    float transitionOffset = 1 - _TransitionTime;
                    alpha = 1 - xPos + transitionOffset * 4 - 1;
                }
                alpha = clamp(alpha, 0, 1);
                alpha = 1 - easeOutSine(1 - alpha);

                return float4(0,0,0,alpha);
            }
            ENDCG
        }
    }
}
