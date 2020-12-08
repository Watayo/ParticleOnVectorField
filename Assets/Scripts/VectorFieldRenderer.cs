using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ComputeShaderScript))]

public class VectorFieldRenderer : MonoBehaviour {
    #region Paremeters
    // 描画するオブジェクトのスケール
    public Vector3 ObjectScale = new Vector3(0.2f, 1.5f, 0.2f);
    #endregion

    #region Script References
    // ComputeShaderスクリプトの参照
    public ComputeShaderScript csScript;
    #endregion

    #region Built-in Resources
    // 描画するメッシュの参照
    public Mesh InstanceMesh;
    // 描画のためのマテリアルの参照
    public Material InstanceRenderMaterial;
    #endregion

    #region Private Variables
    // GPUインスタンシングのための引数（ComputeBufferへの転送用）
    // インスタンスあたりのインデックス数, インスタンス数,
    // 開始インデックス位置, ベース頂点位置, インスタンスの開始位置
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    // GPUインスタンシングのための引数バッファ
    ComputeBuffer argsBuffer;
    #endregion

    /// <summary>
    /// 初期化
    /// </summary>
    void Start()
    {
        // 引数バッファを初期化
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint),
            ComputeBufferType.IndirectArguments);
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update() {
        // メッシュをインスタンシング
        RenderInstancedMesh();
    }

    void OnDisable() {
        // 引数バッファを解放
        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }

    void RenderInstancedMesh() {
        // 描画用マテリアルがNull, または, computeShaderスクリプトがNull,
        // またはGPUインスタンシングがサポートされていなければ, 処理をしない
        if (InstanceRenderMaterial == null || csScript == null || !SystemInfo.supportsInstancing) {
            return;
        }

        // 指定したメッシュのインデックス数を取得
        uint numIndices = (InstanceMesh != null) ?
            (uint)InstanceMesh.GetIndexCount(0) : 0;
        args[0] = numIndices; // メッシュのインデックス数をセット
        args[1] = (uint)csScript.GetGridNum(); // インスタンス数をセット
        argsBuffer.SetData(args); // バッファにセット

        // VectorFieldデータを格納したバッファをマテリアルにセット
        InstanceRenderMaterial.SetBuffer("_VectorFieldDataBuffer",
            csScript.GetVectorFieldDataBuffer());
        // vectorFieldオブジェクトスケールをセット
        InstanceRenderMaterial.SetVector("_ObjectScale", ObjectScale);
        // 境界領域を定義
        var bounds = new Bounds
        (
            csScript.GetAreaCenter(), // 中心
            csScript.GetAreaSize()    // サイズ
        );

        // メッシュをGPUインスタンシングして描画
        Graphics.DrawMeshInstancedIndirect
        (
            InstanceMesh,           // インスタンシングするメッシュ
            0,                      // submeshのインデックス
            InstanceRenderMaterial, // 描画を行うマテリアル
            bounds,                 // 境界領域
            argsBuffer              // GPUインスタンシングのための引数のバッファ
        );
    }
}