param(
    [int]$Count = 50
)

$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:8081" }
$Output = Join-Path $PSScriptRoot "..\k6\tokens.json"

$tokens = @()

for ($i = 1; $i -le $Count; $i++) {
    $email = "k6-test-user-$i@example.com"
    $password = "Test@12345"
    $body = @{ email = $email; password = $password } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method Post -ContentType "application/json" -Body $body | Out-Null
    } catch {
        # 帳號可能已經註冊過（腳本重跑），忽略註冊失敗即可
    }

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $body
    $tokens += $loginResponse.token
}

$json = $tokens | ConvertTo-Json
[System.IO.File]::WriteAllText($Output, $json, (New-Object System.Text.UTF8Encoding $false))

Write-Host "已產生 $Count 個測試帳號的 token，存在 $Output"
