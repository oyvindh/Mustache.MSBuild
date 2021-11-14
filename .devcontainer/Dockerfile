FROM mcr.microsoft.com/dotnet/sdk:5.0

# Install the NuGet credential provider
RUN curl -sSL https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | /bin/bash

ENV SHELL /bin/bash