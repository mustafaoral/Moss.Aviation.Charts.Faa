$key = "moss_aviation_charts_faa_app_publish_path"
$publishPath = [System.Environment]::GetEnvironmentVariable($key)

if ($null -eq $publishPath) {
    Write-Host "Environment variable '$key' missing"

    return;
}

if ((Test-Path -Path $publishPath) -eq $false) {
    Write-Host "Path specified by environment variable '$key' doesn't exist: $publishPath"

    return
}

dotnet publish .\Moss.Aviation.Charts.Faa\Moss.Aviation.Charts.Faa.csproj -c Release -o $publishPath
