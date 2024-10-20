# Import YAML module
Import-Module powershell-yaml

# Read and trim the version from Version.dat
$version = Get-Content -Path "Version.dat" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" } | Select-Object -First 1

# Read credentials from credentials.yaml
$creds = ConvertFrom-Yaml -Yaml (Get-Content -Path "credentials.yaml" -Raw)

$client_id = $creds.apikey.Trim()
$client_secret = $creds.apisecret.Trim()
$username = $creds.username.Trim()
$password = $creds.password.Trim()

# Set User-Agent (Replace with your app's information)
$user_agent = "YourAppName/1.0 by $username"

# Generate Basic Auth header
$authString = "$client_id`:$client_secret"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($authString)
$encodedAuth = [System.Convert]::ToBase64String($bytes)

# Authenticate and get access token
$auth_headers = @{
    "User-Agent"    = $user_agent
    "Authorization" = "Basic $encodedAuth"
}
$auth_body = @{
    grant_type = "password"
    username   = $username
    password   = $password
}
$auth_response = Invoke-RestMethod -Method Post -Uri "https://www.reddit.com/api/v1/access_token" -Headers $auth_headers -Body $auth_body

$access_token = $auth_response.access_token

if (-not $access_token) {
    Write-Host "Failed to obtain access token."
    Write-Host ($auth_response | ConvertTo-Json -Depth 10)
    exit 1
}

# Prepare post details
$subreddit = "DeadditApp"
$title = "New Release $version"
$url = "https://github.com/MrJackSpade/Deaddit/releases/tag/$version"
$kind = "link"

# Create the post
$post_headers = @{
    "User-Agent"    = $user_agent
    "Authorization" = "bearer $access_token"
}
$post_body = @{
    sr    = $subreddit
    title = $title
    url   = $url
    kind  = $kind
}
$post_response = Invoke-RestMethod -Method Post -Uri "https://oauth.reddit.com/api/submit" -Headers $post_headers -Body $post_body

if ($post_response.json.errors.Count -eq 0) {
    Write-Host "Post created successfully!"
} else {
    Write-Host "Failed to create the post."
    Write-Host ($post_response | ConvertTo-Json -Depth 10)
    exit 1
}
