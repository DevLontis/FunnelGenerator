# FunnelGenerator
A Unity procedural generation tool that generates a funnel shape mesh in real time. The funnel can be adjusted via UI controls and supports saving/loading.

## Controls
All parameters are controlled via UI

## Notes
- The mesh is generated procedurally using vertex and triangle buffers
- The funnel consists of outer and inner surfaces to support thickness
- Seam vertices are duplicated to avoid texture distortion
- Parameters are serialised to JSON rather than saved as a mesh
- Mesh data is regenerated on parameter changes
- Material is reused to avoid unnecessary allocations
- invalid inputs do not cause errors
