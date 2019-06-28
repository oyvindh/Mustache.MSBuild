FROM mcr.microsoft.com/dotnet/core/sdk:2.1.700

# Install the NuGet credential provider
RUN curl -sSL https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | /bin/bash

ENV SHELL /bin/bash