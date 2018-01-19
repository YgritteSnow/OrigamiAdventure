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

		List<PolygonEdge> edge_indexes = new List<PolygonEdge>();
		edge_indexes.Add(new PolygonEdge(0, 1, true));
		edge_indexes.Add(new PolygonEdge(1, 2, true));
		edge_indexes.Add(new PolygonEdge(2, 3, true));
		edge_indexes.Add(new PolygonEdge(3, 0, true));
		p.SetEdgeByIndexPair(edge_indexes);

		p.InitAll();
		res.Add(p);

		p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(7, 1));
		p.m_points.Add(new PolygonPoint(7, -1));
		p.m_points.Add(new PolygonPoint(5, -1));
		p.m_points.Add(new PolygonPoint(5, 1));

		List<PolygonEdge> edges = new List<PolygonEdge>();
		edges.Add(new PolygonEdge(0, 1, true));
		edges.Add(new PolygonEdge(1, 2, true));
		edges.Add(new PolygonEdge(2, 3, true));
		edges.Add(new PolygonEdge(3, 0, true));
		p.SetEdgeByIndexPair(edges);

		p.InitAll();
		res.Add(p);

		return res;
	}
}
