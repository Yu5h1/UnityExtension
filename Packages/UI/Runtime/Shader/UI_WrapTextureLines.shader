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
                
                // �ʺA�p�� rect ����
                float4 topVertex = float4(v.vertex.x, v.vertex.y + 1, v.vertex.z, v.vertex.w);
                float4 topWorld = mul(unity_ObjectToWorld, topVertex);
                float4 bottomWorld = mul(unity_ObjectToWorld, v.vertex);
                o.rectHeight = length(topWorld.xyz - bottomWorld.xyz) * 100; // �ഫ������
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = i.color;
                
                if (_UseMathLine > 0.5)
                {
                    // �ƾǽu���Ҧ�
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
                    // �K�Ͻu���Ҧ� - �C���u���O�W�ߪ��K��
                    float pixelY = i.texcoord.y * i.rectHeight + (_StartOffset * i.rectHeight);
                    
                    // �p���e���ĴX��
                    float lineIndex = floor(pixelY / _PixelLineSpacing);
                    
                    // �p��b��e�椺����m�]�����^
                    float pixelInLine = pixelY - lineIndex * _PixelLineSpacing;
                    
                    // �P�_�O�_�b�u���ϰ줺
                    if (pixelInLine <= _LineThickness)
                    {
                        // ���C���u�ЫؿW�ߪ� UV �y��
                        float2 lineUV;
                        
                        // X ��V�G�ϥέ�l texcoord.x�A�C���u���q�Y�}�l tiling
                        lineUV.x = i.texcoord.x;
                        
                        // Y ��V�G�b�u���p�פ��M�g��K�Ϫ����� Y �d�� (0-1)
                        lineUV.y = pixelInLine / _LineThickness;
                        
                        // ���� Unity �� tiling �M offset �]�w
                        lineUV = TRANSFORM_TEX(lineUV, _LineTexture);
                        
                        // �ļ˽u���K��
                        fixed4 lineTexture = tex2D(_LineTexture, lineUV);
                        
                        // �N�K���C��P���I�C��V�X
                        color.rgb *= lineTexture.rgb;
                        color.a *= lineTexture.a;
                    }
                    else
                    {
                        // ���b�u���ϰ줺�A�����z��
                        color.a = 0;
                    }
                }
                
                // RectMask2D ���Ť䴩
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}