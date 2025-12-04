using KeSpider.API;
using KeSpider.OutlinkHandlers;
using System.Text.Json.Serialization;

namespace KeSpider;


[JsonSerializable(typeof(Archive))]
[JsonSerializable(typeof(PostRoot))]
[JsonSerializable(typeof(PostsLegacy))]
[JsonSerializable(typeof(OneDriveOutlinkHandler.DriveItem))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;