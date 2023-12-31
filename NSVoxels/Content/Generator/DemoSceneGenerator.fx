﻿#define GroupSize 4

// simplex noise source: https://gist.github.com/dario-zubovic/e8c4b1f6619b69ba2090123a6e1c2584
// based on https://github.com/keijiro/NoiseShader/blob/master/Assets/GLSL/SimplexNoise2D.glsl
// which itself is modification of https://github.com/ashima/webgl-noise/blob/master/src/noise3D.glsl
// 
// License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//           Distributed under the MIT License. See LICENSE file.
//           https://github.com/keijiro/NoiseShader/blob/master/LICENSE
//           https://github.com/ashima/webgl-noise
//           https://github.com/stegu/webgl-noise

float3 mod289(float3 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float2 mod289(float2 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float3 permute(float3 x)
{
    return mod289((x * 34.0 + 1.0) * x);
}

float3 taylorInvSqrt(float3 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

// output noise is in range [-1, 1]
float snoise(float2 v)
{
    const float4 C = float4(0.211324865405187, // (3.0-sqrt(3.0))/6.0
                            0.366025403784439, // 0.5*(sqrt(3.0)-1.0)
                            -0.577350269189626, // -1.0 + 2.0 * C.x
                            0.024390243902439); // 1.0 / 41.0

    // First corner
    float2 i = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);

    // Other corners
    float2 i1;
    i1.x = step(x0.y, x0.x);
    i1.y = 1.0 - i1.x;

    // x1 = x0 - i1  + 1.0 * C.xx;
    // x2 = x0 - 1.0 + 2.0 * C.xx;
    float2 x1 = x0 + C.xx - i1;
    float2 x2 = x0 + C.zz;

    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    float3 p =
      permute(permute(i.y + float3(0.0, i1.y, 1.0))
                    + i.x + float3(0.0, i1.x, 1.0));

    float3 m = max(0.5 - float3(dot(x0, x0), dot(x1, x1), dot(x2, x2)), 0.0);
    m = m * m;
    m = m * m;

    // Gradients: 41 points uniformly over a line, mapped onto a diamond.
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;

    // Normalise gradients implicitly by scaling m
    m *= taylorInvSqrt(a0 * a0 + h * h);

    // Compute final noise value at P
    float3 g = float3(
        a0.x * x0.x + h.x * x0.y,
        a0.y * x1.x + h.y * x1.y,
        g.z = a0.z * x2.x + h.z * x2.y
    );
    return 130.0 * dot(m, g);
}

float snoise01(float2 v)
{
    return snoise(v) * 0.5 + 0.5;
}


globallycoherent RWTexture3D<int> voxelDataBuffer;
globallycoherent RWTexture3D<int> voxelDataBufferCopy;


void setData(uint3 pixel, int data)
{
    voxelDataBuffer[pixel] = data;
    voxelDataBufferCopy[pixel] = data;
}

[numthreads(GroupSize, GroupSize, GroupSize)]
void CS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
            uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    
    
    
    //if(globalID.y == 470 && globalID.x == 255 && globalID.z == 255)
    //    setData(globalID, 3);
    
    //if (globalID.y == 470 && globalID.x == 256 && globalID.z == 255)
    //    setData(globalID, 4);
    
    //if (globalID.y == 470 && globalID.x == 255 && globalID.z == 256)
    //    setData(globalID, 6);
    
    //if (globalID.y == 290)
    //    setData(globalID, 6);
    
    //if (globalID.x == 50)
    //    setData(globalID, 6);
    
    
    //if (globalID.y == 294 && globalID.x == 200 && globalID.z == 200)
    //    setData(globalID, 1);
    
    //if (globalID.y == 293 && globalID.x == 200 && globalID.z == 200)
    //    setData(globalID, 1);
    
    //if (globalID.y == 292 && globalID.x == 200 && globalID.z == 200)
    //    setData(globalID, 1);
    //if (globalID.y == 291 && globalID.x == 200 && globalID.z == 200)
    //    setData(globalID, 1);
    
    //if (globalID.y == 290)
    //    setData(globalID, 3);
    
    
    //return;
    
    float noiseY = snoise(float2(globalID.x / 256.0f, globalID.z / 256.0f)) * 60 + 70;
    
    float3 dist = globalID - float3(256, 256, 256);
    
    if (dot(dist, dist) <= 60 * 60 && dot(dist, dist) >= 30 * 30)
        setData(globalID, 3);
    else if (dot(dist, dist) >= 60 * 60 && dot(dist, dist) <= 64 * 64)
        setData(globalID, 1);
    else if (dot(dist, dist) <= 30 * 30 && dot(dist, dist) >= 15 * 15)
        setData(globalID, 8 | (1 << 15));
    else if (dot(dist, dist) <= 15 * 15)
        setData(globalID, 9 | (1 << 15));
    
    else if (globalID.y <= noiseY - 5)
        setData(globalID, 3);
    else if (globalID.y >= noiseY - 5 && globalID.y <= noiseY)
        setData(globalID, 6);
    
    else if (globalID.x == 10 && globalID.y <= 200 && (globalID.z <= 200 || globalID.z >= 300))
        setData(globalID, 4);
    
    else if (globalID.x == 10 && globalID.y <= 200 && !(globalID.z <= 200 || globalID.z >= 300))
        setData(globalID, 2 | (1 << 8));
    else if (globalID.y == 200 && (globalID.x >= 50 && globalID.x <= 150 && globalID.z >= 230 && globalID.z <= 270))
        setData(globalID, 2 | (1 << 8));
    else if (globalID.x == 190 && (globalID.y >= 50 && globalID.y <= 150 && globalID.z >= 230 && globalID.z <= 270))
        setData(globalID, 2 | (1 << 8));
   
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 CS();
    }
}