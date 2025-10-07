param(
    [string]$PropsPath = "Directory.Build.props"
)

if (-not (Test-Path $PropsPath)) {
    Write-Host "The file $PropsPath was not found!"
    exit 1
}

$content = Get-Content $PropsPath

# Find nuværende ReleaseChannelNumber
$releaseLine = $content | Where-Object { $_ -match '<ReleaseChannelNumber>(\d+)</ReleaseChannelNumber>' }
if ($releaseLine -match '<ReleaseChannelNumber>(\d+)</ReleaseChannelNumber>') {
    $current = [int]$matches[1]
    $next = $current + 1
    $newContent = $content -replace "<ReleaseChannelNumber>$current</ReleaseChannelNumber>", "<ReleaseChannelNumber>$next</ReleaseChannelNumber>"
    Set-Content $PropsPath $newContent -Encoding UTF8
    Write-Host "ReleaseChannelNumber bumped: $current -> $next"
} else {
    Write-Host "ReleaseChannelNumber not found!"
    exit 2
}