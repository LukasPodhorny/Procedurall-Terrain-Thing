using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderKeywordFilter;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

[ExecuteInEditMode]
public class NoiseMapGenerator : MonoBehaviour
{
    [Header("Texture")]
    Texture2D noisetexture;
    public GameObject map_object;
    public int xsize, ysize;


    [Header("Noise Parameters")]
    public PerlinNoise[] terrain_noises;
    public float offsetX;
    public float offsetY;
    public float frequency;
    public AnimationCurve redistribution;


    [Header("DANGER ZONE")]
    [Tooltip("WHEN CHECKED IT WILL OVERWRITE ALL DATA BASED ON VALUES IN INSPECTOR!")]
    public bool SAVE_DATA;

    void Start()
    {
        noisetexture = new Texture2D(xsize, ysize);
        map_object.gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = noisetexture;
    }
    void OnValidate()
    {
        if (SAVE_DATA)
        {
            NoiseParameters noise_parameters = new NoiseParameters(terrain_noises, offsetX, offsetY, frequency, redistribution);
            JsonHelper.SaveClass(noise_parameters, "terrain_noise_data");
        }

        UpdateNoiseTexture();
    }
    void Update()
    {
        if (Application.IsPlaying(map_object))
        {
            map_object.SetActive(false);
        }
    }
    void UpdateNoiseTexture()
    {
        if (noisetexture != null)
        {
            for (int i = 0; i < terrain_noises.Length; i++)
            {
                if (terrain_noises[i].active)
                {
                    terrain_noises[i].UpdateMaxValue();
                }
            }

            for (int y = 0; y < ysize; y++)
            {
                for (int x = 0; x < xsize; x++)
                {
                    float noise_value = redistribution.Evaluate(GetCombinedNoiseValue(new NoiseParameters(terrain_noises, offsetX, offsetY, frequency, redistribution), x, y, true));

                    if (map_object.activeInHierarchy)
                    {
                        noisetexture.SetPixel(x, y, Color.Lerp(Color.white, Color.black, noise_value));
                    }
                }
            }

            noisetexture.Apply();
        }
    }

    public static float[,] GenerateCombinedNoiseMap(NoiseParameters noise_parameters, int xsize, int ysize, float lod)
    {
        int lod_xsize = Mathf.RoundToInt(xsize * lod);
        int lod_ysize = Mathf.RoundToInt(ysize * lod);

        float[,] height_map = new float[lod_xsize, lod_ysize];

        for (int y = 0; y < lod_ysize; y++)
        {
            for (int x = 0; x < lod_xsize; x++)
            {
                height_map[x, y] = GetCombinedNoiseValue(noise_parameters, x / lod, y / lod);
            }
        }

        return height_map;
    }
    public static float GetCombinedNoiseValue(NoiseParameters noise_parameters, float x, float y, bool only_active_noises = false)
    {
        PerlinNoise[] noises = noise_parameters.noises;
        float offsetX = noise_parameters.offsetX;
        float offsetY = noise_parameters.offsetY;
        float frequency = noise_parameters.frequency;
        AnimationCurve redistribution = noise_parameters.redistribution;


        float value = 0;
        float max_value = 0;

        float sample_x = (x + offsetX) * frequency;
        float sample_y = (y + offsetY) * frequency;

        for (int i = 0; i < noises.Length; i++)
        {
            if (noises[i].active || !only_active_noises)
            {
                value += noises[i].amplitude * noises[i].GetNoiseValue(sample_x, sample_y);
                max_value += noises[i].amplitude;
            }
        }

        return redistribution.Evaluate(value / max_value);
    }
}
public class NoiseParameters
{
    public PerlinNoise[] noises;
    public float offsetX;
    public float offsetY;
    public float frequency;
    public AnimationCurve redistribution;

    public NoiseParameters(PerlinNoise[] noises, float offsetX, float offsetY, float frequency, AnimationCurve redistribution)
    {
        this.noises = noises;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.frequency = frequency;
        this.redistribution = redistribution;
    }
}
