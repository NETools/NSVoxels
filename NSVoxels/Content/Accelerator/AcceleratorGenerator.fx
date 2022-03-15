struct OctreeEntry
{
    int childrenStartIndex;
    int hasData;
};



RWTexture3D<float4> voxelDataBuffer;
globallycoherent RWStructuredBuffer<OctreeEntry>accelerationStructureBuffer;

int volumeInitialSize;
int maxDepth; // is equivalent to maxIterations

inline int getData(uint3 pixel)
{
    int4 data = voxelDataBuffer[pixel] * 255;
    int r = data.r;
    int g = data.g;
    int b = data.b;
    int a = data.a;
    
    return r | (g << 8) | (b << 16) | (a << 24);
}

uint calculateRelativeIndex(uint v, uint d)
{
    return (uint) (v / (volumeInitialSize >> d));
}

uint calculateOctant(uint3 pos, uint d)
{
    return calculateRelativeIndex(pos.x, d) + calculateRelativeIndex(pos.z, d) * 2 + calculateRelativeIndex(pos.y, d) * 4;
}

uint calculateRelativeOctant(uint3 pos, uint d)
{
    return calculateOctant(pos % (uint) (volumeInitialSize >> (d - 1)), d);
}


void updateOctree(uint3 pos, int value)
{
    int relativeIndex = calculateRelativeOctant(pos, 1);
    accelerationStructureBuffer[0].hasData = 1;

    int nextIndex = relativeIndex + 1;

    for (int depth = 1; depth <= maxDepth; depth++)
    {
        OctreeEntry current = accelerationStructureBuffer[nextIndex];
        
        //current.hasData += value;
        InterlockedAdd(accelerationStructureBuffer[nextIndex].hasData, value);
        
        accelerationStructureBuffer[nextIndex] = current;
        
        relativeIndex = calculateRelativeOctant(pos, depth + 1);
        nextIndex = current.childrenStartIndex + relativeIndex;
    }
}

[numthreads(4, 4, 4)]
void CS(uint3 globalID : SV_DispatchThreadID)
{
    if (getData(globalID) < 1)
        return;
    
    updateOctree(globalID, 1);
}


technique AcceleratorTechnique
{
    pass GenerateOctree
    {
        ComputeShader = compile cs_5_0 CS();
    }
}




































