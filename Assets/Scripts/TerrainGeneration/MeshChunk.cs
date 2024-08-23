using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshChunk
{
    public int xsize;
    public int ysize;
    public float lod;
    public float unit_size;
    public float max_depth;

    public MeshChunk(int xsize, int ysize, float lod, float max_depth)
    {
        this.lod = lod;
        this.xsize = Mathf.RoundToInt(xsize * lod);
        this.ysize = Mathf.RoundToInt(ysize * lod);
        this.max_depth = max_depth;
        unit_size = (float)xsize / (float)this.xsize;
    }

    public Mesh mesh;
    public Vector3[] vertices;
    public Vector2[] uvs;
    public Color[] colors;
    public int[] triangles;

    public Mesh GenerateMesh()
    {

        mesh = new Mesh();
        vertices = new Vector3[(xsize + 1) * (ysize + 1)];
        colors = new Color[(xsize + 1) * (ysize + 1)];
        uvs = new Vector2[(xsize + 1) * (ysize + 1)];
        triangles = new int[xsize * ysize * 6];

        for (int z = 0, i = 0; z <= ysize; z++)
        {
            for (int x = 0; x <= xsize; x++)
            {
                vertices[i] = new Vector3(x * unit_size, 0, z * unit_size);
                uvs[i] = new Vector2((float)x / xsize, (float)z / ysize);
                i++;
            }
        }


        for (int z = 0, i = 0; z < ysize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                int n_quad = 6 * i;
                int n_rows = z * (xsize + 1);

                triangles[n_quad + 0] = x + n_rows;
                triangles[n_quad + 1] = 1 + xsize + x + n_rows;
                triangles[n_quad + 2] = 1 + x + n_rows;
                triangles[n_quad + 3] = 1 + x + n_rows;
                triangles[n_quad + 4] = 1 + xsize + x + n_rows;
                triangles[n_quad + 5] = 2 + xsize + x + n_rows;

                i++;
            }
        }

        UpdateMesh();
        return mesh;
    }
    public void UpdateMeshNoiseMap(float[,] noisemap, Vector3Int offset, float height_multiplier, float max_lod, Gradient mesh_color)
    {
        int lod_ratio = (int)(max_lod / lod);
        for (int z = 0, i = 0; z <= ysize; z++)
        {
            for (int x = 0; x <= xsize; x++)
            {
                int sample_x = x * lod_ratio + offset.x;
                int sample_z = z * lod_ratio + offset.z;

                vertices[i].y = (noisemap[sample_x, sample_z]-0.4f) * height_multiplier;
                colors[i] = mesh_color.Evaluate(noisemap[sample_x, sample_z]);
                i++;
            }
        }

        UpdateMesh();
    }
    public void UpdateMeshNoiseMap1D(float[] noisemap, Vector3Int offset, float height_multiplier, int max_lod, int map_width, Color[] mesh_color)
    {
        int lod_ratio = (int)(max_lod / lod);
        
        for (int z = 0, i = 0; z <= ysize; z++)
        {
            for (int x = 0; x <= xsize; x++)
            {
                int noiseMapIndex = x * lod_ratio + offset.x + (z * lod_ratio + offset.z) * map_width * max_lod;
                vertices[i].y = (noisemap[noiseMapIndex] - 0.4f) * height_multiplier;
                colors[i] = mesh_color[Mathf.RoundToInt(noisemap[noiseMapIndex] *  mesh_color.Length)];
                i++;
            }
        }
        
        UpdateMesh();
    }
    public void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
