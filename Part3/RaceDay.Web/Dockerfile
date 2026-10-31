# ---------- Build stage: restore, then publish the MVC app ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the restore step is cached between builds
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# Now copy the rest of the source and publish a Release build
COPY . .
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime stage: small ASP.NET image with just the published app ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
