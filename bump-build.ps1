param(
    [string]$PropsPath = "Directory.Build.props"
)

if (-not (Test-Path $PropsPath)) {
    Write-Host "The file $PropsPath was not found!"
    exit 1
}

$content = Get-Content $PropsPath

# Find nuværende BuildNumber
$buildLine = $content | Where-Object { $_ -match '<BuildNumber>(\d+)</BuildNumber>' }
if ($buildLine -match '<BuildNumber>(\d+)</BuildNumber>') {
    $current = [int]$matches[1]
    $next = $current + 1
    $newContent = $content -replace "<BuildNumber>$current</BuildNumber>", "<BuildNumber>$next</BuildNumber>"
    Set-Content $PropsPath $newContent -Encoding UTF8
    Write-Host "BuildNumber bumped: $current -> $next"
} else {
    Write-Host "BuildNumber not found!"
    exit 2
}