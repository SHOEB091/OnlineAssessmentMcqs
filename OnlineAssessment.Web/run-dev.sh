#!/bin/bash

# Clean and build the project
dotnet clean
dotnet build

# Set environment variables
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5000

# Run the application using dotnet
dotnet run --no-build 