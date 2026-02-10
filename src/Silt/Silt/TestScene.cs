using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silt.CameraManagement;
using Silt.Graphics;
using Silt.Utils;
using Shader = Silt.Graphics.Shader;
using Texture = Silt.Graphics.Texture;

namespace Silt;

public sealed class TestScene : Scene
{
    private VertexArrayObject<float, uint> _vao = null!;
    private BufferObject<float> _vbo = null!;
    private BufferObject<uint> _ebo = null!;
    private Texture _texture = null!;
    private Shader _shader = null!;
    private readonly Transform[] _transforms = new Transform[3];
    
    private static readonly float[] _cubeVertices =
    [
        // X, Y, Z, U, V
        // Z+ (back)
        -0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, 0.5f, 0.0f, 1.0f,

        // Z- (front)
        0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        -0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, -0.5f, 0.0f, 1.0f,

        // X+ (right)
        0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, 0.5f, 0.0f, 1.0f,

        // X- (left)
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
        -0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, -0.5f, 0.0f, 1.0f,

        // Y+ (top)
        -0.5f, 0.5f, 0.5f, 0.0f, 0.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, -0.5f, 0.0f, 1.0f,

        // Y- (bottom)
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, -0.5f, 0.5f, 0.0f, 1.0f
    ];

    private static readonly uint[] _cubeIndices =
    [
        // Z+
        0, 1, 2,
        0, 2, 3,

        // Z-
        4, 5, 6,
        4, 6, 7,

        // X+
        8, 9, 10,
        8, 10, 11,

        // X-
        12, 13, 14,
        12, 14, 15,

        // Y+
        16, 17, 18,
        16, 18, 19,

        // Y-
        20, 21, 22,
        20, 22, 23
    ];

    private int _uTexture;
    private int _uMatM;
    private int _uMatMV;
    private int _uMatMVP;


    public TestScene(GL gl, IWindow window) : base(gl, window)
    {
    }


    public override void Load()
    {
        // Setup scene camera
        CameraManager.MainCamera.Position = new Vector3(0, 0, 5);
        CameraManager.SetActiveController(new FreeCameraController());

        // Create buffers
        _vbo = new BufferObject<float>(GL, _cubeVertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(GL, _cubeIndices, BufferTargetARB.ElementArrayBuffer);

        // Create VAO
        _vao = new VertexArrayObject<float, uint>(GL, _vbo, _ebo);
        _vao.SetVertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.SetVertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

        // Create shader
        _shader = new Shader(GL, "base", "assets/base.vert", "assets/base.frag");
        
        // Cache uniform locations
        _uTexture = _shader.GetUniformLocation("u_texture");
        _uMatM = _shader.GetUniformLocation("u_mat_m");
        _uMatMV = _shader.GetUniformLocation("u_mat_mv");
        _uMatMVP = _shader.GetUniformLocation("u_mat_mvp");

        // Create texture
        _texture = new Texture(GL, "assets/tex_proto_wall.png");

        // Cleanup
        _vao.Unbind();
        _vbo.Unbind();
        _ebo.Unbind();

        // Translation
        _transforms[0] = new Transform();
        _transforms[0].Position = new Vector3(0.5f, 0.5f, 0f);

        // Scaling
        _transforms[1] = new Transform();
        _transforms[1].Scale = 0.5f;

        // Mixed transformation
        _transforms[2] = new Transform();
        _transforms[2].Position = new Vector3(-0.5f, 0.5f, 0f);
        _transforms[2].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1f);
        _transforms[2].Scale = 0.5f;
    }


    public override void Unload()
    {
        _shader.Dispose();
        _texture.Dispose();
        _vbo.Dispose();
        _ebo.Dispose();
        _vao.Dispose();
    }


    public override void Update(double deltaTime)
    {
        foreach (Transform t in _transforms)
        {
            // Convert time to radians for rotation
            float rotDegrees = (float)(Window.Time * 100);
            t.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathUtil.DegreesToRadians(rotDegrees)) *
                         Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathUtil.DegreesToRadians(rotDegrees * 0.6f));
        }
    }


    public override void FixedUpdate(double deltaTime)
    {
        
    }


    public override unsafe void Render(double deltaTime)
    {
        _vao.Bind();

        _shader.Use();
        _shader.SetUniform(_uTexture, _texture, TextureUnit.Texture0);

        Matrix4x4 view = CameraManager.MainCamera.GetViewMatrix();
        Matrix4x4 projection = CameraManager.MainCamera.GetProjectionMatrix();

        foreach (Transform t in _transforms)
        {
            Matrix4x4 mv = t.ModelMatrix * view;
            Matrix4x4 mvp = mv * projection;
            _shader.SetUniform(_uMatM, t.ModelMatrix);
            _shader.SetUniform(_uMatMV, mv);
            _shader.SetUniform(_uMatMVP, mvp);

            GL.DrawElements(PrimitiveType.Triangles, (uint)_cubeIndices.Length, DrawElementsType.UnsignedInt, null);
        }
    }
}