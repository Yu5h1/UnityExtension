Shader "UI/PatternLines"
{
    Properties
    {
        _LineSpacing ("Line Spacing", Float) = 0.2
        _LineThickness ("Line Thickness", Float) = 0.005
        _StartOffset ("Start Offset", Float) = 0
        _RectHeight ("Rect Height", Float) = 100
        _PixelLineSpacing ("Pixel Line Spacing", Float) = 20
        _PixelThickness ("Pixel Thickness", Float) = 1
        
        [Header(Pattern Settings)]
        _PatternType ("Pattern Type", Int) = 0  // 0=solid, 1=dot, 2=dash, 3=texture
        _PatternTexture ("Pattern Texture", 2D) = "white" {}
        _PatternScale ("Pattern Scale", Float) = 1.0
        _DotSpacing ("Dot Spacing", Float) = 5.0
        _DashLength ("Dash Length", Float) = 10.0
        _GapLength ("Gap Length", Float) = 5.0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            float _LineSpacing;
            float _LineThickness;
            float _StartOffset;
            float _RectHeight;
            float _PixelLineSpacing;
            float _PixelThickness;
            float4 _ClipRect;
            
            int _PatternType;
            sampler2D _PatternTexture;
            float4 _PatternTexture_ST;
            float _PatternScale;
            float _DotSpacing;
            float _DashLength;
            float _GapLength;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 基本線條計算
                float pixelY = i.texcoord.y * _RectHeight + (_StartOffset * _RectHeight);
                float lineIndex = round(pixelY / _PixelLineSpacing);
                float lineCenter = lineIndex * _PixelLineSpacing;
                float distanceToLine = abs(pixelY - lineCenter);
                
                float halfThickness = _PixelThickness * 0.5;
                float lineAlpha = 1.0 - smoothstep(halfThickness - 0.5, halfThickness + 0.5, distanceToLine);
                
                // 圖案計算
                float patternAlpha = 1.0;
                
                if (_PatternType == 1) // Dot pattern
                {
                    float dotPhase = fmod(i.texcoord.x * _RectHeight, _DotSpacing) / _DotSpacing;
                    patternAlpha = step(0.5, sin(dotPhase * 6.28318)) * 0.5 + 0.5;
                }
                else if (_PatternType == 2) // Dash pattern
                {
                    float dashPhase = fmod(i.texcoord.x * _RectHeight, _DashLength + _GapLength);
                    patternAlpha = step(dashPhase, _DashLength);
                }
                else if (_PatternType == 3) // Texture pattern
                {
                    float2 patternUV = float2(i.texcoord.x * _PatternScale, lineIndex * 0.1);
                    patternAlpha = tex2D(_PatternTexture, patternUV).a;
                }
                // _PatternType == 0 (solid) 保持 patternAlpha = 1.0
                
                fixed4 color = i.color;
                color.a *= lineAlpha * patternAlpha;
                
                // RectMask2D 裁剪支援
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}