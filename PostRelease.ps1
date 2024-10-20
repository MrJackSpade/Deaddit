# Import YAML module
Import-Module powershell-yaml

# Read and trim the version from Version.dat
$version = Get-Content -Path "Version.dat" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" } | Select-Object -First 1

if (-not $version) {
    Write-Host "Version not found in Version.dat."
    exit 1
}

# Read credentials from credentials.yaml
$creds = ConvertFrom-Yaml -Yaml (Get-Content -Path "credentials.yaml" -Raw)

$client_id = $creds.apikey.Trim()
$client_secret = $creds.apisecret.Trim()
$username = $creds.username.Trim()
$password = $creds.password.Trim()

if (-not $client_id -or -not $client_secret -or -not $username -or -not $password) {
    Write-Host "Missing credentials in credentials.yaml."
    exit 1
}

# Set User-Agent (Replace with your app's information)
$user_agent = "DeadditReleaseScript/1.0 by $username"

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
try {
    $auth_response = Invoke-RestMethod -Method Post -Uri "https://www.reddit.com/api/v1/access_token" -Headers $auth_headers -Body $auth_body
} catch {
    Write-Host "Error during authentication:"
    Write-Host $_.Exception.Message
    exit 1
}

$access_token = $auth_response.access_token

if (-not $access_token) {
    Write-Host "Failed to obtain access token."
    Write-Host ($auth_response | ConvertTo-Json -Depth 10)
    exit 1
}

# Prepare the subreddit
$subreddit = "DeadditApp"

# Set Headers for authenticated requests
$headers = @{
    "User-Agent"    = $user_agent
    "Authorization" = "bearer $access_token"
}

# Define the flair text you want to use
$desired_flair_text = "New Release"  # The flair name you want to use

# Retrieve the list of available flairs
try {
    $flair_response = Invoke-RestMethod -Method Get -Uri "https://oauth.reddit.com/r/$subreddit/api/link_flair_v2" -Headers $headers
} catch {
    Write-Host "Error retrieving flairs:"
    Write-Host $_.Exception.Message
    exit 1
}

# Find the flair template ID
$flair_template = $flair_response | Where-Object { $_.text -eq $desired_flair_text }

if (-not $flair_template) {
    Write-Host "Flair with text '$desired_flair_text' not found."
    Write-Host "Available flairs are:"
    $flair_response | ForEach-Object { Write-Host "- $($_.text)" }
    exit 1
}

$flair_template_id = $flair_template.id

# Prepare post details
$title = "New Release $version"
$url = "https://github.com/MrJackSpade/Deaddit/releases/tag/$version"
$kind = "link"

# Include flair_id in the post submission
$post_body = @{
    sr       = $subreddit
    title    = $title
    url      = $url
    kind     = $kind
    flair_id = $flair_template_id
}

# Create the post
try {
    $post_response = Invoke-RestMethod -Method Post -Uri "https://oauth.reddit.com/api/submit" -Headers $headers -Body $post_body
} catch {
    Write-Host "Error creating post:"
    Write-Host $_.Exception.Message
    exit 1
}

# Check for errors
if ($post_response.json.errors.Count -eq 0) {
    Write-Host "Post created successfully with flair!"
} else {
    Write-Host "Failed to create the post."
    Write-Host ($post_response | ConvertTo-Json -Depth 10)
    exit 1
}
