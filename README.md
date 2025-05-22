![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white) ![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white) ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
# Remix
Little __.NET__ library for manipulate/process __PNG__, __JPEG__ & other images everywhere.
If you like the project, then drop a start to the project. <3

# Required technology
- .NET8 or later

# Currently suported image types:
- PNG

# Installation
Go to the release page & download the __.dll__ file of the library. (NUGET version __not exists__ yet from the library)

# Usage
This sample resize a PNG image & save as an indexed PNG image with 2 colors in the PLTE chunk.

```csharp
using PNG png = await PNG.Load(path: "./your_image.png");
png.Resize(x: 100, y: 100);

await png.Save(builderAction: static (builder) => {
    builder.AddName(name: "resize_indexed")
             .AddOutputDir(dir: "./")
             .AsIndexed(count: 2);
});
```
