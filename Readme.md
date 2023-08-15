# Azure DDNS Updater #
.NET Core console application to push updates on an interval to Azure DNS for a DDNS service.

[![Build Status](https://ericingland.visualstudio.com/Azure-DDNS-Updater/_apis/build/status/eingland.Azure-DDNS-Updater?branchName=master)](https://ericingland.visualstudio.com/Azure-DDNS-Updater/_build/latest?definitionId=9&branchName=master)

## Requirements ##
Windows, Linux, macOS.
Any operating system supported by .NET Core 3.0.

## Installation ##
The application can be extracted and run from anywhere.

## Configuration ##

First create a service principal in Azure and give it a role assignment of DNS Contributor to the Azure DNS resource that will contain the record to update and then fill out the appsettings.config.

### Creating Service Principal in Azure ###

```powershell
$sp = New-AzADServicePrincipal -DisplayName <NameforServicePrincipal>
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sp.Secret)
$UnsecureSecret = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
$UnsecureSecret
```
Make a note of the value in $UnsecureSecret for the secret setting in appsettings.config.


### Filling out appsettings.config ###

Application Insights key is optional.

```json
{
  "ApplicationInsights": 
  {
    "InstrumentationKey": "xxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  },
  "Logging":
  {
      "LogLevel":
      {
          "Default": "Warning"
      }
  },
  "tenantId": "xxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientId": "xxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "secret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "resourceGroupName": "example.com",
  "zoneName": "example.com",
  "recordSetName": "ddns",
  "refreshInterval": 5,
}
```
## Run as a Service ##

### Windows ###

Please see Microsoft Documentation on how to host application in a service.
https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-3.0&tabs=visual-studio

### Linux ###

Creating a systemd service configuration

Azure-DDNS-Updater.service

```
[Unit]
Description=Azure DDNS Updater Service

[Service]
Type=simple
ExecStart=/home/pi/dotnet/dotnet /home/pi/Azure-DDNS-Updater/Azure-DDNS-Updater.dll

[Install]
WantedBy=multi-user.target
```
```bash
sudo cp Azure-DDNS-Updater.service /etc/systemd/system/Azure-DDNS-Updater.service
sudo chmod 644 /etc/systemd/system/Azure-DDNS-Updater.service
sudo systemctl start Azure-DDNS-Updater
sudo systemctl status Azure-DDNS-Updater
```

## Resources ##
Uses https://www.ipify.org to get external IP address.
