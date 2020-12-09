using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class ComputeShaderScript : MonoBehaviour {
    // VectorFieldの構造体
    [System.Serializable]
    struct VectorFieldData {
        public Vector3 position;
        public Vector3 direction;
        public float dirScalar;
    }

    struct ParticleData {
        public Vector3 velocity;
        public Vector3 position;
        public Vector4 color;
        public float scale;
    }

    #region Grid
    private int X_GRID = 8;
    private int Y_GRID = 8;
    private int Z_GRID = 8;
    #endregion
    private int X_THREAD = 8*8*8;
    private int Y_THREAD = 1;
    private int Z_THREAD = 1;
    #region XYZ_THREAD



    #endregion

    #region ComputeShader & Buffer
    public ComputeShader _cs;
    // Direction格納したバッファ
    public ComputeBuffer _VectorFieldDataBuffer;
    public ComputeBuffer _ParticleDataBuffer;
    #endregion

    #region Particle Parameter
    [SerializeField] private int _ParticleCount = 0;
    [SerializeField] private Color _ParticleColor = Color.blue;
    [SerializeField] public Vector3 DebugDir = new Vector3(1, 0, 0);
    #endregion

    #region Accessors
    // 基本データを格納したバッファを取得
    public ComputeShader GetComputeShader()
    {
        return this._cs != null ? this._cs : null;
    }
    public ComputeBuffer GetVectorFieldDataBuffer()
    {
        return this._VectorFieldDataBuffer != null ? this._VectorFieldDataBuffer : null;
    }
    public ComputeBuffer GetParticleDataBuffer() {
        return this._ParticleDataBuffer != null ? this._ParticleDataBuffer : null;
    }
    public int GetGridNum()
    {
        return X_GRID*Y_GRID*Z_GRID;
    }
    public int GetParticleNum() {
        return _ParticleCount;
    }
    public Vector3 GetAreaCenter()
    {
        return new Vector3(0, 0, 0);
    }
    public Vector3 GetAreaSize()
    {
        return new Vector3(500f, 500f, 500f);
    }
    #endregion

    /// <summary>
    /// 破棄
    /// </summary>
    void OnDisable() {
        if(_VectorFieldDataBuffer != null) {
            _VectorFieldDataBuffer.Release();
            _VectorFieldDataBuffer = null;
        }
        if(_ParticleDataBuffer != null) {
            _ParticleDataBuffer.Release();
            _ParticleDataBuffer = null;
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    void Start() {
        InitializeVectorFieldComputeBuffer();
        InitializeParticleComputeBuffer();
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update() {
        ComputeShader cs = _cs;
        int VectorFieldKernel = cs.FindKernel("VectorFieldMain");
        Debug.Log(VectorFieldKernel);
        cs.SetBuffer(VectorFieldKernel, "_VectorFieldDataBuffer", _VectorFieldDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.SetFloat("_FrameCount", Time.frameCount);
        cs.Dispatch(VectorFieldKernel, GetGridNum()/X_THREAD, 1, 1);

        int ParticleKernel = cs.FindKernel("ParticleMain");
        cs.SetBuffer(ParticleKernel, "_VectorFieldDataBuffer", _VectorFieldDataBuffer);
        cs.SetBuffer(ParticleKernel, "_ParticleDataBuffer", _ParticleDataBuffer);
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.Dispatch(ParticleKernel, _ParticleCount/X_THREAD, 1, 1);
    }

    /// <summary>
    /// Vectorfield computebuffer Init
    /// </summary>
    void InitializeVectorFieldComputeBuffer() {
        Vector3[] Position = new Vector3[X_GRID*Y_GRID*Z_GRID];
        Vector3[] DirVector = new Vector3[X_GRID*Y_GRID*Z_GRID];
        float[] DirScalar = new float[X_GRID*Y_GRID*Z_GRID];
        for (int z = 0; z < Z_GRID; z++) {
            for (int y = 0; y < Y_GRID; y++) {
                for(int x = 0; x < X_GRID; x++) {
                    int i = z * (Y_GRID * X_GRID) + y * (X_GRID) + x;
                    Position[i] = new Vector3(x - (float)(X_GRID-1)/2, y - (float)(Y_GRID-1)/2, z - (float)(Z_GRID-1)/2);
                    DirVector[i] = new Vector3(Position[i].x, Position[i].y, Position[i].z);
                    DirScalar[i] = DirVector[i].magnitude;
                }
            }
        }
        // keep the size of VectorFieldData.
        _VectorFieldDataBuffer = new ComputeBuffer(GetGridNum(), Marshal.SizeOf(typeof(VectorFieldData)));

        // keep VectorFieldData Array.
        VectorFieldData[] VFData = new VectorFieldData[_VectorFieldDataBuffer.count];
        for (int i = 0; i < _VectorFieldDataBuffer.count; i++) {
            VFData[i].position = Position[i];
            VFData[i].direction = DirVector[i].normalized;
            VFData[i].dirScalar = DirScalar[i];
        }
        _VectorFieldDataBuffer.SetData(VFData);
    }

    /// <summary>
    /// Particle computebuffer Init
    /// </summary>
    void InitializeParticleComputeBuffer() {
        ParticleData[] particles = new ParticleData[_ParticleCount];

        for (int i = 0; i < _ParticleCount; i++) {
            particles[i] = new ParticleData {
                velocity = new Vector3(0.0f, 0.0f, 0.0f),
                position = Random.onUnitSphere * 1.1f,
                color = _ParticleColor,
                scale = 0.02f,
            };
        }
        // keep the size of ParticleData.
        _ParticleDataBuffer = new ComputeBuffer(_ParticleCount, Marshal.SizeOf(typeof(ParticleData)));
        _ParticleDataBuffer.SetData(particles);
    }
}
