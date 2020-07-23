using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEngine.Rendering;

[CustomEditor(typeof(PW_Water_Control))]
public class PW_Refraction_Manager_GUI : Editor
{
	SerializedProperty sp_renderSize;
	SerializedProperty sp_refractionEnabled;
	SerializedProperty sp_pipeline;
	SerializedProperty sp_directionAngle;

	//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	public static void Enable ( bool i_enabled )
	{
		if ( i_enabled ) 
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

	//============================================================
	public void OnEnable()
	{
		sp_renderSize 			= serializedObject.FindProperty("renderSize");
		sp_refractionEnabled 	= serializedObject.FindProperty("refractionEnabled");
		sp_pipeline 			= serializedObject.FindProperty("pipeline");
		sp_directionAngle   	= serializedObject.FindProperty("directionAngle");
	}

	//============================================================
	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();

		sp_pipeline.enumValueIndex		= (int)EditorGUILayout.Popup ( "Pipeline", sp_pipeline.enumValueIndex, sp_pipeline.enumNames );
		sp_refractionEnabled.boolValue	= EditorGUILayout.Toggle ("Refraction", sp_refractionEnabled.boolValue );
		sp_renderSize.enumValueIndex	= (int)EditorGUILayout.Popup ( "Render Size", sp_renderSize.enumValueIndex, sp_renderSize.enumNames );

		sp_directionAngle.floatValue 	= EditorGUILayout.Slider(new GUIContent("Direction Angle", "Angle of Movement"), sp_directionAngle.floatValue, 0.0f, 360.0f);

		if ( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			Enable(sp_refractionEnabled.boolValue);
		}
	}
}
