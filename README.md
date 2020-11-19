# Introduction

## About

The RecorderBot sample guides you through building, deploying and testing an Azure Communication Services based bot. This sample demonstrates how a bot can make a call, add new participant, record audio stream of participants and end the call.

## Getting Started

This section walks you through the process of deploying and testing the sample bot.

### Prerequisites

* Install the prerequisites:
    * [Visual Studio 2017+](https://visualstudio.microsoft.com/downloads/)
    * [PostMan](https://chrome.google.com/webstore/detail/postman/fhbjgbiflinjbdggehcddcbncdddomop)

* [Download](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads) the Microsoft Visual C++ Redistributable (VC_redist.x64.exe) for Visual Studio 2015, 2017 and 2019. Copy the downloaded file to \RecorderBot\RecorderBotWorkerRole folder.

* Register application with Microsoft
    1. Create a new Communication Services resource from Azure portal. Copy the connection string from `Tools > Keys`.
    2. Create new ACS Application Instance id by executing following executable

        ```text
            CreateApplicationInstance.exe -c <connection-string>
        ```

    3. Provide the generated ACS Application Instance Id to Microsoft for provisioning.

* Setting up client application
    Client application can be setup by going to the following [Document](https://github.com/Azure-Samples/communication-services-web-calling-tutorial/).

### Deploy

#### Local deployment

1. The testing setup requires ngrok to create tunnels to localhost. Go to [ngrok](https://ngrok.com) and sign up for a free account. Once you signed up, go to the dashboard and get your authtoken.

2. Create an ngrok configuration file ngrok.yml with the following data

```text
authtoken: <Your-AuthToken>
tunnels:
  signaling:
    addr: 9442
    proto: http
    host-header: localhost
  media: 
    addr: 8445
    proto: tcp
```

3.  Application Hosted Media uses certificates and TCP tunnels to properly work. The following steps are required in order for proper media establishment.
    1. Ngrok's public TCP endpoints have fixed urls. They are 0.tcp.ngrok.io, 1.tcp.ngrok.io, etc. You should have a dns CNAME entry for your service that points to these urls. In this example, let's say 0.bot.contoso.com is pointing to 0.tcp.ngrok.io, 1.bot.contoso.com is pointing to 1.tcp.ngrok.io and similarly for other urls.
    2. Now you require an SSL certificate for the url you own. To make it easy, use an SSL certificate issued to a wild card domain. In this case, it would be *.bot.contoso.com. This ssl certificate is validated by Media flow so should match your media flow's public url. Note down the thumbprint and install the certificate in your local machine certificates.

4. Now that ngrok configuration is ready, start it up. Download the ngrok executable and run the following command

```text
ngrok.exe start -all -config <path to ngrok.yml>
```

5. Once the ngrok is running, the output will look like this

```http
....
Forwarding  tcp://2.tcp.ngrok.io:11652 -> localhost:8445
Forwarding  http://abcdefg123.ngrok.io -> localhost:9442
Forwarding  https://abcdefg123.ngrok.io -> localhost:9442
```

6. Set up local service configuration
    1. Open powershell and go to the folder that contains `configure_local.ps1` file.
    2. Run the powershell script with parameters

        `.\configure_local.ps1 -p {path to project} -appname {application name} -appidentifier {any guid for app}  -acsid {application instance id} -connstr {resource connection string} -tcpurl {Ngrok TCP Forwarding url} -webfqdn {Ngrok web forwarding fqdn} -certfqdn {Ngrok TCP certificate fqdn} -thumb {your certificate thumbprint}`

        For example:

        `.\configure_local.ps1 -p .\RecorderBot\`

        or

        `.\configure_local.ps1 -p .\RecorderBot\ -appname RecorderBot -appidentifier 00000000-0000-0000-0000-000000000001 -acsid 28:acs:10abcdef-b11a-4706-ba7d-0d71f938e112_00000005-576c-6dc4-6a0b-343a0d0002e0 -connstr endpoint=https://bot.communication.azure.com/;accesskey=abcd -tcpurl tcp://2.tcp.ngrok.io:12345 -webfqdn f5f66cdaf677.ngrok.io -certfqdn 2.bot.contoso.com -thumb ABC0000000000000000000000000000000000CBA`

7. Open solution in Visual Studio (Admin mode). Build and run with x64 configuration.

#### Azure deployment

1. Create a cloud service (classic) in Azure. Get your "Site URL" from Azure portal, this will be your DNS name for later configuration, for example: `bot.cloudapp.net`.

2. Set up SSL certificate and upload to the cloud service
    1. Create a wildcard certificate for your service. This certificate should not be a self-signed certificate. For instance, if your bot is hosted at `bot.contoso.com`, create the certificate for `*.contoso.com`.
    2. Upload the certificate to the cloud service.
    3. Copy the thumbprint for later.

3. Set up cloud service configuration
    1. Open powershell and go to the folder that contains `configure_cloud.ps1` file.
    2. Run the powershell script with parameters

        `.\configure_cloud.ps1 -p {path to project} -appname {application name} -appidentifier {any guid for app} -acsid {application instance id} -connstr {resource connection string} -dns {your DNS name} -fqdn {your Azure certificate Fqdn} -thumb {your certificate thumbprint}`

        For example:

        `.\configure_cloud.ps1 -p .\RecorderBot\`

        or

        `.\configure_cloud.ps1 -p .\RecorderBot\ -appname RecorderBot -appidentifier 00000000-0000-0000-0000-000000000001 -acsid 28:acs:10abcdef-b11a-4706-ba7d-0d71f938e112_00000005-576c-6dc4-6a0b-343a0d0002e0 -connstr endpoint=https://bot.communication.azure.com/;accesskey=abc -dns contoso.cloudapp.net -fqdn bot.contoso.com -thumb ABC0000000000000000000000000000000000CBA`

4. Publish RecorderBot from Visual Studio:
    1. Right click RecorderBot, then click Publish.... Publish it to the cloud service you created earlier.

### Test

* Use the provided [Postman Collection](postman-collection.json) to run different test. Please provide values for the variables accordingly.

1. Make a call from postman by using following request

    #### Request
    ```http
    Post Https://bot.contoso.com/api/MakeCall
    Content-Type: application/json
    {
        "ACSId": "<ACS User Id here>"
    }
    ```

    #### Response
    The guid "491f0500-401f-4f11-8af4-2eff4c0a0643" in the response will be your call id. Use your call id for the next request.

2. After a call get connected, add a new participant by using following request. Here Call id is the response of the MakeCall request.

    #### Request
    ```http
    Post Https://bot.contoso.com/api/Calls/{Call_Id}/addParticipant
    Content-Type: application/json
    {
        "ACSId": "<ACS User Id here>"
    }
    ```

    #### Response
    200 Ok.

3. Call can be hung up by using the following request.

    #### Request
    ```http
    Delete Https://bot.contoso.com/api/Calls/{Call_Id}
    ```

    #### Response
    200 Ok.