Shader "Unlit/Mirror"
{
    Properties
    {
        _Cutoff ("Cutoff", Range(0, 1)) = 1
        _ResolutionX ("Resolution X", Range(1, 1024)) = 1
        _ResolutionY ("Resolution Y", Range(1, 1024)) = 1
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
        [ToggleUI] _Vertical ("Vertical", Float) = 0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha One
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
            float _Cutoff;
            float _ResolutionX;
            float _ResolutionY;
            float4 _TopColor;
            float4 _BottomColor;
            float _Vertical;

            // GLSL Compatability macros
            #define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
            #define iResolution float3(_ResolutionX, _ResolutionY, 0)

            // Global access to uv data
            static v2f vertex_output;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv =  v.uv;
                return o;
            }

            // USING https://www.shadertoy.com/view/XsX3zB FOR NOISE

            float3 random3(float3 c)
            {
                float j = 4096.*sin(dot(c, float3(17., 59.4, 15.)));
                float3 r;
                r.z = frac(512.*j);
                j *= 0.125;
                r.x = frac(512.*j);
                j *= 0.125;
                r.y = frac(512.*j);
                return r-0.5;
            }

            static const float F3 = 0.3333333;
            static const float G3 = 0.1666667;
            float simplex3d(float3 p)
            {
                float3 s = floor(p+dot(p, ((float3)F3)));
                float3 x = p-s+dot(s, ((float3)G3));
                float3 e = step(((float3)0.), x-x.yzx);
                float3 i1 = e*(1.-e.zxy);
                float3 i2 = 1.-e.zxy*(1.-e);
                float3 x1 = x-i1+G3;
                float3 x2 = x-i2+2.*G3;
                float3 x3 = x-1.+3.*G3;
                float4 w, d;
                w.x = dot(x, x);
                w.y = dot(x1, x1);
                w.z = dot(x2, x2);
                w.w = dot(x3, x3);
                w = max(0.6-w, 0.);
                d.x = dot(random3(s), x);
                d.y = dot(random3(s+i1), x1);
                d.z = dot(random3(s+i2), x2);
                d.w = dot(random3(s+1.), x3);
                w *= w;
                w *= w;
                d *= w;
                return dot(d, ((float4)52.));
            }

            static const float3x3 rot1 = transpose(float3x3(-0.37, 0.36, 0.85, -0.14, -0.93, 0.34, 0.92, 0.01, 0.4));
            static const float3x3 rot2 = transpose(float3x3(-0.55, -0.39, 0.74, 0.33, -0.91, -0.24, 0.77, 0.12, 0.63));
            static const float3x3 rot3 = transpose(float3x3(-0.71, 0.52, -0.47, -0.08, -0.72, -0.68, -0.7, -0.45, 0.56));
            float simplex3d_fractal(float3 m)
            {
                return 0.5333333*simplex3d(mul(m,rot1))+0.2666667*simplex3d(mul(2.*m,rot2))+0.1333333*simplex3d(mul(4.*m,rot3))+0.0666667*simplex3d(8.*m);
            }

            static const float PI = 3.1415927;
            float easeOutSine(float n)
            {
                return sin(n*PI/2.);
            }

            float easeOutQuint(float n)
            {
                return 1.-pow(1.-n, 5.);
            }

            float4 frag (v2f __vertex_output) : SV_Target
            {
                vertex_output = __vertex_output;
                if (!_Vertical) {
                    vertex_output.uv.xy = vertex_output.uv.yx;
                    float resX = _ResolutionX;
                    _ResolutionX = _ResolutionY;
                    _ResolutionY = resX;
                }
                float4 fragColor = 0;
                float2 fragCoord = vertex_output.uv * float2(_ResolutionX, _ResolutionY);

                // Normalized pixel coordinates (from 0 to 1)
                float2 uv = fragCoord/iResolution.xy;

                float width = 0.5;
                float2 midPoint = iResolution.xy/2.;
                float2 screenWidth = width*midPoint;

                // Getting Distances
                float xDist = midPoint.x-fragCoord.x;
                float x = xDist/screenWidth.x;
                x = abs(x);
                x = 1.-x;
                if (x>0.) x = easeOutSine(x);

                float yDist = midPoint.y-fragCoord.y;
                float y = yDist/iResolution.y*2.;
                y = abs(y);
                y = 1.-y;
                
                // Add noise
                float2 pos = float2((x+_Time.y * 0.5) * 1.5, fragCoord.y/220.);
                if (_Vertical) pos.y += 1000;
                float noise = simplex3d(float3(pos, _Time.y*0.25));
                float v = x-noise;
                v += simplex3d(float3(pos*3., _Time.y * 0.5))/4.;
                float3 col = lerp(_TopColor, _BottomColor, y).xyz*v;
                fragColor = float4(col, 1);
                fragColor.xyz += 0.3-easeOutQuint(1.-x);

                // Cutoff
                float spacing = 0.2;
                float cutoff = _Cutoff;
                float newSize = (1 + spacing);
                cutoff = cutoff * newSize - spacing;

                float alpha = (spacing - y + cutoff);
                alpha *= 1 / spacing;
                if (y > cutoff) fragColor = float4(fragColor.xyz, alpha);
                return fragColor;
            }
            ENDCG
        }
    }
}
