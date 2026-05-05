param(
    [string]$ApiBase = "http://localhost:8080",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111",
    [string]$UserId = "22222222-2222-2222-2222-222222222222",
    [string]$Theme = "5 ideias de automacao com IA local"
)

$body = @{
    request = @{
        theme = $Theme
        style = "educativo"
        duration = "curto"
        tone = "viral"
        voice = "pt_BR-cadu-medium"
        sceneCount = 4
        imageType = "cinematic"
        format = "reels_9_16"
    }
} | ConvertTo-Json -Depth 5

$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = $TenantId
    "X-User-Id" = $UserId
}

$job = Invoke-RestMethod -Method Post -Uri "$ApiBase/videos/generate" -Headers $headers -Body $body
Write-Host "Job criado:" $job.id

do {
    Start-Sleep -Seconds 3
    $current = Invoke-RestMethod -Method Get -Uri "$ApiBase/videos/$($job.id)" -Headers $headers
    Write-Host ("Status: {0}" -f $current.status)
} while ($current.status -notin @(6, 7, "Completed", "Failed"))

$current | ConvertTo-Json -Depth 10
