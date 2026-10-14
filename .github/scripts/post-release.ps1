# Posts a "New Release" link for the given version to r/DeadditApp.
# Credentials come from the REDDIT_CLIENT_ID / REDDIT_CLIENT_SECRET / REDDIT_USERNAME / REDDIT_PASSWORD env vars.
param(
    [Parameter(Mandatory)][string]$Version
)

$ErrorActionPreference = 'Stop'

foreach ($name in 'REDDIT_CLIENT_ID', 'REDDIT_CLIENT_SECRET', 'REDDIT_USERNAME', 'REDDIT_PASSWORD') {
    if (-not [Environment]::GetEnvironmentVariable($name)) {
        throw "Missing required environment variable $name"
    }
}

$username = $env:REDDIT_USERNAME
$user_agent = "DeadditReleaseScript/1.0 by $username"
$subreddit = "DeadditApp"
$desired_flair_text = "New Release"

# Authenticate and get access token
$encodedAuth = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$($env:REDDIT_CLIENT_ID):$($env:REDDIT_CLIENT_SECRET)"))
$auth_response = Invoke-RestMethod -Method Post -Uri "https://www.reddit.com/api/v1/access_token" -Headers @{
    "User-Agent"    = $user_agent
    "Authorization" = "Basic $encodedAuth"
} -Body @{
    grant_type = "password"
    username   = $username
    password   = $env:REDDIT_PASSWORD
}

$access_token = $auth_response.access_token
if (-not $access_token) {
    throw "Failed to obtain access token: $($auth_response | ConvertTo-Json -Depth 10)"
}

$headers = @{
    "User-Agent"    = $user_agent
    "Authorization" = "bearer $access_token"
}

# Find the flair template ID
$flair_response = Invoke-RestMethod -Method Get -Uri "https://oauth.reddit.com/r/$subreddit/api/link_flair_v2" -Headers $headers
$flair_template = $flair_response | Where-Object { $_.text -eq $desired_flair_text }
if (-not $flair_template) {
    throw "Flair '$desired_flair_text' not found. Available: $(($flair_response | ForEach-Object { $_.text }) -join ', ')"
}

# Create the post
$post_response = Invoke-RestMethod -Method Post -Uri "https://oauth.reddit.com/api/submit" -Headers $headers -Body @{
    sr       = $subreddit
    title    = "New Release $Version"
    url      = "https://github.com/MrJackSpade/Deaddit/releases/tag/$Version"
    kind     = "link"
    flair_id = $flair_template.id
    api_type = "json"
}

if ($post_response.json.errors.Count -ne 0) {
    throw "Failed to create the post: $($post_response | ConvertTo-Json -Depth 10)"
}

Write-Host "Post created successfully with flair!"
