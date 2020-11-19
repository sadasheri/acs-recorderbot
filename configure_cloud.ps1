<#

.SYNOPSIS
Configure the build files in preperation for cloud deployment.

.DESCRIPTION
This script performs a couple tasks to configure the builds.
- Backs up all the relevant files.
- Replaces the configurations in all the relevant files.
- Allows developer to restore the original files from the backups.

.PARAMETER Path
The path where the project is located, this is where the recursive search will begin.

.PARAMETER ApplicationName
Enter your Application name (ex: RecorderBot).

.PARAMETER ApplicationIdentifier
Enter your Application Identifier (ex: any guid).

.PARAMETER ACSAppInstanceId
Enter your Bot's ACS Application Instance Id.

.PARAMETER ACSConnectionString
Enter your Connection String as in the Communication Service resource on Azure portal.

.PARAMETER AzureDnsName
Enter your Azure DNS name (ex: contoso.cloudapp.net).

.PARAMETER AzureCertFqdn
Enter your  Azure Certificate Fqdn (ex: bot.contoso.com).

.PARAMETER CertThumbprint
Provide your certificate thumbprint.

.PARAMETER Reset
If set to true, restores the configurations files with the backups.  If no backups exist, nothing will be done.

.EXAMPLE
Set the parameters:
.\configure_cloud.ps1 -p .\RecorderBot\
.\configure_cloud.ps1 -p .\RecorderBot\ -appname RecorderBot -appidentifier 00000000-0000-0000-0000-000000000001 -acsid 28:acs:10abcdef-b11a-4706-ba7d-0d71f938e112_00000005-576c-6dc4-6a0b-343a0d0002e0 -connstr endpoint=https://bot.communication.azure.com/;accesskey=abc -dns contoso.cloudapp.net -fqdn bot.contoso.com -thumb ABC0000000000000000000000000000000000CBA 

Restore the parameters
.\configure_cloud.ps1 -p .\ -reset true

#>

param(
    [parameter(Mandatory=$true,HelpMessage="The root path to the project you wish to configure.")][alias("p")] $Path,
	[parameter(Mandatory=$false,HelpMessage="Enter your Application name (ex: RecorderBot).")][alias("appname")] $ApplicationName,
	[parameter(Mandatory=$false,HelpMessage="Enter your Application Identifier (ex: any guid).")][alias("appidentifier")] $ApplicationIdentifier,
	[parameter(Mandatory=$false,HelpMessage="Enter your Bot's ACS Application Instance Id.")][alias("acsid")] $ACSAppInstanceId,
    [parameter(Mandatory=$false,HelpMessage="Enter your Connection String from Communication Service resource on Azure Portal.")][alias("connstr")] $ACSConnectionString,
    [parameter(Mandatory=$false,HelpMessage="Enter your Azure DNS name (ex: contoso.cloudapp.net).")][alias("dns")] $AzureDnsName,
    [parameter(Mandatory=$false,HelpMessage="Enter your Azure Certificate Fqdn (ex: bot.contoso.com).")][alias("fqdn")] $AzureCertFqdn,
    [parameter(Mandatory=$false,HelpMessage="Provide your certificate thumbprint.")][alias("thumb")] $CertThumbprint,
    [switch] $Reset
)

Write-Output 'Azure Communication Service SDK - Azure Cloud Configurator'

$Files = "ServiceConfiguration.Cloud.cscfg", "app.config", "appsettings.json", "cloud.xml", "ServiceManifest.xml", "ApplicationManifest.xml", "AzureDeploy.Parameters.json"
[System.Collections.ArrayList]$FilesToReplace = @()

foreach($file in $Files)
{
    $foundFiles = Get-ChildItem $Path -Recurse $file
    foreach($foundFile in $foundFiles) {
        $count = $FilesToReplace.Add($foundFile)
    }
}

if ($reset)
{
    Write-Output "Resetting configuration settings..."
    foreach($file in $FilesToReplace)
    {
        $fileName = $file.Name
        $backupName = "$fileName.original"
        $backupFile = Join-Path $file.DirectoryName $backupName
        Write-Output "  Found configuration"
        Write-Output "  $($file.FullName)"
        if (Test-Path $backupFile)
        {
            Write-Output "  Resetting $fileName using $backupName"
            Copy-Item $backupFile -Destination $file.FullName
            Remove-Item $backupFile
        }
        else
        {
            Write-Output "  No backup found for $file"
        }
    }
    Write-Output "Reset Complete."
    exit;
}

if (-not $ApplicationName) {
    $ApplicationName = (Read-Host 'Enter your Application name (ex: RecorderBot).').Trim()
}

if (-not $ApplicationIdentifier) {
    $ApplicationIdentifier = (Read-Host 'Enter your Application Identifier (ex: any guid).').Trim()
}

if (-not $ACSAppInstanceId) {
    $ACSAppInstanceId = (Read-Host "Enter your Bot's ACS Application Instance Id.").Trim()
}

if (-not $ACSConnectionString) {
    $ACSConnectionString = (Read-Host "Enter your Connection String from Communication Service resource on Azure Portal.").Trim()
}

if (-not $AzureDnsName) {
    $AzureDnsName = (Read-Host 'Enter your Azure DNS name (ex: contoso.cloudapp.net).').Trim()
}

if (-not $AzureCertFqdn) {
    $AzureCertFqdn = (Read-Host 'Enter your  Azure Certificate Fqdn (ex: contoso.com).').Trim()
}

if (-not $CertThumbprint) {
    $CertThumbprint = (Read-Host 'Provide your certificate thumbprint.').Trim()
}

function ReplaceInFile ($file, [string]$pattern, [string]$replaceWith) {
    $fileName = $file.Name
    Write-Output "  Replacing $pattern with $replaceWith in $fileName"

    (Get-Content $file.FullName).replace($pattern, $replaceWith) | Set-Content $file.FullName
}

Write-Output ""
Write-Output "Updating configuration files..."

foreach($file in $FilesToReplace)
{
    $fileName = $file.Name
    $backupName = "$fileName.original"
    $backupFile = Join-Path $file.DirectoryName $backupName
    Write-Output "  Found configuration"
    Write-Output "  $($file.FullName)"

    if (-not (Test-Path $backupFile)) {
        Write-Output "  Backing up $fileName with $backupName"
        Copy-Item $file.FullName -Destination $backupFile
    }

    Copy-Item $backupFile -Destination $file.FullName
	ReplaceInFile $file "%ApplicationName%" $ApplicationName
	ReplaceInFile $file "%ApplicationIdentifier%" $ApplicationIdentifier
    ReplaceInFile $file "%AzureDnsName%" $AzureDnsName
    ReplaceInFile $file "%AzureCertFqdn%" $AzureCertFqdn
    ReplaceInFile $file "ABC0000000000000000000000000000000000CBA" $CertThumbprint
    ReplaceInFile $file "%ACSApplicationInstanceId%" $ACSAppInstanceId
    ReplaceInFile $file "%ACSConnectionString%" $ACSConnectionString
}

Write-Output "Update Complete."
