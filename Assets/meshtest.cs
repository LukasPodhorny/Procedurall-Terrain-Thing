using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class meshtest : MonoBehaviour
{
    public Material mat;
    void Start()
    {
        MeshChunk chunk = new MeshChunk(10,10,1);
        Mesh mesh = chunk.GenerateMesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = mat;
    }
}
