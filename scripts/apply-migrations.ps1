param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$AppServiceName,

    [Parameter(Mandatory=$true)]
    [string]$SlotName,

    [Parameter(Mandatory=$false)]
    [string]$ConnectionStringName = "NotificationDb",

    [Parameter(Mandatory=$false)]
    [int]$TimeoutMinutes = 10
)

# Script to apply EF Core migrations in Azure App Service deployment slots
# This enables zero-downtime deployments by ensuring database schema is updated
# before the new application version is swapped into production

Write-Host "Starting database migration application..." -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Gray
Write-Host "App Service: $AppServiceName" -ForegroundColor Gray
Write-Host "Slot: $SlotName" -ForegroundColor Gray

try {
    # Check if slot exists
    $slotInfo = az webapp deployment slot list --name $AppServiceName --resource-group $ResourceGroupName --query "[?name=='$SlotName']" | ConvertFrom-Json
    if ($null -eq $slotInfo -or $slotInfo.Count -eq 0) {
        throw "Deployment slot '$SlotName' does not exist in App Service '$AppServiceName'"
    }

    Write-Host "Found deployment slot '$SlotName'" -ForegroundColor Green

    # Get the slot's URL
    $slotUrl = az webapp deployment slot show --name $AppServiceName --resource-group $ResourceGroupName --slot $SlotName --query "defaultHostName" -o tsv
    if ([string]::IsNullOrEmpty($slotUrl)) {
        throw "Could not retrieve slot URL for '$SlotName'"
    }

    $slotUrl = "https://$slotUrl"
    Write-Host "Slot URL: $slotUrl" -ForegroundColor Gray

    # Construct the migration endpoint URL
    $migrationUrl = "$slotUrl/api/migrations/apply"

    Write-Host "Applying database migrations..." -ForegroundColor Yellow

    # Call the migration endpoint
    $startTime = Get-Date
    $response = Invoke-WebRequest -Uri $migrationUrl -Method POST -TimeoutSec ($TimeoutMinutes * 60) -UseBasicParsing

    if ($response.StatusCode -eq 200) {
        $endTime = Get-Date
        $duration = $endTime - $startTime

        Write-Host "✅ Database migrations applied successfully!" -ForegroundColor Green
        Write-Host "Duration: $($duration.TotalSeconds) seconds" -ForegroundColor Gray

        # Log the response
        $responseContent = $response.Content
        Write-Host "Migration response: $responseContent" -ForegroundColor Gray

        # Validate the migration was successful
        $validationUrl = "$slotUrl/api/migrations/validate"
        Write-Host "Validating migration..." -ForegroundColor Yellow

        $validationResponse = Invoke-WebRequest -Uri $validationUrl -Method GET -UseBasicParsing

        if ($validationResponse.StatusCode -eq 200) {
            Write-Host "✅ Migration validation successful!" -ForegroundColor Green
        } else {
            Write-Warning "Migration validation returned status code: $($validationResponse.StatusCode)"
        }

    } else {
        throw "Migration endpoint returned status code: $($response.StatusCode)"
    }

} catch {
    Write-Error "Failed to apply database migrations: $($_.Exception.Message)"

    # Log additional error details
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $errorContent = $reader.ReadToEnd()
        Write-Error "Error response: $errorContent"
    }

    # Don't exit with error code in Azure DevOps - let the pipeline handle it
    throw
}

Write-Host "Database migration process completed." -ForegroundColor Green
