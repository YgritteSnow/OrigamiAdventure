using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonJitter : MonoBehaviour {
	private bool m_bShow = true;
	public int m_polygon_depth = 0;
	public float m_offset = -0.2f;
	
	// Update is called once per frame
	void Update () {
		Vector3 pos = transform.position;
		pos.z = m_polygon_depth * -m_offset;// + 0.1f * Mathf.Cos(Time.time));
		transform.position = pos;
	}

	public void SetPolygonDepth(int depth)
	{
		m_polygon_depth = depth;
	}

	public void ShowPolygon(bool bShow)
	{
		m_bShow = bShow;
		enabled = bShow;
	}
}
