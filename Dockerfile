# Збірка (використовуємо SDK, оскільки тут потрібні інструменти для компіляції)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Копіюємо проект
COPY ["Rtm.csproj", "./"]
RUN dotnet restore "Rtm.csproj"

# Копіюємо все інше (Migrations, Controllers тощо)
COPY . .
RUN dotnet publish "Rtm.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime (тут достатньо лише runtime-образу aspnet для запуску)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# Налаштування порту
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rtm.dll"]