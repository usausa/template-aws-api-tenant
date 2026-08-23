# Create a test user that belongs to a tenant group.
# Usage: ./scripts/seed-tenant-user.ps1 -Env dev -Tenant alpha -Email user1@example.com
#
# Self sign-up is disabled (tenant membership is a provisioning decision), so issuing real
# users follows the same procedure as this script.

param(
    [Parameter(Mandatory = $true)]
    [string] $Email,

    [Parameter(Mandatory = $true)]
    [string] $Tenant,

    [ValidateSet('dev', 'prod')]
    [string] $Env = 'dev',

    # When omitted, a random password satisfying the pool policy is generated.
    [string] $Password
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

$root = Split-Path -Parent $PSScriptRoot
$outputs = Get-StackOutputs -Root $root -EnvName $Env
$userPoolId = $outputs.UserPoolId
$clientId = $outputs.UserPoolClientId

if (-not $Password) {
    $Password = 'Aa1!' + [guid]::NewGuid().ToString('N').Substring(0, 12)
}

# 1. Create the user unless it already exists (idempotent re-run).
aws cognito-idp admin-get-user --user-pool-id $userPoolId --username $Email 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    aws cognito-idp admin-create-user --user-pool-id $userPoolId --username $Email `
        --user-attributes "Name=email,Value=$Email" "Name=email_verified,Value=true" `
        --message-action SUPPRESS | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'admin-create-user failed.' }
}
else {
    Write-Host "User already exists, reusing it. ($Email)"
}

aws cognito-idp admin-set-user-password --user-pool-id $userPoolId --username $Email --password $Password --permanent
if ($LASTEXITCODE -ne 0) { throw 'Setting the password failed.' }

# 2. Attach the user to its tenant group (created by the stack for sample tenants; create it
#    first with 'aws cognito-idp create-group' when seeding a brand-new tenant).
aws cognito-idp admin-add-user-to-group --user-pool-id $userPoolId --username $Email --group-name "tenant-$Tenant"
if ($LASTEXITCODE -ne 0) { throw "Adding the user to group tenant-$Tenant failed." }

# 3. Show the result and a token acquisition example.
Write-Host ''
Write-Host '===== Tenant user ====='
Write-Host "Email    : $Email"
Write-Host "Password : $Password"
Write-Host "Tenant   : $Tenant"
Write-Host "API      : $($outputs.ApiEndpoint)"
Write-Host ''
Write-Host 'Get an access token with:'
Write-Host "  aws cognito-idp initiate-auth --auth-flow USER_PASSWORD_AUTH --client-id $clientId --auth-parameters USERNAME=$Email,PASSWORD=<password> --query AuthenticationResult.AccessToken --output text"
Write-Host ''
Write-Host 'Then call the API with:'
Write-Host "  curl -H `"Authorization: Bearer <token>`" $($outputs.ApiEndpoint)/tenant"
