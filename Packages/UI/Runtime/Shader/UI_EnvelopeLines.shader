Shader "UI/EnvelopeLines"
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
                o.color = v.color; // 直接使用 vertex color，已經包含 Graphic.color
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 使用像素級計算，更精確
                float pixelY = (1.0 - i.texcoord.y) * _RectHeight + (_StartOffset * _RectHeight);
                
                // 計算當前像素位於第幾行
                float lineIndex = floor(pixelY / _PixelLineSpacing);
                
                // 計算在當前行內的位置
                float positionInLine = pixelY - lineIndex * _PixelLineSpacing;
                
                // 判斷是否在線條區域內（更穩定的方法）
                float lineAlpha = step(positionInLine, _PixelThickness);
                
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