#!/bin/bash

# API Test Script for Login Flow Validation
# This script tests the login endpoint with various scenarios

API_URL="http://localhost:5285"
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}======================================${NC}"
echo -e "${BLUE}Login Endpoint Test Script${NC}"
echo -e "${BLUE}======================================${NC}"
echo ""

# Function to test endpoint
test_login() {
    local username=$1
    local password=$2
    local description=$3
    local expected_status=$4
    
    echo -e "${YELLOW}Testing: $description${NC}"
    echo "  Username: $username"
    echo "  Password: $password"
    echo ""
    
    response=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"username\":\"$username\",\"password\":\"$password\"}")
    
    # Extract status code and body
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n-1)
    
    echo "  Response Status: $http_code"
    echo "  Response Body:"
    echo "$body" | jq . 2>/dev/null || echo "$body"
    echo ""
    
    if [ "$http_code" == "$expected_status" ]; then
        echo -e "${GREEN}✓ Test PASSED (Status: $http_code)${NC}"
    else
        echo -e "${RED}✗ Test FAILED (Expected: $expected_status, Got: $http_code)${NC}"
    fi
    echo ""
    echo "---"
    echo ""
}

# Check if API is running
echo -e "${YELLOW}Checking if API is running...${NC}"
if ! curl -s "$API_URL/health" > /dev/null 2>&1; then
    echo -e "${RED}API is not running at $API_URL${NC}"
    echo -e "${YELLOW}To start the API, run:${NC}"
    echo "  cd ClaimSubmission.API && dotnet run"
    exit 1
fi
echo -e "${GREEN}✓ API is running${NC}"
echo ""

# Test 1: Valid credentials (assuming database is set up with admin user)
test_login "admin" "Admin@123" "Valid Admin Credentials" "200"

# Test 2: Valid alternate user
test_login "claimmanager" "Admin@123" "Valid Manager Credentials" "200"

# Test 3: Invalid username
test_login "invaliduser" "Admin@123" "Invalid Username" "401"

# Test 4: Wrong password
test_login "admin" "WrongPassword123" "Wrong Password" "401"

# Test 5: Empty username
test_login "" "Admin@123" "Empty Username" "400"

# Test 6: Empty password  
test_login "admin" "" "Empty Password" "400"

# Test 7: Both empty
test_login "" "" "Both Empty" "400"

# Test 8: Null-like string (space)
test_login " " " " "Whitespace Only" "400"

echo -e "${BLUE}======================================${NC}"
echo -e "${BLUE}Testing Complete${NC}"
echo -e "${BLUE}======================================${NC}"
echo ""
echo -e "${YELLOW}Notes:${NC}"
echo "- Tests 1-2 expect 200 only if database is configured and users exist"
echo "- Tests 3-4 expect 401 for authentication failures"
echo "- Tests 5-8 expect 400 for validation failures"
echo ""
echo -e "${YELLOW}To see detailed logs, check:${NC}"
echo "- API logs in the terminal running 'dotnet run' for ClaimSubmission.API"
echo "- Application logs for any request handling details"
