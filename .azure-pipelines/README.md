# Introduction

This baseline tries to be as generic for .NET Core builds as possible and thus also platform independent, so it can run on any agent with small changes. It is tailored to run in a containerized Linux build as that offers the most flexibility, but sometimes there is a need to change it slightly so that legacy modules can be built on a windows machine.

The `Build.yml` file describes the build definition.
The `Vso.Adapter.targets` file is the integration layer for MSBuild when running in the Azure DevOps build environment.

## Switch to a different pool

To use a different pool, simply add this to the `Build.yml`

```yaml
pool:
  name: <Name of the pool>
  demands:
    - <Any demands you have on the agent>
```

Typically Windows builds would require VisualStudio_IDE_16.0 or similar, to be on the agent.

## How to debug the Azure DevOps integration layer

We have tried to keep the local workflow as similar to the one happening in Azure DevOps as possible, but certain things, like signing only happens in the mainline build and might need local debugging.

To trigger the integration layer to be loaded use the `PublicRelease` property when invoking `dotnet` commands, e.g., `dotnet build -p:PublicRelease=true`.

To trigger the signing logic the `IsSigned` property must be passed in addition. Not only that, but the strong name keypair also needs to be supplied. Like so: `dotnet build -p:PublicRelease=true -p:IsSigned=true -p:AssemblyOriginatorKeyFile=foo.snk`. Now, to get the `foo.snk` file you can run `sn -k foo.snk`

### InternalsVisibleTo considerations

First, try not to use this, as it normally creates bad modularization of the libraries, but if there is no way around it then, when testing signing, you need to make all internal visible to directives use the public key of your locally generated `.snk`. In this template we recommend using

```msbuild
<PackageReference Include="Meziantou.MSBuild.InternalsVisibleTo" Version="1.0.2">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

More information on using this package can be found on the [project's github page](https://github.com/meziantou/Meziantou.MSBuild.InternalsVisibleTo)