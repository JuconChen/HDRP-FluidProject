using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimulationFuncLibrary
{
    public static int size = 0;
    public static float diff = 0;
    public static float dt = 0;
    public static float disturbance = 0;
    public static float[] GetGrayscale(Texture2D texture,int size,string channel,float mul)
    {
        float[] grayscale = new float[size * size];
        int subSize = texture.width / size;
        if (subSize == 0 || size == 0 || texture.width != texture.height)
            return grayscale;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float gray = 0;
                for (int x = 0; x < subSize; x++)
                {
                    for (int y = 0; y < subSize; y++)
                    {
                        Color color = texture.GetPixel(i * subSize + x, j * subSize + y);

                        switch (channel)
                        {
                            case "R":
                                gray += color.r;
                                break;
                            case "G":
                                gray += color.g;
                                break;
                            case "B":
                                gray += color.b;
                                break;
                            case "RGB":
                                gray += color.grayscale;
                                break;
                        }
                    }
                }
                grayscale[i + j * size] = mul * gray / (subSize * subSize);
            }
        }
        return grayscale;
    }
    public static float[] GetGrayscale(Texture2D texture, int size, string channel, string negativecChannel, float mul)
    {
        float[] grayscale = new float[size * size + 1];
        int subSize = texture.width / size;
        if (subSize == 0 || size == 0 || texture.width != texture.height)
            return grayscale;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float gray = 0;
                for (int x = 0; x < subSize; x++)
                {
                    for (int y = 0; y < subSize; y++)
                    {
                        Color color = texture.GetPixel(i * subSize + x, j * subSize + y);

                        switch (channel)
                        {
                            case "R":
                                gray += color.r;
                                break;
                            case "G":
                                gray += color.g;
                                break;
                            case "B":
                                gray += color.b;
                                break;
                            case "RGB":
                                gray += color.grayscale;
                                break;
                        }
                        switch (negativecChannel)
                        {
                            case "R":
                                gray -= color.r;
                                break;
                            case "G":
                                gray -= color.g;
                                break;
                            case "B":
                                gray -= color.b;
                                break;
                            case "RGB":
                                gray -= color.grayscale;
                                break;
                        }
                    }
                }
                grayscale[i + j * size] = mul * gray / (subSize * subSize);
            }
        }
        return grayscale;
    }
    public static void CalculateShader(ComputeShader shader, string funcName,string[] refBufferName, ref Dictionary<string, ComputeBuffer> computeBufferMap)
    {
        int kID = shader.FindKernel(funcName);

        for (int i = 0; i < refBufferName.Length; i++)
        {
            string[] key = refBufferName[i].Split('_');
            shader.SetBuffer(kID, key[key.Length - 1], computeBufferMap[key[0]]);
        }
        shader.Dispatch(kID, 32, 32, 1);
    }
    public static ComputeBuffer GetComputeBuffer(float[] value)
    {
        ComputeBuffer buffer = new ComputeBuffer(value.Length, 4);
        buffer.SetData(value);
        return buffer;
    }
    public static void AddDynamicSource(ComputeShader shader,ref RenderTexture rt, ref Dictionary<string, ComputeBuffer> computeBufferMap)
    {
        int kID = shader.FindKernel("AddDynamicSource");
        shader.SetTexture(kID, "dynamicSourceTexture", rt);
        shader.SetBuffer(kID, "SBuffer", computeBufferMap["X0"]);
        shader.Dispatch(kID, 32, 32, 1);
    }
    public static void CalculateCollision(ComputeShader shader,ref RenderTexture inputRT,ref RenderTexture outputRT, ref Dictionary<string, ComputeBuffer> computeBufferMap)
    {
        int kID = shader.FindKernel("Collision");
        shader.SetTexture(kID, "inputTexture", inputRT);
        shader.SetTexture(kID, "outputTexture", outputRT);
        shader.SetBuffer(kID, "XBuffer", computeBufferMap["X"]);
        shader.SetBuffer(kID, "X0Buffer", computeBufferMap["X0"]);
        shader.SetBuffer(kID, "UBuffer", computeBufferMap["U"]);
        shader.SetBuffer(kID, "VBuffer", computeBufferMap["V"]);
        shader.SetBuffer(kID, "CBuffer", computeBufferMap["C"]);
        shader.Dispatch(kID, 32, 32, 1);
    }
    public static void CalculateBrush(ComputeShader shader,Vector2 pos,int size)
    {
        shader.SetInt(Shader.PropertyToID("brushPosX"), (int)pos.x);
        shader.SetInt(Shader.PropertyToID("brushPosY"), (int)pos.y);
        shader.SetInt(Shader.PropertyToID("brushSize"), size);
    }
}
