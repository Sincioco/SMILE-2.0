# Model3DAsset cooking example

`Model3DAssetCooking.smileproj` demonstrates the build-time-only `Model3DAsset` project item. The compiler converts the repository-owned glTF fixture to `Assets/Models/M0Triangle.sm3d`, publishes only that runtime asset, and reuses the same content-addressed cache for native and Web builds.

The runtime never parses or receives the source glTF file.
