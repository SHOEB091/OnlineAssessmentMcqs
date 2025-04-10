# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["OnlineAssessment.Web/OnlineAssessment.Web.csproj", "OnlineAssessment.Web/"]
RUN dotnet restore "OnlineAssessment.Web/OnlineAssessment.Web.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/OnlineAssessment.Web"

# Build and publish
RUN dotnet build "OnlineAssessment.Web.csproj" -c Release -o /app/build
RUN dotnet publish "OnlineAssessment.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published files from build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OnlineAssessment.Web.dll"] 