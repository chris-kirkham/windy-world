Shader "Editor/Wind/WindArrow"
{
    Properties
    {
		_ActiveCol ("Colour when active", Color) = (0,1,0,1)
		_InactiveCol ("Colour when inactive", Color) = (0,0.5,0,1)
		[Toggle(IS_ACTIVE)] _IsActive ("Active?", Float) = 0
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

			#pragma shader_feature IS_ACTIVE

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 normal : TEXCOORD0;
            };

			half4 _ActiveCol, _InactiveCol;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float3 lightDir = float3(0, 1, 0);
				half diffuse = (dot(i.normal, lightDir) * 0.5) + 0.75;
				
				#ifdef IS_ACTIVE
					return _ActiveCol * diffuse;
				#else
					return _InactiveCol * diffuse;
				#endif
            }
            ENDCG
        }
    }
}
