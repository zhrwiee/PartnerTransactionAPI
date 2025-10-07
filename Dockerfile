# Use official ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# ✅ Copy the csproj file from the subfolder and restore dependencies
COPY ["PartnerTransactionAPI/PartnerTransactionAPI.csproj", "PartnerTransactionAPI/"]
RUN dotnet restore "PartnerTransactionAPI/PartnerTransactionAPI.csproj"

# ✅ Copy the rest of the source code
COPY . .
WORKDIR "/src/PartnerTransactionAPI"
RUN dotnet publish "PartnerTransactionAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ✅ Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PartnerTransactionAPI.dll"]
