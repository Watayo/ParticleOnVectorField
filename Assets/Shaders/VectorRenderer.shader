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
		float3 _ObjectScale;

		sampler2D _MainTex; // テクスチャ

		half   _Glossiness; // 光沢
		half   _Metallic;   // 金属特性
		fixed4 _Color;      // カラー

		static const float PI = 3.14159265f;
    static const float DEG2RAD = PI/180.f;

		float4 AngleAxis(float aAngle, float3 aAxis)
 		{
			aAxis = normalize(aAxis);
			// float rad = aAngle * DEG2RAD * 0.5f;
			aAxis *= sin(aAngle * 0.5);
			return float4(aAxis.x, aAxis.y, aAxis.z, cos(aAngle * 0.5));
 		}

		// Quaternion From to Rotation
		float4 fromToRotationQuat(float3 aFrom, float3 aTo) {
			float3 axis = cross(aFrom, aTo);
			float angle = acos(dot(normalize(aFrom), normalize(aTo)));
			return AngleAxis(angle, axis);
		}

		// https://gist.github.com/mattatz/40a91588d5fb38240403f198a938a593
		// A given angle of rotation about a given axis
		float4 rotate_angle_axis(float angle, float3 axis)
		{
				float sn = sin(angle * 0.5);
				float cs = cos(angle * 0.5);
				return float4(axis * sn, cs);
		}

		// https://stackoverflow.com/questions/1171849/finding-quaternion-representing-the-rotation-from-one-vector-to-another
		float4 from_to_rotation(float3 v1, float3 v2)
		{
			float4 q;
			float d = dot(v1, v2);
			if (d < -0.999999)
			{
					float3 right = float3(1, 0, 0);
					float3 up = float3(0, 1, 0);
					float3 tmp = cross(right, v1);
					if (length(tmp) < 0.000001)
					{
							tmp = cross(up, v1);
					}
					tmp = normalize(tmp);
					q = rotate_angle_axis(PI, tmp);
			} else if (d > 0.999999) {
					q = float4(0.0, 0.0, 0.0, 1.0);
			} else {
					q.xyz = cross(v1, v2);
					q.w = 1 + d;
					q = normalize(q);
			}
			return q;
		}

		float4x4 quaternion_to_matrix(float4 quat)
		{
			float4x4 m = float4x4(float4(0, 0, 0, 0), float4(0, 0, 0, 0), float4(0, 0, 0, 0), float4(0, 0, 0, 0));

			float x = quat.x, y = quat.y, z = quat.z, w = quat.w;
			float x2 = x + x, y2 = y + y, z2 = z + z;
			float xx = x * x2, xy = x * y2, xz = x * z2;
			float yy = y * y2, yz = y * z2, zz = z * z2;
			float wx = w * x2, wy = w * y2, wz = w * z2;

			m[0][0] = 1.0 - (yy + zz);
			m[0][1] = xy - wz;
			m[0][2] = xz + wy;

			m[1][0] = xy + wz;
			m[1][1] = 1.0 - (xx + zz);
			m[1][2] = yz - wx;

			m[2][0] = xz - wy;
			m[2][1] = yz + wx;
			m[2][2] = 1.0 - (xx + yy);

			m[3][3] = 1.0;

			return m;
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
			scl.y *= VFData.dirScalar*0.5 + 0.25;
			object2world._11_22_33_44 = float4(scl.xyz, 1.0);

			// quaternion回転行列をかける
			object2world = mul(quaternion_to_matrix(from_to_rotation(float3(0.0, 1.0, 0.0), VFData.direction)), object2world);

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
			o.Albedo = c.rgb * IN.color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

			// o.Emission = IN.color;

		}
		ENDCG
	}
	FallBack "Diffuse"
}