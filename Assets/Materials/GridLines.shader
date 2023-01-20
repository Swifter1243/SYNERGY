Shader "Unlit/GridLines"
{
    Properties
    {
        _Resolution ("Resolution", Range(100, 2000)) = 1000
        _LineSize ("Line Size", Range(0.001, 0.01)) = 0.01
        _LineFrequency ("Line Frequency", Range(1, 200)) = 10
        _Top("Top", Float) = 1
        _Offset("Offset", Range(0, 1)) = 0
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
            float _Resolution;
            float _LineFrequency;
            float _LineSize;
            float _Top;
            float _Offset;

            // Global access to uv data
            static v2f vertex_output;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv =  v.uv;
                return o;
            }


            float4 frag (v2f vertex_output) : SV_Target
            {
                float resolution = _Resolution;
                float lineFreq = 1 / _LineFrequency;
                float lineSize = _LineSize;

                float2 uv = vertex_output.uv;
                float y = uv.y * resolution;
                y -= resolution / 2;

                float lineThickness = lineSize * resolution;
                float alpha = 0;
                float offset = _Offset * resolution;
                float scrollPos = y + offset;
                y += lineThickness / 2;
                float mod = round((y + offset) % (lineFreq * resolution));
                bool isVisible = scrollPos > 0 && scrollPos <= _Top * resolution + lineThickness;
                if (mod >= 0 && mod <= lineThickness && isVisible) alpha = 1;

                return float4(0.9,0.9,0.9,alpha);
            }
            ENDCG
        }
    }
}
