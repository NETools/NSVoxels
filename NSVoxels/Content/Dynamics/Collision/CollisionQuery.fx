
struct DynamicVoxelComponent
{
    float3 position;
    float3 direction;
    int visitedOctants;
};
globallycoherent RWStructuredBuffer<DynamicVoxelComponent> dynamicComponents;



struct CollisionData
{
    float3 netRepellingForces;
    float3 netCorrectionOffsets;
    
    int collisions;
};
globallycoherent RWStructuredBuffer<CollisionData> collisionData;


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
///////////////////////////////////////////////////


///////////////////////////////////////////////////
struct RaytracingResult
{
    int voxelDataPayload;
    
    float3 hitPointF32;
    float3 hitPointI32;
    float3 surfaceNormal;
    
    float depth;
    
    AABB aabb;
    AABB aabbLast;
    
    int iterations;
    bool isNull;
};
///////////////////////////////////////////////////


///////////////////////////////////////////////////
bool checkHit(in Ray ray, in AABB aabb,
                     in float3 sign,
                     in float3 minSubtrahend, in float3 maxSubtrahend,
                     out float lambdaMin, out float lambdaMax,
                     out float3 sideMin, out float3 sideMax)
{
    float3 leftSide = aabb.center + minSubtrahend;
    float3 rigthSide = aabb.center + maxSubtrahend;
    
    float3 leftSideTimesReciprocal = (leftSide - ray.origin) * ray.dirRcp;
    float3 rightSideTimesReciprocal = (rigthSide - ray.origin) * ray.dirRcp;
    
    lambdaMin = max(leftSideTimesReciprocal.x, max(leftSideTimesReciprocal.y, leftSideTimesReciprocal.z));
    lambdaMax = min(rightSideTimesReciprocal.x, min(rightSideTimesReciprocal.y, rightSideTimesReciprocal.z));
    
    sideMin = (leftSideTimesReciprocal == lambdaMin) * sign;
    sideMax = (rightSideTimesReciprocal == lambdaMax) * sign;
    
    return lambdaMax > lambdaMin;
}
bool isInsideVolume(float3 position, AABB aabb)
{
    float3 min1 = aabb.center;
    float3 max1 = aabb.center + aabb.maxSize;
    return
            (position.x >= min1.x) & (position.y >= min1.y) & (position.z >= min1.z) &
            (position.x <= max1.x) & (position.y <= max1.y) & (position.z <= max1.z);
}
///////////////////////////////////////////////////



///////////////////////////////////////////////////
void getCollidingIndex(in Ray ray, in AABB volume, out uint3 arrayIndex)
{
    
    volume.center = max(volume.center - 1, 0);
    volume.maxSize = min(volume.maxSize + 2, volumeInitialSize);
    
    AABB arrayCube = volume;
    
    float lambdaMin = 0;
    float lambdaMax = 0;
    
    float3 sideMin = (float3) 0;
    float3 sideMax = (float3) 0;
    
    float3 biggerThanZero = ray.dirRcp > 0;
    float3 lessThanZero = 1.0f - biggerThanZero;
    
    float3 sgnsPerComps = biggerThanZero - lessThanZero; // alternative: sign(ray.dir);
    
    float3 minCorner = lessThanZero * arrayCube.maxSize;
    float3 maxCorner = biggerThanZero * arrayCube.maxSize;
    bool initialHitStatus = checkHit(ray, arrayCube, sgnsPerComps, minCorner, maxCorner, lambdaMin, lambdaMax, sideMin, sideMax);
    
    float maximumDepth = lambdaMax;
    
        
    bool isOriginInside = !isInsideVolume(ray.origin, arrayCube);
    
    float3 floatPosition3 = ray.origin + lambdaMin * ray.dir * isOriginInside;
    float3 intPosition3 = floor(floatPosition3);
        
    float3 offset = saturate(intPosition3 - floor(floatPosition3 + 0.01f));
    intPosition3 -= offset;
        
    float3 pstvDirComps = saturate(sgnsPerComps);
    float3 ngtvDirComps = 1 - pstvDirComps;
    
    AABB currentVoxel = createAABB(intPosition3, float3(1, 1, 1));
        
    minCorner = lessThanZero * currentVoxel.maxSize;
    maxCorner = biggerThanZero * currentVoxel.maxSize;
    
    int currentData = 0;    
    [loop]
    while (checkHit(ray, currentVoxel, sgnsPerComps, minCorner, maxCorner, lambdaMin, lambdaMax, sideMin, sideMax)
                      & (currentData == 0 | currentData == 5)
                      & (maximumDepth - lambdaMin >= 1))
    {
        
        floatPosition3 = ray.origin + lambdaMax * ray.dir;
        intPosition3 = floor(floatPosition3);
         
        float3 rightRnddComps = (intPosition3 - floor(floatPosition3 + 0.01f)) + 1.0f; // is equivalent to: >= 0
        float3 wrongRnddComps = 1 - rightRnddComps;

        offset = sideMax * ((pstvDirComps * wrongRnddComps) + (ngtvDirComps * rightRnddComps));
        intPosition3 += offset;
            
        currentData = getData(intPosition3);
        currentVoxel.center = intPosition3;
    }
    
    arrayIndex = intPosition3;
    
}

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
float3 com_Position_old;
float3 com_Position;

float3 com_Velocity;
float3 com_Velocity_old;


void updateCurrentIndex(int index)
{
    DynamicVoxelComponent currentData = dynamicComponents[index];
    float3 absolutePosition = currentData.position + com_Position;
    float3 absolutePosition_old = currentData.position + com_Position_old;
    
    
    //////////////////////////// VIEW SIDE ////////////////////////////
    
    uint3 absolutePosition_int = floor(absolutePosition);
    uint3 absolutePosition_old_int = floor(absolutePosition_old);
    
    int visitedOctants = currentData.visitedOctants;

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
    setData(absolutePosition_int, 5);
    //////////////////////////////////////////////////////////////////////
     
    
    ////////////////////////////// PHYSICS ///////////////////////////////
    float3 obstacleCenter = (float3) 0;
    
    Ray collisionRay = (Ray) 0;
    collisionRay.origin = absolutePosition;
    collisionRay.dir = normalize(com_Velocity);
    collisionRay.dirRcp = rcp(collisionRay.dir);
    
    getCollidingIndex(collisionRay, createAABB(0, 512), obstacleCenter);
    obstacleCenter += 0.5f;
    float3 currentVoxelCenter = absolutePosition + 0.5f;
    
    
    float legalDistance = 2; // (1 + 1) -- voxel radii is 1 each [!]
    
    float3 positionVector = obstacleCenter - currentVoxelCenter;
    float distance = length(positionVector);
    
    if (distance <= legalDistance)
    {
        float depth = (legalDistance - distance);
        
        if (distance < 0.1)
            return;
                        
        collisionData[0].collisions++;
        
        if (collisionData[0].collisions < 2)
        {
            collisionData[0].netRepellingForces += normalize(currentData.position) * length(com_Velocity);
            collisionData[0].netCorrectionOffsets += normalize(positionVector) * depth;
        }

    }
    
 
    
    //////////////////////////////////////////////////////////////////////
}


/////////////////////////////////////////////////////////////////////////////

[numthreads(64, 1, 1)]
void CollisionCS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
                    uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    updateCurrentIndex(globalID.x);
}


technique CollisionQueryTechnique
{
    pass GenerateOctree
    {
        ComputeShader = compile cs_5_0 CollisionCS();
    }
}

