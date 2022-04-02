
struct DynamicVoxelComponent
{
    float3 position;
    float3 direction;
    int visitedOctants;
};
globallycoherent RWStructuredBuffer<DynamicVoxelComponent> dynamicComponents;

///////////////////////////////////////////////////
globallycoherent RWTexture3D<int> voxelDataBuffer;
uniform int volumeInitialSize;

struct Voxel
{
    int data;
    bool isReflectable;
    bool isGlass;
};


int getData(uint3 pixel)
{
    return voxelDataBuffer[pixel];
}

void setData(uint3 pixel, int data)
{
    voxelDataBuffer[pixel] = data;
}

void deleteData(uint3 pixel)
{
    voxelDataBuffer[pixel] = 0;
}
///////////////////////////////////////////////////

///////////////////////////////////////////////////
struct Ray
{
    float3 origin;
    float3 dir;
    float3 dirRcp;
    
};
///////////////////////////////////////////////////


/////////////////////////////////////////////////////////////////////////////
struct OctreeEntry
{
    int childrenStartIndex;
    int hasData;
};
globallycoherent RWStructuredBuffer<OctreeEntry> accelerationStructureBuffer;
int maxDepth; // is equivalent to maxIterations


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

/////////////////////////////////////////////////////////////////////////////


/////////////////////////////////////////////////////////////////////////////
float4x4 voxelTransformation;
float4x4 voxelTransformation_old;

float3 com_Position_old;
float3 com_Position;
void updateCurrentIndex(int index)
{
    DynamicVoxelComponent currentData = dynamicComponents[index];
    float3 absolutePosition = mul(float4(currentData.position, 0), voxelTransformation).xyz + com_Position;
    float3 absolutePosition_old = mul(float4(currentData.position, 0), voxelTransformation_old).xyz + com_Position_old;
    
    
    //////////////////////////// VIEW SIDE ////////////////////////////
    
    uint3 absolutePosition_int = floor(absolutePosition);
    uint3 absolutePosition_old_int = floor(absolutePosition_old);
    
    int visitedOctants = currentData.visitedOctants;
    int currentVoxel = getData(absolutePosition_int);
    
    if (visitedOctants == 0)
    {
        dynamicComponents[index].visitedOctants = getOctants(absolutePosition_int);
        addToOctree(absolutePosition_int, 1);
    }
    else
    {
        int nextToVisitOctants = getOctants(absolutePosition_int);
        if (nextToVisitOctants != visitedOctants) // UPDATE OCTREE
        {
            updateOctree(visitedOctants, nextToVisitOctants);
            dynamicComponents[index].visitedOctants = nextToVisitOctants;
        }
    }
    deleteData(absolutePosition_old_int);
    setData(absolutePosition_int, 7);
    //////////////////////////////////////////////////////////////////////
}
/////////////////////////////////////////////////////////////////////////////

[numthreads(64, 1, 1)]
void RotationCS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
                    uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    updateCurrentIndex(globalID.x);
}


technique CollisionQueryTechnique
{
    pass GenerateOctree
    {
        ComputeShader = compile cs_5_0 RotationCS();
    }
}

