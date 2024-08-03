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
                    float noise_value = redistribution.Evaluate(PerlinNoise.GetCombinedNoiseValue(new NoiseParameters(terrain_noises, offsetX, offsetY, frequency, redistribution), x, y, true));

                    if (map_object.activeInHierarchy)
                    {
                        noisetexture.SetPixel(x, y, Color.Lerp(Color.white, Color.black, noise_value));
                    }
                }
            }

            noisetexture.Apply();
        }
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
