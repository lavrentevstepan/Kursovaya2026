FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/SecureKnowledgeBase.Domain/SecureKnowledgeBase.Domain.csproj src/SecureKnowledgeBase.Domain/
COPY src/SecureKnowledgeBase.Application/SecureKnowledgeBase.Application.csproj src/SecureKnowledgeBase.Application/
COPY src/SecureKnowledgeBase.Infrastructure/SecureKnowledgeBase.Infrastructure.csproj src/SecureKnowledgeBase.Infrastructure/
COPY src/SecureKnowledgeBase.Web/SecureKnowledgeBase.Web.csproj src/SecureKnowledgeBase.Web/

RUN dotnet restore src/SecureKnowledgeBase.Web/SecureKnowledgeBase.Web.csproj

COPY src/ src/

WORKDIR /src/src/SecureKnowledgeBase.Web
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

EXPOSE 5000

USER $APP_UID

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SecureKnowledgeBase.Web.dll"]
