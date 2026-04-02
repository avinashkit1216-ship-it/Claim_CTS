# PHASE 3: CONTAINERIZATION & CI/CD PIPELINES

**Status:** Production Deployment Strategy  
**Date:** April 1, 2026  
**Goal:** Enterprise-grade containerization, orchestration, and automated deployment

---

## Part 1: Docker Configuration

### 1.1 Dockerfile for API

**File:** `ClaimSubmission.API/Dockerfile`

```dockerfile
# Multi-stage build for production optimization

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project files
COPY ["ClaimSubmission.API/ClaimSubmission.API.csproj", "ClaimSubmission.API/"]
RUN dotnet restore "ClaimSubmission.API/ClaimSubmission.API.csproj"

# Copy all source
COPY . .
WORKDIR "/src/ClaimSubmission.API"

# Build application
RUN dotnet build "ClaimSubmission.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM builder AS publish
RUN dotnet publish "ClaimSubmission.API.csproj" -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 5285

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5285/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "ClaimSubmission.API.dll"]
```

### 1.2 Dockerfile for Web

**File:** `ClaimSubmission.Web/Dockerfile`

```dockerfile
# Multi-stage build

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

COPY ["ClaimSubmission.Web/ClaimSubmission.Web.csproj", "ClaimSubmission.Web/"]
RUN dotnet restore "ClaimSubmission.Web/ClaimSubmission.Web.csproj"

COPY . .
WORKDIR "/src/ClaimSubmission.Web"

RUN dotnet build "ClaimSubmission.Web.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM builder AS publish
RUN dotnet publish "ClaimSubmission.Web.csproj" -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 5277

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5277/ || exit 1

ENTRYPOINT ["dotnet", "ClaimSubmission.Web.dll"]
```

### 1.3 Docker Compose for Local Development

**File:** `docker-compose.yml`

```yaml
version: '3.8'

services:
  # API Service
  api:
    build:
      context: .
      dockerfile: ClaimSubmission.API/Dockerfile
    container_name: claimsubmission-api
    ports:
      - "5285:5285"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5285
      - Jwt__Key=your-super-secret-key-min-32-characters-long-for-hs256
      - Jwt__Issuer=ClaimSubmissionAPI
      - Jwt__Audience=ClaimSubmissionClients
      - Jwt__ExpirationMinutes=60
    depends_on:
      - db
    networks:
      - claim-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5285/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # Web Service
  web:
    build:
      context: .
      dockerfile: ClaimSubmission.Web/Dockerfile
    container_name: claimsubmission-web
    ports:
      - "5277:5277"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5277
      - ApiBaseUrl=http://api:5285
    depends_on:
      api:
        condition: service_healthy
    networks:
      - claim-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5277/"]
      interval: 30s
      timeout: 10s
      retries: 3

  # SQL Server Database (optional, for production)
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: claimsubmission-db
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
      - ./ClaimDb/CreateDataBase.sql:/scripts/CreateDataBase.sql
    networks:
      - claim-network
    healthcheck:
      test: ["/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "YourPassword123!", "-Q", "select 1"]
      interval: 15s
      timeout: 3s
      retries: 5

volumes:
  sqlserver_data:

networks:
  claim-network:
    driver: bridge
```

### 1.4 Docker Build Commands

```bash
# Build images
docker build -f ClaimSubmission.API/Dockerfile -t claimsubmission-api:latest .
docker build -f ClaimSubmission.Web/Dockerfile -t claimsubmission-web:latest .

# Run with docker-compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

> .dockerignore

```
bin/
obj/
.git
.gitignore
.vs
.vscode
*.md
node_modules
coverage
dist
build
```

---

## Part 2: Kubernetes Deployment

### 2.1 Kubernetes Manifests

**File:** `k8s/api-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: claimsubmission-api
  labels:
    app: claimsubmission-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: claimsubmission-api
  template:
    metadata:
      labels:
        app: claimsubmission-api
    spec:
      containers:
      - name: api
        image: claimsubmission.azurecr.io/api:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 5285
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5285"
        - name: Jwt__Key
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: key
        - name: Jwt__Issuer
          value: "ClaimSubmissionAPI"
        - name: Jwt__Audience
          value: "ClaimSubmissionClients"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5285
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5285
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: claimsubmission-api
spec:
  type: ClusterIP
  ports:
  - port: 5285
    targetPort: 5285
  selector:
    app: claimsubmission-api
```

**File:** `k8s/web-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: claimsubmission-web
spec:
  replicas: 3  # High availability
  selector:
    matchLabels:
      app: claimsubmission-web
  template:
    metadata:
      labels:
        app: claimsubmission-web
    spec:
      containers:
      - name: web
        image: claimsubmission.azurecr.io/web:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 5277
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ApiBaseUrl
          value: "http://claimsubmission-api:5285"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /
            port: 5277
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /
            port: 5277
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: claimsubmission-web
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 5277
  selector:
    app: claimsubmission-web
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: claimsubmission-web-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: claimsubmission-web
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### 2.2 Kubernetes Deployment Commands

```bash
# Apply configuration
kubectl apply -f k8s/api-deployment.yaml
kubectl apply -f k8s/web-deployment.yaml

# Check status
kubectl get pods
kubectl get svc

# View logs
kubectl logs -f deployment/claimsubmission-web

# Scale deployment
kubectl scale deployment claimsubmission-web --replicas=5

# Update image
kubectl set image deployment/claimsubmission-api api=claimsubmission.azurecr.io/api:v1.1
```

---

## Part 3: CI/CD Pipelines

### 3.1 GitHub Actions Workflow

**File:** `.github/workflows/ci-cd.yml`

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  # Build and Test
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    
    - name: Cache NuGet packages
      uses: actions/cache@v3
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore -c Release
    
    - name: Run Unit Tests
      run: dotnet test ClaimSubmission.Web.Tests --no-build -c Release
    
    - name: Run Integration Tests
      run: dotnet test --filter "Integration" --no-build -c Release
    
    - name: Generate Code Coverage
      run: dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
    
    - name: Upload Coverage Reports
      uses: codecov/codecov-action@v3
      with:
        flags: unittests

  # Docker Build and Push
  docker:
    runs-on: ubuntu-latest
    needs: build
    
    permissions:
      contents: read
      packages: write
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2
    
    - name: Log in to Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Build and push API image
      uses: docker/build-push-action@v4
      with:
        context: .
        file: ./ClaimSubmission.API/Dockerfile
        push: true
        tags: |
          ${{ env.REGISTRY }}/
${{ env.IMAGE_NAME }}/api:latest
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:${{ github.sha }}
        cache-from: type=registry,ref=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:buildcache
        cache-to: type=registry,ref=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:buildcache,mode=max
    
    - name: Build and push Web image
      uses: docker/build-push-action@v4
      with:
        context: .
        file: ./ClaimSubmission.Web/Dockerfile
        push: true
        tags: |
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/web:latest
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/web:${{ github.sha }}
        cache-from: type=registry,ref=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/web:buildcache
        cache-to: type=registry,ref=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/web:buildcache,mode=max

  # Security Scanning
  security:
    runs-on: ubuntu-latest
    needs: build
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'
    
    - name: Upload Trivy results to GitHub Security
      uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: 'trivy-results.sarif'

  # Deploy to Staging
  deploy-staging:
    runs-on: ubuntu-latest
    needs: [build, docker]
    if: github.ref == 'refs/heads/develop'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Deploy to Azure App Service
      uses: azure/webapps-deploy@v2
      with:
        app-name: claimsubmission-staging
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        images: |
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/web:${{ github.sha }}

  # Deploy to Production
  deploy-prod:
    runs-on: ubuntu-latest
    needs: [build, docker, security]
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Deploy to Azure App Service
      uses: azure/webapps-deploy@v2
      with:
        app-name: claimsubmission-prod
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE_PROD }}
        images: |
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/web:${{ github.sha }}
    
    - name: Create Release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: v${{ github.run_number }}
        release_name: Release v${{ github.run_number }}
        draft: false
        prerelease: false
```

### 3.2 Azure Pipelines (Alternative)

**File:** `azure-pipelines.yml`

```yaml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '8.0'

stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - task: UseDotNet@2
      inputs:
        version: $(dotnetVersion)
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore NuGet packages'
      inputs:
        command: restore
    
    - task: DotNetCoreCLI@2
      displayName: 'Build'
      inputs:
        command: build
        arguments: '-c $(buildConfiguration)'
    
    - task: DotNetCoreCLI@2
      displayName: 'Run Unit Tests'
      inputs:
        command: test
        arguments: '-c $(buildConfiguration) --no-build'
    
    - task: PublishCodeCoverageResults@1
      inputs:
        codeCoverageTool: Cobertura
        summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'

- stage: Docker
  dependsOn: Build
  condition: succeeded()
  jobs:
  - job: BuildDocker
    steps:
    - task: Docker@2
      displayName: 'Build and push API image'
      inputs:
        command: buildAndPush
        Dockerfile: 'ClaimSubmission.API/Dockerfile'
        repository: '$(ImageRepository)/api'
        tags: |
          latest
          $(Build.BuildNumber)
    
    - task: Docker@2
      displayName: 'Build and push Web image'
      inputs:
        command: buildAndPush
        Dockerfile: 'ClaimSubmission.Web/Dockerfile'
        repository: '$(ImageRepository)/web'
        tags: |
          latest
          $(Build.BuildNumber)

- stage: Deploy_Staging
  dependsOn: Docker
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/develop'))
  jobs:
  - deployment: Deploy_Staging_Job
    environment: staging
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'Azure Connection'
              appType: 'webAppContainer'
              appName: 'claimsubmission-staging'
              containers: '$(ImageRepository)/web:$(Build.BuildNumber)'

- stage: Deploy_Production
  dependsOn: Docker
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: Deploy_Production_Job
    environment: production
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'Azure Connection'
              appType: 'webAppContainer'
              appName: 'claimsubmission-prod'
              containers: '$(ImageRegistry)/web:$(Build.BuildNumber)'
```

---

## Part 4: Azure App Service Deployment

### 4.1 App Service Configuration

**File:** `azure-deploy.json`

```json
{
  "appServicePan": {
    "name": "claim-submission-asp",
    "location": "eastus",
    "sku": {
      "name": "P1V2",
      "tier": "PremiumV2",
      "size": "P1V2",
      "family": "PV2",
      "capacity": 2
    },
    "numberOfWorkers": 1
  },
  "appService": {
    "name": "claimsubmission-web",
    "location": "eastus",
    "appServicePlanId": "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/serverfarms/claim-submission-asp",
    "siteConfig": {
      "numberOfWorkers": 1,
      "defaultDocuments": [],
      "netFrameworkVersion": "v8.0",
      "requestTracingEnabled": true,
      "requestTracingRetentionInDays": 7,
      "remoteDebuggingEnabled": false,
      "httpLoggingEnabled": true,
      "detailedErrorLoggingEnabled": true,
      "publishingUsername": "$claimsubmission-web",
      "connection": [
        {
          "name": "ApiBaseUrl",
          "value": "https://claimsubmission-api.azurewebsites.net"
        }
      ]
    }
  },
  "appInsights": {
    "name": "claimsubmission-insights",
    "location": "eastus",
    "applicationType": "web",
    "kind": "web"
  }
}
```

### 4.2 Deployment Commands

```bash
# Create resource group
az group create \
  --name claimsubmission-rg \
  --location eastus

# Create App Service Plan
az appservice plan create \
  --name claim-asp-plan \
  --resource-group claimsubmission-rg \
  --sku P1V2 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group claimsubmission-rg \
  --plan claim-asp-plan \
  --name claimsubmission-web \
  --runtime "DOTNETCORE:8.0"

# Configure app settings
az webapp config appsettings set \
  --name claimsubmission-web \
  --resource-group claimsubmission-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ApiBaseUrl="https://claimsubmission-api.azurewebsites.net"

# Deploy from Docker
az webapp config container set \
  --name claimsubmission-web \
  --resource-group claimsubmission-rg \
  --docker-custom-image-name ghcr.io/yourorg/web:latest \
  --docker-registry-server-url https://ghcr.io
```

---

## Part 5: Monitoring & Observability

### 5.1 Application Insights Setup

**File:** `Program.cs` (add monitoring)

```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddApplicationInsights();
    logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
        ("", LogLevel.Information);
});
```

### 5.2 Health Checks Endpoint

```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("API Connection", () =>
    {
        try
        {
            // Check API connectivity
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    });

// Map endpoint
app.MapHealthChecks("/health");
```

---

## Deployment Checklist

- [ ] Docker images build successfully
- [ ] Docker Compose runs locally
- [ ] Kubernetes YAML files validated
- [ ] GitHub Actions workflow passes
- [ ] Security scanning completes
- [ ] Staging deployment succeeds
- [ ] Production deployment prepared
- [ ] Monitoring configured
- [ ] Health checks operational
- [ ] Load testing completed

---

**Next: Phase 4 - Production Monitoring & Optimization**
