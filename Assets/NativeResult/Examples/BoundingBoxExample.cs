using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

public class BoundingBoxExample : MonoBehaviour {

    public int particles;

    public float speed;
    public float scatterRadius;

    public float size;
    public Mesh mesh;
    public Material material;

    private NativeArray<float3> _particles;
    private NativeArray<Matrix4x4> _particleMatrices;
    private Matrix4x4[] _renderMatrices;

    private Result<Bounds, MaximumBounds> _bounds;

    private void OnEnable() {
        _particles = new NativeArray<float3>(particles, Allocator.Persistent);
        _particleMatrices = new NativeArray<Matrix4x4>(particles, Allocator.Persistent);
        _renderMatrices = new Matrix4x4[1023];

        _bounds = new Result<Bounds, MaximumBounds>(Allocator.Persistent);

        Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled = true;
        Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = false;
    }

    private void OnDisable() {
        _particles.Dispose();
        _particleMatrices.Dispose();
        _bounds.Dispose();
    }

    private void Update() {
        new SimulateParticlesJob() {
            particles = _particles,
            matrices = _particleMatrices,
            time = Time.time * speed,
            scatterRadius = scatterRadius,
            scale = size
        }.Schedule(_particles.Length, 128).Complete();

        int toDraw = _particles.Length;
        int offset = 0;
        while (toDraw > 0) {
            int batchSize = Mathf.Min(1023, toDraw);

            for (int i = 0; i < batchSize; i++) {
                _renderMatrices[i] = _particleMatrices[i + offset];
            }

            Graphics.DrawMeshInstanced(mesh, 0, material, _renderMatrices, batchSize);

            toDraw -= batchSize;
            offset += batchSize;
        }
    }

    [BurstCompile]
    public struct SimulateParticlesJob : IJobParallelFor {

        public NativeArray<float3> particles;
        public NativeArray<Matrix4x4> matrices;
        public float time;
        public float scatterRadius;
        public float scale;

        public void Execute(int index) {
            float timeOffset = 0.5f * noise.srnoise(new float2(index * 0.01f, index * 0.02f), index * 0.005f);
            timeOffset = math.pow(timeOffset, 2f);

            float3 center = getCenter(time - timeOffset);
            float3 offset = getOffset(index);

            particles[index] = center + offset * math.pow(timeOffset, 0.7f) * 2;
            matrices[index] = Matrix4x4.TRS(particles[index], Quaternion.identity, Vector3.one * scale);
        }

        private float3 getCenter(float time) {
            float3 center;
            center.x = noise.srnoise(new float2(time * 2.4f, time * 1.2f), time * 0.3f) * 2;
            center.y = noise.srnoise(new float2(time * 0.8f, time * 0.2f), time * 0.5f) * 2;
            center.z = noise.srnoise(new float2(time * 1.4f, time * 2.2f), time * 0.2f) * 2;
            return center;
        }

        private float3 getOffset(int index) {
            float oTime = (time * 0.3f + index * 0.0005f);

            float3 offset;
            offset.x = noise.srnoise(new float2(oTime * 1.4f, oTime * 3.1f), oTime * 1.3f) * 2;
            offset.y = noise.srnoise(new float2(oTime * 3.3f, oTime * 1.5f), oTime * 1.5f) * 2;
            offset.z = noise.srnoise(new float2(oTime * 0.4f, oTime * 3.2f), oTime * 1.2f) * 2;
            return offset * scatterRadius;
        }
    }

    [BurstCompile]
    public struct CalculateBoundingBoxJob : IJobParallelFor {

        public NativeArray<float3> particles;
        public Result<Bounds, MaximumBounds>.Concurrent bounds;

        public void Execute(int index) {
            bounds.Write(new Bounds(particles[index], Vector3.zero));
        }
    }

    private void OnDrawGizmos() {
        if (Application.isPlaying) {
            _bounds.Reset();

            new CalculateBoundingBoxJob() {
                particles = _particles,
                bounds = _bounds
            }.Schedule(_particles.Length, 128).Complete();

            Bounds boundingBox = _bounds.Value;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(boundingBox.center, boundingBox.size);
        }
    }
}
