using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidSimulation : MonoBehaviour
{
    public ComputeShader shader;

    [Header("初始密度场")]
    public Texture2D densitySource;
    [Header("初始速度场")]
    public Texture2D velocitySource;
    [Header("持续密度场")]
    public Texture2D continuousDensitySource;
    [Header("持续速度场")]
    public Texture2D continuousVelocitySource;
    public RenderTexture dynamicSourceRT;

    public RenderTexture inputRT;
    public RenderTexture outputRT;

    public bool isUseContinuousVelSource = true;
    public bool isUseContinuousDensSource = true;
    public bool isUseMouseClick = true;
    public bool isBorderless = true;
    public bool isCollision = true;
    public bool isLaunchfromObject = false;
    public int brushSize = 10;

    [Header("解算分辨率")]
    public int size = 1024;
    [Header("project迭代次数")]
    public int projectIterations = 20;
    [Header("diffuse迭代次数")]
    public int diffuseIterations = 20;
    [Header("扰动")]
    public float disturbance = 0.2f;
    public float mulVel;
    public float mulDens;

    [Header("散度")]
    public float diff;
    public float dt;

    private Dictionary<string, ComputeBuffer> computeBufferMap = new Dictionary<string, ComputeBuffer>();
    private float[] sourceDensity;
    private float[] sourceVelocityX;
    private float[] sourceVelocityY;
    private float[] continuousSourceDensity;
    private float[] continuousSourceVelocityX;
    private float[] continuousSourceVelocityY;

    private void Init()
    {
        SetValue(size, diff, dt);
        sourceDensity = FluidSimulationFuncLibrary.GetGrayscale(densitySource, size, "R", "G", mulDens);
        sourceVelocityX = FluidSimulationFuncLibrary.GetGrayscale(velocitySource, size, "R",0.5f, mulVel);
        sourceVelocityY = FluidSimulationFuncLibrary.GetGrayscale(velocitySource, size, "G", 0.5f, mulVel);
        continuousSourceDensity = FluidSimulationFuncLibrary.GetGrayscale(continuousDensitySource, size, "R", "G", mulDens);
        continuousSourceVelocityX = FluidSimulationFuncLibrary.GetGrayscale(continuousVelocitySource, size, "R", 0.5f, mulVel);
        continuousSourceVelocityY = FluidSimulationFuncLibrary.GetGrayscale(continuousVelocitySource, size, "G", 0.5f, mulVel);

        computeBufferMap.Add("U0", FluidSimulationFuncLibrary.GetComputeBuffer(sourceVelocityX));
        computeBufferMap.Add("V0", FluidSimulationFuncLibrary.GetComputeBuffer(sourceVelocityY));
        computeBufferMap.Add("X0", FluidSimulationFuncLibrary.GetComputeBuffer(sourceDensity));
        computeBufferMap.Add("U", FluidSimulationFuncLibrary.GetComputeBuffer(new float[size * size + 1]));
        computeBufferMap.Add("V", FluidSimulationFuncLibrary.GetComputeBuffer(new float[size * size + 1]));
        computeBufferMap.Add("X", FluidSimulationFuncLibrary.GetComputeBuffer(new float[size * size + 1]));
        computeBufferMap.Add("C", FluidSimulationFuncLibrary.GetComputeBuffer(new float[size * size + 1]));
        computeBufferMap.Add("DS", FluidSimulationFuncLibrary.GetComputeBuffer(new float[size * size + 1]));


        //dynamicSourceRT.enableRandomWrite = true;
        inputRT.enableRandomWrite = true;
        outputRT.enableRandomWrite = true;
    }
    private void Simulation()
    {
        VelStep();
        DensStep();
        if (isCollision)
        {
            Collision();
        }
        OutputRT("X");
        //OutputVelDensRT();
    }
    private void DensStep()
    {
        if (isUseContinuousDensSource)
        {
            computeBufferMap["X0"].SetData(continuousSourceDensity);
            //FluidSimulationFuncLibrary.AddDynamicSource(shader, ref dynamicSourceRT, ref computeBufferMap);
            FluidSimulationFuncLibrary.CalculateShader(shader, "AddRandomDensSource",
            new string[] {
                "X0_SBuffer",
            },
            ref computeBufferMap
            );
        }
        if (isLaunchfromObject)
        {
            FluidSimulationFuncLibrary.AddDynamicSource(shader, ref inputRT, ref computeBufferMap);
        }

        FluidSimulationFuncLibrary.CalculateShader(shader, "AddSource",
        new string[] {
            "X0_SBuffer",
            "X_XBuffer"
        },
        ref computeBufferMap
        );

        //SWAP("X", "X0");

        for (int i = 0; i < 1; i++)
        {
            FluidSimulationFuncLibrary.CalculateShader(shader, "Diffusion",
            new string[] {
                "X0_X0Buffer",
                "X_XBuffer"
                },
            ref computeBufferMap
            );
        }

        SWAP("X", "X0");

        FluidSimulationFuncLibrary.CalculateShader(shader, "Advect",
        new string[] {
            "X_DBuffer",
            "X0_D0Buffer",
            "U_UBuffer",
            "V_VBuffer"
        },
        ref computeBufferMap
        );

        FluidSimulationFuncLibrary.CalculateShader(shader, "MoveBuffer",
        new string[] {
            "X_XBuffer"
        },
        ref computeBufferMap
        );
    }
    private void VelStep()
    {
        if (isUseContinuousVelSource)
        {
            computeBufferMap["U0"].SetData(continuousSourceVelocityX);
            computeBufferMap["V0"].SetData(continuousSourceVelocityY);

            FluidSimulationFuncLibrary.CalculateShader(shader, "AddRandomVelSource",
            new string[] {
                "U0_SBuffer",
            },
            ref computeBufferMap
            );
            FluidSimulationFuncLibrary.CalculateShader(shader, "AddRandomVelSource",
            new string[] {
                "V0_SBuffer",
            },
            ref computeBufferMap
            );
        }

        FluidSimulationFuncLibrary.CalculateShader(shader, "AddSource", 
        new string[] {
            "U0_SBuffer",
            "U_XBuffer"
        },
        ref computeBufferMap
        );

        FluidSimulationFuncLibrary.CalculateShader(shader, "AddSource",
        new string[] {
            "V0_SBuffer",
            "V_XBuffer"
        },
        ref computeBufferMap
        );

        //SWAP("U", "U0");
        for (int i = 0; i < 1; i++)
        {
            FluidSimulationFuncLibrary.CalculateShader(shader, "Diffusion",
            new string[] {
                "U0_X0Buffer",
                "U_XBuffer"
                },
            ref computeBufferMap
            );
        }

        //SWAP("V", "V0");
        for (int i = 0; i < 1; i++)
        {
            FluidSimulationFuncLibrary.CalculateShader(shader, "Diffusion",
            new string[] {
                "V0_X0Buffer",
                "V_XBuffer"
                },
            ref computeBufferMap
            );
        }

        //Project       
        Project();

        SWAP("U", "U0");
        SWAP("V", "V0");

        FluidSimulationFuncLibrary.CalculateShader(shader, "Advect",
        new string[] {
            "U_DBuffer",
            "U0_D0Buffer",
            "U0_UBuffer",
            "V0_VBuffer"
        },
        ref computeBufferMap
        );

        FluidSimulationFuncLibrary.CalculateShader(shader, "Advect",
        new string[] {
            "V_DBuffer",
            "V0_D0Buffer",
            "U0_UBuffer",
            "V0_VBuffer"
        },
        ref computeBufferMap
        );

        Project();


        FluidSimulationFuncLibrary.CalculateShader(shader, "MoveBuffer",
        new string[] {
            "U_XBuffer"
        },
        ref computeBufferMap
        );
        FluidSimulationFuncLibrary.CalculateShader(shader, "MoveBuffer",
        new string[] {
            "V_XBuffer"
        },
        ref computeBufferMap
        );
    }
    private void OutputRT(string bufferMapKey)
    {
        int kID = shader.FindKernel("OutputRT");
        shader.SetBuffer(kID, "XBuffer", computeBufferMap[bufferMapKey]);
        shader.SetTexture(kID, "outputTexture", outputRT);
        shader.Dispatch(kID, 32, 32, 1);
    }
    private void OutputVelDensRT()
    {
        int kID = shader.FindKernel("OutputVelDensRT");
        shader.SetBuffer(kID, "XBuffer", computeBufferMap["X"]);
        shader.SetBuffer(kID, "UBuffer", computeBufferMap["U"]);
        shader.SetBuffer(kID, "VBuffer", computeBufferMap["V"]);
        shader.SetTexture(kID, "outputTexture", outputRT);
        shader.Dispatch(kID, 32, 32, 1);
    }
    private void SWAP(string a,string b)
    {
        ComputeBuffer tmp = computeBufferMap[a];
        computeBufferMap[a] = computeBufferMap[b];
        computeBufferMap[b] = tmp;
    }
    private void Project()
    {
        FluidSimulationFuncLibrary.CalculateShader(shader, "ProjectDiv",
        new string[] {
            "U_UBuffer",
            "V_VBuffer",
            "U0_PBuffer",
            "V0_DivBuffer"
            },
        ref computeBufferMap
        );

        for (int i = 0; i < projectIterations; i++)
        {
            FluidSimulationFuncLibrary.CalculateShader(shader, "ProjectP",
            new string[] {
                "U0_PBuffer",
                "V0_DivBuffer"
                },
            ref computeBufferMap
            );
        }

        FluidSimulationFuncLibrary.CalculateShader(shader, "ProjectVel",
        new string[] {
            "U_UBuffer",
            "V_VBuffer",
            "U0_PBuffer"
            },
        ref computeBufferMap
        );
    }
    private void Collision()
    {
        FluidSimulationFuncLibrary.CalculateCollision(shader,ref inputRT,ref outputRT, ref computeBufferMap);
        if (isUseMouseClick && Input.GetKey(KeyCode.Mouse0))
        {
            FluidSimulationFuncLibrary.CalculateBrush(shader, new Vector2(200, 200), brushSize);
        }
    }
    private void SetValue(int size, float diff, float dt)
    {
        if (FluidSimulationFuncLibrary.diff != diff)
        {
            FluidSimulationFuncLibrary.diff = diff;
            shader.SetFloat(Shader.PropertyToID("diff"), diff);
        }
        if (FluidSimulationFuncLibrary.size != size)
        {
            FluidSimulationFuncLibrary.size = size;
            shader.SetInt(Shader.PropertyToID("N"), size);
        }
        if (FluidSimulationFuncLibrary.dt != dt)
        {
            FluidSimulationFuncLibrary.dt = dt;
            shader.SetFloat(Shader.PropertyToID("dt"), dt);
        }
        if (FluidSimulationFuncLibrary.disturbance != disturbance)
        {
            FluidSimulationFuncLibrary.disturbance = disturbance;
            shader.SetFloat(Shader.PropertyToID("disturbance"), disturbance);
        }
        shader.SetFloat(Shader.PropertyToID("randomSeed"), UnityEngine.Random.Range(-1f,1f));
        shader.SetFloat(Shader.PropertyToID("time"), DateTime.Now.Second);
    }

    void Start()
    {
        Init();
        //Simulation();
    }
    
    void FixedUpdate()
    {
        Simulation();
    }
}
