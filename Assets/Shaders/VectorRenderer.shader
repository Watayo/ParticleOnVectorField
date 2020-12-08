Shader "myShader/VectorRenderer"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma instancing_options procedural:setup

		#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
			float4 color : COLOR;
		};
		// Boidの構造体
		struct VectorData
		{
			float3 position;
			float3 direction;
			float dirScalar;
		};

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		// データの構造体バッファ
		StructuredBuffer<VectorData> _VectorFieldDataBuffer;
		#endif

		sampler2D _MainTex; // テクスチャ

		half   _Glossiness; // 光沢
		half   _Metallic;   // 金属特性
		fixed4 _Color;      // カラー

		float3 _ObjectScale;

		// オイラー角（ラジアン）を回転行列に変換
		float4x4 eulerAnglesToRotationMatrix(float3 angles)
		{
			float ch = cos(angles.y); float sh = sin(angles.y); // heading
			float ca = cos(angles.z); float sa = sin(angles.z); // attitude
			float cb = cos(angles.x); float sb = sin(angles.x); // bank

			// Ry-Rx-Rz (Yaw Pitch Roll)
			return float4x4(
				ch * ca + sh * sb * sa, -ch * sa + sh * sb * ca, sh * cb, 0,
				cb * sa, cb * ca, -sb, 0,
				-sh * ca + ch * sb * sa, sh * sa + ch * sb * ca, ch * cb, 0,
				0, 0, 0, 1
			);
		}

		// 頂点シェーダ
		void vert(inout appdata_full v)
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

			// インスタンスIDからBoidのデータを取得
			VectorData VFData = _VectorFieldDataBuffer[unity_InstanceID];

			float3 pos = VFData.position.xyz; // 位置を取得
			float3 scl = _ObjectScale;          // スケールを取得

			// オブジェクト座標からワールド座標に変換する行列を定義
			float4x4 object2world = (float4x4)0;
			// スケール値を代入
			scl.y *= VFData.dirScalar*0.3;
			object2world._11_22_33_44 = float4(scl.xyz, 1.0);

			float3 dir = VFData.direction.xyz;
			float3 u = float3(0.0, 1.0, 0.0);
			float4 quaternion1= rotationTo(dir, u);

			// オイラー角（ラジアン）から回転行列を求める
			object2world = mul(object2world * eulerAnglesToRotationMatrix(rotX, rotY, rotZ));
			object2world._14_24_34 = pos.xyz;

			// // 頂点を座標変換
			v.vertex = mul(object2world, v.vertex);
			// 法線を座標変換
			v.normal = normalize(mul(object2world, v.normal));
			#endif
		}

		void setup()
		{
		}

		// サーフェスシェーダ
		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			// o.Albedo = c.rgb * IN.color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

			o.Emission = IN.color;

		}
		ENDCG
	}
	FallBack "Diffuse"
}