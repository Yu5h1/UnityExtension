Shader "UI/TextureLines"
{
    Properties
    {
        _LineSpacing ("Line Spacing", Float) = 0.2
        _StartOffset ("Start Offset", Float) = 0
        _PixelLineSpacing ("Pixel Line Spacing", Float) = 20
        _LineThickness ("Line Thickness (pixels)", Float) = 2
        
        [Header(Line Texture Settings)]
        _LineTexture ("Line Texture", 2D) = "white" {}
        _UseMathLine ("Use Math Line (no texture)", Float) = 0
        
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
                float rectHeight : TEXCOORD2;
            };

            float _LineSpacing;
            float _StartOffset;
            float _PixelLineSpacing;
            float _LineThickness;
            
            sampler2D _LineTexture;
            float4 _LineTexture_ST;
            
            float _UseMathLine;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                
                // 動態計算 rect 高度
                float4 topVertex = float4(v.vertex.x, v.vertex.y + 1, v.vertex.z, v.vertex.w);
                float4 topWorld = mul(unity_ObjectToWorld, topVertex);
                float4 bottomWorld = mul(unity_ObjectToWorld, v.vertex);
                o.rectHeight = length(topWorld.xyz - bottomWorld.xyz) * 100; // 轉換為像素
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = i.color;
                
                if (_UseMathLine > 0.5)
                {
                    // 數學線條模式
                    float pixelY = i.texcoord.y * i.rectHeight + (_StartOffset * i.rectHeight);
                    float lineIndex = round(pixelY / _PixelLineSpacing);
                    float lineCenter = lineIndex * _PixelLineSpacing;
                    float distanceToLine = abs(pixelY - lineCenter);
                    
                    float halfThickness = _LineThickness * 0.5;
                    float lineAlpha = 1.0 - smoothstep(halfThickness - 0.5, halfThickness + 0.5, distanceToLine);
                    
                    color.a *= lineAlpha;
                }
                else
                {
                    // 貼圖線條模式 - 每條線都是獨立的貼圖
                    float pixelY = i.texcoord.y * i.rectHeight + (_StartOffset * i.rectHeight);
                    
                    // 計算當前位於第幾行
                    float lineIndex = floor(pixelY / _PixelLineSpacing);
                    
                    // 計算在當前行內的位置（像素）
                    float pixelInLine = pixelY - lineIndex * _PixelLineSpacing;
                    
                    // 判斷是否在線條區域內
                    if (pixelInLine <= _LineThickness)
                    {
                        // 為每條線創建獨立的 UV 座標
                        float2 lineUV;
                        
                        // X 方向：使用原始 texcoord.x，每條線都從頭開始 tiling
                        lineUV.x = i.texcoord.x;
                        
                        // Y 方向：在線條厚度內映射到貼圖的完整 Y 範圍 (0-1)
                        lineUV.y = pixelInLine / _LineThickness;
                        
                        // 應用 Unity 的 tiling 和 offset 設定
                        lineUV = TRANSFORM_TEX(lineUV, _LineTexture);
                        
                        // 採樣線條貼圖
                        fixed4 lineTexture = tex2D(_LineTexture, lineUV);
                        
                        // 將貼圖顏色與頂點顏色混合
                        color.rgb *= lineTexture.rgb;
                        color.a *= lineTexture.a;
                    }
                    else
                    {
                        // 不在線條區域內，完全透明
                        color.a = 0;
                    }
                }
                
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