Shader "Unlit/Background"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution ("Resolution (Change if AA is bad)", Range(1, 1024)) = 1
        _Offset ("Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Resolution;
            float4 _Offset;

            #define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
            #define iResolution float3(_Resolution, _Resolution, _Resolution)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
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

            float easeInQuint(float n)
            {
                return pow(n, 5.);
            }

            float easeInSine(float n)
            {
                return 1 - cos((n * PI) / 2);
            }

            float easeInQuad(float n)
            {
                return n * n;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233)))*43758.547);
            }

            float3 lerpVec3(float3 start, float3 end, float fraction)
            {
                return (1.-fraction)*start+fraction*end;
            }

            float4 frag (v2f __vertex_output) : SV_Target
            {
                v2f vertex_output = __vertex_output;
                float2 fragCoord = vertex_output.uv * _Resolution;

                // Normalized pixel coordinates (from 0 to 1)
                float2 uv = fragCoord/iResolution.xy;

                // Get simplex value
                float zoom = 2;
                float3 p3 = float3(uv.x*zoom, uv.y*zoom-_Time.y*0.02, _Time.y*0.02);
                p3 += _Offset;
                float f = 0.5+simplex3d(p3)*1.;

                // Add funny layers
                f *= 3.;
                f = glsl_mod(f, 1.);

                // Add noise
                f += (-0.5 + rand(fragCoord));

                // Color
                float3 lowColor = float3(45.9 / 100, 78.8 / 100, 86.3 / 100) / 3;
                float3 highColor = float3(88.6 / 100, 55.7 / 100, 88.6 / 100) / 3;
                float3 color = lerpVec3(lowColor, highColor, f);

                color += easeInQuad(uv.x) * highColor * 3;
                color += easeInQuad((1 - uv.x)) * lowColor * 3;

                // Output to screen
                return float4(color, 1.);
            }
            ENDCG
        }
    }
}
