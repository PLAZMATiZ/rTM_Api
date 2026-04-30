# Збірка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копіюємо проект
COPY ["Rtm.csproj", "./"]
RUN dotnet restore "Rtm.csproj"

# Копіюємо все інше (Migrations, Controllers тощо)
COPY . .
RUN dotnet publish "Rtm.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Налаштування порту
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rtm.dll"]