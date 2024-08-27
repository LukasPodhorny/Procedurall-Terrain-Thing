using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using System.IO;
using Unity.Burst;

public class MeshGenerator : MonoBehaviour
{
    Chunk[] chunks;

    [Header("Mesh")]

    public int chunk_size;
    public Material mesh_material;
    [Tooltip("Make sure it's in descending order and smaller by double, not less!")]
    public int[] lods;
    int max_lod;

    [Header("Tiling")]

    [Range(1, 20)]
    public float view_distance;
    public float unit_size;
    public Transform player;

    Vector3 player_pos;
    Vector3Int player_grid_pos;
    Vector3Int player_last_grid_pos;
    Vector3Int[] points;

    [Header("NoiseMap")]
    public string DATA_NAME = "terrain_noise_data";
    public float height_multiplier;
    float[] noisemap;
    Color[] gradient_2048;
    NoiseParameters terrain_noise_parameters;
    public ComputeShader noiseCompute;

    void Start()
    {
        terrain_noise_parameters = JsonHelper.LoadClass<NoiseParameters>(DATA_NAME);

        gradient_2048 = NoiseMapGenerator.PreEvGradient(terrain_noise_parameters.color_map, 2048);

        float temp = Time.realtimeSinceStartup;
        max_lod = lods[0];
        noisemap = NoiseMapGenerator.GenerateCombinedNoiseMapGpu(terrain_noise_parameters, 1000, 1000, max_lod, noiseCompute);
        print(Mathf.RoundToInt((Time.realtimeSinceStartup - temp) * 1000) + "ms it took to compute world noisemap on gpu");

        float temp2 = Time.realtimeSinceStartup;
        GenerateChunks();
        print(Mathf.RoundToInt((Time.realtimeSinceStartup - temp2) * 1000) + "ms it took to calculate mesh cpu");
    }
    void Update()
    {
        UpdateChunks();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 255, 0, 0.2f);
        Vector3Int[] points = PointsInViewDist(view_distance, chunk_size);
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawCube((Vector3)points[i] * unit_size + new Vector3(chunk_size / 2, 0, chunk_size / 2) * unit_size, new Vector3(chunk_size * unit_size - 2, 0.2f, chunk_size * unit_size - 2));
        }
    }

    void GenerateChunks()
    {
        points = PointsInViewDist(view_distance, chunk_size);
        chunks = new Chunk[points.Length];

        for (int i = 0; i < chunks.Length; i++)
        {
            int lod_group = Mathf.Max(Mathf.Abs(points[i].x), Mathf.Abs(points[i].z)) / chunk_size;

            chunks[i] = new Chunk(chunk_size, chunk_size, lods[lod_group], mesh_material, lod_group, unit_size);
            chunks[i].chunk_object.transform.position = (Vector3)points[i] * unit_size;


            Vector3Int chunk_offset = (points[i] + new Vector3Int(400, 0, 400)) * max_lod;
            chunks[i].mesh_chunk.UpdateMeshNoiseMap1D(noisemap, chunk_offset, height_multiplier * unit_size, max_lod, 1000, gradient_2048);
            chunks[i].UpdateCollider();
        }
    }

    void UpdateChunks()
    {
        player_pos = player.transform.position;
        player_grid_pos = ToGridPos(player_pos, chunk_size * unit_size);

        if (player_grid_pos != player_last_grid_pos)
        {

            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].chunk_object.transform.position = (Vector3)points[i] * unit_size + (Vector3)player_grid_pos * chunk_size * unit_size;
                Vector3Int chunk_offset = (points[i] + player_grid_pos * chunk_size + new Vector3Int(400, 0, 400)) * max_lod;

                chunks[i].mesh_chunk.UpdateMeshNoiseMap1D(noisemap, chunk_offset, height_multiplier * unit_size, max_lod, 1000, gradient_2048);
                chunks[i].UpdateCollider();
            }
        }

        player_last_grid_pos = player_grid_pos;
    }

    Vector3Int[] PointsInViewDist(float radius, int chunk_size)
    {
        int top = Mathf.CeilToInt(-radius);
        int bottom = Mathf.FloorToInt(radius);
        int left = Mathf.CeilToInt(-radius);
        int right = Mathf.FloorToInt(radius);


        List<Vector3Int> points = new List<Vector3Int>();

        for (int z = top; z <= bottom; z++)
        {
            for (int x = left; x <= right; x++)
            {
                if (InsideCircle(x, z, radius))
                {
                    points.Add(new Vector3Int(x * chunk_size, 0, z * chunk_size));
                }
            }
        }
        return points.ToArray();
    }

    bool InsideCircle(int x, int y, float radius)
    {
        float distance = Mathf.Sqrt(x * x + y * y);
        return distance <= radius;
    }
    Vector3Int ToGridPos(Vector3 pos, float unit_size)
    {
        return new Vector3Int((int)Mathf.Floor(pos.x / unit_size), 0, (int)Mathf.Floor(pos.z / unit_size));
    }
}
class Chunk
{
    public MeshChunk mesh_chunk;
    public Mesh mesh;
    public GameObject chunk_object;
    public float lod_group;
    public float unit_size;
    public Chunk(int xsize, int ysize, float lod, Material mat, int lod_group, float unit_size)
    {
        this.lod_group = lod_group;
        this.unit_size = unit_size;

        mesh_chunk = new MeshChunk(xsize, ysize, lod, unit_size);
        mesh = mesh_chunk.GenerateMesh();

        chunk_object = new GameObject();
        chunk_object.AddComponent<MeshFilter>().mesh = mesh;
        chunk_object.AddComponent<MeshRenderer>().sharedMaterial = mat;

        if (lod_group == 0 || lod_group == 1)
        {
            chunk_object.AddComponent<MeshCollider>();
        }
    }

    public void UpdateCollider()
    {
        if (lod_group == 0 || lod_group == 1)
        {
            chunk_object.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
