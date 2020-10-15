# Introduction

This baseline tries to be as generic for .NET Core builds as possible and thus also platform indepenent, so it can run on any agent with small changes. It is tailored to run in a containerized Linux build as that offers the most flexibility, but sometimes there is a need to change it slightly so that legacy modules can be built on a windows machine.

The `Build.yml` file describes the build definition.
The `Vso.Adapter.targets` file is the integration layer for MSBuild when running in the Azure DevOps build environment.

## How to debug the Azure DevOps integration layer

We have tried to keep the local workflow as similar to the one happening in Azure DevOps as possible, but certain things, like signing only happens in the mainline build and might need local debugging.

To trigger the integration layer to be loaded use the `PublicRelease` property when invoking `dotnet` commands, e.g., `dotnet build -p:PublicRelease=true`.

To trigger the signing logic the `IsSigned` property must be passed in addition. Not only that, but the strong name keypair also needs to be supplied. Like so: `dotnet build -p:PublicRelease=true -p:IsSigned=true -p:AssemblyOriginatorKeyFile=foo.snk`. Now, to get the `foo.snk` file you can run `sn -k foo.snk`



### InternalsVisibleTo considerations

First, try not to use this, as it normally creates bad modularization of the libraries, but if there is no way around it then, when testing signing, you need to make all internal visible to directives use the public key of your locally generated `.snk`