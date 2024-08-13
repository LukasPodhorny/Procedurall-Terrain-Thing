using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderKeywordFilter;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using System.Numerics;
using Unity.VisualScripting;
using System.Runtime.InteropServices;

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
    public ComputeShader NoiseCompute;

    [Header("DANGER ZONE")]
    public string DATA_NAME = "terrain_noise_data";
    [Tooltip("WHEN CHECKED IT WILL OVERWRITE ALL DATA BASED ON VALUES IN INSPECTOR!")]
    public bool SAVE_DATA;
    public bool LOAD_DATA;

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
            JsonHelper.SaveClass(noise_parameters, DATA_NAME);
        }
        else if (LOAD_DATA)
        {
            NoiseParameters noise_parameters = JsonHelper.LoadClass<NoiseParameters>(DATA_NAME);
            
            terrain_noises = noise_parameters.noises;
            offsetX = noise_parameters.offsetX;
            offsetY = noise_parameters.offsetY;
            frequency = noise_parameters.frequency;
            redistribution = noise_parameters.redistribution;
        }

        UpdateNoiseTextureGpu();
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
                    float noise_value = redistribution.Evaluate(GetCombinedNoiseValue(new NoiseParameters(terrain_noises, offsetX, offsetY, frequency, redistribution, false), x, y, true));

                    if (map_object.activeInHierarchy)
                    {
                        noisetexture.SetPixel(x, y, Color.Lerp(Color.white, Color.black, noise_value));
                    }
                }
            }

            noisetexture.Apply();
        }
    }
    void UpdateNoiseTextureGpu()
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

            NoiseParameters noise_parameters = new NoiseParameters(terrain_noises, offsetX, offsetY, frequency, redistribution);
            float[] noise_map = GenerateCombinedNoiseMapGpu(noise_parameters, xsize, ysize, 1, NoiseCompute);

            for (int y = 0; y < ysize; y++)
            {
                for (int x = 0; x < xsize; x++)
                {
                    if (map_object.activeInHierarchy)
                    {
                        noisetexture.SetPixel(x, y, Color.Lerp(Color.white, Color.black, noise_map[y*xsize+x]));
                    }
                }
            }

            noisetexture.Apply();
        }
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
    public static float[] GenerateCombinedNoiseMapGpu(NoiseParameters noise_parameters, int xsize, int ysize, float lod, ComputeShader NoiseCompute)
    {
        int lod_xsize = Mathf.RoundToInt(xsize * lod);
        int lod_ysize = Mathf.RoundToInt(ysize * lod);

        float[] height_map = new float[lod_xsize * lod_ysize]; 

        // setting output buffer
        ComputeBuffer height_map_buffer = new ComputeBuffer(lod_xsize*lod_ysize, sizeof(float));
        NoiseCompute.SetBuffer(0, "height_map", height_map_buffer);
        

        // setting buffer variables to compute shader
        ComputeBuffer noises_buffer = new ComputeBuffer(noise_parameters.noises.Length, Marshal.SizeOf(typeof(ShaderPerlinNoise)));
        ShaderPerlinNoise[] noises = new ShaderPerlinNoise[noise_parameters.noises.Length];
        for(int i = 0;i<noise_parameters.noises.Length;i++)
        {
            noises[i] = new ShaderPerlinNoise(noise_parameters.noises[i]);
        }
        noises_buffer.SetData(noises);
        NoiseCompute.SetBuffer(0, "noises", noises_buffer);
    
        ComputeBuffer redistribution_buffer = new ComputeBuffer(noise_parameters.r_resolution, sizeof(float));
        redistribution_buffer.SetData(noise_parameters.redistribution_data);
        NoiseCompute.SetBuffer(0, "redistribution", redistribution_buffer);

        ComputeBuffer noise_redistributions_buffer = new ComputeBuffer(noise_parameters.r_resolution*noises.Length, sizeof(float));
        noise_redistributions_buffer.SetData(noise_parameters.noise_redistribution_datas);
        NoiseCompute.SetBuffer(0, "noises_redistributions", noise_redistributions_buffer);


        // setting variables to compute shader
        NoiseCompute.SetFloat("lod", lod);
        NoiseCompute.SetFloat("offsetX", noise_parameters.offsetX);
        NoiseCompute.SetFloat("offsetY", noise_parameters.offsetY);
        NoiseCompute.SetFloat("frequency", noise_parameters.frequency);
        NoiseCompute.SetInt("lod_xsize", lod_xsize);
        NoiseCompute.SetInt("lod_ysize", lod_ysize);
        
        // calculating thread groups
        int threadGroupsX = Mathf.CeilToInt(lod_xsize/8);
        int threadGroupsY = Mathf.CeilToInt(lod_ysize/8);

        // executing the kernel and sending data back to height_map
        NoiseCompute.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        height_map_buffer.GetData(height_map);

        // freeing allocated memory in gpu
        height_map_buffer.Release();
        noises_buffer.Release();
        redistribution_buffer.Release();
        noise_redistributions_buffer.Release();


        return height_map;
    }
    public static float[] PreEvAnimationCurve(AnimationCurve animationCurve, int resolution)
    {
        float[] result = new float[resolution];
        float step = 1f/(resolution-1);

        for(int i = 0;i < resolution; i++)
        {
            result[i] = animationCurve.Evaluate(i*step);
        }
        return result;
    }
}
public struct NoiseParameters
{
    public PerlinNoise[] noises;
    public float offsetX;
    public float offsetY;
    public float frequency;
    public AnimationCurve redistribution;

    public float[] redistribution_data;
    public float[] noise_redistribution_datas;
    public int r_resolution;

    public NoiseParameters(PerlinNoise[] noises, float offsetX, float offsetY, float frequency, AnimationCurve redistribution, bool compute_r_data = true, int r_resolution = 10_000):this()
    {
        this.noises = noises;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.frequency = frequency;
        this.redistribution = redistribution;

        this.r_resolution = r_resolution;

        if(compute_r_data)
        {
            redistribution_data = NoiseMapGenerator.PreEvAnimationCurve(redistribution, r_resolution);

            noise_redistribution_datas = new float[noises.Length*r_resolution];
            for(int i = 0; i < noises.Length; i++)
            {
                float[] current_curve = NoiseMapGenerator.PreEvAnimationCurve(noises[i].redistribution, r_resolution);

                for(int j = 0;j < r_resolution; j++)
                {
                    noise_redistribution_datas[r_resolution*i+j] = current_curve[j];
                }
            }
        }
    }
}
public struct ShaderPerlinNoise
{
    // active checkbox is used for noise editor
    // can't pass bools directly to shader
    //public bool active;
    public int octaves;
    public float frequency;
    public float amplitude;
    public float persistence;
    public float lacunarity;
    // can't pass AnimationCurve directly to shader, but can evaluate some some quality
    //public AnimationCurve redistribution;
    public float offsetX;
    public float offsetY;
    public float max_value;

    public ShaderPerlinNoise(PerlinNoise noise)
    {
        octaves = noise.octaves;
        frequency = noise.frequency;
        amplitude = noise.amplitude;
        persistence = noise.persistence;
        lacunarity = noise.lacunarity;
        offsetX = noise.offsetX;
        offsetY = noise.offsetY;
        max_value = noise.max_value;
    }
}