# API Testing Guide - Local Storage Version

## Quick Test Commands

### 1. Health Check
```bash
# Check API is running
curl -k https://localhost:7272/health

# Expected Response:
# {"status":"healthy","timestamp":"2024-03-30T10:30:00.000Z","mode":"local-storage"}
```

### 2. User Login

**Test Credentials:**
- Username: `admin` or `claimmanager`
- Password: `Admin@123`

```bash
# Get authentication token
curl -k -X POST https://localhost:7272/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin@123"
  }'

# Expected Response:
# {
#   "data": {
#     "userId": 1,
#     "username": "admin",
#     "fullName": "Administrator",
#     "email": "admin@claimsystem.com",
#     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
#   }
# }
```

**Save token for subsequent requests:**
```bash
TOKEN=$(curl -s -k -X POST https://localhost:7272/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}' \
  | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo "Token: $TOKEN"
```

---

## Claims API Testing

### 3. Get All Claims (with Pagination)

```bash
# Get first 10 claims
curl -k "https://localhost:7272/api/claims?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# With search filter
curl -k "https://localhost:7272/api/claims?pageNumber=1&pageSize=10&searchTerm=John" \
  -H "Authorization: Bearer $TOKEN"

# Filter by status
curl -k "https://localhost:7272/api/claims?pageNumber=1&pageSize=10&claimStatus=Pending" \
  -H "Authorization: Bearer $TOKEN"

# Sort by claim amount descending
curl -k "https://localhost:7272/api/claims?pageNumber=1&pageSize=10&sortBy=ClaimAmount&sortDirection=DESC" \
  -H "Authorization: Bearer $TOKEN"

# Expected Response:
# {
#   "data": {
#     "claims": [
#       {
#         "claimId": 1,
#         "claimNumber": "CLM-2024-001",
#         "patientName": "John Doe",
#         "providerName": "Smith Medical Clinic",
#         "dateOfService": "2024-01-15T00:00:00Z",
#         "claimAmount": 1500.00,
#         "claimStatus": "Approved",
#         "createdDate": "2024-01-16T00:00:00Z"
#       }
#     ],
#     "totalRecords": 3,
#     "pageNumber": 1,
#     "pageSize": 10,
#     "totalPages": 1
#   },
#   "message": "Claims retrieved successfully"
# }
```

### 4. Get Single Claim by ID

```bash
# Get claim with ID 1
curl -k https://localhost:7272/api/claims/1 \
  -H "Authorization: Bearer $TOKEN"

# Expected Response:
# {
#   "data": {
#     "claimId": 1,
#     "claimNumber": "CLM-2024-001",
#     "patientName": "John Doe",
#     "providerName": "Smith Medical Clinic",
#     "dateOfService": "2024-01-15T00:00:00Z",
#     "claimAmount": 1500.00,
#     "claimStatus": "Approved",
#     "createdDate": "2024-01-16T00:00:00Z"
#   },
#   "message": "Claim retrieved successfully"
# }

# Test not found
curl -k https://localhost:7272/api/claims/999 \
  -H "Authorization: Bearer $TOKEN"
# Expected: 404 Not Found
```

### 5. Create New Claim

```bash
curl -k -X POST https://localhost:7272/api/claims \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "claimNumber": "CLM-2024-004",
    "patientName": "Sarah Williams",
    "providerName": "Downtown Medical Center",
    "dateOfService": "2024-03-25T00:00:00Z",
    "claimAmount": 2500.00,
    "claimStatus": "Pending"
  }'

# Expected Response: 201 Created
# {
#   "data": {
#     "claimId": 4
#   },
#   "message": "Claim created successfully"
# }
```

### 6. Update Claim

```bash
# Update claim ID 1
curl -k -X PUT https://localhost:7272/api/claims/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "patientName": "John Doe Updated",
    "providerName": "Smith Medical Clinic Updated",
    "dateOfService": "2024-01-15T00:00:00Z",
    "claimAmount": 1750.00,
    "claimStatus": "In Progress"
  }'

# Expected Response: 200 OK
# {
#   "message": "Claim updated successfully"
# }
```

### 7. Delete Claim

```bash
# Delete claim ID 4
curl -k -X DELETE https://localhost:7272/api/claims/4 \
  -H "Authorization: Bearer $TOKEN"

# Expected Response: 200 OK
# {
#   "message": "Claim deleted successfully"
# }
```

---

## Error Testing

### Invalid Login Credentials

```bash
curl -k -X POST https://localhost:7272/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "WrongPassword"
  }'

# Expected Response: 401 Unauthorized
# {
#   "error": "Invalid username or password"
# }
```

### Missing Required Fields

```bash
curl -k -X POST https://localhost:7272/api/claims \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "claimNumber": "CLM-2024-005",
    "patientName": "John Doe"
    # Missing required fields
  }'

# Expected Response: 400 Bad Request
# {
#   "errors": [
#     "Provider Name is mandatory",
#     "Date of Service is mandatory",
#     "Claim Amount must be greater than 0",
#     "Claim Status is mandatory"
#   ]
# }
```

### Invalid Pagination

```bash
curl -k "https://localhost:7272/api/claims?pageNumber=0&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# Expected Response: 400 Bad Request
# {
#   "error": "PageNumber and PageSize must be greater than 0"
# }
```

---

## Comprehensive Test Script

Save as `test_api.sh`:

```bash
#!/bin/bash

API_URL="https://localhost:7272"
USERNAME="admin"
PASSWORD="Admin@123"

echo "======================================"
echo "ClaimSubmission API - Local Storage"
echo "======================================"
echo ""

# 1. Health Check
echo "1. Health Check..."
curl -k "$API_URL/health" | jq .
echo ""
sleep 1

# 2. Login
echo "2. Login..."
RESPONSE=$(curl -s -k -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo $RESPONSE | jq -r '.data.token')
USER_ID=$(echo $RESPONSE | jq -r '.data.userId')

echo $RESPONSE | jq .
echo ""
echo "Token: $TOKEN"
echo "User ID: $USER_ID"
sleep 1

# 3. Get All Claims
echo "3. Get All Claims..."
curl -s -k "$API_URL/api/claims?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo ""
sleep 1

# 4. Get Specific Claim
echo "4. Get Claim ID 1..."
curl -s -k "$API_URL/api/claims/1" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo ""
sleep 1

# 5. Create New Claim
echo "5. Create New Claim..."
CREATE_RESPONSE=$(curl -s -k -X POST "$API_URL/api/claims" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "claimNumber": "CLM-2024-TEST-001",
    "patientName": "Test Patient",
    "providerName": "Test Provider",
    "dateOfService": "2024-03-30T00:00:00Z",
    "claimAmount": 1200.00,
    "claimStatus": "Pending"
  }')

echo $CREATE_RESPONSE | jq .
NEW_CLAIM_ID=$(echo $CREATE_RESPONSE | jq -r '.data.claimId')
echo "New Claim ID: $NEW_CLAIM_ID"
sleep 1

# 6. Update Claim
echo "6. Update Claim ID $NEW_CLAIM_ID..."
curl -s -k -X PUT "$API_URL/api/claims/$NEW_CLAIM_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "patientName": "Updated Test Patient",
    "providerName": "Updated Test Provider",
    "dateOfService": "2024-03-30T00:00:00Z",
    "claimAmount": 1500.00,
    "claimStatus": "Approved"
  }' | jq .
echo ""
sleep 1

# 7. Search Claims
echo "7. Search Claims (term: 'John')..."
curl -s -k "$API_URL/api/claims?pageNumber=1&pageSize=10&searchTerm=John" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo ""
sleep 1

# 8. Filter by Status
echo "8. Get Pending Claims..."
curl -s -k "$API_URL/api/claims?pageNumber=1&pageSize=10&claimStatus=Pending" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo ""
sleep 1

# 9. Delete Claim
echo "9. Delete Claim ID $NEW_CLAIM_ID..."
curl -s -k -X DELETE "$API_URL/api/claims/$NEW_CLAIM_ID" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo ""

# 10. Error Test - Invalid Login
echo "10. Test Invalid Login..."
curl -s -k -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"WrongPassword"}' | jq .
echo ""

echo "======================================"
echo "All tests completed!"
echo "======================================"
```

**Run the test script:**
```bash
chmod +x test_api.sh
./test_api.sh
```

---

## Using Postman

### Import Collection

**Create new collection with these requests:**

1. **Auth - Login**
   - Method: POST
   - URL: `{{base_url}}/api/auth/login`
   - Body (raw JSON):
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```
   - Save token: `Tests` tab add: `pm.environment.set("token", pm.response.json().data.token);`

2. **Claims - Get All**
   - Method: GET
   - URL: `{{base_url}}/api/claims?pageNumber=1&pageSize=10`
   - Headers: `Authorization: Bearer {{token}}`

3. **Claims - Get by ID**
   - Method: GET
   - URL: `{{base_url}}/api/claims/1`
   - Headers: `Authorization: Bearer {{token}}`

4. **Claims - Create**
   - Method: POST
   - URL: `{{base_url}}/api/claims`
   - Headers: `Authorization: Bearer {{token}}`
   - Body:
```json
{
  "claimNumber": "CLM-2024-NEW",
  "patientName": "New Patient",
  "providerName": "New Provider",
  "dateOfService": "2024-03-30T00:00:00Z",
  "claimAmount": 2000.00,
  "claimStatus": "Pending"
}
```

5. **Claims - Update**
   - Method: PUT
   - URL: `{{base_url}}/api/claims/1`
   - Headers: `Authorization: Bearer {{token}}`
   - Body:
```json
{
  "patientName": "Updated Name",
  "providerName": "Updated Provider",
  "dateOfService": "2024-03-30T00:00:00Z",
  "claimAmount": 2500.00,
  "claimStatus": "Approved"
}
```

6. **Claims - Delete**
   - Method: DELETE
   - URL: `{{base_url}}/api/claims/1`
   - Headers: `Authorization: Bearer {{token}}`

### Setup Environment
- Variable: `base_url`, Value: `https://localhost:7272`
- Variable: `token`, Initial Value: (empty)

---

## Load Testing

### Using Apache Bench (ab)

```bash
# Test health endpoint (100 requests, 10 concurrent)
ab -n 100 -c 10 -k https://localhost:7272/health

# With authentication (requires pre-generated token)
ab -n 100 -c 10 -k \
  -H "Authorization: Bearer YOUR_TOKEN" \
  https://localhost:7272/api/claims
```

### Using wrk

```bash
# Install: brew install wrk (macOS) or apt-get install wrk (Linux)

# Simple load test
wrk -t12 -c400 -d30s https://localhost:7272/health

# With custom script
wrk -t12 -c400 -d30s -s script.lua https://localhost:7272/api/claims
```

---

## Performance Benchmarks (Expected)

| Scenario | Time | Success Rate |
|----------|------|--------------|
| Login | < 100ms | 100% |
| Get claims (100 items) | < 50ms | 100% |
| Search claims | < 100ms | 100% |
| Create claim | < 50ms | 100% |
| Update claim | < 50ms | 100% |
| Delete claim | < 50ms | 100% |
| 100 concurrent login requests | < 5s | 100% |

---

## Common Issues & Solutions

### SSL Certificate Warning
```bash
# Ignore SSL warnings in curl
curl -k https://localhost:7272/...

# Or add to Postman:
- Settings → General → SSL certificate verification: OFF
```

### CORS Issues
```bash
# Web frontend calling API will have CORS configured
# Check response headers for:
# Access-Control-Allow-Origin: http://localhost:5277
```

### Token Expiration
- JWT tokens expire after 24 hours
- Re-login to get new token
- Check token in https://jwt.io

### Claim Not Found
```bash
# Verify claim exists first
curl -k https://localhost:7272/api/claims | jq '.data.claims[].claimId'

# Then use existing ID
curl -k https://localhost:7272/api/claims/1
```

---

## Continuous Testing (CI/CD)

### GitHub Actions Example

```yaml
name: API Tests

on: [push]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '10.0.x'
      
      - name: Build
        run: dotnet build
      
      - name: Run API
        run: dotnet run --no-build &
        working-directory: ClaimSubmission.API
      
      - name: Wait for API
        run: sleep 5
      
      - name: Run tests
        run: bash test_api.sh
```

---

This testing guide provides complete coverage for validating the local storage implementation works correctly!
