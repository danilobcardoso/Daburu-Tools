﻿#pragma warning disable 0414

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
// Meshedit.cs: Edit the mesh before going into play mode!
public class Meshedit : MonoBehaviour 
{
	public enum EnumPivotType { Position, Origin };

	// Serialised Variables
	[SerializeField] private List<MesheditStep> mList_mesheditStep = new List<MesheditStep>();
	[SerializeField] private EnumPivotType m_enumPivotType = EnumPivotType.Position;

	[SerializeField] private string m_strMeshName = "meshedit_exportmesh";
	//[SerializeField] private string m_strMeshPath = "Meshes";

	[SerializeField] private bool m_bIsDisplayOutput = false;
	[SerializeField] private bool m_bIsDisplayPivot = false;
	[SerializeField] private bool m_bIsExportMesh = false;

	// Un-serialised Variables
	private Mesh m_mesh = null;
	private Mesh m_meshOutput = null;
	
	// Private Functions
	void OnDrawGizmosSelected()
	{
		if (m_bIsDisplayOutput) 
		{
			Vector3 vec3Origin = m_enumPivotType == EnumPivotType.Origin ? Vector3.zero : transform.position;

			// if: display preview is selected
			if (m_meshOutput != null)
			{
				Gizmos.DrawWireMesh (m_meshOutput, vec3Origin);
			}

			// if: display pivot is selected
			if (m_bIsDisplayPivot) 
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine (vec3Origin, vec3Origin + Vector3.right * 100f);
				Gizmos.color = Color.green;
				Gizmos.DrawLine (vec3Origin, vec3Origin + Vector3.up * 100f);
				Gizmos.color = Color.blue;
				Gizmos.DrawLine (vec3Origin, vec3Origin + Vector3.forward * 100f);
			}
		}
	}

	// Awake(): is called when the script is first initialised
	void Awake () 
	{
		if (!Application.isPlaying) 
		{
			if (GetComponent<MeshFilter> () == null) 
			{
				Debug.LogWarning (name + ".Meshedit: No MeshFilter detected on gameObject!", this.gameObject);
				return;
			}

			// Variable Resets
			m_enumPivotType = EnumPivotType.Origin;
			m_strMeshName = null;
			//m_strMeshPath = null;

			m_bIsDisplayOutput = false;
			m_bIsDisplayPivot = false;
			m_bIsExportMesh = false;
		}
	}

	// Update(): is called once per frame
	void Update()
	{
		if (Application.isPlaying) 
		{
			Debug.LogWarning (name + ".Meshedit: You left this script in play mode! This script is designed to be only executed in edit mode! Disabling itself!", this.gameObject);
			this.enabled = false;
		}
	}

	// Public Functions
	/// <summary>
	/// Calculates the output model after it runs through the mesh-edit steps
	/// </summary>
	/// <returns> Returns the calculated mesh </returns>
	public Mesh CalculateOutput()
	{
		// if: There are no items in the list
		if (mList_mesheditStep.Count == 0)
			return null;

		m_mesh = GetComponent<MeshFilter> ().sharedMesh;

		Mesh meshOutput = new Mesh ();
		meshOutput.vertices = new Vector3[m_mesh.vertexCount];
		meshOutput.normals = new Vector3[m_mesh.normals.Length];
		meshOutput.triangles = m_mesh.triangles;
		meshOutput.uv = m_mesh.uv;
		meshOutput.uv2 = m_mesh.uv2;
		meshOutput.uv3 = m_mesh.uv3;
		meshOutput.uv4 = m_mesh.uv4;

		// for: Every mesh-edit step...
		Vector3[] arr_vec3OutputVertices = m_mesh.vertices;
		Vector3[] arr_vec3OutputNormals = m_mesh.normals;
		for (int i = 0; i < mList_mesheditStep.Count; i++) 
		{
			// for: Every vertex in the existing mesh...
			for (int j = 0; j < m_mesh.vertexCount; j++) 
			{
				switch (mList_mesheditStep [i].stepType) 
				{
				case MesheditStep.EnumMesheditStep.Position:
					arr_vec3OutputVertices [j] += mList_mesheditStep [i].vector3Value;
					break;
				case MesheditStep.EnumMesheditStep.EulerRotation:
					arr_vec3OutputVertices [j] = Quaternion.Euler (mList_mesheditStep [i].vector3Value) * arr_vec3OutputVertices [j];
					arr_vec3OutputNormals [j] = Quaternion.Euler (mList_mesheditStep [i].vector3Value) * arr_vec3OutputNormals [j];
					break;
				default:
					arr_vec3OutputVertices [j] = new Vector3(
						arr_vec3OutputVertices[j].x * mList_mesheditStep[i].vector3Value.x,
						arr_vec3OutputVertices[j].y * mList_mesheditStep[i].vector3Value.y,
						arr_vec3OutputVertices[j].z * mList_mesheditStep[i].vector3Value.z);
					break;
				}
			}
		}
		meshOutput.vertices = arr_vec3OutputVertices;
		meshOutput.normals = arr_vec3OutputNormals;
		meshOutput.RecalculateBounds ();

		return meshOutput;
	}

	/// <summary>
	/// Calculates the final model and updates to class' expected mesh output. This will not override the existing mesh! Use to update display of the output model
	/// </summary>
	public void CalculateOutputAndUpdate()
	{
		m_meshOutput = CalculateOutput ();
	}

	public void Export()
	{
		if (m_bIsExportMesh) 
		{
			if (m_strMeshName == null)
				m_strMeshName = "meshedit_exportmesh";
			else if (m_strMeshName.Length == 0)
				m_strMeshName = "meshedit_exportmesh";

			m_meshOutput.name = m_strMeshName;

//			if (m_strMeshPath == null)
//			{
				Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
				GetComponent <MeshFilter> ().mesh = m_meshOutput;
				ObjExporter.MeshToFile (GetComponent <MeshFilter>(), m_strMeshName);
				GetComponent <MeshFilter>().mesh = mesh;
				Debug.Log (name + ".Meshedit.Export(): Exported mesh to Assets", this.gameObject);
//			}
//			else if (m_strMeshPath.Length == 0)
//				UnityEditor.AssetDatabase.CreateAsset (m_meshOutput, "Assets/" + m_strMeshPath  + "/" + m_strMeshName + ".obj");

		}
	}

	/// <summary>
	/// Commit the output model to the current gameObject
	/// </summary>
	public void Commit()
	{
		if (m_meshOutput == null)
		{
			Debug.LogWarning (name + ".Meshedit.Commit(): Trying to commit to an empty mesh! Reverting changes!");
			return;
		}

		if (m_strMeshName == null)
			m_strMeshName = "meshedit_exportmesh";
		else if (m_strMeshName.Length == 0)
			m_strMeshName = "meshedit_exportmesh";

		m_meshOutput.name = m_strMeshName;
		GetComponent<MeshFilter> ().mesh = m_meshOutput;
		Debug.Log (name + ".Meshedit.Commit(): Commited the mesh of the gameObject. Removing this script from gameObject", this.gameObject);
		DestroyImmediate (this);
	}
}



[System.Serializable]
// MesheditStep.cs: The data to show the steps to take 
public struct MesheditStep
{
	public enum EnumMesheditStep { Position, EulerRotation, Scale };

	[SerializeField] private EnumMesheditStep m_enumMesheditStep;
	[SerializeField] private float m_fVariableX;
	[SerializeField] private float m_fVariableY;
	[SerializeField] private float m_fVariableZ;

	public MesheditStep(EnumMesheditStep _enumMesheditStep, float _x, float _y, float _z)
	{
		m_enumMesheditStep = _enumMesheditStep;
		m_fVariableX = _x;
		m_fVariableY = _y;
		m_fVariableZ = _z;
	}

	// Getter-Setter Functions
	/// <summary>
	/// Gets or sets the type of the step.
	/// </summary>
	/// <value>The type of the step.</value>
	public EnumMesheditStep stepType { get { return m_enumMesheditStep; } set { m_enumMesheditStep = value; } }

	/// <summary>
	/// Gets or sets the x value.
	/// </summary>
	/// <value>The x value.</value>
	public float xValue { get { return m_fVariableX; } set { m_fVariableX = value; } }

	/// <summary>
	/// Gets or sets the y value.
	/// </summary>
	/// <value>The y value.</value>
	public float yValue { get { return m_fVariableY; } set { m_fVariableY = value; } }

	/// <summary>
	/// Gets or sets the z value.
	/// </summary>
	/// <value>The z value.</value>
	public float zValue { get { return m_fVariableZ; } set { m_fVariableZ = value; } }

	public Vector3 vector3Value { get { return new Vector3 (m_fVariableX, m_fVariableY, m_fVariableZ); } }
}