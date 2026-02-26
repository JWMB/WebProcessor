# Build the runtime image
# podman build -t trainingapi . -f Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Expose the port the app runs on
EXPOSE 80
EXPOSE 443
# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy the project files into the container
# COPY ["./**", "./"]
COPY ["./ProblemSource/TrainingApi/TrainingApi.csproj", "./ProblemSource/TrainingApi/"]
# COPY ["./ProblemSource/TrainingApi/**", "./ProblemSource/TrainingApi/"]
# COPY ["./ProblemSource/ProblemSourceModule/**", "./ProblemSource/ProblemSourceModule/"]
# COPY ["./Common/**", "./Common/"]
# COPY ["./Common.Web/**", "./Common.Web/"]
# COPY ["./MLTools/**", "./MLTools/"]
# COPY ["./PluginModuleBase/**", "./PluginModuleBase/"]
# COPY ["./ML.Helpers/**", "./ML.Helpers/"]
COPY ["./Directory.Packages.props", "./"]
RUN ls
RUN dotnet restore "./ProblemSource/TrainingApi/TrainingApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ProblemSource/TrainingApi/TrainingApi.csproj" -c Release -o /app/build

# Build the application
FROM build AS publish
RUN dotnet publish "./ProblemSource/TrainingApi/TrainingApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORTS=80
# Define the startup command
ENTRYPOINT ["dotnet", "TrainingApi.dll", "--environment=Docker"]

# FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# WORKDIR /ProblemSource/TrainingApi

# # Copy everything
# COPY . ./
# # Restore as distinct layers
# RUN dotnet restore
# # Build and publish a release
# RUN dotnet publish -o out

# # Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:10.0
# WORKDIR /App
# COPY --from=build /ProblemSource/TrainingApi .
# ENTRYPOINT ["dotnet", "TrainingApi.dll"]