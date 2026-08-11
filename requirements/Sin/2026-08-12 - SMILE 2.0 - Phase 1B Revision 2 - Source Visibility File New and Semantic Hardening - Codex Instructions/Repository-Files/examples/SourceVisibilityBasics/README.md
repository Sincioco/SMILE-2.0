# SourceVisibilityBasics

On first open, Solution Explorer must visibly show:

```text
Program.smile (Startup)
Program-NoDemo.smile
Helpers.smile
Assets
```

The test then dynamically adds `DynamicHelper.smile`, verifies immediate visibility, restarts Visual Studio, verifies persistence, removes it from the project without deleting it, and re-adds it.
