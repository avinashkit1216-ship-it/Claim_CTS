# Performance Testing Plan - Claim Submission System

## Objective
Validate system performance against target SLAs:
- **Process 10,000 records: <5 seconds goal, <10 seconds acceptable**
- **API Response Time: <200ms (p95)**
- **Memory Usage: <150MB (peak)**
- **CPU Usage: <60% sustained**

---

## Test Environment
- Database: SQL Server 2022 (LocalDB for dev, production-grade for staging)
- Cache: Redis (6379)
- Load Generator: k6 (open-source, lightweight)
- Monitoring: Application Insights / Custom metrics
- Test Data: 15,000 realistic claim records

---

## Test Scenarios

### 1. Baseline Performance Test
**Objective:** Measure current system performance

```javascript
// k6-baseline.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 1,
  iterations: 100,
  duration: '10s',
  thresholds: {
    http_req_duration: ['p(95)<200'],
    http_req_failed: ['rate<0.1'],
  },
};

export default function() {
  // Test paginated claims endpoint
  let res = http.get('http://localhost:5277/api/claims?pageNumber=1&pageSize=100', {
    headers: { 'Authorization': 'Bearer ' + __ENV.TOKEN },
  });
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
  
  sleep(0.5);
}
```

### 2. Load Test (10,000 Records)
**Objective:** Verify 10,000 records processed within 5-10 seconds

```javascript
// k6-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 5 },   // Ramp-up
    { duration: '2m', target: 20 },   // Stay at 20 concurrent users
    { duration: '20s', target: 0 },   // Ramp-down
  ],
  thresholds: {
    'http_req_duration': ['p(95)<300', 'p(99)<500'],
    'http_req_failed': ['rate<0.05'],
  },
  ext: {
    loadimpact: {
      projectID: 3334215,
    },
  },
};

export default function() {
  // Fetch 100 records per request
  let res = http.get('http://localhost:5277/api/claims?pageNumber=1&pageSize=100', {
    headers: { 'Authorization': 'Bearer ' + __ENV.TOKEN },
  });
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 300ms': (r) => r.timings.duration < 300,
  });
  
  sleep(1);
}
```

### 3. Pagination Stress Test
**Objective:** Load 10,000 records across 100 pages

```javascript
// k6-pagination-stress.js
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 50,
  iterations: 100,
  thresholds: {
    'http_req_duration': ['p(95)<200'],
    'http_req_failed': ['rate<0.02'],
  },
};

export default function() {
  const pageNumber = Math.floor(Math.random() * 100) + 1;
  const pageSize = 100;
  
  let res = http.get(
    `http://localhost:5277/api/claims?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    { headers: { 'Authorization': 'Bearer ' + __ENV.TOKEN } }
  );
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
    'response has data': (r) => r.body.includes('claims'),
  });
}
```

### 4. Search Performance Test
**Objective:** Verify search functionality scales with 10,000 records

```javascript
// k6-search-test.js
import http from 'k6/http';
import { check } from 'k6';

const searchTerms = [
  'Smith',
  'CLAIM-001',
  'Approved',
  'Pending',
  'John',
  'Medicare',
];

export let options = {
  vus: 30,
  iterations: 300,
  thresholds: {
    'http_req_duration': ['p(95)<400'],
  },
};

export default function() {
  const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
  const status = ['Pending', 'Approved', 'Rejected'][Math.floor(Math.random() * 3)];
  
  let res = http.get(
    `http://localhost:5277/api/claims?pageNumber=1&pageSize=50&searchTerm=${term}&claimStatus=${status}`,
    { headers: { 'Authorization': 'Bearer ' + __ENV.TOKEN } }
  );
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'search time < 400ms': (r) => r.timings.duration < 400,
  });
}
```

### 5. Concurrent User Simulation
**Objective:** Test system under realistic user load

```javascript
// k6-concurrent-users.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '1m', target: 100 },
    { duration: '5m', target: 100 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    'http_req_duration': ['p(95)<300', 'p(99)<500'],
    'http_req_failed': ['rate<0.05'],
  },
};

export default function() {
  // Simulate user browsing claims
  
  // List claims
  let res1 = http.get('http://localhost:5277/api/claims?pageNumber=1&pageSize=20', {
    headers: { 'Authorization': 'Bearer ' + __ENV.TOKEN },
  });
  check(res1, { 'list is 200': (r) => r.status === 200 });
  sleep(2);
  
  // View claim details
  let res2 = http.get('http://localhost:5277/api/claims/1', {
    headers: { 'Authorization': 'Bearer ' + __ENV.TOKEN },
  });
  check(res2, { 'detail is 200': (r) => r.status === 200 });
  sleep(1);
  
  // Update claim
  let res3 = http.put('http://localhost:5277/api/claims/1', 
    JSON.stringify({
      claimStatus: 'Approved',
      claimAmount: 5000,
    }),
    {
      headers: { 
        'Authorization': 'Bearer ' + __ENV.TOKEN,
        'Content-Type': 'application/json',
      },
    }
  );
  check(res3, { 'update is 200': (r) => r.status === 200 });
  sleep(3);
}
```

---

## C# Integration Test

```csharp
// PerformanceTests.cs
using Xunit;
using System.Diagnostics;
using System.Threading.Tasks;

public class ClaimApiPerformanceTests {
    private readonly HttpClient _httpClient;
    
    [Fact]
    public async Task GetClaims_10000Records_ShouldComplete_Within5Seconds() {
        // Arrange
        var sw = Stopwatch.StartNew();
        var pageSize = 100;
        var totalRecords = 10000;
        var expectedPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        
        sw.Start();
        
        // Act
        var tasks = Enumerable.Range(1, expectedPages)
            .Select(page => _httpClient.GetAsync($"/api/claims?pageNumber={page}&pageSize={pageSize}"))
            .ToList();
        
        var responses = await Task.WhenAll(tasks);
        
        sw.Stop();
        
        // Assert
        Assert.True(sw.Elapsed.TotalSeconds <= 5, 
            $"Processing 10,000 records took {sw.Elapsed.TotalSeconds}s, expected ≤5s");
        
        Assert.All(responses, r => Assert.Equal(System.Net.HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task Pagination_Should_Have_Consistent_Performance() {
        // Arrange
        var stopwatches = new Dictionary<int, Stopwatch>();
        var pageSize = 100;
        var iterations = 10;
        
        // Act
        for (int iteration = 0; iteration < iterations; iteration++) {
            var sw = Stopwatch.StartNew();
            
            var response = await _httpClient.GetAsync(
                $"/api/claims?pageNumber={iteration + 1}&pageSize={pageSize}");
            
            sw.Stop();
            stopwatches[iteration] = sw;
        }
        
        // Assert
        var avgTime = stopwatches.Values.Average(s => s.Elapsed.TotalMilliseconds);
        var maxTime = stopwatches.Values.Max(s => s.Elapsed.TotalMilliseconds);
        
        Assert.True(maxTime <= 200, $"Max response time {maxTime}ms exceeds 200ms");
        Assert.True(avgTime <= 100, $"Average response time {avgTime}ms exceeds 100ms");
    }

    [Fact]
    public async Task Search_With_10000Records_Should_Complete_Under400ms() {
        // Arrange
        var sw = Stopwatch.StartNew();
        
        // Act
        var response = await _httpClient.GetAsync(
            "/api/claims?pageNumber=1&pageSize=50&searchTerm=Smith&claimStatus=Approved");
        
        sw.Stop();
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.Elapsed.TotalMilliseconds <= 400,
            $"Search took {sw.Elapsed.TotalMilliseconds}ms, expected ≤400ms");
    }

    [Fact]
    public void ConcurrentRequests_Should_Handle_100Users() {
        // Arrange
        var concurrentUsers = 100;
        var requestsPerUser = 10;
        var sw = Stopwatch.StartNew();
        var successCount = 0;
        var failureCount = 0;
        var responseTimes = new List<long>();
        
        // Act
        Parallel.For(0, concurrentUsers, new ParallelOptions { MaxDegreeOfParallelism = concurrentUsers },
            async (userIndex) => {
                for (int i = 0; i < requestsPerUser; i++) {
                    var itemSw = Stopwatch.StartNew();
                    try {
                        var response = await _httpClient.GetAsync(
                            $"/api/claims?pageNumber={i + 1}&pageSize=20");
                        
                        itemSw.Stop();
                        
                        if (response.IsSuccessStatusCode) {
                            Interlocked.Increment(ref successCount);
                            lock (responseTimes) {
                                responseTimes.Add(itemSw.ElapsedMilliseconds);
                            }
                        } else {
                            Interlocked.Increment(ref failureCount);
                        }
                    } catch {
                        Interlocked.Increment(ref failureCount);
                    }
                }
            });
        
        sw.Stop();
        
        // Assert
        var p95 = responseTimes.OrderBy(t => t).Skip((int)(responseTimes.Count * 0.95)).First();
        
        Assert.Equal(concurrentUsers * requestsPerUser, successCount + failureCount);
        Assert.True(failureCount <= responseTimes.Count * 0.05, // Allow 5% failure rate
            $"Failure rate too high: {failureCount}/{successCount + failureCount}");
        Assert.True(p95 <= 400, $"p95 latency {p95}ms exceeds 400ms");
    }

    [Fact]
    public async Task Memory_Usage_Should_Not_Exceed_150MB() {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(false);
        
        // Act
        var tasks = Enumerable.Range(1, 100)
            .Select(page => _httpClient.GetAsync($"/api/claims?pageNumber={page}&pageSize=100"))
            .ToList();
        
        await Task.WhenAll(tasks);
        
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = (finalMemory - initialMemory) / (1024 * 1024); // Convert to MB
        
        // Assert
        Assert.True(memoryUsed <= 150,
            $"Memory usage {memoryUsed}MB exceeds 150MB limit");
    }
}
```

---

## Running Performance Tests

### Using k6 (CLI)

```bash
# Install k6
choco install k6  # Windows
brew install k6   # macOS
sudo apt install k6  # Linux

# Generate bearer token
TOKEN=$(curl -X POST http://localhost:5285/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}' | jq -r '.data.token')

# Run baseline test
k6 run k6-baseline.js --vus 1 --duration 30s

# Run load test
k6 run k6-load-test.js

# Run pagination stress test
k6 run k6-pagination-stress.js

# Run search test
k6 run k6-search-test.js --duration 5m

# Run concurrent users test
k6 run k6-concurrent-users.js
```

### Using C# Unit Tests

```bash
cd ClaimSubmission.API.Tests
dotnet test --logger "console;verbosity=detailed"

# Run only performance tests
dotnet test --filter "Category=Performance"

# Measure coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

---

## Expected Results

### Baseline (Before Optimization)
- Get Claims (100 records): 300-500ms
- Get Claims (10,000 records, paginated): 8-12s
- Search (filtered): 400-600ms
- Concurrent 100 users: 65% success rate

### After Phase 1 Fixes (Security)
- Get Claims (100 records): 280-450ms
- Get Claims (10,000 records, paginated): 7-10s
- Search (filtered): 380-550ms
- Concurrent 100 users: 70% success rate

### After Phase 2 Fixes (Database Indexes)
- Get Claims (100 records): 150-250ms
- Get Claims (10,000 records, paginated): 4-6s ✓
- Search (filtered): 200-350ms
- Concurrent 100 users: 85% success rate

### After Phase 3 Fixes (Caching)
- Get Claims (100 records): 50-100ms (cached), 150ms (uncached)
- Get Claims (10,000 records, paginated): 2-3s (with caching)
- Search (filtered): 100-200ms (cached)
- Concurrent 100 users: 95%+ success rate

---

## Optimization Checklist

Performance targets:
- [ ] 10,000 records processed in <5 seconds
- [ ] API response time p95 <300ms
- [ ] Search queries complete in <400ms
- [ ] Memory usage stays below 150MB
- [ ] CPU usage <60% sustained
- [ ] 99%+ successful requests under normal load
- [ ] Database queries use indexes effectively
- [ ] Cache hit rate >70% for paginated requests

---

## Monitoring Dashboard Queries

### Application Insights

```kusto
// Average response time by endpoint
customMetrics
| where name == "HttpRequestDuration"
| summarize AvgResponseTime=avg(value) by tostring(customDimensions.Endpoint)
| order by AvgResponseTime desc

// P95 latency
customMetrics
| where name == "HttpRequestDuration"
| summarize P95=percentile(value, 95) by tostring(customDimensions.Endpoint)

// Error rate
customMetrics
| where name == "HttpRequestFailed"
| summarize ErrorRate=sum(value) / count() by tostring(customDimensions.Endpoint)
```

