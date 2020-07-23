using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class PW_Builtin_Refraction : MonoBehaviour
{
	private CameraEvent 	_cameraEvent 				= CameraEvent.AfterForwardOpaque;
	private string          _cbufName 					= "Echo_Refaction";
	private int             _grabID						= 0;
	//private int             _screenWidth 				= 0;
	//private int             _screenHeight 			= 0;
	private Dictionary<Camera, CommandBuffer> _cameras 	= new Dictionary<Camera, CommandBuffer>();

	[System.Serializable]

	public enum PW_RENDER_SIZE
	{
		FULL 	= -1,
		HALF 	= -2,
		QUARTER = -3
	};

	public PW_RENDER_SIZE renderSize = PW_RENDER_SIZE.HALF;

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
		Camera.onPreRender -= PreRender;

        ClearBuffers();
    }

	//=========================================================================
    public void OnEnable()
    {
        ClearBuffers();
		_grabID	= Shader.PropertyToID ( "_EchoTemp" );

		Camera.onPreRender += PreRender;
    }

	//=========================================================================
	public void PreRender( Camera i_cam )
	{
		CommandBuffer 	cbuf;

		if ( _cameras.ContainsKey ( i_cam ) )
            return;

		cbuf 		= new CommandBuffer();
		cbuf.name 	= _cbufName;
		cbuf.Clear();

		cbuf.GetTemporaryRT ( _grabID, (int)renderSize, (int)renderSize, 0, FilterMode.Bilinear );
		cbuf.Blit ( BuiltinRenderTextureType.CurrentActive, _grabID );
		cbuf.SetGlobalTexture ( "_CameraOpaqueTexture", _grabID );

		i_cam.AddCommandBuffer ( _cameraEvent, cbuf );

		_cameras[i_cam] = cbuf;
	}
}
