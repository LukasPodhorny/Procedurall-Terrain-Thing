using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using System.IO;

public class MeshGenerator : MonoBehaviour
{
    Chunk[] chunks;

    [Header("Mesh")]

    public int chunk_size;
    public Material mesh_mat;
    [Tooltip("Make sure it's in descending order and smaller by double, not less!")]
    public int[] lods;
    int max_lod;

    [Header("Tiling")]

    [Range(1, 20)]
    public float view_distance;
    public Transform player;

    Vector3 player_pos;
    Vector3Int player_grid_pos;
    Vector3Int player_last_grid_pos;
    Vector3Int[] points;

    [Header("NoiseMap")]
    public float height_multiplier;
    float[,] noisemap;

    void Start()
    {
        NoiseParameters terrain_noise_parameters = JsonHelper.LoadClass<NoiseParameters>("terrain_noise_data");
        
        max_lod = lods[0];
        var temp = Time.realtimeSinceStartup;
        noisemap = NoiseMapGenerator.GenerateCombinedNoiseMap(terrain_noise_parameters, 1000, 1000, max_lod);
        print(Time.realtimeSinceStartup-temp);
        
        GenerateChunks();
    }
    void Update()
    {
        UpdateChunks();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(255, 0, 0, 0.2f);
        Vector3Int[] points = PointsInViewDist(view_distance, chunk_size);
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawCube(points[i] + new Vector3Int(chunk_size / 2, 0, chunk_size / 2), new Vector3(chunk_size - 0.5f, 0.2f, chunk_size - 0.5f));
        }
    }

    void GenerateChunks()
    {
        points = PointsInViewDist(view_distance, chunk_size);
        chunks = new Chunk[points.Length];

        for (int i = 0; i < chunks.Length; i++)
        {
            int lod_group = Mathf.Max(Mathf.Abs(points[i].x), Mathf.Abs(points[i].z)) / chunk_size;

            chunks[i] = new Chunk(chunk_size, chunk_size, lods[lod_group], height_multiplier, mesh_mat);
            chunks[i].chunk_object.transform.position = points[i];


            Vector3Int chunk_offset = (points[i] + new Vector3Int(400, 0, 400)) * max_lod;
            chunks[i].mesh_chunk.UpdateMeshNoiseMap(noisemap, chunk_offset, height_multiplier, max_lod);
        }
    }

    void UpdateChunks()
    {
        player_pos = player.transform.position;
        player_grid_pos = ToGridPos(player_pos, chunk_size);

        if (player_grid_pos != player_last_grid_pos)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                int lod_group = Mathf.Max(Mathf.Abs(points[i].x), Mathf.Abs(points[i].z)) / chunk_size;
                chunks[i].chunk_object.transform.position = points[i] + player_grid_pos * chunk_size;

                Vector3Int chunk_offset = (points[i] + player_grid_pos * chunk_size + new Vector3Int(400, 0, 400)) * max_lod;
                chunks[i].mesh_chunk.UpdateMeshNoiseMap(noisemap, chunk_offset, height_multiplier, max_lod);
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

    Vector3Int ToGridPos(Vector3 pos, int unit_size)
    {
        return new Vector3Int((int)Mathf.Floor(pos.x / unit_size), 0, (int)Mathf.Floor(pos.z / unit_size));
    }
}
class Chunk
{
    public MeshChunk mesh_chunk;
    public Mesh mesh;
    public GameObject chunk_object;
    public Chunk(int xsize, int ysize, float lod, float amplitude, Material mat)
    {
        mesh_chunk = new MeshChunk(xsize, ysize, lod, amplitude);
        mesh = mesh_chunk.GenerateMesh();

        chunk_object = new GameObject();
        chunk_object.AddComponent<MeshFilter>().mesh = mesh;
        chunk_object.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
