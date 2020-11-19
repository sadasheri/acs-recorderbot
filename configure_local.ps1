<#

.SYNOPSIS
Configure the build files in preperation for local deployment.

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

.PARAMETER NgrokTcpUrl
Enter your Ngrok TCP Forwarding Url for Media (ex: tcp://2.tcp.ngrok.io:11029).

.PARAMETER NgrokWebFqdn
Enter your Ngrok Http/Https Forwarding Fqdn (ex: f5f66cdaf677.ngrok.io).

.PARAMETER NgrokTcpCertFqdn
Enter your Ngrok TCP Certificate Fqdn for e.g. '2.bot.contoso.com' which is mapped to 2.tcp.ngrok.io using CNAME in DNS.

.PARAMETER CertThumbprint
Provide your certificate thumbprint.

.PARAMETER Reset
If set to true, restores the configurations files with the backups.  If no backups exist, nothing will be done.

.EXAMPLE
Set the parameters:
.\configure_local.ps1 -p .\RecorderBot\
.\configure_local.ps1 -p .\RecorderBot\ -appname RecorderBot -appidentifier 00000000-0000-0000-0000-000000000001 -acsid 28:acs:10abcdef-b11a-4706-ba7d-0d71f938e112_00000005-576c-6dc4-6a0b-343a0d0002e0 -connstr endpoint=https://bot.communication.azure.com/;accesskey=abc -tcpurl tcp://2.tcp.ngrok.io:12345 -webfqdn f5f66cdaf677.ngrok.io -certfqdn  2.bot.contoso.com -thumb ABC0000000000000000000000000000000000CBA 

Restore the parameters
.\configure_local.ps1 -p .\ -reset true

#>

param(
    [parameter(Mandatory=$true,HelpMessage="The root path to the project you wish to configure.")][alias("p")] $Path,
	[parameter(Mandatory=$false,HelpMessage="Enter your Application Name (ex: RecorderBot).")][alias("appname")] $ApplicationName,
	[parameter(Mandatory=$false,HelpMessage="Enter your Application Identifier (ex: any guid).")][alias("appidentifier")] $ApplicationIdentifier,
	[parameter(Mandatory=$false,HelpMessage="Enter your Bot's ACS Application Instance Id.")][alias("acsid")] $ACSAppInstanceId,
    [parameter(Mandatory=$false,HelpMessage="Enter your Connection String from Communication Service resource on Azure Portal.")][alias("connstr")] $ACSConnectionString,
    [parameter(Mandatory=$false,HelpMessage="Enter your Ngrok TCP Forwarding Url for Media (ex: tcp://2.tcp.ngrok.io:11029).")][alias("tcpurl")] $NgrokTcpUrl,
    [parameter(Mandatory=$false,HelpMessage="Enter your Ngrok Http/Https Forwarding Fqdn (ex: f5f66cdaf677.ngrok.io).")][alias("webfqdn")] $NgrokWebFqdn,
	[parameter(Mandatory=$false,HelpMessage="Enter your Ngrok TCP Certificate Fqdn for e.g. '2.bot.contoso.com' which is mapped to 2.tcp.ngrok.io using CName in DNS.")][alias("certfqdn")] $NgrokTcpCertFqdn ,
    [parameter(Mandatory=$false,HelpMessage="Provide your certificate thumbprint.")][alias("thumb")] $CertThumbprint,
    [switch] $Reset
)

Write-Output 'Azure Communication Service SDK - Local Configurator'

$Files = "ServiceConfiguration.local.cscfg", "app.config", "appsettings.json", "cloud.xml", "ServiceManifest.xml", "ApplicationManifest.xml", "AzureDeploy.Parameters.json"
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

if (-not $NgrokTcpUrl) {
    $NgrokTcpUrl = (Read-Host 'Enter your Ngrok TCP Forwarding Url for Media (ex: tcp://2.tcp.ngrok.io:11029).').Trim()
}

if (-not $NgrokWebFqdn) {
    $NgrokWebFqdn = (Read-Host 'Enter your Ngrok Http/Https Forwarding Fqdn (ex: f5f66cdaf677.ngrok.io).').Trim()
}

if (-not $NgrokTcpCertFqdn) {
    $NgrokTcpCertFqdn = (Read-Host 'Enter your Ngrok TCP Certificate Fqdn for e.g. '2.bot.contoso.com' which is mapped to 2.tcp.ngrok.io using CName in DNS.').Trim()
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
    ReplaceInFile $file "%NgrokTcpUrl%" $NgrokTcpUrl
    ReplaceInFile $file "%NgrokWebFqdn%" $NgrokWebFqdn
	ReplaceInFile $file "%NgrokTcpCertFqdn%" $NgrokTcpCertFqdn
    ReplaceInFile $file "ABC0000000000000000000000000000000000CBA" $CertThumbprint
    ReplaceInFile $file "%ACSApplicationInstanceId%" $ACSAppInstanceId
    ReplaceInFile $file "%ACSConnectionString%" $ACSConnectionString
}

Write-Output "Update Complete."
