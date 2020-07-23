//////////////////////////////////////////////////////////////////////////
//
//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!
//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!
//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!
//
//////////////////////////////////////////////////////////////////////////

#ifndef UNITY_TERRAIN_TOOL_NOISE_NoneRidge_INC
#define UNITY_TERRAIN_TOOL_NOISE_NoneRidge_INC

/*=========================================================================

    Includes

=========================================================================*/

#include "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Implementation/RidgeImpl.hlsl"
#include "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/NoiseCommon.hlsl"



/*=========================================================================
    
    NoneRidge Noise Functions - Non-Fractal, Non-Warped

=========================================================================*/

float noise_NoneRidge( float pos )
{
    return get_noise_Ridge( pos );
}

float noise_NoneRidge( float2 pos )
{
    return get_noise_Ridge( pos );
}

float noise_NoneRidge( float3 pos )
{
    return get_noise_Ridge( pos );
}

float noise_NoneRidge( float4 pos )
{
    return get_noise_Ridge( pos );
}

#endif // UNITY_TERRAIN_TOOL_NOISE_NoneRidge_INC