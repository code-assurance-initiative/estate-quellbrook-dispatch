# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/Quellbrook.Dispatch.Api/Dockerfile -t ghcr.io/code-assurance-initiative/quellbrook-dispatch .
#
# Base images are pinned by digest (the 10.0 tag at the last bump); Dependabot proposes the next digest.
# No HEALTHCHECK: the image runs only on Kubernetes, which ignores it; probes live in deploy/k8s/deployment.yaml.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/.editorconfig src/
COPY src/Quellbrook.Dispatch.Domain/Quellbrook.Dispatch.Domain.csproj src/Quellbrook.Dispatch.Domain/packages.lock.json src/Quellbrook.Dispatch.Domain/
COPY src/Quellbrook.Dispatch.Contracts/Quellbrook.Dispatch.Contracts.csproj src/Quellbrook.Dispatch.Contracts/packages.lock.json src/Quellbrook.Dispatch.Contracts/
COPY src/Quellbrook.Dispatch.Application/Quellbrook.Dispatch.Application.csproj src/Quellbrook.Dispatch.Application/packages.lock.json src/Quellbrook.Dispatch.Application/
COPY src/Quellbrook.Dispatch.Infrastructure/Quellbrook.Dispatch.Infrastructure.csproj src/Quellbrook.Dispatch.Infrastructure/packages.lock.json src/Quellbrook.Dispatch.Infrastructure/
COPY src/Quellbrook.Dispatch.Api/Quellbrook.Dispatch.Api.csproj src/Quellbrook.Dispatch.Api/packages.lock.json src/Quellbrook.Dispatch.Api/
RUN dotnet restore src/Quellbrook.Dispatch.Api/Quellbrook.Dispatch.Api.csproj --locked-mode
COPY src/ src/
RUN dotnet publish src/Quellbrook.Dispatch.Api/Quellbrook.Dispatch.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# An unprivileged UID above any host account range, by number so the kubelet can verify runAsNonRoot.
USER 10001:10001
ENTRYPOINT ["dotnet", "Quellbrook.Dispatch.Api.dll"]
