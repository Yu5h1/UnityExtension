Shader "UI/EnvelopeLinesAA"
{
    Properties
    {
        _LineSpacing ("Line Spacing (in UV)", Float) = 0.2
        _LineThickness ("Line Thickness", Float) = 0.005
        _StartOffset ("Start Offset (in UV)", Float) = 0
        _RectHeight ("Rect Height", Float) = 100
        _PixelLineSpacing ("Pixel Line Spacing", Float) = 20
        _PixelThickness ("Pixel Thickness", Float) = 1
        
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


                // 使用像素級計算，並加入抗鋸齒
                float pixelY = (1.0 - i.texcoord.y) * _RectHeight - (_StartOffset * _RectHeight);
                
                // 計算到最近線條的距離
                float lineIndex = round(pixelY / _PixelLineSpacing);
                float lineCenter = lineIndex * _PixelLineSpacing;
                float distanceToLine = abs(pixelY - lineCenter);
                
                // 抗鋸齒的線條邊緣
                float halfThickness = _PixelThickness * 0.5;
                float lineAlpha = 1.0 - smoothstep(halfThickness - 0.5, halfThickness + 0.5, distanceToLine);
                
                fixed4 color = i.color;
                color.a *= lineAlpha;
                
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