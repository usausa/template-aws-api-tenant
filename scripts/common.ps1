# Shared helpers dot-sourced by deploy-api.ps1 / seed-tenant-user.ps1.

$Script:Region = 'ap-northeast-1'

function Get-StackOutputs {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $EnvName
    )

    $path = Join-Path $Root "cdk-outputs.$EnvName.json"
    if (-not (Test-Path $path)) {
        throw "cdk outputs not found: $path`nRun this in the Template.IaC directory: npx --yes aws-cdk@latest deploy -c env=$EnvName --outputs-file ../cdk-outputs.$EnvName.json"
    }

    $stack = "template-aws-api-tenant-$EnvName"
    $outputs = (Get-Content $path -Raw | ConvertFrom-Json).$stack
    if ($null -eq $outputs) {
        throw "Stack $stack not found in outputs: $path"
    }

    return $outputs
}
