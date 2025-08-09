#pragma author ducc
#pragma description Overgrowth Cached Model File

using GLFloat = float;
using Index = u32;

struct Header {
    u16 Checksum;
    u16 Version;
};

struct Vector3 {
    GLFloat x;
    GLFloat y;
    GLFloat z;
};

struct Vector2 {
    GLFloat x;
    GLFloat y;
};

struct VertexData {
    u32 VertexCount;
    Vector3 Positions[VertexCount];
    Vector3 Normals[VertexCount];
    Vector3 Tangents[VertexCount];
    Vector3 Bitangents[VertexCount];
    Vector2 TexCoords[VertexCount];
};

struct Triangle {
    Index i0;
    Index i1;
    Index i2;
};


struct TriangleData{
    u32 TriangleCount;
    Triangle Triangles[TriangleCount];
    Vector3 Normals[TriangleCount];
};

struct ReorderData {
    u32 PrecollapseNumVertices;
    u32 PrecollapseVertReorderCount;
    Index PrecollapseVertReorder[PrecollapseVertReorderCount];
    u32 OptimizeVertReorderCount;
    Index OptimizeVertReorder[OptimizeVertReorderCount];
};

struct ModelCache {
    Header Header;
    VertexData VertexData;
    TriangleData TriangleData;
    ReorderData ReorderData;
    u32 TexCoords2Count;
    Vector2 TexCoords2[TexCoords2Count];
    Vector3 MinCoords;
    Vector3 MaxCoords;
    Vector3 CenterCoords;
    Vector3 OldCenter;
    Vector3 BoundingSphereOrigin;
    float BoundingSphereRadius;
    float TexelDensity;
    float AverageTriangleEdgeLength;
};

ModelCache ModelCache @ 0x0;