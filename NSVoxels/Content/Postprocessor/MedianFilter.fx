
Texture2D SpriteTexture : register(t0);

sampler SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

uniform float screenWidth;
uniform float screenHeight;


inline float4 getColor(float2 currentPosition, float2 dydx)
{
    return SpriteTexture.Sample(SpriteTextureSampler, currentPosition + dydx);
}
const float4 GRAY_SCALE_FACTORS = float4(0.299f, 0.587f, 0.114, 0);
inline bool compare(float4 a, float4 b)
{
    return dot(GRAY_SCALE_FACTORS, a) > dot(GRAY_SCALE_FACTORS, b);
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
    float dx = 1.0f / screenWidth;
    float dy = 1.0f / screenHeight;

    float2 lookups[] =
    {
        float2(-dx, -dy), float2(0, -dy), float2(dx, -dy),
		float2(-dx, 0), float2(0, 0), float2(dx, 0),
		float2(-dx, dy), float2(0, dy), float2(dx, dy),
    };
	
	
	// STACK DEFINITION
    float4 colorStack[9];
	
    int currentIndex = 0;

	/*
	1, 5, 9, 2, 10, 15
	push(1) => 1
	push(5) => 5, 1
	push(9) => 9, 5, 1
	push(2) => 9, 5, 2, 1
	*/
	
    [unroll(9)]
    for (int j = 0; j < 9; j++)
    {
        float4 currentColor = getColor(input.TextureCoordinates, lookups[j]);
		
		////////////// STACK CODE /////////////////
		
        if (currentIndex == 0)
            colorStack[currentIndex++] = currentColor;
        else
        {
            if (compare(currentColor, colorStack[currentIndex - 1]))
            {
                float4 temp = colorStack[currentIndex - 1];
                colorStack[currentIndex - 1] = currentColor;
                colorStack[currentIndex] = temp;
				
                for (int k = currentIndex - 2; k >= 0; k--)
                {
                    if (compare(currentColor, colorStack[k]))
                    {
                        temp = colorStack[k];
                        colorStack[k] = currentColor;
                        colorStack[k + 1] = temp;
                    }
                }
				
                currentIndex++;

            }
            else
                colorStack[currentIndex++] = currentColor;
			
        }

		///////////////////////////////////////////
		
    }
	
    return colorStack[4];
	
	
	
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile ps_5_0 MainPS();
    }
};
