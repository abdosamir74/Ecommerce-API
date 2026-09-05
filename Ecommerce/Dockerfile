# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# ‰”Œ „·›«  csproj »«·≈”Â«„ «·„»«‘— «·„ÿ«»ﬁ ··ﬁ—’
COPY ["Ecommerce/Ecommerce.csproj", "Ecommerce/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
RUN dotnet restore "Ecommerce/Ecommerce.csproj"
# ‰”Œ «·ﬂÊœ «·ﬂ«„·
COPY Ecommerce/ Ecommerce/
COPY Infrastructure/ Infrastructure/
COPY Application/ Application/
COPY Domain/ Domain/
WORKDIR "/src/Ecommerce"
RUN dotnet build "Ecommerce.csproj" -c Release -o /app/build

# 2. Publish Stage
FROM build AS publish
RUN dotnet publish "Ecommerce.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.dll"]