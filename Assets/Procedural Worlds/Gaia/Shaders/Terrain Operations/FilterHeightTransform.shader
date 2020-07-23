    Shader "Hidden/GaiaPro/HeightTransform" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}	
				//The brush texture
				_BrushTex("Brush Texture", any) = "" {}
				//1-pixel high distance mask texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}
				 //Flag to determine whether the Distance mask is inverted or not
				 _InvertImageMask("Invert Image Mask", Float) = 0
				 //Strength from 0 to 1 to determine how "strong" the distance mask effect is applied
				 _Strength ("Strength", Float) = 0

				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _BrushTex;
			sampler2D _HeightTransformTex;
			float _InvertImageMask;
			float _Strength;
			
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

           

            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                return o;
            }
		ENDCG
            

         Pass    // 0 Height Transform
        {
            Name "Height Transform"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment HeightTransform

            float4 HeightTransform(v2f i) : SV_Target
            {
				float inputHeight = tex2D(_InputTex, i.pcUV);
				float test = UnpackHeightmap(tex2D(_HeightTransformTex, i.pcUV));
				float transformedHeight = lerp(0.0f,0.5f,UnpackHeightmap(tex2D(_HeightTransformTex, inputHeight*2.0f)));
				float brushStrength = UnpackHeightmap(tex2D(_BrushTex, i.pcUV));
				//return PackHeightmap(transformedHeight * brushStrength);
				return PackHeightmap(lerp(inputHeight, transformedHeight, brushStrength));
            }
            ENDCG
        }

    }
    Fallback Off
}
