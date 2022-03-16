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

globallycoherent RWTexture3D<int> voxelDataBufferOld;
globallycoherent RWTexture3D<int> voxelDataBufferNew;

globallycoherent RWStructuredBuffer<OctreeEntry> accelerationStructureBuffer;

int volumeInitialSize;
int maxDepth; // is equivalent to maxIterations

int getData(uint3 pixel)
{
    return voxelDataBufferOld[pixel];
}

void setData(uint3 pixel, int data)
{
    voxelDataBufferNew[pixel] = data;
}

void deleteData(uint3 pixel)
{
    voxelDataBufferOld[pixel] = 0;
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

struct AABB
{
    float3 center;
    float3 maxSize;
};
AABB createAABB(float3 center, float3 max)
{
    AABB aabb = (AABB) 0;
    aabb.center = center;
    aabb.maxSize = max;
    return aabb;
}
bool isInsideVolume(float3 position, AABB aabb)
{
    float3 min1 = aabb.center;
    float3 max1 = aabb.center + aabb.maxSize;
    return
            (position.x >= min1.x) & (position.y >= min1.y) & (position.z >= min1.z) &
            (position.x <= max1.x) & (position.y <= max1.y) & (position.z <= max1.z);
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
        dynamicComponents[globalID.x].visitedOctants = getOctants(currentVoxelPosition);
            
        int currentVoxel = getData(currentVoxelPosition);
        if (currentVoxel == 0) // NO DATA YET, UPDATE OCTTREE
            addToOctree(currentVoxelPosition, 1);
        
        setData(currentVoxelPosition, 5); // EXAMPLEDATA
    }
    else
    {
        float3 nextAbsolutePosition = absolutePosition + currentComponent.direction;
        uint3 nextVoxelPosition = uint3(
                        (int) nextAbsolutePosition.x,
                        (int) nextAbsolutePosition.y,
                        (int) nextAbsolutePosition.z);
        
        
        if (!isInsideVolume(nextVoxelPosition, createAABB(float3(1, 1, 1), float3(511, 511, 511))))
        {
            dynamicComponents[globalID.x].direction = (float3) 0;
            return;
        }
        
        
        int nextToVisitOctants = getOctants(nextVoxelPosition);
        
        if (nextToVisitOctants != visitedOctants) // UPDATE OCTREE
        {
            dynamicComponents[globalID.x].visitedOctants = nextToVisitOctants;
            updateOctree(visitedOctants, nextToVisitOctants);
        }
        
       
        deleteData(currentVoxelPosition);
        setData(nextVoxelPosition, 5);
   
        
        
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




































