param(
    [string]$InputJson = "$PSScriptRoot\Protocol\Data\block_permutations.json",
    [string]$OutputFile = "$PSScriptRoot\Protocol\Enums\BlockIdentifier.cs"
)

function ConvertTo-PascalCase {
    param([string]$Identifier)
    $name = $Identifier -replace '^minecraft:', ''
    $parts = $name -split '_'
    $pascal = ($parts | ForEach-Object {
        if ($_ -match '^\d+$') {
            $_
        } else {
            $_.Substring(0,1).ToUpper() + $_.Substring(1)
        }
    }) -join ''
    return $pascal
}

$json = Get-Content $InputJson -Raw | ConvertFrom-Json
$identifiers = $json | ForEach-Object { $_.identifier } | Sort-Object -Unique
$entries = @()
$seen = @{}
$sb = [System.Text.StringBuilder]::new()

foreach ($id in $identifiers) {
    $pascal = ConvertTo-PascalCase $id
    if ($seen.ContainsKey($pascal)) {
        continue
    }
    $seen[$pascal] = $id
    $entries += [PSCustomObject]@{
        PascalName = $pascal
        MinecraftId = $id
    }
}

[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("using System.Collections.Generic;")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("public enum BlockIdentifier")
[void]$sb.AppendLine("{")

for ($i = 0; $i -lt $entries.Count; $i++) {
    $comma = if ($i -lt $entries.Count - 1) { "," } else { "" }
    [void]$sb.AppendLine("    $($entries[$i].PascalName)$comma")
}

[void]$sb.AppendLine("}")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("public static class BlockIdentifierExtensions")
[void]$sb.AppendLine("{")

[void]$sb.AppendLine("    private static readonly Dictionary<BlockIdentifier, string> ToIdentifierMap = new()")
[void]$sb.AppendLine("    {")

foreach ($entry in $entries) {
    [void]$sb.AppendLine("        [BlockIdentifier.$($entry.PascalName)] = `"$($entry.MinecraftId)`",")
}

[void]$sb.AppendLine("    };")
[void]$sb.AppendLine("")

[void]$sb.AppendLine("    private static readonly Dictionary<string, BlockIdentifier> FromIdentifierMap = new(StringComparer.Ordinal)")
[void]$sb.AppendLine("    {")

foreach ($entry in $entries) {
    [void]$sb.AppendLine("        [`"$($entry.MinecraftId)`"] = BlockIdentifier.$($entry.PascalName),")
}

[void]$sb.AppendLine("    };")
[void]$sb.AppendLine("")

[void]$sb.AppendLine("    public static string ToIdentifier(this BlockIdentifier self)")
[void]$sb.AppendLine("        => ToIdentifierMap[self];")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    public static BlockIdentifier FromIdentifier(string identifier)")
[void]$sb.AppendLine("        => FromIdentifierMap.TryGetValue(identifier, out var value)")
[void]$sb.AppendLine("            ? value")
[void]$sb.AppendLine("            : throw new ArgumentException(`$`"Unknown block identifier: {identifier}`", nameof(identifier));")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    public static bool TryFromIdentifier(string identifier, out BlockIdentifier result)")
[void]$sb.AppendLine("        => FromIdentifierMap.TryGetValue(identifier, out result);")
[void]$sb.AppendLine("}")
[void]$sb.AppendLine("")

$sb.ToString() | Set-Content $OutputFile -Encoding UTF8 -NoNewline

Write-Host "Generated $($entries.Count) enum entries to $OutputFile"
