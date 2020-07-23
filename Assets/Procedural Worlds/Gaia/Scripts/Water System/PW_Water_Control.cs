using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class PW_Water_Control : MonoBehaviour
{
	static private Dictionary<Camera, CommandBuffer> _cameras 	= new Dictionary<Camera, CommandBuffer>();

	private Renderer 		_ren;
	private Material 		_matWater;
	private Material 		_matWaterUnder;

	private int      		_normalLayer0_ID;
	private int      		_normalLayer1_ID;

	private int      		_normalLayer0Scale_ID;
	private int      		_normalLayer1Scale_ID;

	private int      		_normalTile_ID;

	private int      		_waveShoreClamp_ID;
	private int      		_waveLength_ID;
	private int      		_waveSteepness_ID;
	private int      		_waveSpeed_ID;
	private int      		_waveDirection_ID;

	private int      		_refractionToggle_ID;

	private int      		_edgeWaterColor_ID;
	private int      		_edgeWaterDist_ID;

	private CameraEvent 	_cameraEvent 				= CameraEvent.BeforeForwardAlpha;
	private string          _cbufName 					= "Echo_Refaction";
	private int             _grabID						= 0;

	public enum PW_RENDER_PIPELINE
	{
		BUILTIN,
		RENDER_PIPELINE
	};

	public enum PW_RENDER_SIZE
	{
		FULL 	= -1,
		HALF 	= -2,
		QUARTER = -3
	};

	public PW_RENDER_SIZE 		renderSize 					= PW_RENDER_SIZE.HALF;
	public bool 				refractionEnabled 			= true;
	public PW_RENDER_PIPELINE 	pipeline 					= PW_RENDER_PIPELINE.BUILTIN;
	public float                directionAngle				= 0;

	//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	public void RefractionInit()
	{
		if ( refractionEnabled ) 
		{
			Shader.EnableKeyword ( "_PW_MC_REFRACTION_ON" );
			Shader.SetGlobalInt ("_PW_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One );
			Shader.SetGlobalInt  ("_PW_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero );
		}
		else
		{
			Shader.DisableKeyword ( "_PW_MC_REFRACTION_ON" );
			Shader.SetGlobalInt ("_PW_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha );
			Shader.SetGlobalInt  ("_PW_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
		}
	}

	//-------------------------------------------------------------------------
	private void SendWindDirection()
	{
		Vector2 dir = (Vector2) ( Quaternion.Euler ( 0, 0, directionAngle ) * Vector2.right );

		Vector4 vec4Dir = Vector4.zero;

		vec4Dir.x = dir.x;
		vec4Dir.z = dir.y;

		_matWater.SetVector ( _waveDirection_ID, vec4Dir );
		_matWaterUnder.SetVector ( _waveDirection_ID, vec4Dir );
	}

	//-------------------------------------------------------------------------
	private void SendWaterEdge()
	{
		_matWaterUnder.SetVector ( _edgeWaterColor_ID, _matWater.GetVector ( _edgeWaterColor_ID ) );
		_matWaterUnder.SetFloat ( _edgeWaterDist_ID, _matWater.GetFloat ( _edgeWaterDist_ID ) );
	}

	//-------------------------------------------------------------------------
	private void SendNormalMaps()
	{
		_matWaterUnder.SetTexture ( _normalLayer0_ID, _matWater.GetTexture ( _normalLayer0_ID ) );
		_matWaterUnder.SetTexture ( _normalLayer1_ID, _matWater.GetTexture ( _normalLayer1_ID ) );
	}

	//-------------------------------------------------------------------------
	private void SendWaveData()
	{
		_matWaterUnder.SetFloat ( _normalLayer0_ID, _matWater.GetFloat ( _normalLayer0Scale_ID ) );
		_matWaterUnder.SetFloat ( _normalLayer1_ID, _matWater.GetFloat ( _normalLayer1Scale_ID ) );

		_matWaterUnder.SetFloat ( _normalTile_ID, _matWater.GetFloat ( _normalTile_ID ) );

		_matWaterUnder.SetFloat ( _waveShoreClamp_ID, _matWater.GetFloat ( _waveShoreClamp_ID ) );
		_matWaterUnder.SetFloat ( _waveLength_ID, _matWater.GetFloat ( _waveLength_ID ) );
		_matWaterUnder.SetFloat ( _waveSteepness_ID, _matWater.GetFloat ( _waveSteepness_ID ) );
		_matWaterUnder.SetFloat ( _waveSpeed_ID, _matWater.GetFloat ( _waveSpeed_ID ) );
		_matWaterUnder.SetVector ( _waveDirection_ID, _matWater.GetVector ( _waveDirection_ID ) );
	}

	//=========================================================================
    void Start()
    {
		SendWindDirection();
	}

	//-------------------------------------------------------------------------
	private void ClearBuffers()
    {
        foreach ( var cam in _cameras )
        {
            if ( cam.Key )
                cam.Key.RemoveCommandBuffer (_cameraEvent, cam.Value );
        }

        _cameras.Clear();
    }

	//=========================================================================
	public void OnDisable()
    {
		if ( pipeline == PW_RENDER_PIPELINE.BUILTIN ) 
			Camera.onPreRender -= PreRender;

        ClearBuffers();
    }

	//=========================================================================
    public void OnEnable()
    {
		Material [] mats;

		_normalLayer0_ID 		= Shader.PropertyToID("_NormalLayer0");       
		_normalLayer1_ID 		= Shader.PropertyToID("_NormalLayer1");       
		_normalLayer0Scale_ID 	= Shader.PropertyToID("_NormalLayer0Scale");  
		_normalLayer1Scale_ID 	= Shader.PropertyToID("_NormalLayer1Scale");  
		_normalTile_ID 			= Shader.PropertyToID("_NormalTile");         
		_waveShoreClamp_ID 		= Shader.PropertyToID("_WaveShoreClamp");     
		_waveLength_ID 			= Shader.PropertyToID("_WaveLength");     
		_waveSteepness_ID 		= Shader.PropertyToID("_WaveSteepness");     
		_waveSpeed_ID 			= Shader.PropertyToID("_WaveSpeed");     
		_waveDirection_ID 		= Shader.PropertyToID("_WaveDirection"); 
		_edgeWaterColor_ID      = Shader.PropertyToID("_EdgeWaterColor");
		_edgeWaterDist_ID       = Shader.PropertyToID("_EdgeWaterDist");

		_ren = GetComponent<Renderer>();

		if ( _ren != null ) 
		{
			mats = _ren.sharedMaterials;

			for ( int loop = 0; loop < mats.Length; loop++ ) 
			{
				switch (mats[loop].shader.name) 
				{
				case "PWS/PW_Water":
					_matWater = mats[loop];
					break;

				case "PWS/PW_Water_Under":
					_matWaterUnder = mats[loop];
					break;

				default:
					break;
				}
			}
		}

		SendNormalMaps();
		SendWaterEdge();
		SendWaveData();

		// refraction grab setup
		RefractionInit();

		if ( pipeline == PW_RENDER_PIPELINE.BUILTIN ) 
		{
			ClearBuffers();

			_grabID	= Shader.PropertyToID ( "_EchoTemp" );

			Camera.onPreRender += PreRender;
		}
    }

	//=========================================================================
	public void PreRender( Camera i_cam )
	{
		CommandBuffer 	cbuf;

		if ( pipeline != PW_RENDER_PIPELINE.BUILTIN ) 
			return;

		if ( !refractionEnabled ) 
			return;

		if ( _cameras.ContainsKey ( i_cam ) )
            return;

		cbuf 		= new CommandBuffer();
		cbuf.name 	= _cbufName;
		cbuf.Clear();

		cbuf.GetTemporaryRT ( _grabID, (int)renderSize, (int)renderSize, 0, FilterMode.Bilinear );
//		cbuf.Blit ( BuiltinRenderTextureType.CurrentActive, _grabID );
		cbuf.Blit ( BuiltinRenderTextureType.CameraTarget, _grabID );
		cbuf.SetGlobalTexture ( "_CameraOpaqueTexture", _grabID );

		i_cam.AddCommandBuffer ( _cameraEvent, cbuf );

		_cameras[i_cam] = cbuf;
	}


	//=========================================================================
    void Update()
    {
		SendWaveData();
		SendWindDirection();

	#if UNITY_EDITOR
		SendNormalMaps();
		SendWaterEdge();
	#endif
    }
}
