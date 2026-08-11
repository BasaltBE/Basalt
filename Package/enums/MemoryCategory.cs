using System;

namespace BedrockProtocol.Enums;

public enum MemoryCategory {
    Unknown = 0,
    Invalid_SizeUnknown = 1,
    Actor = 2,
    ActorAnimation = 3,
    ActorRendering = 4,
    BlockTickingQueues = 5,
    Biome_Storage = 6,
    Blobs = 7,
    Cereal = 8,
    CircuitSystem = 9,
    Client = 10,
    Commands = 11,
    DBStorage = 12,
    Debug = 13,
    Documentation = 14,
    ECSSystems = 15,
    FMOD = 16,
    Fonts = 17,
    ImGui = 18,
    Input = 19,
    JsonUI = 20,
    JsonUI_ControlFactory_Json = 21,
    JsonUI_ControlTree = 22,
    JsonUI_ControlTree_ControlElement = 23,
    JsonUI_ControlTree_PopulateDataBinding = 24,
    JsonUI_ControlTree_PopulateFocus = 25,
    JsonUI_ControlTree_PopulateLayout = 26,
    JsonUI_ControlTree_PopulateOther = 27,
    JsonUI_ControlTree_PopulateSprite = 28,
    JsonUI_ControlTree_PopulateText = 29,
    JsonUI_ControlTree_PopulateTTS = 30,
    JsonUI_ControlTree_Visibility = 31,
    JsonUI_CreateUI = 32,
    JsonUI_Defs = 33,
    JsonUI_LayoutManager = 34,
    JsonUI_LayoutManager_RemoveDependencies = 35,
    JsonUI_LayoutManager_InitVariable = 36,
    Languages = 37,
    Level = 38,
    LevelStructures = 39,
    LevelChunk = 40,
    LevelChunkGen = 41,
    LevelChunkGenThreadLocal = 42,
    LightVolumeManager = 43,
    Network = 44,
    Marketplace = 45,
    Material_DragonCompiledDefinition = 46,
    Material_DragonMaterial = 47,
    Material_DragonResource = 48,
    Material_DragonUniformMap = 49,
    Material_RenderMaterial = 50,
    Material_RenderMaterialGroup = 51,
    Material_VariationManager = 52,
    Molang = 53,
    OreUI = 54,
    OreUI_Client = 55,
    Persona_Pieces = 56,
    Persona_Animations = 57,
    Persona_Textures = 58,
    Persona_Characters = 59,
    Persona_SkinPacks = 60,
    Persona_Repo = 61,
    Player = 62,
    RenderChunk = 63,
    RenderChunk_IndexBuffer = 64,
    RenderChunk_VertexBuffer = 65,
    Rendering = 66,
    Rendering_BgfxInit = 67,
    Rendering_BgfxStartFrame = 68,
    Rendering_BlockTessellator = 69,
    Rendering_EndFrame = 70,
    Rendering_GraphicsTasksInit = 71,
    Rendering_Library = 72,
    Rendering_PolygonOperatorPool = 73,
    Rendering_PBRTextureData = 74,
    Rendering_RenderRegistry = 75,
    Rendering_Setup = 76,
    Rendering_Vertices = 77,
    RequestLog = 78,
    ResourcePacks = 79,
    Sound = 80,
    SubChunk_BiomeData = 81,
    SubChunk_BlockData = 82,
    SubChunk_LightData = 83,
    Textures = 84,
    WeatherRenderer = 85,
    World_Generator = 86,
    Tasks = 87,
    Test = 88,
    Test_LoadTestTags = 89,
    Scripting = 90,
    Scripting_Runtime = 91,
    Scripting_Context = 92,
    Scripting_Context_Bindings_MC = 93,
    Scripting_Context_Bindings_GT = 94,
    Scripting_Context_Run = 95,
    DataDrivenUI = 96,
    DataDrivenUI_Defs = 97,
    Gameface = 98,
    Gameface_System = 99,
    Gameface_DOM = 100,
    Gameface_CSS = 101,
    Gameface_Display = 102,
    Gameface_TempAllocator = 103,
    Gameface_PoolAllocator = 104,
    Gameface_Dump = 105,
    Gameface_Media = 106,
    Gameface_JSON = 107,
    Gameface_ScriptEngine = 108,
    Gameface_Script = 109,
    Gameface_Layout = 110,
}

public static class MemoryCategoryExtensions {
    public static string ToProtoString(this MemoryCategory value) => value.ToProtocolString();

    public static string ToProtocolString(this MemoryCategory value) {
        return value switch {
            MemoryCategory.Unknown => "Unknown",
            MemoryCategory.Invalid_SizeUnknown => "Invalid_SizeUnknown",
            MemoryCategory.Actor => "Actor",
            MemoryCategory.ActorAnimation => "ActorAnimation",
            MemoryCategory.ActorRendering => "ActorRendering",
            MemoryCategory.BlockTickingQueues => "BlockTickingQueues",
            MemoryCategory.Biome_Storage => "Biome_Storage",
            MemoryCategory.Blobs => "Blobs",
            MemoryCategory.Cereal => "Cereal",
            MemoryCategory.CircuitSystem => "CircuitSystem",
            MemoryCategory.Client => "Client",
            MemoryCategory.Commands => "Commands",
            MemoryCategory.DBStorage => "DBStorage",
            MemoryCategory.Debug => "Debug",
            MemoryCategory.Documentation => "Documentation",
            MemoryCategory.ECSSystems => "ECSSystems",
            MemoryCategory.FMOD => "FMOD",
            MemoryCategory.Fonts => "Fonts",
            MemoryCategory.ImGui => "ImGui",
            MemoryCategory.Input => "Input",
            MemoryCategory.JsonUI => "JsonUI",
            MemoryCategory.JsonUI_ControlFactory_Json => "JsonUI_ControlFactory_Json",
            MemoryCategory.JsonUI_ControlTree => "JsonUI_ControlTree",
            MemoryCategory.JsonUI_ControlTree_ControlElement => "JsonUI_ControlTree_ControlElement",
            MemoryCategory.JsonUI_ControlTree_PopulateDataBinding => "JsonUI_ControlTree_PopulateDataBinding",
            MemoryCategory.JsonUI_ControlTree_PopulateFocus => "JsonUI_ControlTree_PopulateFocus",
            MemoryCategory.JsonUI_ControlTree_PopulateLayout => "JsonUI_ControlTree_PopulateLayout",
            MemoryCategory.JsonUI_ControlTree_PopulateOther => "JsonUI_ControlTree_PopulateOther",
            MemoryCategory.JsonUI_ControlTree_PopulateSprite => "JsonUI_ControlTree_PopulateSprite",
            MemoryCategory.JsonUI_ControlTree_PopulateText => "JsonUI_ControlTree_PopulateText",
            MemoryCategory.JsonUI_ControlTree_PopulateTTS => "JsonUI_ControlTree_PopulateTTS",
            MemoryCategory.JsonUI_ControlTree_Visibility => "JsonUI_ControlTree_Visibility",
            MemoryCategory.JsonUI_CreateUI => "JsonUI_CreateUI",
            MemoryCategory.JsonUI_Defs => "JsonUI_Defs",
            MemoryCategory.JsonUI_LayoutManager => "JsonUI_LayoutManager",
            MemoryCategory.JsonUI_LayoutManager_RemoveDependencies => "JsonUI_LayoutManager_RemoveDependencies",
            MemoryCategory.JsonUI_LayoutManager_InitVariable => "JsonUI_LayoutManager_InitVariable",
            MemoryCategory.Languages => "Languages",
            MemoryCategory.Level => "Level",
            MemoryCategory.LevelStructures => "LevelStructures",
            MemoryCategory.LevelChunk => "LevelChunk",
            MemoryCategory.LevelChunkGen => "LevelChunkGen",
            MemoryCategory.LevelChunkGenThreadLocal => "LevelChunkGenThreadLocal",
            MemoryCategory.LightVolumeManager => "LightVolumeManager",
            MemoryCategory.Network => "Network",
            MemoryCategory.Marketplace => "Marketplace",
            MemoryCategory.Material_DragonCompiledDefinition => "Material_DragonCompiledDefinition",
            MemoryCategory.Material_DragonMaterial => "Material_DragonMaterial",
            MemoryCategory.Material_DragonResource => "Material_DragonResource",
            MemoryCategory.Material_DragonUniformMap => "Material_DragonUniformMap",
            MemoryCategory.Material_RenderMaterial => "Material_RenderMaterial",
            MemoryCategory.Material_RenderMaterialGroup => "Material_RenderMaterialGroup",
            MemoryCategory.Material_VariationManager => "Material_VariationManager",
            MemoryCategory.Molang => "Molang",
            MemoryCategory.OreUI => "OreUI",
            MemoryCategory.OreUI_Client => "OreUI_Client",
            MemoryCategory.Persona_Pieces => "Persona_Pieces",
            MemoryCategory.Persona_Animations => "Persona_Animations",
            MemoryCategory.Persona_Textures => "Persona_Textures",
            MemoryCategory.Persona_Characters => "Persona_Characters",
            MemoryCategory.Persona_SkinPacks => "Persona_SkinPacks",
            MemoryCategory.Persona_Repo => "Persona_Repo",
            MemoryCategory.Player => "Player",
            MemoryCategory.RenderChunk => "RenderChunk",
            MemoryCategory.RenderChunk_IndexBuffer => "RenderChunk_IndexBuffer",
            MemoryCategory.RenderChunk_VertexBuffer => "RenderChunk_VertexBuffer",
            MemoryCategory.Rendering => "Rendering",
            MemoryCategory.Rendering_BgfxInit => "Rendering_BgfxInit",
            MemoryCategory.Rendering_BgfxStartFrame => "Rendering_BgfxStartFrame",
            MemoryCategory.Rendering_BlockTessellator => "Rendering_BlockTessellator",
            MemoryCategory.Rendering_EndFrame => "Rendering_EndFrame",
            MemoryCategory.Rendering_GraphicsTasksInit => "Rendering_GraphicsTasksInit",
            MemoryCategory.Rendering_Library => "Rendering_Library",
            MemoryCategory.Rendering_PolygonOperatorPool => "Rendering_PolygonOperatorPool",
            MemoryCategory.Rendering_PBRTextureData => "Rendering_PBRTextureData",
            MemoryCategory.Rendering_RenderRegistry => "Rendering_RenderRegistry",
            MemoryCategory.Rendering_Setup => "Rendering_Setup",
            MemoryCategory.Rendering_Vertices => "Rendering_Vertices",
            MemoryCategory.RequestLog => "RequestLog",
            MemoryCategory.ResourcePacks => "ResourcePacks",
            MemoryCategory.Sound => "Sound",
            MemoryCategory.SubChunk_BiomeData => "SubChunk_BiomeData",
            MemoryCategory.SubChunk_BlockData => "SubChunk_BlockData",
            MemoryCategory.SubChunk_LightData => "SubChunk_LightData",
            MemoryCategory.Textures => "Textures",
            MemoryCategory.WeatherRenderer => "WeatherRenderer",
            MemoryCategory.World_Generator => "World_Generator",
            MemoryCategory.Tasks => "Tasks",
            MemoryCategory.Test => "Test",
            MemoryCategory.Test_LoadTestTags => "Test_LoadTestTags",
            MemoryCategory.Scripting => "Scripting",
            MemoryCategory.Scripting_Runtime => "Scripting_Runtime",
            MemoryCategory.Scripting_Context => "Scripting_Context",
            MemoryCategory.Scripting_Context_Bindings_MC => "Scripting_Context_Bindings_MC",
            MemoryCategory.Scripting_Context_Bindings_GT => "Scripting_Context_Bindings_GT",
            MemoryCategory.Scripting_Context_Run => "Scripting_Context_Run",
            MemoryCategory.DataDrivenUI => "DataDrivenUI",
            MemoryCategory.DataDrivenUI_Defs => "DataDrivenUI_Defs",
            MemoryCategory.Gameface => "Gameface",
            MemoryCategory.Gameface_System => "Gameface_System",
            MemoryCategory.Gameface_DOM => "Gameface_DOM",
            MemoryCategory.Gameface_CSS => "Gameface_CSS",
            MemoryCategory.Gameface_Display => "Gameface_Display",
            MemoryCategory.Gameface_TempAllocator => "Gameface_TempAllocator",
            MemoryCategory.Gameface_PoolAllocator => "Gameface_PoolAllocator",
            MemoryCategory.Gameface_Dump => "Gameface_Dump",
            MemoryCategory.Gameface_Media => "Gameface_Media",
            MemoryCategory.Gameface_JSON => "Gameface_JSON",
            MemoryCategory.Gameface_ScriptEngine => "Gameface_ScriptEngine",
            MemoryCategory.Gameface_Script => "Gameface_Script",
            MemoryCategory.Gameface_Layout => "Gameface_Layout",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MemoryCategory value.")
        };
    }

    public static MemoryCategory FromProtocolString(string value) {
        return value switch {
            "Unknown" => MemoryCategory.Unknown,
            "Invalid_SizeUnknown" => MemoryCategory.Invalid_SizeUnknown,
            "Actor" => MemoryCategory.Actor,
            "ActorAnimation" => MemoryCategory.ActorAnimation,
            "ActorRendering" => MemoryCategory.ActorRendering,
            "BlockTickingQueues" => MemoryCategory.BlockTickingQueues,
            "Biome_Storage" => MemoryCategory.Biome_Storage,
            "Blobs" => MemoryCategory.Blobs,
            "Cereal" => MemoryCategory.Cereal,
            "CircuitSystem" => MemoryCategory.CircuitSystem,
            "Client" => MemoryCategory.Client,
            "Commands" => MemoryCategory.Commands,
            "DBStorage" => MemoryCategory.DBStorage,
            "Debug" => MemoryCategory.Debug,
            "Documentation" => MemoryCategory.Documentation,
            "ECSSystems" => MemoryCategory.ECSSystems,
            "FMOD" => MemoryCategory.FMOD,
            "Fonts" => MemoryCategory.Fonts,
            "ImGui" => MemoryCategory.ImGui,
            "Input" => MemoryCategory.Input,
            "JsonUI" => MemoryCategory.JsonUI,
            "JsonUI_ControlFactory_Json" => MemoryCategory.JsonUI_ControlFactory_Json,
            "JsonUI_ControlTree" => MemoryCategory.JsonUI_ControlTree,
            "JsonUI_ControlTree_ControlElement" => MemoryCategory.JsonUI_ControlTree_ControlElement,
            "JsonUI_ControlTree_PopulateDataBinding" => MemoryCategory.JsonUI_ControlTree_PopulateDataBinding,
            "JsonUI_ControlTree_PopulateFocus" => MemoryCategory.JsonUI_ControlTree_PopulateFocus,
            "JsonUI_ControlTree_PopulateLayout" => MemoryCategory.JsonUI_ControlTree_PopulateLayout,
            "JsonUI_ControlTree_PopulateOther" => MemoryCategory.JsonUI_ControlTree_PopulateOther,
            "JsonUI_ControlTree_PopulateSprite" => MemoryCategory.JsonUI_ControlTree_PopulateSprite,
            "JsonUI_ControlTree_PopulateText" => MemoryCategory.JsonUI_ControlTree_PopulateText,
            "JsonUI_ControlTree_PopulateTTS" => MemoryCategory.JsonUI_ControlTree_PopulateTTS,
            "JsonUI_ControlTree_Visibility" => MemoryCategory.JsonUI_ControlTree_Visibility,
            "JsonUI_CreateUI" => MemoryCategory.JsonUI_CreateUI,
            "JsonUI_Defs" => MemoryCategory.JsonUI_Defs,
            "JsonUI_LayoutManager" => MemoryCategory.JsonUI_LayoutManager,
            "JsonUI_LayoutManager_RemoveDependencies" => MemoryCategory.JsonUI_LayoutManager_RemoveDependencies,
            "JsonUI_LayoutManager_InitVariable" => MemoryCategory.JsonUI_LayoutManager_InitVariable,
            "Languages" => MemoryCategory.Languages,
            "Level" => MemoryCategory.Level,
            "LevelStructures" => MemoryCategory.LevelStructures,
            "LevelChunk" => MemoryCategory.LevelChunk,
            "LevelChunkGen" => MemoryCategory.LevelChunkGen,
            "LevelChunkGenThreadLocal" => MemoryCategory.LevelChunkGenThreadLocal,
            "LightVolumeManager" => MemoryCategory.LightVolumeManager,
            "Network" => MemoryCategory.Network,
            "Marketplace" => MemoryCategory.Marketplace,
            "Material_DragonCompiledDefinition" => MemoryCategory.Material_DragonCompiledDefinition,
            "Material_DragonMaterial" => MemoryCategory.Material_DragonMaterial,
            "Material_DragonResource" => MemoryCategory.Material_DragonResource,
            "Material_DragonUniformMap" => MemoryCategory.Material_DragonUniformMap,
            "Material_RenderMaterial" => MemoryCategory.Material_RenderMaterial,
            "Material_RenderMaterialGroup" => MemoryCategory.Material_RenderMaterialGroup,
            "Material_VariationManager" => MemoryCategory.Material_VariationManager,
            "Molang" => MemoryCategory.Molang,
            "OreUI" => MemoryCategory.OreUI,
            "OreUI_Client" => MemoryCategory.OreUI_Client,
            "Persona_Pieces" => MemoryCategory.Persona_Pieces,
            "Persona_Animations" => MemoryCategory.Persona_Animations,
            "Persona_Textures" => MemoryCategory.Persona_Textures,
            "Persona_Characters" => MemoryCategory.Persona_Characters,
            "Persona_SkinPacks" => MemoryCategory.Persona_SkinPacks,
            "Persona_Repo" => MemoryCategory.Persona_Repo,
            "Player" => MemoryCategory.Player,
            "RenderChunk" => MemoryCategory.RenderChunk,
            "RenderChunk_IndexBuffer" => MemoryCategory.RenderChunk_IndexBuffer,
            "RenderChunk_VertexBuffer" => MemoryCategory.RenderChunk_VertexBuffer,
            "Rendering" => MemoryCategory.Rendering,
            "Rendering_BgfxInit" => MemoryCategory.Rendering_BgfxInit,
            "Rendering_BgfxStartFrame" => MemoryCategory.Rendering_BgfxStartFrame,
            "Rendering_BlockTessellator" => MemoryCategory.Rendering_BlockTessellator,
            "Rendering_EndFrame" => MemoryCategory.Rendering_EndFrame,
            "Rendering_GraphicsTasksInit" => MemoryCategory.Rendering_GraphicsTasksInit,
            "Rendering_Library" => MemoryCategory.Rendering_Library,
            "Rendering_PolygonOperatorPool" => MemoryCategory.Rendering_PolygonOperatorPool,
            "Rendering_PBRTextureData" => MemoryCategory.Rendering_PBRTextureData,
            "Rendering_RenderRegistry" => MemoryCategory.Rendering_RenderRegistry,
            "Rendering_Setup" => MemoryCategory.Rendering_Setup,
            "Rendering_Vertices" => MemoryCategory.Rendering_Vertices,
            "RequestLog" => MemoryCategory.RequestLog,
            "ResourcePacks" => MemoryCategory.ResourcePacks,
            "Sound" => MemoryCategory.Sound,
            "SubChunk_BiomeData" => MemoryCategory.SubChunk_BiomeData,
            "SubChunk_BlockData" => MemoryCategory.SubChunk_BlockData,
            "SubChunk_LightData" => MemoryCategory.SubChunk_LightData,
            "Textures" => MemoryCategory.Textures,
            "WeatherRenderer" => MemoryCategory.WeatherRenderer,
            "World_Generator" => MemoryCategory.World_Generator,
            "Tasks" => MemoryCategory.Tasks,
            "Test" => MemoryCategory.Test,
            "Test_LoadTestTags" => MemoryCategory.Test_LoadTestTags,
            "Scripting" => MemoryCategory.Scripting,
            "Scripting_Runtime" => MemoryCategory.Scripting_Runtime,
            "Scripting_Context" => MemoryCategory.Scripting_Context,
            "Scripting_Context_Bindings_MC" => MemoryCategory.Scripting_Context_Bindings_MC,
            "Scripting_Context_Bindings_GT" => MemoryCategory.Scripting_Context_Bindings_GT,
            "Scripting_Context_Run" => MemoryCategory.Scripting_Context_Run,
            "DataDrivenUI" => MemoryCategory.DataDrivenUI,
            "DataDrivenUI_Defs" => MemoryCategory.DataDrivenUI_Defs,
            "Gameface" => MemoryCategory.Gameface,
            "Gameface_System" => MemoryCategory.Gameface_System,
            "Gameface_DOM" => MemoryCategory.Gameface_DOM,
            "Gameface_CSS" => MemoryCategory.Gameface_CSS,
            "Gameface_Display" => MemoryCategory.Gameface_Display,
            "Gameface_TempAllocator" => MemoryCategory.Gameface_TempAllocator,
            "Gameface_PoolAllocator" => MemoryCategory.Gameface_PoolAllocator,
            "Gameface_Dump" => MemoryCategory.Gameface_Dump,
            "Gameface_Media" => MemoryCategory.Gameface_Media,
            "Gameface_JSON" => MemoryCategory.Gameface_JSON,
            "Gameface_ScriptEngine" => MemoryCategory.Gameface_ScriptEngine,
            "Gameface_Script" => MemoryCategory.Gameface_Script,
            "Gameface_Layout" => MemoryCategory.Gameface_Layout,
            _ => throw new ArgumentException($"Unknown MemoryCategory protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MemoryCategory result) {
        switch (value) {
            case "Unknown":
                result = MemoryCategory.Unknown;
                return true;
            case "Invalid_SizeUnknown":
                result = MemoryCategory.Invalid_SizeUnknown;
                return true;
            case "Actor":
                result = MemoryCategory.Actor;
                return true;
            case "ActorAnimation":
                result = MemoryCategory.ActorAnimation;
                return true;
            case "ActorRendering":
                result = MemoryCategory.ActorRendering;
                return true;
            case "BlockTickingQueues":
                result = MemoryCategory.BlockTickingQueues;
                return true;
            case "Biome_Storage":
                result = MemoryCategory.Biome_Storage;
                return true;
            case "Blobs":
                result = MemoryCategory.Blobs;
                return true;
            case "Cereal":
                result = MemoryCategory.Cereal;
                return true;
            case "CircuitSystem":
                result = MemoryCategory.CircuitSystem;
                return true;
            case "Client":
                result = MemoryCategory.Client;
                return true;
            case "Commands":
                result = MemoryCategory.Commands;
                return true;
            case "DBStorage":
                result = MemoryCategory.DBStorage;
                return true;
            case "Debug":
                result = MemoryCategory.Debug;
                return true;
            case "Documentation":
                result = MemoryCategory.Documentation;
                return true;
            case "ECSSystems":
                result = MemoryCategory.ECSSystems;
                return true;
            case "FMOD":
                result = MemoryCategory.FMOD;
                return true;
            case "Fonts":
                result = MemoryCategory.Fonts;
                return true;
            case "ImGui":
                result = MemoryCategory.ImGui;
                return true;
            case "Input":
                result = MemoryCategory.Input;
                return true;
            case "JsonUI":
                result = MemoryCategory.JsonUI;
                return true;
            case "JsonUI_ControlFactory_Json":
                result = MemoryCategory.JsonUI_ControlFactory_Json;
                return true;
            case "JsonUI_ControlTree":
                result = MemoryCategory.JsonUI_ControlTree;
                return true;
            case "JsonUI_ControlTree_ControlElement":
                result = MemoryCategory.JsonUI_ControlTree_ControlElement;
                return true;
            case "JsonUI_ControlTree_PopulateDataBinding":
                result = MemoryCategory.JsonUI_ControlTree_PopulateDataBinding;
                return true;
            case "JsonUI_ControlTree_PopulateFocus":
                result = MemoryCategory.JsonUI_ControlTree_PopulateFocus;
                return true;
            case "JsonUI_ControlTree_PopulateLayout":
                result = MemoryCategory.JsonUI_ControlTree_PopulateLayout;
                return true;
            case "JsonUI_ControlTree_PopulateOther":
                result = MemoryCategory.JsonUI_ControlTree_PopulateOther;
                return true;
            case "JsonUI_ControlTree_PopulateSprite":
                result = MemoryCategory.JsonUI_ControlTree_PopulateSprite;
                return true;
            case "JsonUI_ControlTree_PopulateText":
                result = MemoryCategory.JsonUI_ControlTree_PopulateText;
                return true;
            case "JsonUI_ControlTree_PopulateTTS":
                result = MemoryCategory.JsonUI_ControlTree_PopulateTTS;
                return true;
            case "JsonUI_ControlTree_Visibility":
                result = MemoryCategory.JsonUI_ControlTree_Visibility;
                return true;
            case "JsonUI_CreateUI":
                result = MemoryCategory.JsonUI_CreateUI;
                return true;
            case "JsonUI_Defs":
                result = MemoryCategory.JsonUI_Defs;
                return true;
            case "JsonUI_LayoutManager":
                result = MemoryCategory.JsonUI_LayoutManager;
                return true;
            case "JsonUI_LayoutManager_RemoveDependencies":
                result = MemoryCategory.JsonUI_LayoutManager_RemoveDependencies;
                return true;
            case "JsonUI_LayoutManager_InitVariable":
                result = MemoryCategory.JsonUI_LayoutManager_InitVariable;
                return true;
            case "Languages":
                result = MemoryCategory.Languages;
                return true;
            case "Level":
                result = MemoryCategory.Level;
                return true;
            case "LevelStructures":
                result = MemoryCategory.LevelStructures;
                return true;
            case "LevelChunk":
                result = MemoryCategory.LevelChunk;
                return true;
            case "LevelChunkGen":
                result = MemoryCategory.LevelChunkGen;
                return true;
            case "LevelChunkGenThreadLocal":
                result = MemoryCategory.LevelChunkGenThreadLocal;
                return true;
            case "LightVolumeManager":
                result = MemoryCategory.LightVolumeManager;
                return true;
            case "Network":
                result = MemoryCategory.Network;
                return true;
            case "Marketplace":
                result = MemoryCategory.Marketplace;
                return true;
            case "Material_DragonCompiledDefinition":
                result = MemoryCategory.Material_DragonCompiledDefinition;
                return true;
            case "Material_DragonMaterial":
                result = MemoryCategory.Material_DragonMaterial;
                return true;
            case "Material_DragonResource":
                result = MemoryCategory.Material_DragonResource;
                return true;
            case "Material_DragonUniformMap":
                result = MemoryCategory.Material_DragonUniformMap;
                return true;
            case "Material_RenderMaterial":
                result = MemoryCategory.Material_RenderMaterial;
                return true;
            case "Material_RenderMaterialGroup":
                result = MemoryCategory.Material_RenderMaterialGroup;
                return true;
            case "Material_VariationManager":
                result = MemoryCategory.Material_VariationManager;
                return true;
            case "Molang":
                result = MemoryCategory.Molang;
                return true;
            case "OreUI":
                result = MemoryCategory.OreUI;
                return true;
            case "OreUI_Client":
                result = MemoryCategory.OreUI_Client;
                return true;
            case "Persona_Pieces":
                result = MemoryCategory.Persona_Pieces;
                return true;
            case "Persona_Animations":
                result = MemoryCategory.Persona_Animations;
                return true;
            case "Persona_Textures":
                result = MemoryCategory.Persona_Textures;
                return true;
            case "Persona_Characters":
                result = MemoryCategory.Persona_Characters;
                return true;
            case "Persona_SkinPacks":
                result = MemoryCategory.Persona_SkinPacks;
                return true;
            case "Persona_Repo":
                result = MemoryCategory.Persona_Repo;
                return true;
            case "Player":
                result = MemoryCategory.Player;
                return true;
            case "RenderChunk":
                result = MemoryCategory.RenderChunk;
                return true;
            case "RenderChunk_IndexBuffer":
                result = MemoryCategory.RenderChunk_IndexBuffer;
                return true;
            case "RenderChunk_VertexBuffer":
                result = MemoryCategory.RenderChunk_VertexBuffer;
                return true;
            case "Rendering":
                result = MemoryCategory.Rendering;
                return true;
            case "Rendering_BgfxInit":
                result = MemoryCategory.Rendering_BgfxInit;
                return true;
            case "Rendering_BgfxStartFrame":
                result = MemoryCategory.Rendering_BgfxStartFrame;
                return true;
            case "Rendering_BlockTessellator":
                result = MemoryCategory.Rendering_BlockTessellator;
                return true;
            case "Rendering_EndFrame":
                result = MemoryCategory.Rendering_EndFrame;
                return true;
            case "Rendering_GraphicsTasksInit":
                result = MemoryCategory.Rendering_GraphicsTasksInit;
                return true;
            case "Rendering_Library":
                result = MemoryCategory.Rendering_Library;
                return true;
            case "Rendering_PolygonOperatorPool":
                result = MemoryCategory.Rendering_PolygonOperatorPool;
                return true;
            case "Rendering_PBRTextureData":
                result = MemoryCategory.Rendering_PBRTextureData;
                return true;
            case "Rendering_RenderRegistry":
                result = MemoryCategory.Rendering_RenderRegistry;
                return true;
            case "Rendering_Setup":
                result = MemoryCategory.Rendering_Setup;
                return true;
            case "Rendering_Vertices":
                result = MemoryCategory.Rendering_Vertices;
                return true;
            case "RequestLog":
                result = MemoryCategory.RequestLog;
                return true;
            case "ResourcePacks":
                result = MemoryCategory.ResourcePacks;
                return true;
            case "Sound":
                result = MemoryCategory.Sound;
                return true;
            case "SubChunk_BiomeData":
                result = MemoryCategory.SubChunk_BiomeData;
                return true;
            case "SubChunk_BlockData":
                result = MemoryCategory.SubChunk_BlockData;
                return true;
            case "SubChunk_LightData":
                result = MemoryCategory.SubChunk_LightData;
                return true;
            case "Textures":
                result = MemoryCategory.Textures;
                return true;
            case "WeatherRenderer":
                result = MemoryCategory.WeatherRenderer;
                return true;
            case "World_Generator":
                result = MemoryCategory.World_Generator;
                return true;
            case "Tasks":
                result = MemoryCategory.Tasks;
                return true;
            case "Test":
                result = MemoryCategory.Test;
                return true;
            case "Test_LoadTestTags":
                result = MemoryCategory.Test_LoadTestTags;
                return true;
            case "Scripting":
                result = MemoryCategory.Scripting;
                return true;
            case "Scripting_Runtime":
                result = MemoryCategory.Scripting_Runtime;
                return true;
            case "Scripting_Context":
                result = MemoryCategory.Scripting_Context;
                return true;
            case "Scripting_Context_Bindings_MC":
                result = MemoryCategory.Scripting_Context_Bindings_MC;
                return true;
            case "Scripting_Context_Bindings_GT":
                result = MemoryCategory.Scripting_Context_Bindings_GT;
                return true;
            case "Scripting_Context_Run":
                result = MemoryCategory.Scripting_Context_Run;
                return true;
            case "DataDrivenUI":
                result = MemoryCategory.DataDrivenUI;
                return true;
            case "DataDrivenUI_Defs":
                result = MemoryCategory.DataDrivenUI_Defs;
                return true;
            case "Gameface":
                result = MemoryCategory.Gameface;
                return true;
            case "Gameface_System":
                result = MemoryCategory.Gameface_System;
                return true;
            case "Gameface_DOM":
                result = MemoryCategory.Gameface_DOM;
                return true;
            case "Gameface_CSS":
                result = MemoryCategory.Gameface_CSS;
                return true;
            case "Gameface_Display":
                result = MemoryCategory.Gameface_Display;
                return true;
            case "Gameface_TempAllocator":
                result = MemoryCategory.Gameface_TempAllocator;
                return true;
            case "Gameface_PoolAllocator":
                result = MemoryCategory.Gameface_PoolAllocator;
                return true;
            case "Gameface_Dump":
                result = MemoryCategory.Gameface_Dump;
                return true;
            case "Gameface_Media":
                result = MemoryCategory.Gameface_Media;
                return true;
            case "Gameface_JSON":
                result = MemoryCategory.Gameface_JSON;
                return true;
            case "Gameface_ScriptEngine":
                result = MemoryCategory.Gameface_ScriptEngine;
                return true;
            case "Gameface_Script":
                result = MemoryCategory.Gameface_Script;
                return true;
            case "Gameface_Layout":
                result = MemoryCategory.Gameface_Layout;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
