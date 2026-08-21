FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Seckill.slnx .
COPY src/Seckill.Worker/Seckill.Worker.csproj src/Seckill.Worker/
COPY src/Seckill.Application/Seckill.Application.csproj src/Seckill.Application/
COPY src/Seckill.Domain/Seckill.Domain.csproj src/Seckill.Domain/
COPY src/Seckill.Infrastructure/Seckill.Infrastructure.csproj src/Seckill.Infrastructure/
RUN dotnet restore src/Seckill.Worker/Seckill.Worker.csproj

COPY src/ src/
RUN dotnet publish src/Seckill.Worker/Seckill.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Seckill.Worker.dll"]