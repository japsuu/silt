# Silt

A chunk-based voxel engine built in C# / .NET 10 as part of a bachelor's thesis on efficient voxel mesh generation. The engine uses [Silk.NET](https://github.com/dotnet/Silk.NET) for OpenGL 3.3 rendering and GLFW windowing, and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) for a debug overlay.

The project serves as a research platform for comparing and benchmarking voxel meshing algorithms. It implements a progression of optimizations from a naïve cube-per-voxel baseline to a binary greedy mesher with compact vertex encoding and cache-oriented data access, and includes an automated benchmarking framework to measure their effects.

## Features

- Chunked voxel world with 32³ chunks and procedural 3D noise generation
- Meshing pipeline with hidden-face culling, cross-chunk boundary culling, greedy meshing, and binary greedy meshing
- Compact packed vertex format (36 -> 4 bytes per vertex)
- CPU-side optimizations: flat array layout, structure-of-arrays voxel storage, bitmask transposition, zero-allocation hot path
- Automated benchmark state machine with warm-up and sample phases
- ImGui diagnostics overlay for live performance inspection

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A GPU and driver supporting OpenGL 3.3

## Building and Running

```sh
cd src/Silt
dotnet build
dotnet run --project Silt
```

This launches the interactive test scene. Press `ESC` to enable free camera controls.
Use WASD + mouse to navigate while in free camera mode.

## Benchmarking

Run a benchmark scene to collect meshing and rendering performance data:

```sh
dotnet run --project Silt -- --benchmark normal
```

Available benchmark scenes:

| Scene         | Chunks   | Noise frequency | Description                                      |
|---------------|----------|-----------------|--------------------------------------------------|
| `normal-fast` | 4³ (64)  | 0.05            | Quick smoke test                                 |
| `normal`      | 8³ (512) | 0.05            | Smooth terrain, low surface-to-volume ratio      |
| `worst-case`  | 8³ (512) | 1.0             | Fragmented terrain, high surface-to-volume ratio |

Results are written to `benchmark_results.txt` by default. Use `--benchmark-out <path>` to change the output path.

### Additional Options

| Option                                           | Description                                  |
|--------------------------------------------------|----------------------------------------------|
| `--benchmark-out <path>`                         | Benchmark output file path                   |
| `--benchmark-warmup-meshing-seconds <s>`         | Meshing warm-up duration (default: 10)       |
| `--benchmark-sample-meshing-seconds <s>`         | Meshing sample duration (default: 30)        |
| `--benchmark-warmup-rendering-seconds <s>`       | Rendering warm-up duration (default: 10)     |
| `--benchmark-sample-rendering-seconds <s>`       | Rendering sample duration (default: 30)      |
| `--benchmark-batch-remesh-warmup-iterations <n>` | Batch remesh warm-up iterations (default: 3) |
| `--benchmark-batch-remesh-sample-iterations <n>` | Batch remesh sample iterations (default: 5)  |
| `--camera-position <x,y,z>`                      | Initial camera position                      |
| `--camera-pitch <deg>`                           | Initial camera pitch                         |
| `--camera-yaw <deg>`                             | Initial camera yaw                           |
