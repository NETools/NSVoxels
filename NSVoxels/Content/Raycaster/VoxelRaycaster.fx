uniform float screenWidth;
uniform float screenHeight;

uniform float2 aspectRatio;
uniform float focalDistance;

uniform int volumeInitialSize;
uniform float oneOverVolumeInitialSize;

uniform int nodeMinimumSize;

uniform RWTexture2D<float4> backBuffer;

uniform float4x4 cameraRotation;
uniform float3 cameraPosition;

uniform float3 lightPosition0;

uniform bool useAccelerator;

uniform bool showShadow;
uniform int shadowMaxAcceleratorIterations;

uniform bool showReflection;
uniform int reflectionMaxAcceleratorIterations;
uniform int maxBounces;


uniform bool showIterations;
uniform float iterationScale;

uniform bool showNormals;

uniform bool calculateIndirectLightning;


uniform float gameTimeSeconds;

///////////////////////////////////////////////////

struct Voxel
{
    int data;
    bool isReflectable;
    bool isGlass;
    bool isBright;
};

uniform RWTexture3D<int> voxelDataBuffer;
int getData(int3 position)
{
    position = clamp(position, 0, volumeInitialSize);
    return voxelDataBuffer[position];
}

Voxel getVoxel(int packedVoxel)
{
    Voxel voxel = (Voxel) 0;
    voxel.data = packedVoxel & 0xff;
    voxel.isReflectable = (packedVoxel & (1 << 8)) >> 8;
    voxel.isGlass = (packedVoxel & (1 << 9)) >> 9;
    voxel.isBright = (packedVoxel & (1 << 15)) >> 15;
    
    return voxel;
}

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
bool checkHit(in Ray ray,              in AABB aabb,
                     in float3 sign,
                     in float3 minSubtrahend, in float3 maxSubtrahend,
                     out float lambdaMin,     out float lambdaMax,
                     out float3 sideMin,      out float3 sideMax)
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

///

bool arrayRayHit(in Ray ray,
                 in AABB volume,
                 in bool excludeGlass,
                 out float depth,
                 out float3 hitPointF32,
                 out float3 hitPointI32,
                 out float3 normal,
                 out int voxelData,
                 out AABB voxelAABB, out int iterations)
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
    
    AABB nextVoxel = createAABB(intPosition3, float3(1, 1, 1));
        
    minCorner = lessThanZero * nextVoxel.maxSize;
    maxCorner = biggerThanZero * nextVoxel.maxSize;
    
    int currentData = 0;
    [loop]
    while (checkHit(ray, nextVoxel, sgnsPerComps, minCorner, maxCorner, lambdaMin, lambdaMax, sideMin, sideMax)
                      & (((currentData == 0) || (currentData >> 9) & 1 == 1))
                      & (maximumDepth - lambdaMin >= 1))
    {
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
    
    return voxelData > 0;
}

///

///////////////////////////////////////////////////
bool arrayRayHit(in Ray ray,
                 in AABB volume,
                 out float depth,
                 out float3 hitPointF32,
                 out float3 hitPointI32,
                 out float3 normal,
                 out int voxelData,
                 out AABB voxelAABB, out int iterations)
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
    
    AABB nextVoxel = createAABB(intPosition3, float3(1, 1, 1));
        
    minCorner = lessThanZero * nextVoxel.maxSize;
    maxCorner = biggerThanZero * nextVoxel.maxSize;
    
    int currentData = 0;
    [loop]
    while (checkHit(ray, nextVoxel, sgnsPerComps, minCorner, maxCorner, lambdaMin, lambdaMax, sideMin, sideMax)
                      & (currentData == 0)
                      & (maximumDepth - lambdaMin >= 1))
    {
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
                        int voxelData = 0;
                        int traversalIterations = 0;
                        bool result = arrayRayHit(ray, childVoxel, depth, hitPoint, hitPointInt, normal, voxelData, voxelAABB, traversalIterations);
                        
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


float2 getTextureCoordinate(float3 position, float3 normal)
{
    float2 texel1 = position.xy;
    float2 texel2 = position.xz;
    float2 texel3 = position.yz;
    
    return (texel1 * normal.z + texel2 * normal.y + texel3 * normal.x);
}

Texture2DArray VoxelTextures;
sampler voxelTexturesSampler = sampler_state
{
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    ADDRESSU = Wrap;
    ADDRESSV = Wrap;
};


RaytracingResult volumeRayTest(Ray ray, int maxIterations)
{
    if (useAccelerator)
        return acceleratedVolumeRayTest(ray, maxIterations);
    
 
    float depth = 0;
    float3 hitPoint = (float3) 0;
    float3 hitPointInt = (float3) 0;
    float3 normal = (float3) 0;
    AABB voxelAABB = (AABB) 0;
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
                        voxelAABB, iterations);
        
    RaytracingResult rtrcRslt = (RaytracingResult) 0;
    
    rtrcRslt.voxelDataPayload = voxelData;
    rtrcRslt.hitPointF32 = hitPoint;
    rtrcRslt.hitPointI32 = hitPointInt;
    rtrcRslt.surfaceNormal = normal;
    rtrcRslt.depth = depth;
    rtrcRslt.aabb = voxelAABB;
    
    rtrcRslt.iterations = iterations;
    rtrcRslt.isNull = !result;
        
    return rtrcRslt;
    
}

float4 calculateShadow(RaytracingResult rtrslt, float4 currentColor, bool isInShadow)
{
    float3 lightVoxelDirection = normalize(lightPosition0 - rtrslt.hitPointF32);
    float3 shadowCastOrigin = rtrslt.hitPointF32;
        
    Ray shadowRay = (Ray) 0;
    shadowRay.dir = lightVoxelDirection;
    shadowRay.dirRcp = rcp(shadowRay.dir);
    shadowRay.origin = shadowCastOrigin + rtrslt.surfaceNormal * .1f;
        
    RaytracingResult shadowRaycastRslt = volumeRayTest(shadowRay, shadowMaxAcceleratorIterations);
    currentColor.rgb *= !shadowRaycastRslt.isNull * 0.2f + (1.0f - !shadowRaycastRslt.isNull);
    
    isInShadow = !shadowRaycastRslt.isNull;
    
    return currentColor;
}

// ONLY PROOF OF CONCEPT
float4 calculateTransparency(Ray ray, RaytracingResult rtrslt, float4 currentColor)
{
    int3 biggerThanZeroA = ray.dirRcp > 0;
    int3 lessThanZeroA = 1.0f - biggerThanZeroA;
    
    float3 minCornerA = lessThanZeroA * rtrslt.aabb.maxSize;
    float3 maxCornerA = biggerThanZeroA * rtrslt.aabb.maxSize;
    
    float3 sgnsPerCompsA = biggerThanZeroA - lessThanZeroA;
    
    float lambdaMinA = 0;
    float lambdaMaxA = 0;
    
    float3 sideMinA = (float3) 0;
    float3 sideMaxA = (float3) 0;
    
    bool b = checkHit(ray, rtrslt.aabb, sgnsPerCompsA, minCornerA, maxCornerA, lambdaMinA, lambdaMaxA, sideMinA, sideMaxA);
    
    
    float3 rayExitPoint = ray.origin + ray.dir * (lambdaMaxA + 0.1);
    float3 refractedDir = refract(ray.dir, rtrslt.surfaceNormal, 1.5f);
    
    Ray propagatedRay = (Ray) 0;
    propagatedRay.origin = rayExitPoint;
    propagatedRay.dir = refractedDir;
    propagatedRay.dirRcp = rcp(propagatedRay.dir);
    
    /*
    RaytracingResult rslt = volumeRayTest(propagatedRay, 64);
    */
    
    float depth = 0;
    float3 hitPoint = (float3) 0;
    float3 hitPointInt = (float3) 0;
    float3 normal = (float3) 0;
    AABB voxelAABB = (AABB) 0;
    int voxelData = 0;
    int iterations = 0;
                        
    bool result = arrayRayHit(
                        propagatedRay,
                        createAABB(float3(0, 0, 0), float3(volumeInitialSize, volumeInitialSize, volumeInitialSize)), true,
                        depth,
                        hitPoint,
                        hitPointInt,
                        normal,
                        voxelData,
                        voxelAABB, iterations);
    
    
    if (result)
    {
        Voxel appearingVoxel = getVoxel(voxelData);
        float4 appearingVoxelColor = VoxelTextures.SampleLevel(
                                voxelTexturesSampler,
                                float3(getTextureCoordinate(hitPoint, normal) * oneOverVolumeInitialSize, appearingVoxel.data - 1),
                                0);
        
        currentColor.rgb =  appearingVoxelColor.rgb;
    }
 
    
    return currentColor;
}

float4 raytraceScene(Ray ray, out bool result)
{
    float4 finalColor = (float4) 0;
    
    RaytracingResult initialRaycastRslt = volumeRayTest(ray, 64);
    if (initialRaycastRslt.isNull)
    {
        result = false;
        return float4(0, 0, 0, 0);
    }
    
    Voxel voxel = getVoxel(initialRaycastRslt.voxelDataPayload);
    
    finalColor = float4(float3(1, 1, 1) * abs(initialRaycastRslt.surfaceNormal), 1) * showNormals +
    float4(initialRaycastRslt.iterations, initialRaycastRslt.iterations, initialRaycastRslt.iterations, 1000000) 
    * iterationScale * showIterations * !showNormals
    
    + VoxelTextures.SampleLevel(
                                voxelTexturesSampler,
                                float3(getTextureCoordinate(initialRaycastRslt.hitPointF32 + (voxel.isBright) * gameTimeSeconds * 5.0f, initialRaycastRslt.surfaceNormal) * oneOverVolumeInitialSize, voxel.data - 1),
                                0) * !showIterations * !showNormals;

    
    bool liesInShadow = false;
    if (showShadow & !voxel.isBright)
        finalColor = calculateShadow(initialRaycastRslt, finalColor, liesInShadow);
    
    if (showReflection & voxel.isReflectable)
    {
        Ray reflectionRay = (Ray) 0;
        reflectionRay.dir = reflect(ray.dir, initialRaycastRslt.surfaceNormal);
        reflectionRay.dirRcp = rcp(reflectionRay.dir);
        reflectionRay.origin = initialRaycastRslt.hitPointF32 + initialRaycastRslt.surfaceNormal * .1f;
        
        RaytracingResult reflectionRaycastRslt = volumeRayTest(reflectionRay, reflectionMaxAcceleratorIterations);
        if (!reflectionRaycastRslt.isNull)
        {
            Voxel reflectedVoxel = getVoxel(reflectionRaycastRslt.voxelDataPayload);
        
            float4 reflectedEntityColor =
                                VoxelTextures.SampleLevel(
                                voxelTexturesSampler,
                                float3(getTextureCoordinate(reflectionRaycastRslt.hitPointF32 + (reflectedVoxel.isBright) * gameTimeSeconds * 5.0f, reflectionRaycastRslt.surfaceNormal) * oneOverVolumeInitialSize, reflectedVoxel.data - 1),
                                0);
 
            for (int i = 0; i < maxBounces; i++)
            {
                float3 lightReflectedVoxelDirection = normalize(lightPosition0 - reflectionRaycastRslt.hitPointF32);
                float3 shadowCastOriginRefl = reflectionRaycastRslt.hitPointF32;
        
                bool dummy = false;
                if (showShadow & !reflectedVoxel.isBright)
                    reflectedEntityColor = calculateShadow(reflectionRaycastRslt, reflectedEntityColor, dummy);
            
                if (reflectedVoxel.isReflectable)
                {
                    reflectionRay.dir = reflect(reflectionRay.dir, reflectionRaycastRslt.surfaceNormal);
                    reflectionRay.dirRcp = rcp(reflectionRay.dir);
                    reflectionRay.origin = reflectionRaycastRslt.hitPointF32 + reflectionRaycastRslt.surfaceNormal * .1f;
        
                    reflectionRaycastRslt = volumeRayTest(reflectionRay, reflectionMaxAcceleratorIterations);
                 
                    reflectedVoxel = getVoxel(reflectionRaycastRslt.voxelDataPayload);
        
                    reflectedEntityColor = reflectedEntityColor * 0.55 +
                                0.45 * VoxelTextures.SampleLevel(
                                voxelTexturesSampler,
                                float3(getTextureCoordinate(reflectionRaycastRslt.hitPointF32 + (reflectedVoxel.isBright) * gameTimeSeconds * 5.0f, reflectionRaycastRslt.surfaceNormal) * oneOverVolumeInitialSize, reflectedVoxel.data - 1),
                                0) * !reflectionRaycastRslt.isNull + float4(0.39, 0.58, 0.93, 1) * reflectionRaycastRslt.isNull;
                    
                    
                    
                }
                else
                    break;
            }
        
            finalColor = finalColor * 0.55 + 0.45 * reflectedEntityColor;
        }
        else
            finalColor = float4(0.39, 0.58, 0.93, 1);
    }
    
    if (voxel.isGlass)
        finalColor = calculateTransparency(ray, initialRaycastRslt, finalColor);
    
    
    if(voxel.isBright)
        finalColor *= 2.5f;
    
    result = true;
    return finalColor;
    
}

[numthreads(8, 8, 1)]
void RaycastingCS(  uint3 localID : SV_GroupThreadID,   uint3 groupID : SV_GroupID,
                    uint localIndex : SV_GroupIndex,    uint3 globalID : SV_DispatchThreadID)
{
    
    int screenCoordinateX = globalID.x;
    int screenCoordinateY = globalID.y;
        
    float2 texCoord = float2((float) screenCoordinateX * screenWidth, (float) screenCoordinateY * screenHeight);
    
    float2 uv = (texCoord * 2.0f) - float2(1, 1);
    uv *= aspectRatio;
    
    Ray ray = (Ray) 0;
	
    float3 newPosition = cameraPosition;
    float3 newDirection = mul(float4(normalize(float3(uv.x, -uv.y, focalDistance)), 0), cameraRotation).xyz;
	
    ray.origin = newPosition;
    ray.dir = newDirection;
    ray.dirRcp = rcp(ray.dir);
    
    bool result = false;
    float4 finalColor = (float4) 0;
    
    finalColor = raytraceScene(ray, result);
   
    
    if(result)
        backBuffer[globalID.xy] = finalColor;
    else
        backBuffer[globalID.xy] = float4(0.39, 0.58, 0.93, 1);
}

technique
{
    pass Raycasting
    {
        ComputeShader = compile cs_5_0 RaycastingCS();

    }
}