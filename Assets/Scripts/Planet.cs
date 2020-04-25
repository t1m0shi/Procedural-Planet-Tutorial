using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The Planet class is responsible for generating a tiny procedural planet. It does this by subdividing an Icosahedron, then
// randomly selecting groups of Polygons to extrude outwards. These become the lowlands and hills of the planet, while the
// unextruded Polygons become the ocean.
public class Planet : MonoBehaviour
{
    // These public parameters can be tweaked to give different styles to your planet.
    public enum PlanetType { Earthlike, Volcanic, Fungal, Desolate, Metallic, Ice }
    public PlanetType planetType;
    public float planetSize = 1f;

    public Color32 colorOcean;
    public Color32 colorGrass;
    public Color32 colorDirt;
    public Color32 colorDeepOcean;

    public Material m_GroundMaterial;
    public Material m_OceanMaterial;

    public int   m_NumberOfContinents;// = 5;
    public int minNumContinents = 2;
    public float m_ContinentSizeMax   = 1.0f;
    public float m_ContinentSizeMin   = 0.2f;

    public int   m_NumberOfHills;// = 5;
    public int minNumHills = 0;
    public float m_HillSizeMax   = 1.0f;
    public float m_HillSizeMin   = 0.1f;

    // Internally, the Planet object stores its meshes as a child GameObjects:
    GameObject m_GroundMesh;
    GameObject m_OceanMesh;

    // The subdivided icosahedron that we use to generate our planet is represented as a list
    // of Polygons, and a list of Vertices for those Polygons:
    List<Polygon> m_Polygons;
    List<Vector3> m_Vertices;

    [HideInInspector] public Vector3 initPos;
    [HideInInspector] public int sunDist;


    public void Start()
    {
        CreatePlanet();
        MovePlanet(this, initPos);
        DrawOrbit();
        //this.GetComponent<LineRenderer>().enabled = false; //turn back on when enter large zoom?
    }
    public void DrawOrbit()
    {
        var segments = 360;
        var line = this.gameObject.GetComponent<LineRenderer>();
        float lineWidth = 0.05f;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = segments + 1;
        //line.material.color = new Color(90, 90, 90, 0f);
        //line.material.shader
        //line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
        var points = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            var rad = Mathf.Deg2Rad * (i * 360f / segments);
            points[i] = new Vector3(Mathf.Sin(rad) * sunDist, 0, Mathf.Cos(rad) * sunDist);
        }

        line.SetPositions(points);
    }
    public void MovePlanet(Planet target, Vector3 newPos)
    {
        this.transform.position = newPos;
    }
    public void CreatePlanet()
    {
        // Create an icosahedron, subdivide it three times so that we have plenty of polys to work with.
        m_NumberOfContinents = minNumContinents;
        System.Random rnd = new System.Random();
        //planetSize = rnd.Next(1, 11);
        planetSize = 10;

        InitAsIcosohedron(planetSize);
        Subdivide(3);

        // When we begin extruding polygons, we'll need each one to know who its immediate
        //neighbors are. Calculate that now.

        CalculateNeighbors();

        // By default, everything is colored blue. As we extrude land forms, we'll change their colors to match.
        // Picks a random planet type initially
        planetType = (PlanetType)rnd.Next(5);
        //planetType = PlanetType.Volcanic;

        //  = new Color32(255, 0, 255, 255); hot pink
        switch (planetType)
        {
            case PlanetType.Earthlike:
                colorOcean = new Color32(0, 80, 220, 0);
                colorGrass = new Color32(0, 220, 0, 0);
                colorDirt = new Color32(180, 140, 20, 0);
                colorDeepOcean = new Color32(0, 40, 110, 0);
                break;
            case PlanetType.Volcanic:
                //colorOcean = new Color32(0, 80, 220, 0);
                colorOcean = HueColour.HueColourValue(HueColour.HueColorNames.Red);
                colorGrass = HueColour.HueColourValue(HueColour.HueColorNames.GrayDark);
                colorDirt = HueColour.HueColourValue(HueColour.HueColorNames.RedDeep);
                colorDeepOcean = HueColour.HueColourValue(HueColour.HueColorNames.RedDeep);
                break;
            default:
                colorOcean = new Color32(0, 80, 220, 0);
                colorGrass = new Color32(0, 220, 0, 0);
                colorDirt = new Color32(180, 140, 20, 0);
                colorDeepOcean = new Color32(0, 40, 110, 0);
                break;
        }

        //pick the planet size attributes
        m_NumberOfContinents = rnd.Next(1, 10);
        m_NumberOfHills = rnd.Next(0, 10);



        foreach (Polygon p in m_Polygons)
            p.m_Color = colorOcean;

        // Now we build a set of Polygons that will become the land. We do this by generating
        // randomly sized spheres on the surface of the planet, and adding any Polygon that falls
        // inside that sphere.

        PolySet landPolys = new PolySet();
        PolySet sides;

        // Grab polygons that are inside random spheres. These will be the basis of our planet's continents.

        for (int i = 0; i < m_NumberOfContinents; i++)
        {
            float continentSize = Random.Range(m_ContinentSizeMin, m_ContinentSizeMax);

            PolySet newLand = GetPolysInSphere(Random.onUnitSphere, continentSize, m_Polygons);

            landPolys.UnionWith(newLand);
        }

        // While we're here, let's make a group of oceanPolys. It's pretty simple: Any Polygon that isn't in the landPolys set
        // must be in the oceanPolys set instead.

        var oceanPolys = new PolySet();

        foreach (Polygon poly in m_Polygons)
        {
            if (!landPolys.Contains(poly))
                oceanPolys.Add(poly);
        }

        // Let's create the ocean surface as a separate mesh.
        // First, let's make a copy of the oceanPolys so we can
        // still use them to also make the ocean floor later.

        var oceanSurface = new PolySet(oceanPolys);

        sides = Inset(oceanSurface, 0.05f);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        if (m_OceanMesh != null)
            Destroy(m_OceanMesh);

        m_OceanMesh = GenerateMesh("Ocean Surface", m_OceanMaterial);

        // Back to land for a while! We start by making it green. =)

        foreach (Polygon landPoly in landPolys)
        {
            landPoly.m_Color = colorGrass;
        }

        // The Extrude function will raise the land Polygons up out of the water.
        // It also generates a strip of new Polygons to connect the newly raised land
        // back down to the water level. We can color this vertical strip of land brown like dirt.

        sides = Extrude(landPolys, 0.05f);

        sides.ApplyColor(colorDirt);

        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        // Grab additional polygons to generate hills, but only from the set of polygons that are land.

        PolySet hillPolys = landPolys.RemoveEdges();

        sides = Inset(hillPolys, 0.03f);
        sides.ApplyColor(colorGrass);
        sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        sides = Extrude(hillPolys, 0.05f);
        sides.ApplyColor(colorDirt);

        //Hills have dark ambient occlusion on the bottom, and light on top.
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        // Time to return to the oceans.

        sides = Extrude(oceanPolys, -0.02f);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        sides = Inset(oceanPolys, 0.02f);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        var deepOceanPolys = oceanPolys.RemoveEdges();

        sides = Extrude(deepOceanPolys, -0.05f);
        sides.ApplyColor(colorDeepOcean);

        deepOceanPolys.ApplyColor(colorDeepOcean);

        // Okay, we're done! Let's generate an actual game mesh for this planet.

        if (m_GroundMesh != null)
            Destroy(m_GroundMesh);

        m_GroundMesh = GenerateMesh("Ground Mesh", m_GroundMaterial);
    }

    public void InitAsIcosohedron(float size)
    {
        m_Polygons = new List<Polygon>();
        m_Vertices = new List<Vector3>();

        // An icosahedron has 12 vertices, and
        // since they're completely symmetrical the
        // formula for calculating them is kind of
        // symmetrical too:

        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
        //t *= size;
        //Debug.Log(t);

        m_Vertices.Add(new Vector3(-1,  t,  0).normalized);
        m_Vertices.Add(new Vector3( 1,  t,  0).normalized);
        m_Vertices.Add(new Vector3(-1, -t,  0).normalized);
        m_Vertices.Add(new Vector3( 1, -t,  0).normalized);
        m_Vertices.Add(new Vector3( 0, -1,  t).normalized);
        m_Vertices.Add(new Vector3( 0,  1,  t).normalized);
        m_Vertices.Add(new Vector3( 0, -1, -t).normalized);
        m_Vertices.Add(new Vector3( 0,  1, -t).normalized);
        m_Vertices.Add(new Vector3( t,  0, -1).normalized);
        m_Vertices.Add(new Vector3( t,  0,  1).normalized);
        m_Vertices.Add(new Vector3(-t,  0, -1).normalized);
        m_Vertices.Add(new Vector3(-t,  0,  1).normalized);

        // And here's the formula for the 20 sides,
        // referencing the 12 vertices we just created.

        m_Polygons.Add(new Polygon( 0, 11,  5));
        m_Polygons.Add(new Polygon( 0,  5,  1));
        m_Polygons.Add(new Polygon( 0,  1,  7));
        m_Polygons.Add(new Polygon( 0,  7, 10));
        m_Polygons.Add(new Polygon( 0, 10, 11));
        m_Polygons.Add(new Polygon( 1,  5,  9));
        m_Polygons.Add(new Polygon( 5, 11,  4));
        m_Polygons.Add(new Polygon(11, 10,  2));
        m_Polygons.Add(new Polygon(10,  7,  6));
        m_Polygons.Add(new Polygon( 7,  1,  8));
        m_Polygons.Add(new Polygon( 3,  9,  4));
        m_Polygons.Add(new Polygon( 3,  4,  2));
        m_Polygons.Add(new Polygon( 3,  2,  6));
        m_Polygons.Add(new Polygon( 3,  6,  8));
        m_Polygons.Add(new Polygon( 3,  8,  9));
        m_Polygons.Add(new Polygon( 4,  9,  5));
        m_Polygons.Add(new Polygon( 2,  4, 11));
        m_Polygons.Add(new Polygon( 6,  2, 10));
        m_Polygons.Add(new Polygon( 8,  6,  7));
        m_Polygons.Add(new Polygon( 9,  8,  1));
    }

    public void Subdivide(int recursions)
    {
        var midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < recursions; i++)
        {
            var newPolys = new List<Polygon>();
            foreach (var poly in m_Polygons)
            {
                int a = poly.m_Vertices[0];
                int b = poly.m_Vertices[1];
                int c = poly.m_Vertices[2];

                // Use GetMidPointIndex to either create a
                // new vertex between two old vertices, or
                // find the one that was already created.

                int ab = GetMidPointIndex(midPointCache, a, b);
                int bc = GetMidPointIndex(midPointCache, b, c);
                int ca = GetMidPointIndex(midPointCache, c, a);

                // Create the four new polygons using our original
                // three vertices, and the three new midpoints.
                newPolys.Add(new Polygon(a, ab, ca));
                newPolys.Add(new Polygon(b, bc, ab));
                newPolys.Add(new Polygon(c, ca, bc));
                newPolys.Add(new Polygon(ab, bc, ca));
            }
            // Replace all our old polygons with the new set of
            // subdivided ones.
            m_Polygons = newPolys;
        }
    }

    public int GetMidPointIndex(Dictionary<int, int> cache, int indexA, int indexB)
    {
        // We create a key out of the two original indices
        // by storing the smaller index in the upper two bytes
        // of an integer, and the larger index in the lower two
        // bytes. By sorting them according to whichever is smaller
        // we ensure that this function returns the same result
        // whether you call
        // GetMidPointIndex(cache, 5, 9)
        // or...
        // GetMidPointIndex(cache, 9, 5)

        int smallerIndex = Mathf.Min(indexA, indexB);
        int greaterIndex = Mathf.Max(indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;

        // If a midpoint is already defined, just return it.

        int ret;
        if (cache.TryGetValue(key, out ret))
            return ret;

        // If we're here, it's because a midpoint for these two
        // vertices hasn't been created yet. Let's do that now!

        Vector3 p1 = m_Vertices[indexA];
        Vector3 p2 = m_Vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = m_Vertices.Count;
        m_Vertices.Add(middle);

        // Add our new midpoint to the cache so we don't have
        // to do this again. =)

        cache.Add(key, ret);
        return ret;
    }

    public void CalculateNeighbors()
    {
        foreach (Polygon poly in m_Polygons)
        {
            foreach (Polygon other_poly in m_Polygons)
            {
                if (poly == other_poly)
                    continue;

                if (poly.IsNeighborOf(other_poly))
                    poly.m_Neighbors.Add(other_poly);
            }
        }
    }

    public List<int> CloneVertices(List<int> old_verts)
    {
        List<int> new_verts = new List<int>();
        foreach (int old_vert in old_verts)
        {
            Vector3 cloned_vert = m_Vertices[old_vert];
            new_verts.Add(m_Vertices.Count);
            m_Vertices.Add(cloned_vert);
        }
        return new_verts;
    }

    public PolySet StitchPolys(PolySet polys, out EdgeSet stitchedEdge)
    {
        PolySet stichedPolys = new PolySet();

        stichedPolys.m_StitchedVertexThreshold = m_Vertices.Count;

        stitchedEdge      = polys.CreateEdgeSet();
        var originalVerts = stitchedEdge.GetUniqueVertices();
        var newVerts      = CloneVertices(originalVerts);

        stitchedEdge.Split(originalVerts, newVerts);

        foreach (Edge edge in stitchedEdge)
        {
            // Create new polys along the stitched edge. These
            // will connect the original poly to its former
            // neighbor.

            var stitch_poly1 = new Polygon(edge.m_OuterVerts[0],
                                           edge.m_OuterVerts[1],
                                           edge.m_InnerVerts[0]);
            var stitch_poly2 = new Polygon(edge.m_OuterVerts[1],
                                           edge.m_InnerVerts[1],
                                           edge.m_InnerVerts[0]);
            // Add the new stitched faces as neighbors to
            // the original Polys.
            edge.m_InnerPoly.ReplaceNeighbor(edge.m_OuterPoly, stitch_poly2);
            edge.m_OuterPoly.ReplaceNeighbor(edge.m_InnerPoly, stitch_poly1);

            m_Polygons.Add(stitch_poly1);
            m_Polygons.Add(stitch_poly2);

            stichedPolys.Add(stitch_poly1);
            stichedPolys.Add(stitch_poly2);
        }

        //Swap to the new vertices on the inner polys.
        foreach (Polygon poly in polys)
        {
            for (int i = 0; i < 3; i++)
            {
                int vert_id = poly.m_Vertices[i];
                if (!originalVerts.Contains(vert_id))
                    continue;
                int vert_index = originalVerts.IndexOf(vert_id);
                poly.m_Vertices[i] = newVerts[vert_index];
            }
        }

        return stichedPolys;
    }

    public PolySet Extrude(PolySet polys, float height)
    {
        EdgeSet stitchedEdge;
        PolySet stitchedPolys = StitchPolys(polys, out stitchedEdge);
        List<int> verts = polys.GetUniqueVertices();

        // Take each vertex in this list of polys, and push it
        // away from the center of the Planet by the height
        // parameter.

        foreach (int vert in verts)
        {
            Vector3 v = m_Vertices[vert];
            v = v.normalized * (v.magnitude + height);
            m_Vertices[vert] = v;
        }

        return stitchedPolys;
    }

    public PolySet Inset(PolySet polys, float insetDistance)
    {
        EdgeSet stitchedEdge;
        PolySet stitchedPolys = StitchPolys(polys, out stitchedEdge);

        Dictionary<int, Vector3> inwardDirections = stitchedEdge.GetInwardDirections(m_Vertices);

        // Push each vertex inwards, then correct
        // it's height so that it's as far from the center of
        // the planet as it was before.

        foreach (KeyValuePair<int, Vector3> kvp in inwardDirections)
        {
            int     vertIndex       = kvp.Key;
            Vector3 inwardDirection = kvp.Value;

            Vector3 vertex = m_Vertices[vertIndex];
            float originalHeight = vertex.magnitude;

            vertex += inwardDirection * insetDistance;
            vertex  = vertex.normalized * originalHeight;
            m_Vertices[vertIndex] = vertex;
        }

        return stitchedPolys;
    }

    public PolySet GetPolysInSphere(Vector3 center, float radius, IEnumerable<Polygon> source)
    {
        PolySet newSet = new PolySet();

        foreach(Polygon p in source)
        {
            foreach(int vertexIndex in p.m_Vertices)
            {
                float distanceToSphere = Vector3.Distance(center, m_Vertices[vertexIndex]);

                if (distanceToSphere <= radius)
                {
                    newSet.Add(p);
                    break;
                }
            }
        }

        return newSet;
    }

    public GameObject GenerateMesh(string name, Material material)
    {
        GameObject meshObject       = new GameObject(name);
        meshObject.transform.parent = transform;

        MeshRenderer surfaceRenderer = meshObject.AddComponent<MeshRenderer>();
        surfaceRenderer.material     = material;

        Mesh terrainMesh = new Mesh();

        int vertexCount = m_Polygons.Count * 3;

        int[] indices = new int[vertexCount];

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals  = new Vector3[vertexCount];
        Color32[] colors   = new Color32[vertexCount];
        Vector2[] uvs      = new Vector2[vertexCount];

        for (int i = 0; i < m_Polygons.Count; i++)
        {
            var poly = m_Polygons[i];

            indices[i * 3 + 0] = i * 3 + 0;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;

            vertices[i * 3 + 0] = m_Vertices[poly.m_Vertices[0]];
            vertices[i * 3 + 1] = m_Vertices[poly.m_Vertices[1]];
            vertices[i * 3 + 2] = m_Vertices[poly.m_Vertices[2]];

            uvs[i * 3 + 0] = poly.m_UVs[0];
            uvs[i * 3 + 1] = poly.m_UVs[1];
            uvs[i * 3 + 2] = poly.m_UVs[2];

            colors[i * 3 + 0] = poly.m_Color;
            colors[i * 3 + 1] = poly.m_Color;
            colors[i * 3 + 2] = poly.m_Color;

            if(poly.m_SmoothNormals)
            {
                normals[i * 3 + 0] = m_Vertices[poly.m_Vertices[0]].normalized;
                normals[i * 3 + 1] = m_Vertices[poly.m_Vertices[1]].normalized;
                normals[i * 3 + 2] = m_Vertices[poly.m_Vertices[2]].normalized;
            }
            else
            {
                Vector3 ab = m_Vertices[poly.m_Vertices[1]] - m_Vertices[poly.m_Vertices[0]];
                Vector3 ac = m_Vertices[poly.m_Vertices[2]] - m_Vertices[poly.m_Vertices[0]];

                Vector3 normal = Vector3.Cross(ab, ac).normalized;

                normals[i * 3 + 0] = normal;
                normals[i * 3 + 1] = normal;
                normals[i * 3 + 2] = normal;
            }
        }

        terrainMesh.vertices = vertices;
        terrainMesh.normals  = normals;
        terrainMesh.colors32 = colors;
        terrainMesh.uv       = uvs;

        terrainMesh.SetTriangles(indices, 0);

        MeshFilter terrainFilter = meshObject.AddComponent<MeshFilter>();
        terrainFilter.mesh = terrainMesh;

        return meshObject;
    }
}


public class HueColour
{

    public enum HueColorNames
    {
        Lime,
        Green,
        Aqua,
        Blue,
        Navy,
        Purple,
        Pink,
        Red,
        Orange,
        Yellow,
        Brown,
        LightGreen,
        White,
        GrayDark,
        Gray,
        BlueOcean,
        BlueDeep,
        RedDeep
    }

    private static Hashtable hueColourValues = new Hashtable{
         { HueColorNames.Lime,     new Color32( 166 , 254 , 0, 1 ) },
         { HueColorNames.LightGreen,     new Color32( 0 , 254 , 111, 1 ) },
         { HueColorNames.Aqua,     new Color32( 0 , 201 , 254, 1 ) },
         { HueColorNames.Blue,     new Color32( 0 , 122 , 254, 1 ) },
         { HueColorNames.Navy,     new Color32( 60 , 0 , 254, 1 ) },
         { HueColorNames.Purple, new Color32( 143 , 0 , 254, 1 ) },
         { HueColorNames.Pink,     new Color32( 232 , 0 , 254, 1 ) },
         { HueColorNames.Red,     new Color32( 243 , 57 , 57, 0 ) },
         { HueColorNames.RedDeep,     new Color32( 95 , 0 , 0, 0 ) },
         { HueColorNames.Orange, new Color32( 254 , 161 , 0, 1 ) },
         { HueColorNames.Yellow, new Color32( 254 , 224 , 0, 1 ) },
         { HueColorNames.Brown,  new Color32(180, 140,  20,   0) },
         { HueColorNames.Green, new Color32(0, 220, 0, 0) },
         { HueColorNames.White, new Color32( 255, 255, 255, 1 ) },
         { HueColorNames.Gray, new Color32( 161, 156, 156, 1 ) },
         { HueColorNames.BlueOcean, new Color32(0, 80, 220, 0) },
         { HueColorNames.BlueDeep, new Color32(0, 40, 110, 0) },
         { HueColorNames.GrayDark, new Color32(61, 61, 61, 1) },
};

    public static Color32 HueColourValue(HueColorNames color)
    {
        return (Color32)hueColourValues[color];
    }

}
