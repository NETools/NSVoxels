struct BrushData
{
    float3 position;
};


globallycoherent RWStructuredBuffer<BrushData> brushBuffer;

uniform float4x4 cameraRotation;
uniform float3 cameraPosition;

uniform float focalDistance;


uniform int volumeInitialSize;
uniform float oneOverVolumeInitialSize;

uniform int nodeMinimumSize;


uniform bool brushAdd;
uniform int brushId;


///////////////////////////////////////////////////
struct Voxel
{
    int data;
    bool isReflectable;
    bool isGlass;
};

globallycoherent RWTexture3D<int> voxelDataBuffer;
int getData(int3 position)
{
    return voxelDataBuffer[position];
}

void setData(uint3 pixel, int data)
{
    voxelDataBuffer[pixel] = data;
}

Voxel getVoxel(int packedVoxel)
{
    Voxel voxel = (Voxel) 0;
    voxel.data = packedVoxel & 0xff;
    voxel.isReflectable = packedVoxel & (1 << 8);
    voxel.isGlass = packedVoxel & (1 << 9);
    
    return voxel;
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
struct OctreeData
{
    int childStartIndex;
    int childrenCount;
};
globallycoherent RWStructuredBuffer<OctreeData> accelerationStructureBuffer;
uniform Texture2D<float4> octantVectorLookUp;

struct InterimOctreeData
{
    OctreeData octreeData;
    
    float3 currentPosition;
    float currentSize;
   
    bool hasData;
    bool isNull;
};

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
InterimOctreeData getNode(uint index)
{
    InterimOctreeData node = (InterimOctreeData) 0;
    node.octreeData = accelerationStructureBuffer[index];
    node.hasData = node.octreeData.childrenCount > 0;
    
    return node;
}

float3 getOctantVector(int index)
{
    return octantVectorLookUp[uint2(index, 0)].xyz;
}
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
bool checkHit(in Ray ray, in AABB aabb,
                     in float3 minSubtrahend, in float3 maxSubtrahend,
                     out float tMin, out float tMax)
{
    float3 leftSide = aabb.center + minSubtrahend;
    float3 rigthSide = aabb.center + maxSubtrahend;
    
    float3 leftSideTimesReciprocal = (leftSide - ray.origin) * ray.dirRcp;
    float3 rightSideTimesReciprocal = (rigthSide - ray.origin) * ray.dirRcp;
    
    tMin = max(leftSideTimesReciprocal.x, max(leftSideTimesReciprocal.y, leftSideTimesReciprocal.z));
    tMax = min(rightSideTimesReciprocal.x, min(rightSideTimesReciprocal.y, rightSideTimesReciprocal.z));
    
    return tMax > tMin * (tMin + tMax >= 0);
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
bool arrayRayHit(in Ray ray,
                 in AABB volume,
                 out float depth,
                 out float3 hitPointF32,
                 out float3 hitPointI32,
                 out float3 normal,
                 out int voxelData,
                 out AABB voxelAABB, out AABB voxelAABBLast, 
                 out int iterations)
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
    if (((lambdaMin < 0) & (lambdaMax < 0)) | !initialHitStatus)
        return false;
        
    float maximumDepth = lambdaMax;
    
        
    bool isOriginInside = !isInsideVolume(ray.origin, arrayCube);
    
    float3 floatPosition3 = ray.origin + lambdaMin * ray.dir * isOriginInside;
    float3 intPosition3 = floor(floatPosition3);
        
    float3 offset = saturate(intPosition3 - floor(floatPosition3 + 0.01f));
    intPosition3 -= offset;
        
    float3 pstvDirComps = saturate(sgnsPerComps);
    float3 ngtvDirComps = 1 - pstvDirComps;
    
    AABB lastVoxel = createAABB(intPosition3, float3(1, 1, 1));
    AABB nextVoxel = createAABB(intPosition3, float3(1, 1, 1));
        
    minCorner = lessThanZero * nextVoxel.maxSize;
    maxCorner = biggerThanZero * nextVoxel.maxSize;
    
    int currentData = 0;
    [loop]
    while (checkHit(ray, nextVoxel, sgnsPerComps, minCorner, maxCorner, lambdaMin, lambdaMax, sideMin, sideMax)
                      & (currentData == 0)
                      & (maximumDepth - lambdaMin >= 1))
    {
        
        lastVoxel.center = intPosition3;
        
        floatPosition3 = ray.origin + lambdaMax * ray.dir;
        intPosition3 = floor(floatPosition3);
         
        float3 rightRnddComps = (intPosition3 - floor(floatPosition3 + 0.01f)) + 1.0f; // is equivalent to: >= 0
        float3 wrongRnddComps = 1 - rightRnddComps;

        offset = sideMax * ((pstvDirComps * wrongRnddComps) + (ngtvDirComps * rightRnddComps));
        intPosition3 += offset;
            
        normal = -sideMax;

        currentData = getData(intPosition3);
        nextVoxel.center = intPosition3;
            
        depth = lambdaMax;
        iterations++;

    }
    
    
    voxelData = currentData;
    hitPointF32 = floatPosition3;
    hitPointI32 = intPosition3;
    
    voxelAABB = nextVoxel;
    voxelAABBLast = lastVoxel;
    
    return voxelData > 0;
}
RaytracingResult acceleratedVolumeRayTest(Ray ray, int maxIterations)
{
    // REGISTERS
    int skipListPerDepth[5];
    int parentListPerDepth[5];
    float3 positionListPerDepth[5];
    
    for (int i = 0; i < 5; i++)
        skipListPerDepth[i] = 0;
    
    
    int depthCounter = 0;
    
    // DATASTRUCTURE
    InterimOctreeData parent = getNode(0);
    parent.currentPosition = float3(0, 0, 0);
    parent.currentSize = volumeInitialSize;
    
     // RETURN VALUE
    RaytracingResult finalResult = (RaytracingResult) 0;
    finalResult.isNull = true;
    
    AABB rootVolume = createAABB(parent.currentPosition, float3(1, 1, 1) * parent.currentSize);

    float lambdaMin = 0;
    float lambdaMax = 0;
    
    int3 biggerThanZero = ray.dirRcp > 0;
    int3 lessThanZero = 1.0f - biggerThanZero;
    
    float3 minCorner = lessThanZero * rootVolume.maxSize;
    float3 maxCorner = biggerThanZero * rootVolume.maxSize;
    
    if (checkHit(ray, rootVolume, minCorner, maxCorner, lambdaMin, lambdaMax))
    {
        int currentIndex = 0;
        bool voxelFound = false;
        
        int iterations = 0;
        int additionalIterations = 0;
        [loop]
        for (iterations = 0; iterations <= maxIterations; iterations++)
        {
            int childStartIndex = parent.octreeData.childStartIndex;
            
            InterimOctreeData nextParent = (InterimOctreeData) 0;
            nextParent.isNull = true;
            
            float minDistToNode = 1E10;
            float minDistToVoxel = 1E10;

            int usedIndex = 0;
            int nextIndex = 0;
            
            for (int i = 0; i < 8; i++)
            {
                if (((skipListPerDepth[depthCounter] >> i) & 1))
                    continue;
                
                InterimOctreeData child = getNode(childStartIndex + i);
                
                if (!child.hasData)
                    continue;
                
                child.currentSize = parent.currentSize * .5f;
                child.currentPosition = parent.currentPosition + getOctantVector(i) * child.currentSize;
                
                AABB childVoxel = createAABB(child.currentPosition, child.currentSize);
                    
                minCorner = lessThanZero * childVoxel.maxSize;
                maxCorner = biggerThanZero * childVoxel.maxSize;
                
                bool result = checkHit(ray, childVoxel, minCorner, maxCorner, lambdaMin, lambdaMax);
                
                if (result && lambdaMin < minDistToNode)
                {
                    minDistToNode = lambdaMin;
                    if (child.currentSize <= nodeMinimumSize)
                    {
                        float depth = 0;
                        float3 hitPoint = (float3) 0;
                        float3 hitPointInt = (float3) 0;
                        float3 normal = (float3) 0;
                        AABB voxelAABB = (AABB) 0;
                        AABB voxelAABBLast = (AABB) 0;
                        int voxelData = 0;
                        int traversalIterations = 0;
                        bool result = arrayRayHit(ray, childVoxel, depth, hitPoint, hitPointInt, normal, voxelData, voxelAABB, voxelAABBLast, traversalIterations);
                        
                        additionalIterations += traversalIterations;
                        
                        if (result)
                        {
                            voxelFound = true;
                            
                            if (lambdaMin < minDistToVoxel)
                            {
                                minDistToVoxel = lambdaMin;
                                
                                RaytracingResult result = (RaytracingResult) 0;
                                result.voxelDataPayload = voxelData;
                                result.hitPointF32 = hitPoint;
                                result.hitPointI32 = hitPointInt;
                                result.surfaceNormal = normal;
                                result.depth = depth;
                                
                                result.aabb = voxelAABB;
                                result.aabbLast = voxelAABBLast;
                                
                                result.isNull = false;
                                finalResult = result;
                            }
                            
                        }
                        else
                            minDistToNode = 1E10;
                    }
                    else
                    {
                        nextParent = child;
                        nextParent.isNull = false;
                        usedIndex = i;
                        nextIndex = childStartIndex + i;
                    }
                }
            }
            
            if (voxelFound)
                break;
            
            
            if (nextParent.isNull)
            {
                skipListPerDepth[depthCounter] = 0;
                
                depthCounter--;
                if (depthCounter < 0)
                    break;
                
                int parentIndex = parentListPerDepth[depthCounter];
                nextIndex = parentIndex;
                
                nextParent = getNode(parentIndex);
                nextParent.currentSize = volumeInitialSize >> depthCounter;
                nextParent.currentPosition = positionListPerDepth[depthCounter];
                
            }
            else
            {
                skipListPerDepth[depthCounter] |= (1 << usedIndex);
                positionListPerDepth[depthCounter] = parent.currentPosition;
                parentListPerDepth[depthCounter] = currentIndex;

                depthCounter++;
            }
            
            parent = nextParent;
            currentIndex = nextIndex;
        }
        
        iterations += additionalIterations;
        finalResult.iterations = iterations;
    }
    return finalResult;
}
///////////////////////////////////////////////////

//////////////////////////////////////////////////
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


void updateOctree(uint3 pos, int value)
{
    int relativeIndex = calculateRelativeOctant(pos, 1);
    accelerationStructureBuffer[0].childrenCount = 1;

    int nextIndex = relativeIndex + 1;

    for (int depth = 1; depth <= maxDepth; depth++)
    {
        OctreeData current = accelerationStructureBuffer[nextIndex];
        
        //current.hasData += value;
        InterlockedAdd(accelerationStructureBuffer[nextIndex].childrenCount, value);
      
        accelerationStructureBuffer[nextIndex] = current;
        
        relativeIndex = calculateRelativeOctant(pos, depth + 1);
        nextIndex = current.childStartIndex + relativeIndex;
    }
    

}

//////////////////////////////////////////////////

RaytracingResult volumeRayTest(Ray ray, int maxIterations)
{
    float depth = 0;
    float3 hitPoint = (float3) 0;
    float3 hitPointInt = (float3) 0;
    float3 normal = (float3) 0;
    AABB voxelAABB = (AABB) 0;
    AABB voxelAABBLast = (AABB) 0;
    int voxelData = 0;
    int iterations = 0;
                        
    bool result = arrayRayHit(
                        ray,
                        createAABB(float3(0, 0, 0), float3(volumeInitialSize, volumeInitialSize, volumeInitialSize)),
                        depth,
                        hitPoint,
                        hitPointInt,
                        normal,
                        voxelData,
                        voxelAABB, voxelAABBLast, iterations);
        
    RaytracingResult rtrcRslt = (RaytracingResult) 0;
        
    rtrcRslt.voxelDataPayload = voxelData;
    rtrcRslt.hitPointF32 = hitPoint;
    rtrcRslt.hitPointI32 = hitPointInt;
    rtrcRslt.surfaceNormal = normal;
    rtrcRslt.depth = depth;
    rtrcRslt.aabb = voxelAABB;
    rtrcRslt.aabbLast = voxelAABBLast;
    
    rtrcRslt.iterations = iterations;
    rtrcRslt.isNull = !result;
        
    return rtrcRslt;
    
}


void modifyVolume(Ray ray, int brushIndex)
{
    BrushData currentBrushData = brushBuffer[brushIndex];
    ray.origin += currentBrushData.position;
    
    RaytracingResult result = volumeRayTest(ray, 256);
        
    if (result.isNull)
        return;
    
    if (brushAdd)
    {
        if (getData(result.aabbLast.center) == 0 && getData(result.aabb.center) > 0)
        {
            setData(result.aabbLast.center, 3);
            updateOctree(result.aabbLast.center, 1);
        }
    }
    else
    {
        if (getData(result.aabb.center) > 0)
        {
            setData(result.aabb.center, 0);
            //updateOctree(result.aabb.center, -1);
        }
    }

}

[numthreads(32, 1, 1)]
void VolumeModifier(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
                    uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    
 
    Ray ray = (Ray) 0;
	
    float3 newPosition = cameraPosition;
    float3 newDirection = mul(float4(normalize(float3(0, 0, focalDistance)), 0), cameraRotation).xyz;
	
    ray.origin = newPosition;
    ray.dir = newDirection;
    ray.dirRcp = rcp(ray.dir);

    modifyVolume(ray, globalID.x);
}



technique VolumeModificationTechnique
{
    pass GenerateOctree
    {
        ComputeShader = compile cs_5_0 VolumeModifier();
    }
}

