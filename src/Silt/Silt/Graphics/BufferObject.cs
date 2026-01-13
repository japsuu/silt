using Silk.NET.OpenGL;

namespace Silt.Graphics;

/// <summary>
/// Represents a buffer object on the GPU.
/// </summary>
/// <typeparam name="T">The type of data stored in the buffer. Must be an unmanaged type.</typeparam>
public sealed class BufferObject<T> : GraphicsResource where T : unmanaged
{
    public readonly uint DataLength;
    
    private readonly BufferTargetARB _bufferTarget;


    /// <summary>
    /// Creates a new buffer object.
    /// </summary>
    /// <param name="gl">The OpenGL context</param>
    /// <param name="data">The data to store in the buffer</param>
    /// <param name="target">The type of buffer to create</param>
    public unsafe BufferObject(GL gl, ReadOnlySpan<T> data, BufferTargetARB target) : base(gl)
    {
        _bufferTarget = target;
        Handle = Gl.GenBuffer();
        Bind();

        fixed (void* d = data)
        {
            uint dataLength = (uint)data.Length;
            Gl.BufferData(_bufferTarget, (nuint)(dataLength * sizeof(T)), d, BufferUsageARB.StaticDraw);
            DataLength = dataLength;
        }
    }


    /// <summary>
    /// Binds this buffer to the current OpenGL context.
    /// </summary>
    public void Bind()
    {
        Gl.BindBuffer(_bufferTarget, Handle);
    }


    /// <summary>
    /// Unbinds this buffer from the current OpenGL context.
    /// </summary>
    public void Unbind()
    {
        Gl.BindBuffer(_bufferTarget, 0);
    }


    protected override void DisposeResources(bool manual)
    {
        Gl.DeleteBuffer(Handle);
    }
}