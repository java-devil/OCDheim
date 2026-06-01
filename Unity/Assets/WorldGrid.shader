Shader "OCDheim/WorldGrid"
{
    Properties
    {
        _MinColor ("Min Color", Color) = (0.3, 0.3, 0.3, 0.3)
        _MaxColor ("Max Color", Color) = (0.5, 0.5, 0.6, 0.5)
        _AAThickness ("AA Thickness", Float) = 0.02
        _LineThickness ("Line Thickness", Float) = 0.02
        _BorderThickness ("Border Thickness", Float) = 0.15
        _GridRadius ("Grid Radius", Float) = 8.0
        _GridResolution ("Grid Resolution", Float) = 1.0
        _PlayerPosition ("Player Position", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        ColorMask RGB
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil 
        {
            Ref 1
            Comp NotEqual
        }

        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata_t
        {
            float4 vertex : POSITION;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float4 worldPos : TEXCOORD0;
        };

        half4 _MinColor;
        half4 _MaxColor;

        float _AAThickness;
        float _LineThickness;
        float _BorderThickness;

        float _GridRadius;
        float _GridResolution;
        float4 _PlayerPosition;

        v2f vert(appdata_t v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex);
            return o;
        }

        float lineAlfa(float gridMod, float halfLineThickness)
        {
            float dist = min(gridMod, 1.0 - gridMod);
            float halfAA = (_AAThickness / _GridResolution) * 0.5;
            return 1.0 - smoothstep(halfLineThickness - halfAA, halfLineThickness + halfAA, dist);
        }

        half4 fragShared(v2f i, half4 color)
        {
            float halfAA = _AAThickness * 0.5;
            float dist = distance(i.worldPos.xz, _PlayerPosition.xz);

            float outerBorderAlpha = 1.0 - smoothstep(_GridRadius - halfAA, _GridRadius + halfAA, dist);
            if (outerBorderAlpha <= 0.0) discard;

            float innerBorderRadius = _GridRadius - _BorderThickness;
            float innerBorderAlpha = smoothstep(innerBorderRadius - halfAA, innerBorderRadius + halfAA, dist);

            float2 gridPos = i.worldPos.xz / _GridResolution;
            float2 gridMod = frac(gridPos);

            float halfLineThickness = (_LineThickness / _GridResolution) * 0.5;
            float alphaX = lineAlfa(gridMod.x, halfLineThickness);
            float alphaZ = lineAlfa(gridMod.y, halfLineThickness);
            float isVisibleX = step(0.5, frac(i.worldPos.x * 2.0 - 0.25));
            float isVisibleZ = step(0.5, frac(i.worldPos.z * 2.0 - 0.25));

            float isSolidLine = step(1.0, _GridResolution);
            alphaX *= max(isVisibleZ, isSolidLine);
            alphaZ *= max(isVisibleX, isSolidLine);
            float lineAlpha = max(alphaX, alphaZ);

            float alpha = lerp(lineAlpha, 1.0, innerBorderAlpha);
            alpha *= outerBorderAlpha;

            color.a *= alpha;
            return color;
        }
        ENDCG

        // ----------------------------------------------------
        // PASS 1: OCCLUDED
        // ----------------------------------------------------
        Pass
        {
            ZTest Greater

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag (v2f i) : SV_Target
            {
                return fragShared(i, _MinColor);
            }
            ENDCG
        }

        // ----------------------------------------------------
        // PASS 2: EXPOSED
        // ----------------------------------------------------
        Pass
        {
            ZTest LEqual
            Offset -16, -16

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag (v2f i) : SV_Target
            {
                return fragShared(i, _MaxColor);
            }
            ENDCG
        }
    }
}
