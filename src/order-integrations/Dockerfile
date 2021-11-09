FROM mcr.microsoft.com/azure-functions/dotnet:3.0.13614-appservice

ENV AZURE_FUNCTIONS_ENVIRONMENT=Development
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \ AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV WEBSITE_HOSTNAME localhost
ENV WEBSITE_SITE_NAME OrderIntegrations
ENV AZURE_FUNCTIONS_ENVIRONMENT Development

COPY . /home/site/wwwroot