using UnityEngine;
using Verse;
namespace CombatAI
{
    [StaticConstructorOnStartup]
    public static class CombatAI_MeshMaker
    {
        public const float DEPTH_SUPER = -0.0000f;
        public const float DEPTH_TOP   = -0.0075f;
        public const float DEPTH_MID   = -0.0150f;
        public const float DEPTH_BOT   = -0.0300f;

        public static readonly Mesh plane10Super;
        public static readonly Mesh plane10Top;
        public static readonly Mesh plane10Mid;
        public static readonly Mesh plane10Bot;

        public static readonly Mesh plane10FlipSuper;
        public static readonly Mesh plane10FlipTop;
        public static readonly Mesh plane10FlipMid;
        public static readonly Mesh plane10FlipBot;

        static CombatAI_MeshMaker()
        {
            plane10Super = NewPlaneMesh(Vector2.zero, Vector2.one, 0.01f);
            plane10Top   = NewPlaneMesh(Vector2.zero, Vector2.one);
            plane10Mid   = NewPlaneMesh(Vector2.zero, Vector2.one, -0.01f);
            plane10Bot   = NewPlaneMesh(Vector2.zero, Vector2.one, -0.02f);

            plane10FlipSuper = NewPlaneMesh(Vector2.zero, Vector2.one, 0.01f, true);
            plane10FlipTop   = NewPlaneMesh(Vector2.zero, Vector2.one, -0.00f, true);
            plane10FlipMid   = NewPlaneMesh(Vector2.zero, Vector2.one, -0.01f, true);
            plane10FlipBot   = NewPlaneMesh(Vector2.zero, Vector2.one, -0.02f, true);
        }

        public static Mesh NewPlaneMesh(Vector2 position, Vector2 scale, float depth = 0, bool flipped = false)
        {
            Vector3[] vertices = new Vector3[4];
            Vector2[] uv       = new Vector2[4];
            int[]     indexes  = new int[6];

            // This is the default form 
            // vertices[0] = new Vector3(-0.5f * scale.x, depth, -0.5f * scale.y);
            // vertices[1] = new Vector3(-0.5f * scale.x, depth, 0.5f * scale.y);
            // vertices[2] = new Vector3(0.5f * scale.x, depth, 0.5f * scale.y);
            // vertices[3] = new Vector3(0.5f * scale.x, depth, -0.5f * scale.y);

            if (flipped)
            {
                vertices[0] = new Vector3(position.x, depth, position.y);
                vertices[1] = vertices[0] + new Vector3(0f, 0f, scale.y);
                vertices[2] = vertices[0] + new Vector3(scale.x, 0f, scale.y);
                vertices[3] = vertices[0] + new Vector3(scale.x, 0f, 0f);
            }
            else
            {
                vertices[0] = new Vector3(position.x, depth, position.y);
                vertices[1] = vertices[0] + new Vector3(0f, 0f, scale.y);
                vertices[2] = vertices[0] + new Vector3(scale.x, 0f, scale.y);
                vertices[3] = vertices[0] + new Vector3(scale.x, 0f, 0f);
            }

            if (!flipped)
            {
                uv[0] = new Vector2(0f, 0f);
                uv[1] = new Vector2(0f, 1f);
                uv[2] = new Vector2(1f, 1f);
                uv[3] = new Vector2(1f, 0f);
            }
            else
            {
                uv[0] = new Vector2(1f, 0f);
                uv[1] = new Vector2(1f, 1f);
                uv[2] = new Vector2(0f, 1f);
                uv[3] = new Vector2(0f, 0f);
            }
            indexes[0] = 0;
            indexes[1] = 1;
            indexes[2] = 2;
            indexes[3] = 0;
            indexes[4] = 2;
            indexes[5] = 3;
            Mesh mesh = new Mesh();
            mesh.name     = "NewPlaneMesh()";
            mesh.vertices = vertices;
            mesh.uv       = uv;
            mesh.SetTriangles(indexes, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
