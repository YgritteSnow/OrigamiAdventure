using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugOrigamiPaper : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
		GetComponent<OrigamiPaper>().InitOriginalPolygons(DebugPolygons());
	}

	List<Polygon> DebugPolygons()
	{
		List<Polygon> res = new List<Polygon>();

		Polygon p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(1, 1));
		p.m_points.Add(new PolygonPoint(1, -1));
		p.m_points.Add(new PolygonPoint(-1, -1));
		p.m_points.Add(new PolygonPoint(-1, 1));

		p.m_edges = new List<PolygonEdge>();
		p.m_edges.Add(new PolygonEdge(1, 0, 1, true));
		p.m_edges.Add(new PolygonEdge(2, 1, 2, true));
		p.m_edges.Add(new PolygonEdge(3, 2, 3, true));
		p.m_edges.Add(new PolygonEdge(4, 3, 0, true));

		p.InitAll();
		res.Add(p);

		p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(7, 1));
		p.m_points.Add(new PolygonPoint(7, -1));
		p.m_points.Add(new PolygonPoint(5, -1));
		p.m_points.Add(new PolygonPoint(5, 1));

		p.m_edges = new List<PolygonEdge>();
		p.m_edges.Add(new PolygonEdge(1, 0, 1, true));
		p.m_edges.Add(new PolygonEdge(2, 1, 2, true));
		p.m_edges.Add(new PolygonEdge(3, 2, 3, true));
		p.m_edges.Add(new PolygonEdge(4, 3, 0, true));

		p.InitAll();
		res.Add(p);

		return res;
	}
}
