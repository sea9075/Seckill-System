# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先只複製 .csproj，讓 Docker layer cache 在「只改程式碼、沒改套件」時可以重複使用，加速之後的 build
COPY Seckill.slnx .
COPY src/Seckill.Api/Seckill.Api.csproj src/Seckill.Api/
COPY src/Seckill.Application/Seckill.Application.csproj src/Seckill.Application/
COPY src/Seckill.Domain/Seckill.Domain.csproj src/Seckill.Domain/
COPY src/Seckill.Infrastructure/Seckill.Infrastructure.csproj src/Seckill.Infrastructure/
RUN dotnet restore src/Seckill.Api/Seckill.Api.csproj

# 再複製其餘原始碼並發布
COPY src/ src/
RUN dotnet publish src/Seckill.Api/Seckill.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Seckill.Api.dll"]