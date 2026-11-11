<p align="center">
  <img src=".github/logo.png" alt="Basalt Logo" width="120">
</p>

<h1 align="center">Basalt</h1>

<p align="center">
  Customizable Minecraft Bedrock Edition server software written in C# for .NET 10.
</p>

<p align="center">
  <a href="https://github.com/BasaltBE/Basalt/stargazers">
    <img src="https://img.shields.io/github/stars/BasaltBE/Basalt?style=flat&logo=github&label=Stars" alt="GitHub Stars">
  </a>
  <a href="https://discord.gg/q6GtKqwUM4">
    <img src="https://img.shields.io/badge/Discord-Join-5865F2?style=flat&logo=discord&logoColor=white" alt="Join Discord">
  </a>
</p>

<h2 align="center">Features</h2>

<table align="center">
<tr>
<td width="50%" valign="top">

* Custom items, blocks, and entities
* Custom terrain generators and storage providers
* Multiple worlds and dimensions with custom height limits
* LevelDB world storage and entity persistence
* Crafting, loot tables, and enchantments
* Custom commands, selectors, and overloads

</td>
<td width="50%" valign="top">

* Chests, shulker boxes, furnaces, hoppers, and dispensers
* Farming, trees, structures, and world features
* Redstone, pistons, TNT, water, and lava
* Health, hunger, armor, durability, and experience
* Plugins, gameplay events, and plugin CLI
* World worker threads and optional Tracy profiling

</td>
</tr>
</table>

Requires the .NET 10 SDK.

## Build

```sh
dotnet build BasaltBE.sln -c Release
```

Publish a standalone executable for Windows:

```sh
dotnet publish src/BasaltBE.DedicatedServer -c Release -r win-x64 --self-contained true
```

Use `linux-x64` or `osx-x64` for Linux or macOS.

## CLI

Install the CLI:

```sh
dotnet tool install --global BasaltBE.Cli
```

Create and build a plugin locally:

```sh
basalt plugin new Example
basalt plugin build --project Example/Example.csproj
basalt plugin inspect Example/artifacts/Example-1.0.0.plugin
```

Copy the generated `.plugin` file into the server's `plugins` folder, then restart the server.

Or place the plugin source folder in the server's `plugins` folder and start the server with `--roslyn` to build it on startup.

Use `basalt --help` to list options.

<h2 align="center">Contributors</h2>

<p align="center">
  <a href="https://github.com/BasaltBE/Basalt/graphs/contributors">
    <img src="https://contrib.rocks/image?repo=BasaltBE/Basalt" alt="Basalt Contributors">
  </a>
</p>
