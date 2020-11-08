
Shader "Instanced/Boid_InstancedIndirectCompute"
{
	Properties{
		_ArrowScale("Arrow scale", Float) = 1.0
		_Colour("Colour", Color) = (1, 1, 1, 1)
		_Emission("Emission", Color) = (0, 0, 0, 1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
			// Physically based Standard lighting model
			#pragma surface surf Standard vertex:vert addshadow
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			#include "../Cgincs/WindFieldPoint.cginc"

			float _ArrowScale;

			float4 _Colour;
			float4 _Emission;
			half _Glossiness;
			half _Metallic;

			//assigned to in setup()
			float4x4 _LookAt;
			float4 _Position;

			struct Input {
				float2 uv_MainTex;
			};

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			//https://github.com/Shinao/Unity-GPU-Boids/blob/master/Assets/3-GPU_Boids_Compute_Draw/Boids_Simple.shader - has lookat matrix
			//also https://stackoverflow.com/questions/11190786/3d-rotation-matrix-from-direction-vector-forward-up-right
			StructuredBuffer<WindFieldPoint> points;
			#endif

		//constructs a look-at matrix from the given position and forward/up vectors
		float4x4 lookAtMatrix(float3 position, float3 forward, float3 up)
		{
			float3 zAxis = normalize(position - forward);
			float3 xAxis = normalize(cross(up, zAxis));
			float3 yAxis = cross(zAxis, xAxis);

			return float4x4
				(
					xAxis.x, yAxis.x, zAxis.x, 0,
					xAxis.y, yAxis.y, zAxis.y, 0,
					xAxis.z, yAxis.z, zAxis.z, 0,
					0, 0, 0, 1
					);
		}

		void setup()
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			
				/// Positions are calculated in the compute shader.
				/// here we just use them.
				_Position = points[unity_InstanceID].pos;
				float scale = Length(points[unity_InstanceID].wind) * _ArrowScale;
				float3 forward = points[unity_InstanceID].wind;

				_LookAt = lookAtMatrix(_Position, _Position + (forward * -1), float3(0.0, 1.0, 0.0));

				unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
				unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
				unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
				unity_ObjectToWorld._14_24_34_44 = float4(_Position.xyz, 1);
				unity_WorldToObject = unity_ObjectToWorld;
				unity_WorldToObject._14_24_34 *= -1;
				unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
			#endif
		}

		void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				v.vertex = mul(_LookAt, v.vertex);
				//v.vertex.xyz += _BoidPos;
			#endif
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float4 col = 1.0f;

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				col = _Colour;
			#else
				col = float4(1, 0, 1, 1);
			#endif
				o.Albedo = col.rgb;
				o.Emission = _Emission.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = col.a;
		}
		ENDCG
		}
			FallBack "Diffuse"
}