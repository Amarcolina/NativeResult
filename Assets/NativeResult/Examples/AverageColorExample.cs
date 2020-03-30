using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

public class AverageColorExample : MonoBehaviour {

    [Range(8, 512)]
    public int resolution = 128;
    public float scale = 1;
    public float speed = 1;
    public bool useResultOp = true;
    public Renderer textureQuad;
    public Renderer averageQuad;

    private Texture2D _texture;
    private NativeArray<Color> _colors;

    private Result<Color, Sum> sum;

    private void OnEnable() {
        _texture = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, mipChain: false, linear: true);
        _texture.filterMode = FilterMode.Bilinear;
        _texture.wrapMode = TextureWrapMode.Clamp;

        _colors = _texture.GetRawTextureData<Color>();

        sum = new Result<Color, Sum>(Allocator.Persistent);

        Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled = true;
        Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = false;
    }

    private void OnDisable() {
        sum.Dispose();
    }

    private void Update() {
        new ProceduralTextureJob() {
            colors = _colors,
            scale = scale,
            resolution = resolution,
            time = Time.time * speed
        }.Schedule(_colors.Length, resolution).Complete();

        _texture.Apply();

        sum.Reset();

        new SumColorsWithResultJob() {
            colors = _colors,
            sum = sum
        }.Schedule(_colors.Length, resolution).Complete();

        Color averageColor = sum.Value / (resolution * resolution);

        textureQuad.material.mainTexture = _texture;
        averageQuad.material.color = averageColor;
    }

    [BurstCompile]
    public struct ProceduralTextureJob : IJobParallelFor {

        public NativeArray<Color> colors;
        public float scale;
        public int resolution;
        public float time;

        public void Execute(int index) {
            float2 pos = scale * new float2(index % resolution, index / resolution) / resolution;

            Color c = new Color();
            c.r = sample(pos, 0, 0, 0);
            c.g = sample(pos, 0.1f, 0.1f, 0.1f);
            c.b = sample(pos, 0.2f, 0.2f, 0.2f);
            c.a = 1;

            colors[index] = c;
        }

        private float sample(float2 pos, float dx, float dy, float offset) {
            float2 offsetPos = pos + new float2(dx, dy);

            return math.saturate(noise.srnoise(offsetPos + new float2(time, time), time + offset) +
                                 noise.srnoise(offsetPos - new float2(time, time), time + offset));
        }
    }

    [BurstCompile]
    public struct SumColorsWithResultJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<Color> colors;
        public Result<Color, Sum>.Concurrent sum;

        public void Execute(int index) {
            sum.Write(colors[index]);
        }
    }
}
