using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silt.CameraManagement;
using Silt.SceneManagement;

namespace Silt.Scenes;

public sealed class BenchmarkScene1 : Scene
{
    public BenchmarkScene1(GL gl, IWindow window) : base(gl, window)
    {
    }


    public override void Load()
    {
        // Setup scene camera
        CameraManager.MainCamera.Position = new Vector3(0, 0, 5);
        CameraManager.SetActiveController(new FreeCameraController());
    }


    public override void Unload()
    {
        
    }


    public override void Update(double deltaTime)
    {
        
    }


    public override void FixedUpdate(double deltaTime)
    {
        
    }


    public override unsafe void Render(double deltaTime)
    {
        
    }
}