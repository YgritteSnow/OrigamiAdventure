Shader "* JJ/unlit_cull_off"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeTex("Texture", 2D) = "white" {}
		_EdgeColor("Color", Color) = (1,1,1,1)
		_EdgeShadow("EdgeShadow", Range(0,50)) = 10.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off

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
				float2 uv_shadow : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float2 uv_shadow : TEXCOORD1;
			};

			sampler2D _MainTex;
			sampler2D _EdgeTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			float _EdgeShadow;
			fixed4 _EdgeColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv_shadow = v.uv_shadow;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				//col = col * (1-pow(i.uv_shadow.x, _EdgeShadow) * (1-_EdgeColor));
				fixed4 edge_col = tex2D(_EdgeTex, i.uv_shadow);
				col = col * edge_col;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
