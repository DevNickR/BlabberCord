# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["BlabberCord.csproj", "./"]
RUN dotnet restore "BlabberCord.csproj"
COPY . .
RUN dotnet build "BlabberCord.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "BlabberCord.csproj" -c Release -o /app/publish

# Stage 3: Create the final runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0-bullseye-slim
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlabberCord.dll"]