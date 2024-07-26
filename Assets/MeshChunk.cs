using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
//přidat noise, tak aby jako input byla pozice na noise mapě, a bylo to správně i při lod levelech
public class MeshChunk
{
    int xsize;
    int ysize;
    float lod;
    float unit_size;

    public MeshChunk(int xsize, int ysize, float lod)
    {
        this.lod = lod;
        this.xsize = (int)(xsize * lod);
        this.ysize = (int)(ysize * lod);
        unit_size = (float)xsize/(float)this.xsize;
    }

    public Mesh mesh;
    public Vector3[] vertices;
    public int[] triangles;

    public Mesh GenerateMesh(){  

        mesh = new Mesh();
        vertices = new Vector3[(xsize+1)*(ysize+1)];
        triangles = new int[xsize*ysize*6];

        for(int z = 0, i = 0; z <= ysize; z++){
            for(int x = 0; x <= xsize; x++){
                vertices[i] = new Vector3(x*unit_size,0,z*unit_size);
                i++;
            }
        }


        for(int z = 0, i = 0; z < ysize; z++){
            for(int x = 0; x < xsize; x++){
                int n_quad = 6*i;
                int n_rows = z*(xsize+1);
                
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
    public void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
