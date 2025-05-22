    # Stage 1: Build the application
    # Use the official .NET SDK image as a parent image. Choose the version matching your project.
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
    WORKDIR /app

    # Copy csproj files and restore as distinct layers to leverage Docker cache
    # Copy the .sln file and all .csproj files first
    COPY *.sln .
    COPY StockTrader.Domain/*.csproj ./StockTrader.Domain/
    COPY StockTrader.Application/*.csproj ./StockTrader.Application/
    COPY StockTrader.Infrastructure/*.csproj ./StockTrader.Infrastructure/
    COPY StockTrader.API/*.csproj ./StockTrader.API/

    # Restore dependencies for all projects
    RUN dotnet restore "StockTrader.sln"

    # Copy the rest of the application code
    COPY . .

    # Publish the API project
    # Ensure the output path is simple for the next stage
    RUN dotnet publish "StockTrader.API/StockTrader.API.csproj" -c Release -o /app/publish --no-restore

    # Stage 2: Build the runtime image
    # Use the official ASP.NET Core runtime image as a parent image
    FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime-env
    WORKDIR /app

    # Copy the published output from the build stage
    COPY --from=build-env /app/publish .

    # Expose the port the app runs on (check your API's launchSettings.json for HTTP/HTTPS ports)
    # Docker containers typically listen on HTTP internally, HTTPS is often handled by a reverse proxy upstream.
    # Let's assume your app listens on port 8080 for HTTP inside the container.
    # You can configure this in your API's Program.cs or launchSettings.json (for Kestrel).
    EXPOSE 8080
    # If you also want to expose an HTTPS port from the container (requires certs in container):
    # EXPOSE 8081

    # Define environment variables (can be overridden at runtime)
    # ASPNETCORE_URLS tells Kestrel which URLs to listen on inside the container.
    ENV ASPNETCORE_URLS="http://+:8080;https://+:8081"
    # Set the environment to Production by default for containers
    ENV ASPNETCORE_ENVIRONMENT="Production"

    # Entry point for the container
    # This command runs when the container starts
    ENTRYPOINT ["dotnet", "StockTrader.API.dll"]
    