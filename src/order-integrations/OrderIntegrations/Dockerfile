FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY . ./
RUN dotnet publish . --configuration release --output app

FROM mcr.microsoft.com/azure-functions/dotnet:3.0.14492-appservice as runtime
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \ AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# ENV WEBSITE_HOSTNAME localhost
# ENV WEBSITE_SITE_NAME OrderIntegrations
# ENV AZURE_FUNCTIONS_ENVIRONMENT Development

COPY --from=build /src/app /home/site/wwwroot


# FROM mcr.microsoft.com/azure-functions/dotnet:3.0.13614-appservice

# ENV AZURE_FUNCTIONS_ENVIRONMENT=Development
# ENV AzureWebJobsScriptRoot=/home/site/wwwroot \ AzureFunctionsJobHost__Logging__Console__IsEnabled=true
# ENV WEBSITE_HOSTNAME localhost
# ENV WEBSITE_SITE_NAME OrderIntegrations
# ENV AZURE_FUNCTIONS_ENVIRONMENT Development

# COPY . /home/site/wwwroot