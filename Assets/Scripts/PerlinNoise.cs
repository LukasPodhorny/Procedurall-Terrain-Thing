using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Threading.Tasks;

[System.Serializable]
public class PerlinNoise
{
    // active checkbox is used for noise editor
    public bool active;
    public int octaves;
    [Range(0,1)]
    public float frequency;
    public float amplitude;
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;
    public AnimationCurve redistribution;
    public float offsetX;
    public float offsetY;

    [HideInInspector]
    public float max_value;

    public PerlinNoise(AnimationCurve redistribution, int octaves = 4, float frequency = 0.25f, float amplitude = 1, float persistence = 0.5f, float lacunarity = 2, float offsetX = 0, float offsetY = 0)
    {
        this.octaves = octaves;
        this.frequency = frequency;
        this.amplitude = amplitude;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.redistribution = redistribution;

        max_value = NoiseMaxValue(octaves, amplitude, persistence);
    }
    public float GetNoiseValue(float x, float y)
    {
        float n_frequency = frequency;
        float n_amplitude = amplitude;
        float value = 0;

        for (int i = 0; i < octaves; i++)
        {
            value += n_amplitude * redistribution.Evaluate(Mathf.PerlinNoise((x + offsetX) * n_frequency, (y + offsetY) * n_frequency));
            n_amplitude *= persistence;
            n_frequency *= lacunarity;
        }

        return value / max_value;
    }
    public float[,] GenerateNoiseMap(int xsize, int ysize, float lod)
    {
        int lod_xsize = Mathf.RoundToInt(xsize * lod);
        int lod_ysize = Mathf.RoundToInt(ysize * lod);

        float[,] height_map = new float[lod_xsize, lod_ysize];

        Parallel.For(0, lod_ysize, y =>
        {
            for (int x = 0; x < lod_xsize; x++)
            {
                float value = GetNoiseValue(x / lod, y / lod);
                height_map[x, y] = value;
            }
        });

        return height_map;
    }
    public void UpdateMaxValue()
    {
        max_value = NoiseMaxValue(octaves, amplitude, persistence);
    }
    public static float NoiseMaxValue(int octaves, float amplitude, float persistence)
    {
        float max_value = 0;
        float n_amplitude = amplitude;

        for (int i = 0; i < octaves; i++)
        {
            max_value += n_amplitude;
            n_amplitude *= persistence;
        }

        return max_value;
    }
}

