struct DynamicVoxelComponent
{
    float3 position;
    float3 direction;
    int visitedOctants;
};



globallycoherent RWStructuredBuffer<DynamicVoxelComponent> dynamicComponents;


/////////////////////////////////////////////////////////////////////////////
struct OctreeEntry
{
    int childrenStartIndex;
    int hasData;
};

globallycoherent RWTexture3D<float4> voxelDataBuffer;
globallycoherent RWStructuredBuffer<OctreeEntry> accelerationStructureBuffer;

int volumeInitialSize;
int maxDepth; // is equivalent to maxIterations

int getData(uint3 pixel)
{
    int4 data = voxelDataBuffer[pixel] * 255;
    int r = data.r;
    int g = data.g;
    int b = data.b;
    int a = data.a;
    
    return r | (g << 8) | (b << 16) | (a << 24);
}


void setData(uint3 pixel, float4 data)
{
    voxelDataBuffer[pixel] = data / 256.0f;
}
/////////////////////////////////////////////////////////////////////////////

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

void addToOctree(uint3 pos, int value)
{
    int relativeIndex = calculateRelativeOctant(pos, 1);
    accelerationStructureBuffer[0].hasData = 1;

    int nextIndex = relativeIndex + 1;

    for (int depth = 1; depth <= maxDepth; depth++)
    {
        OctreeEntry current = accelerationStructureBuffer[nextIndex];
        InterlockedAdd(accelerationStructureBuffer[nextIndex].hasData, value);
        relativeIndex = calculateRelativeOctant(pos, depth + 1);
        nextIndex = current.childrenStartIndex + relativeIndex;
    }
}


int getRelativeOctant(int packed, int index)
{
    return (packed >> (3 * index)) & 7;
}

void updateOctree(int oldOctants, int newOctants)
{
    int oldIndex = getRelativeOctant(oldOctants, 0) + 1;
    int newIndex = getRelativeOctant(newOctants, 0) + 1;
    
    for (int depth = 1; depth <= maxDepth; depth++)
    {
        OctreeEntry oldNode = accelerationStructureBuffer[oldIndex];
        OctreeEntry newNode = accelerationStructureBuffer[newIndex];
        
        if (oldIndex != newIndex)
        {
            InterlockedAdd(accelerationStructureBuffer[oldIndex].hasData, -1);
            InterlockedAdd(accelerationStructureBuffer[newIndex].hasData, +1);   
        }
        
        
        
        oldIndex = oldNode.childrenStartIndex + getRelativeOctant(oldOctants, depth);
        newIndex = newNode.childrenStartIndex + getRelativeOctant(newOctants, depth);
    }
    
    
    
}

int getOctants(uint3 pos)
{
    int visitedOctants = 0;
    
    int relativeIndex = calculateRelativeOctant(pos, 1);
    int nextIndex = relativeIndex + 1;

    visitedOctants = nextIndex - 1;
    
    for (int depth = 1; depth <= maxDepth; depth++)
    {
        OctreeEntry current = accelerationStructureBuffer[nextIndex];
        relativeIndex = calculateRelativeOctant(pos, depth + 1);
        nextIndex = current.childrenStartIndex + relativeIndex;
        
        visitedOctants |= relativeIndex << (3 * depth);
    }
    
    return visitedOctants;
}




[numthreads(64, 1, 1)]
void CS(uint3 globalID : SV_DispatchThreadID)
{
    DynamicVoxelComponent currentComponent = dynamicComponents[globalID.x];
    float3 absolutePosition = currentComponent.position;
    
    uint3 currentVoxelPosition = uint3(
                        (int) absolutePosition.x,
                        (int) absolutePosition.y,
                        (int) absolutePosition.z);
    
    int visitedOctants = currentComponent.visitedOctants;
    
    if (visitedOctants == 0) // DYNAMIC OBJECT INITIALIZATION
    {
        setData(currentVoxelPosition, float4(1, 0, 0, 0)); // EXAMPLEDATA
        dynamicComponents[globalID.x].visitedOctants = getOctants(currentVoxelPosition);
            
        int currentVoxel = getData(currentVoxelPosition);
        if (currentVoxel == 0) // NO DATA YET, UPDATE OCTTREE
            addToOctree(currentVoxelPosition, 1);
    }
    else
    {
        float3 nextAbsolutePosition = absolutePosition + currentComponent.direction;
        uint3 nextVoxelPosition = uint3(
                        (int) nextAbsolutePosition.x,
                        (int) nextAbsolutePosition.y,
                        (int) nextAbsolutePosition.z);
        
        
        int nextToVisitOctants = getOctants(nextVoxelPosition);
        
        if (nextToVisitOctants != visitedOctants) // UPDATE OCTREE
        {
            dynamicComponents[globalID.x].visitedOctants = nextToVisitOctants;
            updateOctree(visitedOctants, nextToVisitOctants);
        }
        
        setData(currentVoxelPosition, float4(0, 0, 0, 0)); // DEBUG MODE
        setData(nextVoxelPosition, float4(5, 1, 0, 0));

        dynamicComponents[globalID.x].position = nextAbsolutePosition;
        
    }
    
    

    
    
    
}


technique AcceleratorTechnique
{
    pass GenerateOctree
    {
        ComputeShader = compile cs_5_0 CS();
    }
}




































